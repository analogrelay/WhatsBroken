using System.Security.Cryptography.X509Certificates;

namespace WhatsBroken.Web
{
    class KustoOptions
    {
        public string ClusterUrl { get; set; }
        public string DatabaseName { get; set; }
    }
}