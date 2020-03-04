using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.Test.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using WhatsBroken.Worker.Model;

using AzDoBuild = Microsoft.TeamFoundation.Build.WebApi.Build;
using AzDoTestRun = Microsoft.TeamFoundation.TestManagement.WebApi.TestRun;

using Build = WhatsBroken.Worker.Model.Build;
using TestRun = WhatsBroken.Worker.Model.TestRun;

namespace WhatsBroken.Worker
{
    public class AzDoQueryService : BackgroundService
    {
        private readonly ILogger<AzDoQueryService> _logger;
        readonly IOptionsMonitor<AzDoQueryOptions> _options;
        readonly WhatsBrokenDbContext _db;

        public AzDoQueryService(ILogger<AzDoQueryService> logger, IOptionsMonitor<AzDoQueryOptions> options, WhatsBrokenDbContext db)
        {
            _logger = logger;
            _options = options;
            _db = db;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var _1 = stoppingToken.CanBeCanceled ?
                // NULLABLE: We know it's safe to do "self!" because we passed this in and really don't care about situations where that could be null (they DO exist though!)
                (IDisposable)stoppingToken.Register((self) => ((AzDoQueryService)self!)._logger.LogDebug("AzDoQueryService stopping."), this) :
                null;

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var options = _options.CurrentValue;

                    // Set up the connection
                    var vss = new VssConnection(
                        new Uri(options.OrganizationUrl ?? throw new InvalidOperationException("Missing required configuration option: 'AzDo:OrganizationUrl'")),
                        new VssBasicCredential(userName: string.Empty, password: options.AccessToken ?? throw new InvalidOperationException("Missing required configuration option: 'AzDo:AccessToken'")));

                    // Run the queris
                    await SyncDataAsync(options, vss, stoppingToken);

                    _logger.LogInformation("Sleeping for {PollInterval}.", options.PollInterval);
                    await Task.Delay(options.PollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed with error: {ExceptionMessage}.", ex.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("AzDoQueryService stopped.");
            }
        }

        private async Task SyncDataAsync(AzDoQueryOptions options, VssConnection vss, CancellationToken cancellationToken)
        {
            // Create a Db Session
            var db = await DbSession.CreateAsync(_db, cancellationToken);

            var buildsClient = await vss.GetClientAsync<BuildHttpClient>(cancellationToken);
            var testsClient = await vss.GetClientAsync<TestManagementHttpClient>(cancellationToken);

            // First, resolve all the pipelines
            var buildDefinitions = await ResolveDefinitionsAsync(options, buildsClient, cancellationToken);

            // Now iterate through the pipelines and grab new builds to sync
            foreach (var (definition, pipelineOptions) in buildDefinitions)
            {
                await SyncDefinitionAsync(db, definition, pipelineOptions, buildsClient, testsClient, cancellationToken);
            }
        }

        private async Task SyncDefinitionAsync(DbSession db, BuildDefinition definition, PipelineOptions options, BuildHttpClient buildsClient, TestManagementHttpClient testsClient, CancellationToken cancellationToken)
        {
            // Ensure the definition has a pipeline record
            var pipeline = await db.GetOrCreatePipelineAsync(definition, cancellationToken);
            await db.SaveChangesAsync();

            // Grab the latest successful build
            // TODO: Sync status
            var builds = await buildsClient.GetBuildsAsync2(
                project: definition.Project.Id,
                definitions: new[] { definition.Id },
                statusFilter: BuildStatus.Completed,
                queryOrder: BuildQueryOrder.FinishTimeDescending,
                top: 1,
                cancellationToken: cancellationToken);
            _logger.LogTrace("Fetched {Count} new builds {Project}:{DefinitionId}", builds.Count, definition.Project.Id, definition.Id);

            foreach (var build in builds)
            {
                _logger.LogDebug("Synchronizing build {Project}/{Pipeline}#{BuildNumber}", definition.Project.Id, definition.Name, build.BuildNumber);
                await SyncBuildAsync(db, build, pipeline, options, buildsClient, testsClient, cancellationToken);
            }
        }

        private async Task SyncBuildAsync(DbSession db, AzDoBuild azDoBuild, Pipeline pipeline, PipelineOptions options, BuildHttpClient buildsClient, TestManagementHttpClient testsClient, CancellationToken cancellationToken)
        {
            var build = await db.GetOrCreateBuildAsync(pipeline, azDoBuild, cancellationToken);

            // Grab all the test runs
            var runs = await testsClient.GetTestRunsAsync(
                project: azDoBuild.Project.Id,
                buildUri: azDoBuild.Uri.ToString(),
                cancellationToken: cancellationToken);

            foreach (var run in runs)
            {
                _logger.LogDebug("Synchronizing test run {Project}/{Pipeline}#{BuildNumber}/{RunName}", azDoBuild.Project.Id, azDoBuild.Definition.Name, azDoBuild.BuildNumber, run.Name);
                await SyncRunAsync(db, azDoBuild.Project, build, run, testsClient, cancellationToken);
            }

            await db.SaveChangesAsync();
        }

        private async Task SyncRunAsync(DbSession db, TeamProjectReference project, Build build, AzDoTestRun azDoRun, TestManagementHttpClient testsClient, CancellationToken cancellationToken)
        {
            var run = db.CreateRun(build, azDoRun);

            // Grab all the results
            var results = await testsClient.GetTestResultsAsync(
                project: project.Id,
                runId: azDoRun.Id,
                detailsToInclude: ResultDetails.SubResults,
                cancellationToken: cancellationToken);

            foreach(var result in results)
            {
                if(result.ResultGroupType != ResultGroupType.None)
                {
                    throw new NotSupportedException("Not supported: tests with subresults.");
                }
                await db.CreateResultAsync(run, result, cancellationToken);
            }
        }

        private async Task<List<(BuildDefinition Definition, PipelineOptions Options)>> ResolveDefinitionsAsync(AzDoQueryOptions options, BuildHttpClient buildsClient, CancellationToken cancellationToken)
        {
            var buildDefinitions = new List<(BuildDefinition Definition, PipelineOptions Options)>();
            foreach (var pipelinesInProject in options.Pipelines.GroupBy(p => p.Project))
            {
                var allDefinitions = (await buildsClient.GetDefinitionsAsync2(project: pipelinesInProject.Key, cancellationToken: cancellationToken)).ToDictionary(d => $@"{d.Path}\{d.Name}");
                foreach (var pipeline in pipelinesInProject)
                {
                    // We support '/' as a path separator in JSON to avoid escaping, but '\' is what AzDo uses.
                    if (pipeline.Pipeline != null && allDefinitions.TryGetValue(pipeline.Pipeline.Replace('/', '\\'), out var definitionRef))
                    {
                        _logger.LogTrace("Resolved {Project}:{QualifiedPipelineName} to {ProjectId}:{DefinitionId}.", pipeline.Project, pipeline.Pipeline, definitionRef.Project.Id, definitionRef.Id);
                        var definition = await buildsClient.GetDefinitionAsync(
                            project: definitionRef.Project.Id,
                            definitionId: definitionRef.Id,
                            revision: definitionRef.Revision,
                            cancellationToken: cancellationToken);
                        buildDefinitions.Add((definition, pipeline));
                    }
                    else
                    {
                        _logger.LogWarning("Unable to resolve pipeline: {Project}:{QualifiedPipelineName}.", pipeline.Project, pipeline.Pipeline);
                    }
                }
            }

            return buildDefinitions;
        }
    }

    public class AzDoQueryOptions
    {
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(10);
        public string? AccessToken { get; set; }
        public string? OrganizationUrl { get; set; }
        public IList<PipelineOptions>? Pipelines { get; set; }
    }

    public class PipelineOptions
    {
        public string? Project { get; set; }
        public string? Pipeline { get; set; }
        public string? RunType { get; set; }
    }
}
