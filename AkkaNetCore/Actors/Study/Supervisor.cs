using System;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors.Study
{
    public class Child : ReceiveActor
    {
        private int state = 0;

        private readonly ILoggingAdapter logger = Context.GetLogger();

        public Child()
        {
            ReceiveAsync<object>(async message =>
            {
                switch (message)
                {
                    case Exception ex:
                        throw ex;
                    case int x:
                        state = x;
                        break;
                    case "get":
                        Sender.Tell(state);
                        break;
                }
            });
        }
    }

    public class Supervisor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public Supervisor()
        {
            ReceiveAsync<Props>(async p =>
            {
                var child = Context.ActorOf(p); // create child
                Sender.Tell(child); // send back reference to child actor
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 10,
                withinTimeRange: TimeSpan.FromMinutes(1),
                localOnlyDecider: ex =>
                {
                    switch (ex)
                    {
                        case ArithmeticException ae:
                            return Directive.Resume;
                        case NullReferenceException nre:
                            return Directive.Restart;
                        case ArgumentException are:
                            return Directive.Stop;
                        default:
                            return Directive.Escalate;
                    }
                });
        }
    }
}
