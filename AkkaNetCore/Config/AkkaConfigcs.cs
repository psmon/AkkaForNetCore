using System;
using System.IO;
using System.Text;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;

namespace AkkaNetCore.Config
{
    public class AkkaConfig
    {
        public static bool IsLeeader = false;
        public static string LeaderNodeName = "akkanetcore";

        public static Akka.Configuration.Config Load(string environment, IConfiguration configuration)
        {
            return LoadConfig(environment, "akka{0}.conf", configuration);
        }

        private static Akka.Configuration.Config LoadConfig(string environment, string configFile, IConfiguration configuration)
        {
            string akkaip = configuration.GetSection("akkaip").Value ?? "127.0.0.1";
            string akkaport = configuration.GetSection("akkaport").Value ?? "5100";
            string akkaseed = configuration.GetSection("akkaseed").Value ?? "127.0.0.1:5100";
            string roles = configuration.GetSection("roles").Value ?? "akkanet";

            // 개발 디버깅용 : 특정 조건을 Leader 노드라고 가정 - 클러스터에서 Leader 는 자동 변경됨
            if( akkaip== "akkanetcore" || (akkaip=="127.0.0.1" && akkaport =="7100"))
            {
                IsLeeader = true;
            }

            if(akkaip == "127.0.0.1")
            {
                LeaderNodeName = "127.0.0.1";
            }


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
