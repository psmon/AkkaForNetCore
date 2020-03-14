using System;
using System.Threading.Tasks;
using Akka.TestKit;
using AkkaNetCore.Adapters;
using AkkaNetCore.Models.Message;
using Xunit;
using Xunit.Abstractions;

namespace AkkaNetCoreTest.Adapters
{
    public class KafkaConsumerTest : TestKitXunit
    {
        KafkaProduce kafkaProduce;
        KafkaConsumer kafkaConsumer;
        TestProbe probe;

        public KafkaConsumerTest(ITestOutputHelper output) : base(output)
        {
            Setup();
        }
        
        public void Setup()
        {
            kafkaConsumer = new KafkaConsumer("kafka:9092", "test_consumer");
            
            probe = this.CreateTestProbe();
            
            kafkaConsumer.CreateConsumer(probe).Start();

            kafkaProduce = new KafkaProduce("kafka:9092", "test_consumer");

            // Wait for SystemLoad
            Task.Delay(3000).Wait();
        }

        [Theory]
        [InlineData(10,10)]
        public void ProduceAndConsumerTest(int cutoff,int repeat)
        {
            for(int i=1;i<repeat+1; i++)
                kafkaProduce.Produce("SomeMessage:"+i);

            Within(TimeSpan.FromSeconds(cutoff), () => {

                for (int i = 0; i < repeat; i++)
                    probe.ExpectMsg<KafkaMessage>(TimeSpan.FromSeconds(3));

                KafkaMessage lastMessage = probe.LastMessage as KafkaMessage;

                Assert.Equal($"SomeMessage:{repeat}", lastMessage.Value as string);

            });
        }

    }
}
