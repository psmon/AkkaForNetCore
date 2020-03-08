using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace AkkaNetCore.Adapters
{
    public class KafkaProduce
    {
        private readonly String topic;

        private IProducer<Null,string> producer;

        ProducerConfig config;

        public KafkaProduce(string server, string _topic)
        {
            config = new ProducerConfig { BootstrapServers = server };
            topic = _topic;
            producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public void Produce(string data)
        {
            producer.Produce(topic, new Message<Null, string> { Value = data });            
        }

        public async Task ProduceAsync(string data)
        {
            await producer.ProduceAsync(topic, new Message<Null, string> { Value = data });
        }

        public void Flush(TimeSpan span)
        {
            producer.Flush(span);
        }
    }
}
