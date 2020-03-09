using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Config;
using AkkaNetCore.Models.Message;

namespace AkkaNetCore.Actors.Study
{
    public class HighPassGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int msgCnt;
        private Random random;        
        private bool MonitorMode = true;                
        protected IActorRef MatrixSingleActor;

        public HighPassGateActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"Create HigPassGateActor:{id}");
            msgCnt = 0;
            random = new Random();
            MatrixSingleActor = AkkaLoad.ActorSelect("SingleToneActor");

            ReceiveAsync<DelayMsg>(async msg =>
            {
                if (MonitorMode) Context.IncrementMessagesReceived();
                
                msgCnt++;

                int auto_delay = msg.Delay == 0 ? random.Next(1, 100) : msg.Delay;                
                await Task.Delay(auto_delay);
                
                var completeMsg = new DelayMsg() 
                { 
                    State = DelayMsgState.Completed,
                    Message = msg.Message,
                    Seq = msg.Seq
                };
                                
                MatrixSingleActor.Tell(completeMsg);

                if (MonitorMode) Context.IncrementCounter("akka.custom.received1");

                logger.Info($"Msg:{msg.Message} Count:{msgCnt} Delay:{auto_delay}");

            });
        }

        protected override void PreStart()
        {
            if(MonitorMode) Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            if(MonitorMode) Context.IncrementActorStopped();
        }
    }
}
