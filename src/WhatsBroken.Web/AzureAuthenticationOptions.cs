using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.Extensions.Options;

namespace WhatsBroken.Web
{
    class AzureAuthenticationOptions
    {
        public string ClientId { get; set; }
        public X509Certificate2 Certificate { get; set; }
    }

    class ConfigureAzureAuthenticationOptions : IConfigureOptions<AzureAuthenticationOptions>
    {
        private readonly IOptions<AzureADOptions> _azureAdOptions;

        public ConfigureAzureAuthenticationOptions(IOptions<AzureADOptions> azureAdOptions)
        {
            _azureAdOptions = azureAdOptions;
        }

        public void Configure(AzureAuthenticationOptions options)
        {
            options.ClientId = _azureAdOptions.Value.ClientId;

            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            var certs = store.Certificates.Find(X509FindType.FindBySubjectName, "WhatsBroken", validOnly: true);
            options.Certificate = certs.Cast<X509Certificate2>().FirstOrDefault();
        }
    }
}