using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Models.Actor
{
    public enum DelayMsgState
    {
        Reserved = 0,
        Completed = 1
    }

    public class DelayMsg
    {
        public String Seq { get; set; }

        public DelayMsgState State {get;set;}

        public string Message { get; set; }

        public int Delay { get; set; }
    }
}
