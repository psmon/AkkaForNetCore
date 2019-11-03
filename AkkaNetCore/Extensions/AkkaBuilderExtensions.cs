﻿using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Akka.Actor;

namespace Microsoft.AspNetCore.Builder
{
    public static class AkkaBuilderExtensions
    {
        public static IApplicationBuilder UseAkka(this IApplicationBuilder app, IApplicationLifetime lifetime, params Type[] actors)
        {
            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>();// start Akka.NET
                if (actors != null)
                {
                    foreach(var actor in actors)
                    {
                        app.ApplicationServices.GetService(actor);// start Akka Actor
                    }
                }
            });
            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });

            return app;
        }
    }
}