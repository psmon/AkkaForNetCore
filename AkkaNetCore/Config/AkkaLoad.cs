using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using AkkaConfig = Akka.Configuration.Config;

namespace AkkaNetCore.Config
{
    public class AkkaLoad
    {
        public static ConcurrentDictionary<string, IActorRef> ActorList = new ConcurrentDictionary<string, IActorRef>();

        public static IActorRef RegisterActor(string name, IActorRef actorRef)
        {
            if (ActorList.ContainsKey(name)) throw new Exception("이미 등록된 액터입니다.");
            ActorList[name] = actorRef;
            return actorRef;
        }

        public static IActorRef ActorSelect(string name)
        {
            if(ActorList.ContainsKey(name))
                return ActorList[name];

            return null;
        }

        public static AkkaConfig Load(string environment, IConfiguration configuration)
        {
            if(environment.ToLower()!= "production")
            {
                environment = "Development";
            }

            return LoadConfig(environment, "akka{0}.conf", configuration);
        }

        private static AkkaConfig LoadConfig(string environment, string configFile, IConfiguration configuration)
        {
            string akkaip = configuration.GetSection("akkaip").Value ?? "127.0.0.1";
            string akkaport = configuration.GetSection("akkaport").Value ?? "5100";
            string akkaseed = configuration.GetSection("akkaseed").Value ?? "127.0.0.1:5100";
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
