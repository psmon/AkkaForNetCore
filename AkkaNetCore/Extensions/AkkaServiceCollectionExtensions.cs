using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Monitoring;
using Akka.Monitoring.PerformanceCounters;
using Akka.Monitoring.Prometheus;
using Akka.Monitoring.StatsD;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AkkaServiceCollectionExtensions
    {
        public static IServiceCollection AddAkka(this IServiceCollection services, string actorSystemName, Config config)
        {
            // Register ActorSystem
            services.AddSingleton<ActorSystem>((provider) => ActorSystem.Create(actorSystemName, config));

            return services;
        }

        public static IServiceCollection AddAkkaActor<TService>(this IServiceCollection services, Func<IServiceProvider, IActorRefFactory, TService> implementationFactory) where TService : class
        {
            // Register Actor
            services.AddSingleton<TService>((provider) =>
            {
                var actorSystem = provider.GetService<ActorSystem>();

                var didMonitorRegister = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorPrometheusMonitor());

                var registeredMonitor = ActorMonitoringExtension.RegisterMonitor(actorSystem,
                    new ActorPerformanceCountersMonitor(
                        new CustomMetrics
                        {
                            Counters = { "akka.custom.metric1", "akka.custom.metric2" },
                            Gauges = { "akka.messageboxsize" },
                            Timers = { "akka.handlertime" }
                        }));

                ActorMonitoringExtension.Monitors(actorSystem).IncrementDebugsLogged();

                var actor = implementationFactory(provider, actorSystem);

                return actor;
            });

            return services;
        }
    }
}
