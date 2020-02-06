using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WhatsBroken.Web.Model;

namespace WhatsBroken.Web
{
    /// <summary>
    /// Manages persistent data like quarantined test status, or the list of known test projects/types/methods for background refreshing.
    /// </summary>
    public class BackgroundDataStore
    {
        ISet<TestCase> _quarantinedTests = new HashSet<TestCase>();
        TestCollection _testCollection = new TestCollection();
        readonly ILogger<BackgroundDataStore> _logger;

        readonly TaskCompletionSource<object?> _quarantinedTestLoaded = new TaskCompletionSource<object?>();
        readonly TaskCompletionSource<object?> _testCollectionLoaded = new TaskCompletionSource<object?>();

        public BackgroundDataStore(ILogger<BackgroundDataStore> logger)
        {
            _logger = logger;
        }

        public Task QuarantinedTestsLoaded => _quarantinedTestLoaded.Task;
        public Task TestCollectionLoaded => _testCollectionLoaded.Task;

        // This is a method because we want to encourage callers to take a local copy to avoid it being swapped out from under them.
        public TestCollection GetTestCollection() => _testCollection;

        public bool IsTestQuarantined(TestResult result) => IsTestQuarantined(new TestCase(result.Project, result.Type, result.Method, result.Arguments, result.ArgumentHash));
        public bool IsTestQuarantined(TestCase identity)
        {
            var localSet = _quarantinedTests;
            return localSet.Contains(identity);
        }

        public void UpdateQuarantinedTests(IEnumerable<TestCase> testIdentities)
        {
            _quarantinedTestLoaded.TrySetResult(null);
            var newSet = new HashSet<TestCase>(testIdentities);
            Interlocked.Exchange(ref _quarantinedTests, newSet);
        }

        public void UpdateTestCollection(TestCollection testCollection)
        {
            _testCollectionLoaded.TrySetResult(null);
            Interlocked.Exchange(ref _testCollection, testCollection);
        }
    }

    public class BackgroundDataService : BackgroundService
    {
        readonly IOptionsMonitor<BackgroundDataOptions> _options;
        readonly BackgroundDataStore _backgroundDataStore;
        readonly KustoContextFactory _kustoContextFactory;
        readonly ILogger<BackgroundDataService> _logger;

        public BackgroundDataService(IOptionsMonitor<BackgroundDataOptions> options, BackgroundDataStore backgroundDataStore, KustoContextFactory kustoContextFactory, ILogger<BackgroundDataService> logger)
        {
            _options = options;
            _backgroundDataStore = backgroundDataStore;
            _kustoContextFactory = kustoContextFactory;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.CurrentValue.SkipBlockingLoad)
            {
                // Do one blocking load
                _logger.LogDebug("Doing initial load of quarantined tests...");
                await UpdateAllDataAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Doing initial load non-blocking...");
                _ = UpdateAllDataAsync(cancellationToken);
            }

            // Continue with the start-up
            _logger.LogDebug("Continuing with start-up");
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var reloadInterval = _options.CurrentValue.ReloadInterval;
                    _logger.LogDebug("Sleeping for {ReloadInterval}", reloadInterval);
                    await Task.Delay(reloadInterval);

                    await UpdateAllDataAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task UpdateAllDataAsync(CancellationToken cancellationToken)
        {
            var context = await _kustoContextFactory.CreateContextAsync();
            await Task.WhenAll(
                UpdateQuarantinedTestsAsync(context, cancellationToken),
                UpdateTestCaseListAsync(context, cancellationToken));
        }

        private async Task UpdateTestCaseListAsync(KustoContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reloading test case lists...");

            var list = await context.GetAllTestIdentitiesAsync(
                new[] { "dotnet/aspnetcore", "dotnet-aspnetcore" },
                cancellationToken);

            // Build a data structure to hold all the data
            var testCollection = TestCollection.Build(list);
            _backgroundDataStore.UpdateTestCollection(testCollection);
        }

        private async Task UpdateQuarantinedTestsAsync(KustoContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reloading quarantined tests lists...");

            var list = await context.GetQuarantinedTestsAsync(
                new[] { "dotnet/aspnetcore", "dotnet-aspnetcore" },
                new[] { "refs/heads/master" },
                cancellationToken);

            _backgroundDataStore.UpdateQuarantinedTests(list);
        }
    }

    public class BackgroundDataOptions
    {
        public TimeSpan ReloadInterval { get; set; }
        public bool SkipBlockingLoad { get; set; }
    }

    public static class BackgroundDataServiceCollectionExtensions
    {
        public static IServiceCollection AddQuarantineTracking(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHostedService<BackgroundDataService>();
            services.AddSingleton<BackgroundDataStore>();
            services.Configure<BackgroundDataOptions>(configuration.GetSection("BackgroundData"));
            return services;
        }
    }
}
