using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaNetCore.Models.Message;
using Confluent.Kafka;

namespace AkkaNetCore.Adapters
{
    public class KafkaConsumer
    {
        private string server;
        private string topic;

        private CancellationToken ct;
        CancellationTokenSource tokenSource2;

        public Boolean HasMessage { get; set; }

        public KafkaConsumer(string _server, string _topic)
        {
            server = _server;
            topic = _topic;
            tokenSource2 = new CancellationTokenSource();
            ct = tokenSource2.Token;
        }

        public void Stop()
        {
            tokenSource2.Cancel();
        }

        public Task CreateConsumer(IActorRef consumeAoctor)
        {
            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = server,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            Console.WriteLine("kafka StartConsumer ");

            var task = new Task(() => {

                // Were we already canceled?
                ct.ThrowIfCancellationRequested();

                using (var consumer = new ConsumerBuilder<Ignore, string>(conf).Build())
                {
                    consumer.Subscribe(topic);
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                if (ct.IsCancellationRequested)
                                {
                                    // Clean up here, then...
                                    ct.ThrowIfCancellationRequested();
                                }
                                else
                                {
                                    var cr = consumer.Consume(ct);
                                    if (consumeAoctor != null) consumeAoctor.Tell(new KafkaMessage(cr.Topic, cr.Value));
                                    HasMessage = true;
                                    Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                                }
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Error occured: {e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Ensure the consumer leaves the group cleanly and final offsets are committed.
                        consumer.Close();
                    }
                }

            }, tokenSource2.Token);

            return task;
        }
    }
}
