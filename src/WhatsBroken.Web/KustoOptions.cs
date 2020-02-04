using System.Security.Cryptography.X509Certificates;

namespace WhatsBroken.Web
{
    public class KustoOptions
    {
        public string? ClusterUrl { get; set; }
        public string? DatabaseName { get; set; }
    }
}