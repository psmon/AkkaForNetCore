using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Streams;
using Akka.Monitoring;
using Akka.Streams.Dsl;
using AkkaNetCore.Models.Entity;
using AkkaNetCore.Models.Message;
using AkkaNetCore.Repositories;
using Z.EntityFramework.Extensions;
using System.Linq;

namespace AkkaNetCore.Actors.Utils
{
    // 일괄 처리(데이터 인입)    
    public class ThrottleWork : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        private IActorRef consumer;

        public ThrottleWork(int element,int maxBust)
        {
            
            ReceiveAsync<SetTarget>(async target =>
            {
                consumer = target.Ref;
            });

            ReceiveAsync<object>(async message =>
            {
                if (message is Batch batchMessage)
                {
                    int Count = batchMessage.Obj.Count;
                    Context.IncrementMessagesReceived();
                    Source<object, NotUsed> source = Source.From(batchMessage.Obj);

                    using (var materializer = Context.Materializer())
                    {
                        var factorials = source;
                        factorials
                             //.ZipWith(Source.From(Enumerable.Range(0, 100)), (num, idx) => $"{idx}! = {num}")
                             .Throttle(element, TimeSpan.FromSeconds(1), maxBust, ThrottleMode.Shaping)
                             .RunForeach(obj => {
                                 //consumer.Tell("");
                                 var nowstr = DateTime.Now.ToString("mm:ss");
                                 if(obj is DelayMsg delayMsg)
                                 {                                     
                                     Console.WriteLine($"[{nowstr}] -  {delayMsg.Message}");
                                     if (consumer != null) consumer.Tell(delayMsg);
                                 }                                 
                             }, materializer)
                             .Wait();
                    }
                }
            });
        }
    }


    public class ThrottleActor : FSM<State, IData>
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private int CollectSec;

        public ThrottleActor(int _CollectSec)
        {
            CollectSec = _CollectSec;

            StartWith(State.Idle, Uninitialized.Instance);

            When(State.Idle, state =>
            {
                if (state.FsmEvent is SetTarget target && state.StateData is Uninitialized)
                {
                    return Stay().Using(new Todo(target.Ref, ImmutableList<object>.Empty));
                }

                return null;
            });

            When(State.Active, state =>
            {
                if ((state.FsmEvent is Flush || state.FsmEvent is StateTimeout)
                    && state.StateData is Todo t)
                {
                    return GoTo(State.Idle).Using(t.Copy(ImmutableList<object>.Empty));
                }

                return null;
            }, TimeSpan.FromSeconds(CollectSec));

            WhenUnhandled(state =>
            {
                if (state.FsmEvent is Queue q && state.StateData is Todo t)
                {
                    return GoTo(State.Active).Using(t.Copy(t.Queue.Add(q.Obj)));
                }
                else
                {
                    logger.Warning($"Received unhandled request {state.FsmEvent} in state {StateName}/{state.StateData}");
                    return Stay();
                }
            });

            OnTransition((initialState, nextState) =>
            {
                if (initialState == State.Active && nextState == State.Idle)
                {
                    if (StateData is Todo todo)
                    {
                        todo.Target.Tell(new Batch(todo.Queue));
                    }
                    else
                    {
                        // nothing to do
                    }
                }
            });

            Initialize();
        }
    }
}
