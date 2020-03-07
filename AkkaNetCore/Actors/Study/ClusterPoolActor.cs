using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using Akka.Monitoring;
using Akka.Routing;
using AkkaNetCore.Config;
using Akka.Streams;
using AkkaNetCore.Models.Message;
using Akka.Streams.Dsl;
using Akka;
using AkkaNetCore.Models;
using Reactive.Streams;
using System.Linq;
using Akka.Streams.Implementation;

namespace AkkaNetCore.Actors.Study
{
    public class ClusterPoolActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        protected Cluster Cluster = Akka.Cluster.Cluster.Get(Context.System);

        private readonly IActorRef workActor;

        private ActorMaterializer materializer;

        public ClusterPoolActor()
        {
            materializer = Context.Materializer();

            workActor = Context.ActorOf(Props.Create<ClusterWorkActor>()
                .WithDispatcher("fast-dispatcher")
                .WithRouter(new RoundRobinPool(100))
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
