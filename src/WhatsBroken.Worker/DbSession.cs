using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
        readonly Dictionary<(string Project, string Type, string Method, string ArgumentHash), TestCase> _testCaseCache;

        private DbSession(WhatsBrokenDbContext db, Dictionary<(string Project, string Type, string Method, string ArgumentHash), TestCase> testCaseCache)
        {
            _db = db;
            _testCaseCache = testCaseCache;
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

        public async Task<Build> GetOrCreateBuildAsync(Pipeline pipeline, AzDoBuild azDoBuild, CancellationToken cancellationToken)
        {
            var build = pipeline.Id == 0 ? null : await _db.Builds.FirstOrDefaultAsync(b => b.ProjectId == azDoBuild.Project.Id && b.AzDoId == azDoBuild.Id, cancellationToken);
            if (build == null)
            {
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
            }
            return build;
        }

        public async Task<TestCase> GetOrCreateTestCaseAsync(string project, string type, string method, string args, CancellationToken cancellationToken)
        {
            if (_testCaseCache.TryGetValue((project, type, method, args), out var testCase))
            {
                return testCase;
            }
            else
            {
                testCase = await _db.TestCases.FirstOrDefaultAsync(t => t.Project == project && t.Type == type && t.Method == method && t.ArgumentHash == args, cancellationToken);
                if (testCase == null)
                {
                    testCase = new TestCase()
                    {
                        Project = project,
                        Type = type,
                        Method = method,
                        Arguments = args,
                        ArgumentHash = args,
                    };
                    _db.TestCases.Add(testCase);
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

        internal static async Task<DbSession> CreateAsync(WhatsBrokenDbContext db, CancellationToken cancellationToken)
        {
            // Pre-load the cache of test cases
            var cases = await db.TestCases.ToDictionaryAsync(t => (t.Project, t.Type, t.Method, t.ArgumentHash), cancellationToken);
            return new DbSession(db, cases);
        }

        public TestRun CreateRun(Build build, AzDoTestRun azDoRun)
        {
            var run = new TestRun
            {
                ProjectId = build.ProjectId,
                AzDoId = azDoRun.Id,
                Name = azDoRun.Name,
                Build = build,
            };
            _db.TestRuns.Add(run);
            _dirty = true;
            return run;
        }

        public async Task<TestResult> CreateResultAsync(TestRun run, TestCaseResult azDoResult, CancellationToken cancellationToken)
        {
            var project = Path.GetFileNameWithoutExtension(azDoResult.AutomatedTestStorage);

            // Parse the name into the type, method, args, etc.
            var (type, method, args) = ParseTestName(azDoResult.AutomatedTestName);

            var testCase = await GetOrCreateTestCaseAsync(project, type, method, args, cancellationToken);

            var result = new TestResult()
            {
                Run = run,
                Case = testCase,
                Outcome = azDoResult.Outcome switch
                {
                    "Passed" => TestResultOutcome.Passed,
                    "Failed" => TestResultOutcome.Failed,
                    _ => TestResultOutcome.Unknown,
                }
            };
            _db.TestResults.Add(result);
            _dirty = true;
            return result;
        }

        private (string type, string method, string args) ParseTestName(string automatedTestName)
        {
            // Parse up to a '(', if there is one
            var parenIndex = automatedTestName.IndexOf('(');
            var preArgs = parenIndex < 0 ? automatedTestName : automatedTestName.Substring(0, parenIndex);
            var args = parenIndex < 0 ? string.Empty : automatedTestName.Substring(parenIndex);

            // Take everything up to the last '.' as the type
            var lastDotIdx = preArgs.LastIndexOf('.');
            var type = lastDotIdx < 0 ? string.Empty : preArgs.Substring(0, lastDotIdx);
            var method = lastDotIdx < 0 ? preArgs : preArgs.Substring(lastDotIdx + 1);
            return (type, method, args);
        }
    }
}
