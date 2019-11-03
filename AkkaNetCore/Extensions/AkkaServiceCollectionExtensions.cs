using System;
using Akka.Actor;
using Akka.Configuration;

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
                var actor = implementationFactory(provider, actorSystem);
                return actor;
            });

            return services;
        }
    }
}
