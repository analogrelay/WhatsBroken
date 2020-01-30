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
    class KustoContext
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

        public Task<IReadOnlyList<BranchInfo>> GetBranchInfoAsync(CancellationToken cancellationToken = default)
            => ExecuteQueryAsync<BranchInfo>((string)GetQuery((string)"GetBranchInfo"), cancellationToken);

        async Task<IReadOnlyList<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken)
        {
            // Yes yes, I know. We log things here though and can't return a task :(.
            async void OnCancellation((KustoContext Context, string RequestId) state)
            {
                try
                {
                    state.Context._logger.LogDebug("Attempting to cancel query '{RequestId}'", state.RequestId);
                    await _admin.ExecuteControlCommandAsync(state.Context._databaseName, $".cancel query {state.RequestId}");
                    state.Context._logger.LogDebug("Cancelled query '{RequestId}'", state.RequestId);
                }
                catch (Exception ex)
                {
                    state.Context._logger.LogError(ex, "Error cancelling query '{RequestId}'", state.RequestId);
                }
            }

            var requestId = $"WhatsBroken;{Guid.NewGuid():N}";
            var properties = new ClientRequestProperties()
            {
                ClientRequestId = requestId
            };
            _logger.LogTrace("Executing {Query} (RequestId: {RequestId})", query, requestId);

            using var registration = cancellationToken.CanBeCanceled ? cancellationToken.Register(OnCancellation, (this, requestId)) : null;
            var reader = await _query.ExecuteQueryAsync(_databaseName, query, properties);
            return reader.Parse<T>().ToList();
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