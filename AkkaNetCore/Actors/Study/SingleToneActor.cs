using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Actors.Utils;
using AkkaNetCore.Models.Message;
using AkkaNetCore.Service;

namespace AkkaNetCore.Actors
{
    public class SingleToneActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int totalCount;        
        private DateTime startTime;
        private List<DelayMsg> reservedMsg;

        private IActorRef BatchWriter_Rev;
        private IActorRef BatchManager_Rev;
        private IActorRef BatchWriter_Comp;
        private IActorRef BatchManager_Comp;

        public SingleToneActor()
        {
            BatchWriter_Rev = Context.ActorOf<BatchWriterActor>();
            BatchManager_Rev = Context.ActorOf(Props.Create(() => new BatchActor(10)));
            BatchManager_Rev.Tell(new SetTarget(BatchWriter_Rev));

            BatchWriter_Comp = Context.ActorOf<BatchWriterActor>();
            BatchManager_Comp = Context.ActorOf(Props.Create(() => new BatchActor(10)));
            BatchManager_Comp.Tell(new SetTarget(BatchWriter_Comp));


            id = Guid.NewGuid().ToString();
            logger.Info($"싱글톤 액터 생성:{id}");
            startTime = DateTime.Now;            
            totalCount = 0;
            reservedMsg = new List<DelayMsg>();

            ReceiveAsync<DelayMsg>(async msg =>
            {
                if(msg.State == DelayMsgState.Completed)
                {
                    totalCount++;                    
                    //100개씩마다 로그찍음
                    if ((totalCount % 100) == 0)
                    {
                        DateTime endTime = DateTime.Now;
                        TimeSpan timeSpan = endTime - startTime;
                        logger.Info($"====== Process Total:{totalCount} Seconds(100):{timeSpan.TotalSeconds} Msg:{msg.Message}");
                        Context.IncrementCounter("akka.custom.received1");
                        Context.Gauge("akka.gauge.msg100", (int)Math.Truncate(timeSpan.TotalMilliseconds));
                        startTime = endTime;
                    }

                    Context.IncrementCounter("akka.custom.received1");
                    BatchManager_Comp.Tell(new Queue(msg));
                }
                else if(msg.State == DelayMsgState.Reserved)
                {
                    Context.IncrementMessagesReceived();
                    BatchManager_Rev.Tell(new Queue(msg));
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
