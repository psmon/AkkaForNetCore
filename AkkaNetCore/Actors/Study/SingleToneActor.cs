using System;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Models.Actor;

namespace AkkaNetCore.Actors
{
    public class SingleToneActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int totalCount;        
        private DateTime startTime;
                
        public SingleToneActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"싱글톤 액터 생성:{id}");
            startTime = DateTime.Now;            
            totalCount = 0;

            ReceiveAsync<DelayMsg>(async msg =>
            {
                totalCount++;
                Context.IncrementMessagesReceived();
                //100개씩마다 로그찍음
                if ( (totalCount % 100) == 0)
                {                    
                    DateTime endTime = DateTime.Now;
                    TimeSpan timeSpan = endTime - startTime;                    
                    logger.Info($"====== Process Total:{totalCount} Seconds(100):{timeSpan.TotalSeconds} Msg:{msg.Message}");
                    Context.IncrementCounter("akka.custom.received1");
                    Context.Gauge("akka.gauge.msg100", (int)Math.Truncate(timeSpan.TotalMilliseconds) );
                    startTime = endTime;
                }
            });
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }
    }
}
