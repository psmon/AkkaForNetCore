using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaNetCore.Models.Message
{
    public class KafkaMessage
    {
        public KafkaMessage(string topic,object value)
        {
            Topic = topic;
            Value = value;
        }

        public string Topic { get; set; }

        public object Value { get; set; }

    }
}
