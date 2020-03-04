﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Akka.Actor;
using Akka.Monitoring;
using Akka.Monitoring.ApplicationInsights;
using Akka.Monitoring.Datadog;
using Akka.Monitoring.PerformanceCounters;
using Akka.Monitoring.Prometheus;
using Akka.Routing;
using AkkaNetCore.Actors;
using AkkaNetCore.Config;
using AkkaNetCore.Extensions;
using AkkaNetCore.Models.Message;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NLog;
using Prometheus;
using StatsdClient;
using static AkkaNetCore.Actors.ActorProviders;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;



namespace AkkaNetCore
{
    public class Startup
    {
        private string AppName = "AkkaNetCore";
        private string Company = "웹노리";
        private string Email = "psmon@live.co.kr";
        private string CompanyUrl = "http://wiki.webnori.com/";
        private string DocUrl = "http://wiki.webnori.com/display/webfr/.NET+Core+With+Akka";

        static public string SystemNameForCluster = "actor-cluster";

        public static IActorRef SingleToneActor;    //Todo : 다른위치로 옮길것
        public static ActorSystem ActorSystem;
        private static readonly ManualResetEvent asTerminatedEvent = new ManualResetEvent(false);
        public static AppSettings AppSettings;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private async void MemberRemoved(ActorSystem actorSystem)
        {
            await actorSystem.Terminate();
            asTerminatedEvent.Set();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddControllers();

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
                var actor = actorFactory.ActorOf(Props.Create<HighPassGateActor>()
                    .WithDispatcher("fast-dispatcher")
                    //.WithRouter(FromConfig.Instance), "highpass-roundrobin");
                    .WithRouter(FromConfig.Instance), "highpass-gate-pool");        
                return () => actor;
            });

            services.AddActor<CashGateActorProvider>((provider, actorFactory) =>
            {
                var actor = actorFactory.ActorOf(Props.Create<CashGateActor>()
                    .WithDispatcher("slow-dispatcher")
                    .WithRouter(FromConfig.Instance), "cashpass-gate-pool");
                return () => actor;
            });


            // For Cluster
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
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = AppName,
                    Description = $"{AppName} ASP.NET Core Web API",
                    TermsOfService = new Uri("http://wiki.webnori.com/display/codesniper/TermsOfService"),
                    Contact = new OpenApiContact
                    {
                        Name = Company,
                        Email = Email,
                        Url = new Uri(CompanyUrl),
                    },
                    License = new OpenApiLicense
                    {
                        Name = $"Document",
                        Url = new Uri(DocUrl),
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });

            // API주소룰 소문자로...
            //services.AddRouting(options => options.LowercaseUrls = true);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApplicationLifetime lifetime)
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
                        
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            MetricServer metricServer = null;
            var appConfig = app.ApplicationServices.GetService<AppSettings>();
            AppSettings = appConfig;

            //APP Life Cycle
            lifetime.ApplicationStarted.Register(() =>
            {
                app.ApplicationServices.GetService<ILogger>();

                var actorSystem = app.ApplicationServices.GetService<ActorSystem>(); // start Akka.NET

                ActorSystem = actorSystem;

                //싱글톤 클러스터 액터                
                var actor = AkkaBoostrap.BootstrapSingleton<SingleToneActor>(actorSystem, "SingleToneActor", "akkanet");
                SingleToneActor = AkkaBoostrap.BootstrapSingletonProxy(actorSystem, "SingleToneActor", "akkanet", "/user/SingleToneActor", "singleToneActorProxy");
    

                try
                {
                    var MonitorTool = Environment.GetEnvironmentVariable("MonitorTool");
                    var MonitorToolCon = Environment.GetEnvironmentVariable("MonitorToolCon");

                    switch (MonitorTool)
                    {
                        case "win":
                            var win = ActorMonitoringExtension.RegisterMonitor(actorSystem,
                                new ActorPerformanceCountersMonitor(
                                    new CustomMetrics
                                    {
                                        Counters = { 
                                            "akka.custom.metric1","akka.custom.metric2","akka.custom.metric3",
                                            "akka.custom.received1", "akka.custom.received2" },
                                        Gauges = { "akka.gauge.msg10", "akka.gauge.msg100", "akka.gauge.msg1000", "akka.gauge.msg10000" },
                                        Timers = { "akka.handlertime" }
                                    }));
                            
                            // 윈도우 성능 모니터링 수집대상항목은 최초 Admin권한으로 akka항목으로 레지스트리에 프로그램실행시 자동 등록되며
                            // 커스텀항목 결정및 최초 1번 작동후 변경되지 않음으로
                            // 수집 항목 변경시 아래 Register 삭제후 다시 최초 Admin권한으로 작동
                            // Actor명으로 매트릭스가 분류됨으로 기능단위의 네이밍이 권장됨
                            // HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Akka\Performance
                            break;
                        case "azure":
                            var azure = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorAppInsightsMonitor(appConfig.MonitorToolCon));
                            break;
                        case "prometheus":
                            // prometheusMonotor 를 사용하기위해서, MerticServer를 켠다...(수집형 모니터)
                            // http://localhost:10250/metrics
                            metricServer = new MetricServer(10250);
                            metricServer.Start();
                            var prometheus = ActorMonitoringExtension.RegisterMonitor(actorSystem, new ActorPrometheusMonitor(actorSystem));
                            break;
                        case "datadog":
                            var statsdConfig = new StatsdConfig
                            {
                                StatsdServerName = MonitorToolCon
                            };
                            var dataDog = ActorMonitoringExtension.
                                RegisterMonitor(actorSystem, new ActorDatadogMonitor(statsdConfig));
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
                Console.WriteLine("=============== Start Graceful Down ===============");
                var actorSystem = app.ApplicationServices.GetService<ActorSystem>();
                                
                if (appConfig.MonitorTool == "prometheus") metricServer.Stop();

                // Graceful Down Test,Using CashGateActor Actor
                IActorRef cashGate = app.ApplicationServices.GetService<CashGateActorProvider>()();
                cashGate.Ask(new StopActor()).Wait();


                var cluster = Akka.Cluster.Cluster.Get(actorSystem);
                cluster.RegisterOnMemberRemoved(() => MemberRemoved(actorSystem));
                cluster.Leave(cluster.SelfAddress);
                asTerminatedEvent.WaitOne();

                Console.WriteLine($"=============== Completed Graceful Down : {cluster.SelfAddress} ===============");
            });
        }
    }
}
