using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using WhatsBroken.Worker.Model;

using AzDoBuild = Microsoft.TeamFoundation.Build.WebApi.Build;
using AzDoTestRun = Microsoft.TeamFoundation.TestManagement.WebApi.TestRun;

using Build = WhatsBroken.Worker.Model.Build;
using TestRun = WhatsBroken.Worker.Model.TestRun;

namespace WhatsBroken.Worker
{
    class DbSession
    {
        bool _dirty;

        readonly WhatsBrokenDbContext _db;
        readonly Dictionary<(string Project, string Type, string Method, string? ArgumentHash, string? Kind), TestCase> _testCaseCache;
        readonly ILogger _logger;
        readonly SHA1 _sha = SHA1.Create();

        private DbSession(WhatsBrokenDbContext db, Dictionary<(string Project, string Type, string Method, string? ArgumentHash, string? Kind), TestCase> testCaseCache, ILogger logger)
        {
            _db = db;
            _testCaseCache = testCaseCache;
            _logger = logger;
        }

        public async Task<Pipeline> GetOrCreatePipelineAsync(BuildDefinition definition, CancellationToken cancellationToken)
        {
            var pipeline = await _db.Pipelines.FirstOrDefaultAsync(p => p.ProjectId == definition.Project.Id && p.AzDoId == definition.Id, cancellationToken);
            if (pipeline == null)
            {
                pipeline = new Pipeline()
                {
                    ProjectId = definition.Project.Id,
                    AzDoId = definition.Id,
                    Project = definition.Project.Name,
                    Path = definition.Path,
                    Name = definition.Name
                };
                _db.Pipelines.Add(pipeline);
                _dirty = true;
            }
            return pipeline;
        }

        public async Task<TestCase> GetOrCreateTestCaseAsync(string project, string type, string method, string? args, string? kind, CancellationToken cancellationToken)
        {
            var argHash = string.IsNullOrEmpty(args) ?
                null :
                Convert.ToBase64String(_sha.ComputeHash(Encoding.UTF8.GetBytes(args)));
            var key = (project, type, method, argHash, kind);
            if (_testCaseCache.TryGetValue(key, out var testCase))
            {
                return testCase;
            }
            else
            {
                testCase = await _db.TestCases.FirstOrDefaultAsync(t => t.Project == project && t.Type == type && t.Method == method && t.ArgumentHash == argHash && t.Kind == kind, cancellationToken);
                if (testCase == null)
                {
                    testCase = new TestCase()
                    {
                        Project = project,
                        Type = type,
                        Method = method,
                        Arguments = args,
                        ArgumentHash = argHash,
                        Kind = kind?.ToLowerInvariant(),
                    };
                    _db.TestCases.Add(testCase);
                    _testCaseCache[key] = testCase;
                    _dirty = true;
                }
                return testCase;
            }
        }

        public async Task SaveChangesAsync()
        {
            if (_dirty)
            {
                await _db.SaveChangesAsync();
                _dirty = false;
            }
        }

        internal static async Task<DbSession> CreateAsync(WhatsBrokenDbContext db, ILogger logger, CancellationToken cancellationToken)
        {
            // Pre-load the cache of test cases
            var cases = await db.TestCases.ToDictionaryAsync(t => (t.Project, t.Type, t.Method, t.ArgumentHash, t.Kind), cancellationToken);
            return new DbSession(db, cases, logger);
        }

        public async Task<Build?> TryCreateBuildAsync(Pipeline pipeline, AzDoBuild azDoBuild, CancellationToken cancellationToken)
        {
            var build = pipeline.Id == 0 ? null : await _db.Builds.FirstOrDefaultAsync(b => b.ProjectId == azDoBuild.Project.Id && b.AzDoId == azDoBuild.Id, cancellationToken);
            if (build != null)
            {
                if (build.SyncEndDate == null)
                {
                    // Retry the sync
                    _db.Builds.Remove(build);
                    _dirty = true;
                }
                else
                {
                    // Already synced!
                    return null;
                }
            }

            build = new Build()
            {
                Pipeline = pipeline,
                ProjectId = pipeline.ProjectId,
                AzDoId = azDoBuild.Id,
                BuildNumber = azDoBuild.BuildNumber,
                FinishedDate = azDoBuild.FinishTime
            };
            _db.Builds.Add(build);
            _dirty = true;
            return build;
        }

        public TestRun CreateRun(Build build, AzDoTestRun azDoRun, string? runType)
        {
            var run = new TestRun
            {
                ProjectId = build.ProjectId,
                AzDoId = azDoRun.Id,
                Name = azDoRun.Name,
                Build = build,
                Type = runType,
            };
            _db.TestRuns.Add(run);
            _dirty = true;
            return run;
        }

        public async Task CreateResultsAsync(TestRun run, TestCaseResult azDoResult, CancellationToken cancellationToken)
        {
            // For now, skip jUnit tests
            if(string.Equals(azDoResult.AutomatedTestType, "junit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }


            void AddResult(TestCase testCase, string outcome)
            {
                var result = new TestResult()
                {
                    Run = run,
                    Case = testCase,
                    Outcome = outcome
                };
                _db.TestResults.Add(result);
                _dirty = true;
            }

            var project = ParseProject(azDoResult);

            if (azDoResult.SubResults == null)
            {
                // Parse the name into the type, method, args, etc.
                var (type, method, args) = ParseTestName(azDoResult.AutomatedTestName);

                var testCase = await GetOrCreateTestCaseAsync(project, type, method, args, azDoResult.AutomatedTestType, cancellationToken);
                AddResult(testCase, azDoResult.Outcome);
            }
            else
            {
                foreach (var subresult in azDoResult.SubResults)
                {
                    // Parse the name into the type, method, args, etc.
                    var (type, method, args) = ParseTestName(subresult.DisplayName);

                    var testCase = await GetOrCreateTestCaseAsync(project, type, method, args, azDoResult.AutomatedTestType, cancellationToken);
                    AddResult(testCase, subresult.Outcome);
                }
            }
        }

        private string ParseProject(TestCaseResult result)
        {
            if (result.AutomatedTestStorage.EndsWith(".dll"))
            {
                return Path.GetFileNameWithoutExtension(result.AutomatedTestStorage);
            }
            else
            {
                var doubleDashIdx = result.AutomatedTestStorage.IndexOf("--");
                if (doubleDashIdx >= 0)
                {
                    return result.AutomatedTestStorage.Substring(0, doubleDashIdx);
                }
                _logger.LogWarning("Unexpected Test Storage Name: '{TestStorageName}'.", result.AutomatedTestStorage);
                return result.AutomatedTestStorage;
            }
        }

        private (string type, string method, string args) ParseTestName(string automatedTestName)
        {
            // Parse up to a '(', if there is one
            var parenIndex = automatedTestName.IndexOf('(');
            var preArgs = automatedTestName;
            var args = string.Empty;
            if (parenIndex >= 0)
            {
                preArgs = automatedTestName.Substring(0, parenIndex);
                args = automatedTestName.Substring(parenIndex + 1, automatedTestName.Length - parenIndex - 2);
            }

            // Take everything up to the last '.' as the type
            var lastDotIdx = preArgs.LastIndexOf('.');
            var type = lastDotIdx < 0 ? string.Empty : preArgs.Substring(0, lastDotIdx);
            var method = lastDotIdx < 0 ? preArgs : preArgs.Substring(lastDotIdx + 1);
            return (type, method, args);
        }
    }
}
