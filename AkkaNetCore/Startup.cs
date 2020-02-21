using System;
using Akka.Actor;
using Akka.Routing;
using AkkaNetCore.Actors;
using AkkaNetCore.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Swashbuckle.AspNetCore.Swagger;
using static AkkaNetCore.Actors.ActorProviders;

namespace AkkaNetCore
{
    public class Startup
    {
        private string AppName = "AkkaNetCore";
        private string Company = "웹노리";
        private string CompanyUrl = "http://wiki.webnori.com/";
        private string DocUrl = "http://wiki.webnori.com/category/dev";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // *** Akka Service Setting

            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            services.AddAkka("AkkaNetCore", AkkaConfig.Load(envName,Configuration) );

            services.AddAkkaActor<PrinterActorProvider>((provider, actorFactory) =>
            {
                var printerActor = actorFactory.ActorOf(Props.Create<PrinterActor>()
                    .WithDispatcher("custom-dispatcher")
                    .WithRouter(FromConfig.Instance).WithDispatcher("custom-task-dispatcher"),
                    "printer-pool");

                return () => printerActor;
            });

            services.AddAkkaActor<TonerActorProvider>((provider, actorFactory) =>
            {
                var tonerActor = actorFactory.ActorOf(Props.Create(() => new TonerActor()).WithRouter(new RoundRobinPool(1)),
                    "toner");
                return () => tonerActor;
            });

            services.AddAkkaActor<HigPassGateActorProvider>((provider, actorFactory) =>
            {
                var actor = actorFactory.ActorOf(Props.Create<HigPassGateActor>()
                    .WithDispatcher("fast-dispatcher")
                    //.WithRouter(FromConfig.Instance), "highpass-roundrobin");
                    .WithRouter(FromConfig.Instance), "highpass-gate-pool");        
                return () => actor;
            });

            services.AddAkkaActor<CashGateActorProvider>((provider, actorFactory) =>
            {
                var actor = actorFactory.ActorOf(Props.Create<CashGateActor>(0)
                    .WithDispatcher("slow-dispatcher")
                    .WithRouter(FromConfig.Instance), "cashpass-gate-pool");
                return () => actor;
            });

            // Swagger
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = AppName,
                    Description = $"{AppName} ASP.NET Core Web API",
                    //TermsOfService = "None",
                    Contact = new Contact
                    {
                        Name = $"{Company} {AppName} Document",
                        Url = DocUrl
                    },
                    License = new License
                    {
                        Name = $"{Company}",
                        Url = CompanyUrl
                    }
                });
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            // API주소룰 소문자로...
            services.AddRouting(options => options.LowercaseUrls = true);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            app.UseSwagger();
            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.               
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", AppName + "V1");
            });


            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseHttpsRedirection()
                .UseMvc()
                .UseAkka(lifetime, typeof(PrinterActorProvider))
                .UseAkka(lifetime, typeof(TonerActorProvider))
                .UseAkka(lifetime, typeof(HigPassGateActorProvider))
                .UseAkka(lifetime, typeof(CashGateActorProvider));

            //APP Life Cycle
            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<ILogger>();
                app.ApplicationServices.GetService<ActorSystem>(); // start Akka.NET                
            });
            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
            });

        }
    }
}
