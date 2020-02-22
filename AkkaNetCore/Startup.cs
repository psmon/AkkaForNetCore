using System;
using Akka.Actor;
using Akka.Monitoring;
using Akka.Monitoring.ApplicationInsights;
using Akka.Monitoring.PerformanceCounters;
using Akka.Monitoring.Prometheus;
using Akka.Routing;
using AkkaNetCore.Actors;
using AkkaNetCore.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Prometheus;
using Swashbuckle.AspNetCore.Swagger;
using static AkkaNetCore.Actors.ActorProviders;

namespace AkkaNetCore
{
    public class Startup
    {
        private string AppName = "AkkaNetCore";
        private string Company = "웹노리";
        private string CompanyUrl = "http://wiki.webnori.com/";
        private string DocUrl = "http://wiki.webnori.com/display/webfr/.NET+Core+With+Akka";
        private string SystemNameForCluster = "actor-cluster";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSingleton(Configuration.GetSection("AppSettings").Get<AppSettings>());// * AppSettings

            // *** Akka Service Setting
            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            services.AddAkka(SystemNameForCluster, AkkaConfig.Load(envName,Configuration) );

            services.AddActor<PrinterActorProvider>((provider, actorFactory) =>
            {
                var printerActor = actorFactory.ActorOf(Props.Create<PrinterActor>()
                    .WithDispatcher("custom-dispatcher")
                    .WithRouter(FromConfig.Instance).WithDispatcher("custom-task-dispatcher"),
                    "printer-pool");

                return () => printerActor;
            });

            services.AddActor<TonerActorProvider>((provider, actorFactory) =>
            {
                var tonerActor = actorFactory.ActorOf(Props.Create(() => new TonerActor()).WithRouter(new RoundRobinPool(1)),
                    "toner");
                return () => tonerActor;
            });

            services.AddActor<HigPassGateActorProvider>((provider, actorFactory) =>
            {
                var actor = actorFactory.ActorOf(Props.Create<HigPassGateActor>()
                    .WithDispatcher("fast-dispatcher")
                    //.WithRouter(FromConfig.Instance), "highpass-roundrobin");
                    .WithRouter(FromConfig.Instance), "highpass-gate-pool");        
                return () => actor;
            });

            services.AddActor<CashGateActorProvider>((provider, actorFactory) =>
            {
                var actor = actorFactory.ActorOf(Props.Create<CashGateActor>(0)
                    .WithDispatcher("slow-dispatcher")
                    .WithRouter(FromConfig.Instance), "cashpass-gate-pool");
                return () => actor;
            });

            services.AddActor<ClusterMsgActorProvider>((provider, actorFactory) =>
            {
                var actor = actorFactory.ActorOf(Props.Create<ClusterMsgActor>(0)
                    .WithDispatcher("fast-dispatcher")
                    .WithRouter(FromConfig.Instance), "cluster-roundrobin");
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
            app.UserActor(lifetime, typeof(PrinterActorProvider))
                .UserActor(lifetime, typeof(TonerActorProvider))
                .UserActor(lifetime, typeof(HigPassGateActorProvider))
                .UserActor(lifetime, typeof(ClusterMsgActorProvider))
                .UserActor(lifetime, typeof(CashGateActorProvider));

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
            
            app.UseMvc();

            MetricServer metricServer = null;
            var appConfig = app.ApplicationServices.GetService<AppSettings>();

            //APP Life Cycle
            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<ILogger>();

                var actorSystem = app.ApplicationServices.GetService<ActorSystem>(); // start Akka.NET

                try
                {
                    switch (appConfig.MonitorTool)
                    {
                        case "win":
                            var win = ActorMonitoringExtension.RegisterMonitor(actorSystem,
                                new ActorPerformanceCountersMonitor(
                                    new CustomMetrics
                                    {
                                        Counters = { "akka.custom.metric1", "akkacore.message" },
                                        Gauges = { "akka.messageboxsize" },
                                        Timers = { "akka.handlertime" }
                                    }));
                            break;
                        case "azure":
                            var azure = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorAppInsightsMonitor(appConfig.MonitorToolApiKey));
                            break;
                        case "prometheus":
                            // prometheusMonotor 를 사용하기위해서, MerticServer를 켠다...(수집형 모니터)
                            // http://localhost:10250/metrics
                            metricServer = new MetricServer(10250);
                            metricServer.Start();
                            var prometheus = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorPrometheusMonitor(actorSystem));
                            break;
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("=============== Not Suport Window Monitor Tools ===============");
                }
                
                ActorMonitoringExtension.Monitors(actorSystem).IncrementDebugsLogged();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
                if(appConfig.MonitorTool == "prometheus") metricServer.Stop();
            });
        }
    }
}
