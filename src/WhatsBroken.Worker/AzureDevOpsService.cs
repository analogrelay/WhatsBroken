using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WhatsBroken.Worker
{
    public class AzureDevOpsOptions
    {
        public string? AccessToken { get; set; }
        public string? OrganizationUrl { get; set; }
        public TimeSpan SyncInterval { get; set; }
        public IList<PipelineOptions>? Pipelines { get; set; }
    }

    public class PipelineOptions
    {
        public string? Project { get; set; }
        public int DefinitionId { get; set; }
        public bool IsQuarantined { get; set; }
    }

    public class AzureDevOpsService : BackgroundService
    {
        readonly IOptionsMonitor<AzureDevOpsOptions> _options;
        readonly ILogger<AzureDevOpsService> _logger;

        public AzureDevOpsService(IOptionsMonitor<AzureDevOpsOptions> options, ILogger<AzureDevOpsService> logger)
        {
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var options = _options.CurrentValue;
                var vss = new VssConnection(
                    new Uri(options.OrganizationUrl ?? throw new InvalidOperationException("Missing required option: 'AzDO:OrganizationUrl'")),
                    new VssBasicCredential(string.Empty, options.AccessToken ?? throw new InvalidOperationException("Missing required option: 'AzDO:AccessToken'")));
                await SyncBuildsAsync(vss, options, stoppingToken);
                await Task.Delay(options.SyncInterval)
            }
        }

        private async Task SyncBuildsAsync(VssConnection vss, AzureDevOpsOptions options, CancellationToken cancellationToken)
        {
            var buildClient = await vss.GetClientAsync<BuildHttpClient>(cancellationToken);

            foreach (var pipeline in options.Pipelines ?? Array.Empty<PipelineOptions>())
            {
                var definition = await buildClient.GetDefinitionAsync(pipeline.Project, pipeline.DefinitionId);
                _logger.LogDebug("Syncing build logs for {Project}/{Path}/{Pipeline}", definition.Project.Name, definition.Path, definition.Name);

                // Get the last 7 days of builds
                await buildClient.GetBuildsAsync2(definition.Project.Id)
            }
        }
    }
}
