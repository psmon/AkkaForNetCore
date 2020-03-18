using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;
using Akka.Routing;
using Akka.Streams;
using AkkaNetCore.Models.Message;

namespace AkkaNetCore.Actors.Study
{
    public class ClusterPoolActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        private readonly IActorRef workActor;

        private ActorMaterializer materializer;

        private int WorkCount = 1;

        public ClusterPoolActor()
        {
            materializer = Context.Materializer();

            workActor = Context.ActorOf(Props.Create<ClusterWorkActor>()
                .WithDispatcher("fast-dispatcher")                
                .WithRouter(new RoundRobinPool(WorkCount))
            );

            ReceiveAsync<DelayMsg>(async msg =>
            {
                Context.IncrementMessagesReceived();
                Context.IncrementCounter("akka.custom.received1");
                workActor.Forward(msg);
            });
        }

        protected override void PreStart()
        {
            Cluster.Subscribe(Self, ClusterEvent.InitialStateAsEvents,new[] { typeof(ClusterEvent.IMemberEvent), typeof(ClusterEvent.UnreachableMember) });
            Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            Cluster.Unsubscribe(Self);
            Context.IncrementActorStopped();
        }

    }
}
