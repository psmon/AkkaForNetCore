using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Models.Actor;
using AkkaNetCore.Models.Entity;
using AkkaNetCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Extensions;
using Z.EntityFramework.Plus;



namespace AkkaNetCore.Actors.Utils
{
    // 일괄 처리(데이터 인입)    
    public class BatchWriterActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        private readonly int BatchSize = 1000;
        
        public BatchWriterActor()
        {
            ReceiveAsync<object>(async message =>
            {                
                if (message is Batch batchMessage)
                {
                    Context.IncrementMessagesReceived();

                    var bulkItems_reseverd = new List<MessageReseved>();
                    var bulkItems_completed = new List<MessageCompleted>();
                    foreach (var item in batchMessage.Obj)
                    {
                        if(item is DelayMsg delayMsg)
                        {
                            if(delayMsg.State == DelayMsgState.Reserved)
                            {
                                bulkItems_reseverd.Add(new MessageReseved()
                                {
                                    Seq = delayMsg.Seq,
                                    Message = delayMsg.Message,
                                    updateTime = DateTime.Now
                                });
                            }
                            else if (delayMsg.State == DelayMsgState.Completed)
                            {
                                bulkItems_completed.Add(new MessageCompleted()
                                {
                                    Seq = delayMsg.Seq,
                                    Message = delayMsg.Message,
                                    updateTime = DateTime.Now
                                });
                            }
                        }                        
                    }

                    if (bulkItems_reseverd.Count > 0)
                    {
                        EntityFrameworkManager.ContextFactory = context => new BatchRepository(Startup.AppSettings);
                        using (var context = new BatchRepository(Startup.AppSettings))
                        {
                            context.BulkInsert(bulkItems_reseverd, options => {
                                options.BatchSize = BatchSize;
                            });
                            Context.IncrementCounter("akka.custom.received1", bulkItems_reseverd.Count);
                        }
                    }

                    if (bulkItems_completed.Count > 0)
                    {
                        EntityFrameworkManager.ContextFactory = context => new BatchRepository(Startup.AppSettings);
                        using (var context = new BatchRepository(Startup.AppSettings))
                        {
                            context.BulkInsert(bulkItems_completed, options => {
                                options.BatchSize = BatchSize;
                            });
                            Context.IncrementCounter("akka.custom.received1", bulkItems_completed.Count);
                        }
                    }
                    logger.Info("========= BulkInsert:" + batchMessage.Obj.Count.ToString());
                }
            });
        }
    }

    public class BatchActor : FSM<State, IData>
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private int CollectSec;

        public BatchActor()
        {
            CollectSec = 3;

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
