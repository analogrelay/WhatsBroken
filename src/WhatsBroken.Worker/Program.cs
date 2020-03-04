using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WhatsBroken.Worker.Model;

namespace WhatsBroken.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<WhatsBrokenDbContext>(options =>
                    {
                        options.UseSqlServer(hostContext.Configuration["Sql:ConnectionString"] ?? throw new InvalidOperationException("Missing required setting 'Sql:ConnectionString'"));
                    }, contextLifetime: ServiceLifetime.Singleton);

                    services.Configure<AzDoQueryOptions>(hostContext.Configuration.GetSection("AzDo"));
                    services.AddHostedService<AzDoQueryService>();
                });
    }
}
