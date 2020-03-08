using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Akka.TestKit;
using Akka.TestKit.NUnit3;
using AkkaNetCore.Adapters;
using AkkaNetCore.Models.Message;
using NUnit.Framework;

namespace AkkaNetCoreTest.Adapters
{
    class KafkaConsumerTest : TestKit
    {
        KafkaProduce kafkaProduce;
        KafkaConsumer kafkaConsumer;
        TestProbe probe;

        [SetUp]
        public void Setup()
        {
            kafkaConsumer = new KafkaConsumer("kafka:9092", "test_consumer");
            probe = this.CreateTestProbe();
            kafkaConsumer.CreateConsumer(probe).Start();

            kafkaProduce = new KafkaProduce("kafka:9092", "test_consumer");
        }

        [Test]
        public void ProduceAndConsumerTest()
        {            
            kafkaProduce.Produce("SomeMessage");

            Within(TimeSpan.FromSeconds(10), () => {

                AwaitCondition(() => probe.HasMessages);

                probe.ExpectMsg<KafkaMessage>(TimeSpan.FromSeconds(0));

                KafkaMessage lastMessage = probe.LastMessage as KafkaMessage;

                Assert.AreEqual("SomeMessage", lastMessage.Value as string);

            });
        }

    }
}
