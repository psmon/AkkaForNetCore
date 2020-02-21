using System;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;

namespace AkkaNetCore.Actors
{
    public class HigPassGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int msgCnt;
        private Random random;
        private bool ClusterMode = false;
        
        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        public HigPassGateActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"하이패스 액터 생성:{id}");
            msgCnt = 0;
            random = new Random();

            ReceiveAsync<string>(async msg =>
            {
                msgCnt++;

                Context.IncrementMessagesReceived();
                
                Context.IncrementCounter("akka.custom.metric1");

                Context.Gauge("akka.messageboxsize", random.Next(1, 10) );
                //하이패스는 그냥 지나가면됨
                if ( (msgCnt % 100) == 0)
                {
                    logger.Debug($"{id}-{msg}-{msgCnt}");                    
                }                                    
            });
        }

        protected override void PreStart()
        {
            // subscribe to IMemberEvent and UnreachableMember events
            if (ClusterMode)
            {
                Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents,
                new[] { typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.UnreachableMember) });
            }
            
            Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            if(ClusterMode) Cluster.Unsubscribe(Self);

            Context.IncrementActorStopped();
        }

    }
}
