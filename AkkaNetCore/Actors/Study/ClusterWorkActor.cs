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
    public class ClusterWorkActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int msgCnt;        
        private Random random;
                
        protected IActorRef StatisticsActor;

        public ClusterWorkActor()
        {
            id = Guid.NewGuid().ToString();
            //logger.Info($"Create ClusterMsgActor:{id}");
            msgCnt = 0;            
            random = new Random();

            StatisticsActor = AkkaLoad.ActorSelect("SingleToneActor");

            ReceiveAsync<DelayMsg>(async msg =>
            {
                Context.IncrementMessagesReceived();
                
                if (msgCnt < 10)
                {
                    logger.Debug($"### Message ClusterMsgActor {msgCnt}");
                }
                msgCnt++;                
                //랜덤 Delay를 줌( 외부 요소 : API OR DB )
                int auto_delay = msg.Delay == 0 ? random.Next(1, 100) : msg.Delay;
                await Task.Delay(auto_delay);

                Context.IncrementCounter("akka.custom.received1");
                msg.State = DelayMsgState.Completed;
                StatisticsActor.Tell(msg);

                if ((msgCnt % 100) == 0)
                {
                    logger.Info($"Msg:{msg} Count:{msgCnt} Delay:{auto_delay}");                    
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
