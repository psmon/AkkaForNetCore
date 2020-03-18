using System;
using Akka;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using Akka.Streams;
using Akka.Streams.Dsl;
using AkkaNetCore.Models.LoadTest;
using AkkaNetCore.Models.Message;

namespace AkkaNetCore.Actors.LoadTest
{

    // 작업량을 조절합니다.
    public class ThrottleLimitWork : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        private IActorRef consumer;

        private int elementPerSec = 1;  //기본능력은 초당 1입니다.

        public ThrottleLimitWork()
        {

            //초당 최대 처리수를 실시간 조정가능합니다.
            ReceiveAsync<int>(async limit =>
            {
                elementPerSec = limit;
            });

            //조절처리 이후 방류된 메시지를 소비할 액터를 설정합니다.
            ReceiveAsync<SetTarget>(async target =>
            {
                consumer = target.Ref;
            });

            ReceiveAsync<object>(async message =>
            {
                if (message is Batch batchMessage)
                {
                    int Count = batchMessage.Obj.Count;
                    Context.IncrementMessagesReceived();
                    Source<object, NotUsed> source = Source.From(batchMessage.Obj);

                    using (var materializer = Context.Materializer())
                    {
                        var factorials = source;
                        factorials                             
                             .Throttle(elementPerSec, TimeSpan.FromSeconds(1), 1, ThrottleMode.Shaping)
                             .RunForeach(obj => {
                                 var nowstr = DateTime.Now.ToString("mm:ss");
                                 if (obj is ApiCallSpec apiCallSpec)
                                 {
                                     //방류조절기에의해 셋팅된 TPS에의해 명령수행합니다.
                                     if (consumer != null) consumer.Tell(apiCallSpec);
                                 }
                             }, materializer)
                             .Wait();
                    }
                }
            });
        }
    }
}
