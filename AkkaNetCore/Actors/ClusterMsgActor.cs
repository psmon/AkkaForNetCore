using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;


namespace AkkaNetCore.Actors
{
    public class ClusterMsgActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int msgCnt;
        private int totalMsgCnt;
        private Random random;
        private bool ClusterMode = true;

        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        public ClusterMsgActor(int delay)
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"Create ClusterMsgActor:{id}");
            msgCnt = 0;
            totalMsgCnt = 0;
            random = new Random();

            ReceiveAsync<string>(async msg =>
            {
                msgCnt++;
                totalMsgCnt++;
                //랜덤 Delay를 줌( 외부 요소 : API OR DB )
                int auto_delay = delay == 0 ? random.Next(1, 100) : delay;                
                await Task.Delay(auto_delay);
                Context.IncrementCounter("akka.custom.metric1");
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

            Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            if (ClusterMode) Cluster.Unsubscribe(Self);

            Context.IncrementActorStopped();
        }

    }
}
