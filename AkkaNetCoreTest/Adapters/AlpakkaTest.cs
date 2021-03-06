﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Configuration;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Kafka.Dsl;
using Akka.Streams.Kafka.Messages;
using Akka.Streams.Kafka.Settings;
using Akka.TestKit;
using Confluent.Kafka;
using Xunit;
using Xunit.Abstractions;
using AkkaConfig = Akka.Configuration.Config;

// Kafka를 Reactive Stream 버전으로 사용하기
// https://github.com/akkadotnet/Akka.Streams.Kafka
// https://github.com/akka/alpakka

namespace AkkaNetCoreTest.Adapters
{
    // 테스트 목적 : Kafka와 같은 메시지 큐시스템은,Akka의 Stream과 연결하여 Reactive Stream을 준수할수 있습니다.
    public class AlpakkaTest : TestKitXunit
    {
        protected TestProbe probe;

        protected ProducerSettings<Null,string> producerSettings;

        protected ConsumerSettings<Null, string> consumerSettings;

        protected string testTopic;

        protected IAutoSubscription subscription;

        protected ActorMaterializer materializer_producer;
        protected ActorMaterializer materializer_consumer;

        public AlpakkaTest(ITestOutputHelper output) : base(output)
        {
            Setup();
        }

        protected void Setup()
        {
            testTopic = "akka100";

            subscription = Subscriptions.Topics(testTopic);

            probe = this.CreateTestProbe();

            string configText = File.ReadAllText("akka.test.conf");

            var config = ConfigurationFactory.ParseString(configText);

            var system_producer = ActorSystem.Create("TestKafka", config);
            materializer_producer = system_producer.Materializer();

            var system_consumer = ActorSystem.Create("TestKafka", config);
            materializer_consumer = system_producer.Materializer();

            this.Sys.Settings.Config.WithFallback(config);

            producerSettings = ProducerSettings<Null, string>.Create(system_producer, null, null)
                .WithBootstrapServers("kafka:9092");

            consumerSettings = ConsumerSettings<Null, string>.Create(system_consumer, null, null)
                .WithBootstrapServers("kafka:9092")
                .WithGroupId("group1");                

        }

        [Theory(DisplayName = "카프카_생산과소비는_일치해야한다 초당5개씩만 처리해야한다")]
        [InlineData(20,10)] //20 개의 메시지를 생산하고,소비한다,테스트는 10초이내에 완료되어야함(완료시 종료됨)
        public void Test1(int limit, int cutoff)
        {
            string lastSignal = Guid.NewGuid().ToString();
            int readyTimeForConsume = 3;
            int recCnt = 0;

            KafkaConsumer.CommittableSource(consumerSettings, subscription)
            .RunForeach(result =>
            {
                Console.WriteLine($"Consumer: {result.Record.Topic}/{result.Record.Partition} {result.Record.Offset}: {result.Record.Value}");                
                if (lastSignal == result.Record.Value)
                    probe.Tell("처리모두완료");

                result.CommitableOffset.Commit();

            }, materializer_consumer);
            
            Source<int, NotUsed> source = Source.From(Enumerable.Range(1, limit));
            
            source
                .Throttle(2, TimeSpan.FromSeconds(1), 1, ThrottleMode.Shaping)      //출력 조절 : 초당 2개처리
                .Select(c => 
                {
                    var result = $"No:{c.ToString()}";
                    if(c == limit)
                    {
                        result = lastSignal;
                    }                    
                    return result;
                })                
                .Select(elem => ProducerMessage.Single(new ProducerRecord<Null, string>(testTopic, elem)))
                .Via(KafkaProducer.FlexiFlow<Null, string, NotUsed>(producerSettings))
                .Select(result =>
                {
                    var response = result as Result<Null, string, NotUsed>;
                    Console.WriteLine($"Producer: {response.Metadata.Topic}/{response.Metadata.Partition} {response.Metadata.Offset}: {response.Metadata.Value}");
                    return result;
                })
                .RunWith(Sink.Ignore<IResults<Null, string, NotUsed>>(), materializer_producer);

            Within(TimeSpan.FromSeconds(cutoff), () =>
            {
                probe.ExpectMsg("처리모두완료", TimeSpan.FromSeconds(cutoff));
            });
        }
    }
}
