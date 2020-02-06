using Dapper;
using Kusto.Data.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WhatsBroken.Web.Model;

namespace WhatsBroken.Web
{
    public class KustoContext
    {
        static readonly ConcurrentDictionary<string, string> _queryCache = new ConcurrentDictionary<string, string>();

        readonly ICslQueryProvider _query;
        readonly ICslAdminProvider _admin;
        readonly string _databaseName;
        readonly ILogger _logger;

        public KustoContext(ICslQueryProvider query, ICslAdminProvider admin, string databaseName, ILogger logger)
        {
            _query = query;
            _admin = admin;
            _databaseName = databaseName;
            _logger = logger;
        }

        public async Task<IReadOnlyList<TestCase>> GetAllTestIdentitiesAsync(IEnumerable<string> repositories, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending Query: GetAllTestIdentitiesAsync([{Repositories}])", repositories);
            var results = await ExecuteQueryAsync<TestCase>(
                GetQuery("GetAllTestIdentities"),
                cancellationToken,
                ("Repositories", string.Join(";", repositories)));
            _logger.LogDebug("Query Complete: GetAllTestIdentitiesAsync([{Repositories}])", repositories);
            return results;
        }

        public async Task<IReadOnlyList<TestCase>> GetQuarantinedTestsAsync(IEnumerable<string> repositories, IEnumerable<string> branches, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending Query: GetQuarantinedTests([{Repositories}], [{Branches}])", repositories, branches);
            var results = await ExecuteQueryAsync<TestCase>(
                GetQuery("GetQuarantinedTests"),
                cancellationToken,
                ("Repositories", string.Join(";", repositories)),
                ("Branches", string.Join(";", branches)));
            _logger.LogDebug("Query Complete: GetQuarantinedTests([{Repositories}], [{Branches}])", repositories, branches);
            return results;
        }

        public async Task<IReadOnlyList<TestResult>> GetRunHistoryAsync(string? project, string? type, string? method, string? argumentHash, int limit = 1000, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending Query: GetRunHistory({Project}, {Type}, {Method}, {ArgumentHash}, {Limit})", project, type, method, argumentHash, limit);
            var results = await ExecuteQueryAsync<TestResult>(
                GetQuery("GetRunHistory"),
                cancellationToken,
                ("TestProject", project),
                ("TestType", type),
                ("TestMethod", method),
                ("TestArgumentHash", argumentHash ?? string.Empty),
                ("Limit", limit));
            _logger.LogDebug("Query Complete: GetRunHistory({Project}, {Type}, {Method}, {ArgumentHash}, {Limit})", project, type, method, argumentHash, limit);
            return results;
        }

        public async Task<IReadOnlyList<TestResult>> GetFailingTestsAsync(DateTime startUtc, DateTime endUtc, IEnumerable<string> repositories, IEnumerable<string> branches, bool includeQuarantined, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Sending Query: GetFailingTests({StartUtc}, {EndUtc}, [{Repositories}], [{Branches}])", startUtc, endUtc, repositories, branches);
            var results = await ExecuteQueryAsync<TestResult>(
                GetQuery("GetFailingTests"),
                cancellationToken,
                ("Start", startUtc),
                ("End", endUtc),
                ("Repositories", string.Join(";", repositories)),
                ("Branches", string.Join(";", branches)),
                ("IncludeQuarantined", includeQuarantined));
            _logger.LogDebug("Query Complete: GetFailingTests({StartUtc}, {EndUtc}, [{Repositories}], [{Branches}])", startUtc, endUtc, repositories, branches);
            return results;
        }

        async Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            // Yes yes, I know. We log things here though and can't return a task :(.
            static async void OnCancellation((KustoContext Context, string RequestId) state)
            {
                try
                {
                    state.Context._logger.LogDebug("Attempting to cancel query '{RequestId}'", state.RequestId);
                    await state.Context._admin.ExecuteControlCommandAsync(state.Context._databaseName, $".cancel query {state.RequestId}");
                    state.Context._logger.LogDebug("Cancelled query '{RequestId}'", state.RequestId);
                }
                catch (Exception ex)
                {
                    state.Context._logger.LogError(ex, "Error cancelling query '{RequestId}'", state.RequestId);
                }
            }

            static void ApplyParameter(ClientRequestProperties properties, string name, object? value)
            {
                switch (value)
                {
                    case null:
                        break;
                    case string sv:
                        properties.SetParameter(name, sv);
                        break;
                    case DateTime dtv:
                        properties.SetParameter(name, dtv);
                        break;
                    case TimeSpan tsv:
                        properties.SetParameter(name, tsv);
                        break;
                    case bool bv:
                        properties.SetParameter(name, bv);
                        break;
                    case int iv:
                        properties.SetParameter(name, iv);
                        break;
                    case long lv:
                        properties.SetParameter(name, lv);
                        break;
                    case Guid gv:
                        properties.SetParameter(name, gv);
                        break;
                    case double dv:
                        properties.SetParameter(name, dv);
                        break;
                    default:
                        throw new NotSupportedException($"Parameter type not supported: {value.GetType().FullName}");
                }
            }

            var requestId = $"WhatsBroken;{Guid.NewGuid():N}";
            var properties = new ClientRequestProperties()
            {
                ClientRequestId = requestId
            };

            foreach (var (Name, Value) in parameters)
            {
                ApplyParameter(properties, Name, Value);
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var paramsString = string.Join(",", parameters.Select(p => $"{p.Name}=[{p.Value}]"));
                _logger.LogTrace("Executing request {RequestId}. Parameters ({Parameters}). Query: {Query}", requestId, paramsString, query);
            }

            using var registration = cancellationToken.CanBeCanceled ? cancellationToken.Register(OnCancellation, (this, requestId)) : null;
            var reader = await _query.ExecuteQueryAsync(_databaseName, query, properties);
            var results = reader.Parse<T>().ToList();
            _logger.LogDebug("Retrieved {ResultCount} results.", results.Count);
            return results;
        }

        string GetQuery(string name)
        {
            return _queryCache.GetOrAdd(name, n =>
            {
                var queryResourceName = $"{typeof(Program).Namespace}.Queries.{n}.kql";
                using var stream = typeof(KustoContext).Assembly.GetManifestResourceStream(queryResourceName);
                using var reader = new StreamReader(stream ?? throw new KeyNotFoundException($"Query '{queryResourceName}' does not exist!"));
                return reader.ReadToEnd().Trim();
            });
        }
    }
}




