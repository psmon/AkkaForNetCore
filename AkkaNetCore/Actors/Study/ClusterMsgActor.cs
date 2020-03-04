using System;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Config;
using AkkaNetCore.Extensions;
using AkkaNetCore.Models.Message;

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

        protected IActorRef MaxtRixSingleActor;

        public ClusterMsgActor(int delay)
        {
            id = Guid.NewGuid().ToString();

            //logger.Info($"Create ClusterMsgActor:{id}");

            msgCnt = 0;
            totalMsgCnt = 0;
            random = new Random();

            MaxtRixSingleActor = Startup.SingleToneActor;

            ReceiveAsync<DelayMsg>(async msg =>
            {
                Context.IncrementMessagesReceived();
                
                if (msgCnt == 0)
                {
                    logger.Debug($"### Message ClusterMsgActor {msgCnt}");
                }

                msgCnt++;
                totalMsgCnt++;
                //랜덤 Delay를 줌( 외부 요소 : API OR DB )
                int auto_delay = msg.Delay == 0 ? random.Next(1, 100) : msg.Delay;
                await Task.Delay(auto_delay);

                Context.IncrementCounter("akka.custom.received1");

                msg.State = DelayMsgState.Completed;
                MaxtRixSingleActor.Tell(msg);

                if ((msgCnt % 100) == 0)
                {
                    //logger.Info($"Msg:{msg} Count:{msgCnt} Delay:{auto_delay}");                    
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
