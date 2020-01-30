using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace WhatsBroken.Web
{
    class AzureAuthenticationService
    {
        public AzureAuthenticationService(IOptions<AzureAuthenticationOptions> options)
        {
            ConfidentialClientApplicationBuilder.Create(options.Value.ClientId)
                .WithCertificate(options.Value.Certificate);
        }
    }
}
