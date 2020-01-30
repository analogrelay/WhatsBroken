﻿using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace WhatsBroken.Web
{
    class AzureAuthenticationService
    {
        readonly SemaphoreSlim _acquireTokenLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        readonly IConfidentialClientApplication _app;
        readonly ILogger<AzureAuthenticationService> _logger;
        readonly string[] _scopes;

        AuthenticationResult? _token;

        public AzureAuthenticationService(IOptionsMonitor<AzureADOptions> azureAdOptions, IOptions<KustoOptions> kustoOptions, ILogger<AzureAuthenticationService> logger)
        {
            var adOptions = azureAdOptions.Get(AzureADDefaults.AuthenticationScheme);

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser, OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "WhatsBroken", validOnly: false);
            var certificate = certs.Cast<X509Certificate2>().FirstOrDefault();

            _app = ConfidentialClientApplicationBuilder.Create(adOptions.ClientId)
                .WithTenantId(adOptions.TenantId)
                .WithCertificate(certificate)
                .Build();
            _logger = logger;

            var resource = kustoOptions.Value.ClusterUrl ?? throw new ArgumentException("Missing required configuration option: 'Kusto:ClusterUrl'");
            _scopes = new[] { $"{resource}/.default" };
        }

        public ValueTask<AuthenticationResult> AcquireTokenAsync()
        {
            var token = _token;
            if (token != null && token.ExpiresOn >= DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return new ValueTask<AuthenticationResult>(token);
            }
            return AcquireTokenAsyncWithAwait();

            async ValueTask<AuthenticationResult> AcquireTokenAsyncWithAwait()
            {
                await _acquireTokenLock.WaitAsync();
                try
                {
                    // Recheck the token in the lock
                    token = _token;
                    if (token != null && token.ExpiresOn >= DateTimeOffset.UtcNow.AddMinutes(5))
                    {
                        return token;
                    }

                    // Acquire a new token
                    _logger.LogDebug("Acquiring new authentication token...");
                    _token = await _app.AcquireTokenForClient(_scopes)
                        .ExecuteAsync();
                    _logger.LogDebug("Token acquired.");

                    return _token;
                }
                finally
                {
                    _acquireTokenLock.Release();
                }
            }
        }

    }
}
