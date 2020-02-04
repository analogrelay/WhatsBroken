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
    public class QuarantineTracker
    {
        ISet<TestCaseIdentity> _quarantinedTests = new HashSet<TestCaseIdentity>();
        readonly ILogger<QuarantineTracker> _logger;

        public QuarantineTracker(ILogger<QuarantineTracker> logger)
        {
            _logger = logger;
        }

        public void UpdateQuarantinedTests(IEnumerable<TestCaseIdentity> testIdentities)
        {
            var newSet = new HashSet<TestCaseIdentity>(testIdentities);
            Interlocked.Exchange(ref _quarantinedTests, newSet);
        }

        public bool IsTestQuarantined(TestResult result) => IsTestQuarantined(new TestCaseIdentity(result.Project, result.Type, result.Method));
        public bool IsTestQuarantined(TestCaseIdentity identity)
        {
            var localSet = _quarantinedTests;
            return localSet.Contains(identity);
        }
    }

    public class QuarantineTrackingService : BackgroundService
    {
        private readonly IOptionsMonitor<QuarantineTrackingOptions> _options;
        readonly QuarantineTracker _quarantineTracker;
        readonly KustoContextFactory _kustoContextFactory;
        readonly ILogger<QuarantineTrackingService> _logger;

        public QuarantineTrackingService(IOptionsMonitor<QuarantineTrackingOptions> options, QuarantineTracker quarantineTracker, KustoContextFactory kustoContextFactory, ILogger<QuarantineTrackingService> logger)
        {
            _options = options;
            _quarantineTracker = quarantineTracker;
            _kustoContextFactory = kustoContextFactory;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.CurrentValue.SkipBlockingLoad)
            {
                // Do one blocking load
                _logger.LogDebug("Doing initial load of quarantined tests...");
                await UpdateQuarantinedTestsAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Doing initial load non-blocking...");
                _ = UpdateQuarantinedTestsAsync(cancellationToken);
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
                    await UpdateQuarantinedTestsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task UpdateQuarantinedTestsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Reloading quarantined tests lists...");

            var context = await _kustoContextFactory.CreateContextAsync();

            var list = await context.GetQuarantinedTestsAsync(
                new[] { "dotnet/aspnetcore", "dotnet-aspnetcore" },
                new[] { "refs/heads/master" },
                cancellationToken);

            _quarantineTracker.UpdateQuarantinedTests(list.Select(c => new TestCaseIdentity(c.Project, c.Type, c.Method)));
        }
    }

    public class QuarantineTrackingOptions
    {
        public TimeSpan ReloadInterval { get; set; }
        public bool SkipBlockingLoad { get; set; }
    }

    public static class QuarantineTrackingServiceCollectionExtensions
    {
        public static IServiceCollection AddQuarantineTracking(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHostedService<QuarantineTrackingService>();
            services.AddSingleton<QuarantineTracker>();
            services.Configure<QuarantineTrackingOptions>(configuration.GetSection("QuarantineTracking"));
            return services;
        }
    }
}
