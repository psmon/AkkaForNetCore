using System;

namespace AkkaNetCore.Models.Message
{
    public enum DelayMsgState
    {
        Reserved = 0,
        Completed = 1
    }

    public class DelayMsg : BaseMessage
    {
        public String Seq { get; set; }

        public DelayMsgState State {get;set;}

        public string Message { get; set; }

        public int Delay { get; set; }
    }
}
