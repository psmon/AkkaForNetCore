using System;
using System.Collections.Immutable;
using Akka.Actor;

namespace AkkaNetCore.Models.Message
{
    // received events
    public class SetTarget
    {
        public SetTarget(IActorRef @ref)
        {
            Ref = @ref;
        }

        public IActorRef Ref { get; }
    }

    public class Queue
    {
        public Queue(object obj)
        {
            Obj = obj;
        }

        public Object Obj { get; }
    }

    public class Flush { }

    // send events
    public class Batch
    {
        public Batch(ImmutableList<object> obj)
        {
            Obj = obj;
        }

        public ImmutableList<object> Obj { get; }
    }

    public enum State
    {
        Idle,
        Active
    }

    // data
    public interface IData { }

    public class Uninitialized : IData
    {
        public static Uninitialized Instance = new Uninitialized();

        private Uninitialized() { }
    }

    public class Todo : BaseMessage,IData
    {
        public Todo(IActorRef target, ImmutableList<object> queue)
        {
            Target = target;
            Queue = queue;
        }

        public IActorRef Target { get; }

        public ImmutableList<object> Queue { get; }

        public Todo Copy(ImmutableList<object> queue)
        {
            return new Todo(Target, queue);
        }
    }
}
