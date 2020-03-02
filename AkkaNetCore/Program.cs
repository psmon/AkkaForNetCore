using System;
using System.Collections.Generic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using Prometheus;

namespace AkkaNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var nlogEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (nlogEnvironment == "Production")
            {
                LogManager.LoadConfiguration("NLog.config");
            }
            else
            {
                LogManager.LoadConfiguration("NLog.Development.config");
                //LogManager.LoadConfiguration(nlogEnvironment == "" ? "NLog.config" : $"NLog.{nlogEnvironment}.config");
            }

            CreateHostBuilder(args).Build().Run();
            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
              .ConfigureWebHostDefaults(webBuilder =>
        {
            var config = GetServerUrlsFromCommandLine(args);
            var hostUrl = config.GetValue<string>("server.urls");

            webBuilder.ConfigureKestrel(serverOptions =>
            {
                // Set properties and call methods on options
            })
            .UseUrls(hostUrl)
            .UseStartup<Startup>()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .UseNLog();

        });

        public static IWebHostBuilder CreateWebHostBuilder_22(string[] args)
        {
            var config = GetServerUrlsFromCommandLine(args);
            var hostUrl = config.GetValue<string>("server.urls");

            var builder = WebHost.CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls(hostUrl)
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .UseNLog();

            return builder;
        }

        public static IConfigurationRoot GetServerUrlsFromCommandLine(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            var serverport = config.GetValue<int?>("port") ?? 5000;
            var serverurls = config.GetValue<string>("server.urls") ?? string.Format("http://0.0.0.0:{0}", serverport);

            var configDictionary = new Dictionary<string, string>
            {
                {"server.urls", serverurls},
                {"port", serverport.ToString()}
            };

            return new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddInMemoryCollection(configDictionary)
                .Build();
        }
    }
}
