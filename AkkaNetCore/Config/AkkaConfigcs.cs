using System;
using System.IO;
using System.Text;
using Akka.Configuration;

namespace AkkaNetCore.Config
{
    public class AkkaConfig
    {
        public static Akka.Configuration.Config Load(string environment)
        {
            return LoadConfig(environment, "akka{0}.conf");
        }

        private static Akka.Configuration.Config LoadConfig(string environment, string configFile)
        {
            var configFilePath = string.Format(configFile, environment.ToLower() != "production" ? string.Concat(".", environment) : "");
            if (File.Exists(configFilePath))
            {
                string config = File.ReadAllText(configFilePath, Encoding.UTF8);
                var akkaConfig = ConfigurationFactory.ParseString(config);
                Console.WriteLine($"=== AkkaConfig:{configFilePath}\r\n{akkaConfig}\r\n===");
                return akkaConfig;
            }
            return Akka.Configuration.Config.Empty;
        }
    }
}
