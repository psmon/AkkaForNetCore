using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;
using Akka.Routing;
using AkkaNetCore.Models.LoadTest;

namespace AkkaNetCore.Actors.LoadTest
{

    public class ApiWorkActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();        
        protected IActorRef StatisticsActor;

        Random rnd;

        public ApiWorkActor()
        {
            rnd = new Random();

            ReceiveAsync<ApiCallSpec>(async msg =>
            {
                Context.IncrementMessagesReceived();
                //시도 카운트
                Context.IncrementCounter("akka.custom.received1");

                int autoDelay = rnd.Next(1000, 3000);
                logger.Debug($"API호출시도 {msg} {autoDelay}");
                await Task.Delay(autoDelay);

                //성공시에만 카운트( 실패= received1 - received2)
                Context.IncrementCounter("akka.custom.received2");
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

    public class ClusterWorkerPoolActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        private readonly IActorRef workActor;

        public ClusterWorkerPoolActor()
        {
            int workCount = 800; //컴퓨터의 성능에따라 최대 능력치 조절가능합니다.
            logger.Debug($"========== Create ApiWorkActor {workCount}");

            workActor = Context.ActorOf(Props.Create<ApiWorkActor>()
                .WithDispatcher("fast-dispatcher")
                .WithRouter(new RoundRobinPool(workCount))   //라운드로빈 가능 ApiWork Actor수를 셋팅한다. (멀티 Thread에 대응)
            );
            ReceiveAsync<ApiCallSpec>(async msg =>
            {
                workActor.Tell(msg);
            });
        }

        protected override void PreStart()
        {
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents, new[] { typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.UnreachableMember) });
            Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
            Context.IncrementActorStopped();
        }

    }
}
