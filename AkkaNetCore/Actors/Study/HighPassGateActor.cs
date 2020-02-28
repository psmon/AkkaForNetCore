using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;

namespace AkkaNetCore.Actors
{
    public class HighPassGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int msgCnt;
        private Random random;
        private bool ClusterMode = false;
        private bool MonitorMode = true;
        
        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        public HighPassGateActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"Create HigPassGateActor:{id}");
            msgCnt = 0;
            random = new Random();

            ReceiveAsync<string>(async msg =>
            {
                if (msgCnt == 0)
                {
                    logger.Debug("### FirstMessage HigPassGateActor");
                }

                msgCnt++;
                int auto_delay = random.Next(1, 100);
                await Task.Delay(auto_delay);
                if (MonitorMode)
                {
                    Context.IncrementCounter("akka.custom.metric1");
                    Context.IncrementCounter("akka.custom.highpassactor");
                }

                if ((msgCnt % 100) == 0)
                {                    
                    logger.Info($"Msg:{msg} Count:{msgCnt} Delay:{auto_delay}");
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
            
            if(MonitorMode) Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            if(ClusterMode) Cluster.Unsubscribe(Self);

            if(MonitorMode) Context.IncrementActorStopped();

        }
    }
}
