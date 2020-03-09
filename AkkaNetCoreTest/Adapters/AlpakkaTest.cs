using System;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Streams.Kafka.Dsl;
using Akka.Streams.Kafka.Messages;
using Akka.Streams.Kafka.Settings;
using Akka.TestKit;
using Confluent.Kafka;
using Hocon;
using Xunit;
using Xunit.Abstractions;


//https://github.com/akkadotnet/Akka.Streams.Kafka
//https://github.com/akka/alpakka

namespace AkkaNetCoreTest.Adapters
{
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

            var config = HoconConfigurationFactory.FromFile("akka.kafka.conf");
            //akka.loglevel = DEBUG
            //akka.suppress - json - serializer - warning = true

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

        [Theory]
        [InlineData(20,10)]        
        public void ProduceAndConsumeAreOK(int testCnt, int cutoff)
        {            
            int readyTimeForConsume = 3;            
            Source<int,NotUsed> source = Source.From( Enumerable.Range(1, testCnt));

            int recCnt = 0;
            KafkaConsumer.PlainSource(consumerSettings, subscription)
            .RunForeach(result =>
            {
                Console.WriteLine($"Consumer: {result.Topic}/{result.Partition} {result.Offset}: {result.Value}");
                recCnt++;
                if (recCnt == testCnt)
                    probe.Tell("카프카수신OK");
            }, materializer_consumer);

            Task.Delay(TimeSpan.FromSeconds(readyTimeForConsume)).Wait();

            source                
                .Select(c => c.ToString())
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
                probe.ExpectMsg("카프카수신OK",TimeSpan.FromSeconds(cutoff));
            });
        }
    }
}
