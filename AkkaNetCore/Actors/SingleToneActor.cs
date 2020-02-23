using System;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class SingleToneActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private UInt64 totalCount;
        private bool ClusterMode = true;

        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);


        public SingleToneActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"싱글톤 액터 생성:{id}");

            ReceiveAsync<UInt64>(async amount =>
            {                
                totalCount += amount;

                if(totalCount < 50)                
                    logger.Debug($"================= 싱글톤 메시지 인입 =================");

                //100개씩마다 로그찍음
                if ( (totalCount % 100) == 0)
                    logger.Info($"====== 메시지 처리량:{totalCount}");
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

        }

        protected override void PostStop()
        {
            if (ClusterMode) Cluster.Unsubscribe(Self);
        }
    }
}
