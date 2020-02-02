using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;

namespace WhatsBroken.Web
{
    public class Program
    {
        public static readonly string? BuildNumber;
        public static readonly string? BuildDefinitionName;
        public static readonly string? BuildId;
        public static readonly string? SourceVersion;
        public static readonly string? SourceBranch;

        static Program()
        {
            var metadata = typeof(Program).Assembly
                .GetCustomAttributes()
                .OfType<AssemblyMetadataAttribute>()
                .ToDictionary(m => m.Key, m => m.Value);
            BuildNumber = metadata.TryGetValue("Build.BuildNumber", out var buildNum) ? buildNum : null;
            BuildDefinitionName = metadata.TryGetValue("Build.BuildDefinitionName", out var buildDef) ? buildDef : null;
            BuildId = metadata.TryGetValue("Build.BuildId", out var buildId) ? buildId : null;
            SourceVersion = metadata.TryGetValue("Build.SourceVersion", out var sourceVer) ? sourceVer : null;
            SourceBranch = metadata.TryGetValue("Build.SourceBranch", out var sourceBranch) ? sourceBranch : null;
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.AddAzureWebAppDiagnostics();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
