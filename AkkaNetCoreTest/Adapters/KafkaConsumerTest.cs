using System;
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
        }

        [Fact]
        public void ProduceAndConsumerTest()
        {            
            kafkaProduce.Produce("SomeMessage");

            Within(TimeSpan.FromSeconds(10), () => {

                AwaitCondition(() => probe.HasMessages);

                probe.ExpectMsg<KafkaMessage>(TimeSpan.FromSeconds(0));

                KafkaMessage lastMessage = probe.LastMessage as KafkaMessage;

                Assert.Equal("SomeMessage", lastMessage.Value as string);

            });
        }

    }
}
