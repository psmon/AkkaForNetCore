using System;
using System.Threading.Tasks;
using Akka.TestKit;
using AkkaNetCore.Adapters;
using AkkaNetCore.Models.Message;
using Xunit;
using Xunit.Abstractions;

namespace AkkaNetCoreTest.Adapters
{
    // 테스트 목적 : Kafka 모듈을 액터모듈과 연동하여 메시지를 더 우아하게 소비할수 있습니다.
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

        [Theory(DisplayName = "카프카_생산과소비는_일치해야한다 초당5개씩만 처리해야한다")]
        [InlineData(10,10)]
        public void Test1(int cutoff,int repeat)
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
