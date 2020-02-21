using System;
using System.IO;
using System.Text;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;

namespace AkkaNetCore.Config
{
    public class AkkaConfig
    {
        public static Akka.Configuration.Config Load(string environment, IConfiguration configuration)
        {
            return LoadConfig(environment, "akka{0}.conf", configuration);
        }

        private static Akka.Configuration.Config LoadConfig(string environment, string configFile, IConfiguration configuration)
        {
            string akkaip = configuration.GetSection("akkaip").Value ?? "localhost";
            string akkaport = configuration.GetSection("akkaport").Value ?? "5100";
            string akkaseed = configuration.GetSection("akkaseed").Value ?? "localhost:5100";
            string roles = configuration.GetSection("roles").Value ?? "akkanet";            

            var configFilePath = string.Format(configFile, environment.ToLower() != "production" ? string.Concat(".", environment) : "");
            if (File.Exists(configFilePath))
            {
                string config = File.ReadAllText(configFilePath, Encoding.UTF8)
                    .Replace("$akkaport", akkaport)
                                .Replace("$akkaip", akkaip)
                                .Replace("$akkaseed", akkaseed)
                                .Replace("$roles", roles);

                var akkaConfig = ConfigurationFactory.ParseString(config);                                

                Console.WriteLine($"=== AkkaConfig:{configFilePath}\r\n{akkaConfig}\r\n===");
                return akkaConfig;
            }
            return Akka.Configuration.Config.Empty;
        }
    }
}
