using System;
using Akka.Actor;
using Akka.Monitoring;
using Akka.Monitoring.PerformanceCounters;
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

            // *** Akka Service Setting

            var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            services.AddAkka(SystemNameForCluster, AkkaConfig.Load(envName,Configuration) );

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

            services.AddAkkaActor<ClusterMsgActorProvider>((provider, actorFactory) =>
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

            app.UseAkka(lifetime, typeof(PrinterActorProvider))
                .UseAkka(lifetime, typeof(TonerActorProvider))
                .UseAkka(lifetime, typeof(HigPassGateActorProvider))
                .UseAkka(lifetime, typeof(ClusterMsgActorProvider))
                .UseAkka(lifetime, typeof(CashGateActorProvider));

            MetricServer metricServer = null;

            //APP Life Cycle
            lifetime.ApplicationStarted.Register(() =>
            {
                // prometheusMonotor 를 사용하기위해서, MerticServer를 켠다...(수집형 모니터)
                // http://localhost:10250/metrics
                //metricServer = new MetricServer(10250);
                //metricServer.Start();

                app.ApplicationServices.GetService<ILogger>();
                var actorSystem = app.ApplicationServices.GetService<ActorSystem>(); // start Akka.NET

                // http://localhost:10250/metrics

                //var prometheusMonotor = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorPrometheusMonitor(actorSystem));

                //var azureMonotor = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorAppInsightsMonitor(""));

                //윈도우전용 모니터링(로컬전용)
                try
                {
                    var windowPerformanceMonitor = ActorMonitoringExtension.RegisterMonitor(actorSystem,
                    new ActorPerformanceCountersMonitor(
                        new CustomMetrics
                        {
                            Counters = { "akka.custom.metric1", "akkacore.message" },
                            Gauges = { "akka.messageboxsize" },
                            Timers = { "akka.handlertime" }
                        }));
                }
                catch(Exception)
                {
                    Console.WriteLine("=============== Not Suport Window Monitor Tools ===============");
                }                

                ActorMonitoringExtension.Monitors(actorSystem).IncrementDebugsLogged();
            });

            lifetime.ApplicationStopping.Register(() =>
            {
                app.ApplicationServices.GetService<ActorSystem>().Terminate().Wait();
                //metricServer.Stop();
            });

        }
    }
}
