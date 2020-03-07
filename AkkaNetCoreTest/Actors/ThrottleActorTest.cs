using System;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.NUnit3;
using AkkaNetCore.Actors.Utils;
using AkkaNetCore.Models.Message;
using NUnit.Framework;

namespace AkkaNetCoreTest.Actors
{
    class ThrottleActorTest : TestKit
    {
        protected TestProbe probe;

        [SetUp]
        public void Setup()
        {
            //스트림을 제공받는 최종 소비자 ( 물을 제공 받는 고객 )
            probe = this.CreateTestProbe();
        }

        [TestCase(15)]
        public void ThrottleActorAreOK(int cutoffSec)
        {
            // 초당 5개 처리한정 ( 더 처리하고 싶으면 이값을 늘린다.)
            int timeSec = 1;
            int elemntPerSec = 5;            
            var throttleActor = Sys.ActorOf(Props.Create(() => new ThrottleActor(timeSec)));
            var throttleWork = Sys.ActorOf(Props.Create(() => new ThrottleWork(elemntPerSec, timeSec)));

            // 밸브에게 작업자 지정 ( 밸브는 초당 스트림을 모아서 방출한다 )
            // 작업자는 방류된 스트림을 기본적으로 쌓아두고, 초당 지정된 개수만 처리한다.
            throttleActor.Tell(new SetTarget(throttleWork));            

            // 소비자지정 : 소비자는 몇개가 초당 처리되던 상관없이, 완료된 작업만 제공받는다.
            throttleWork.Tell(new SetTarget(probe));

            Within(TimeSpan.FromSeconds(cutoffSec), () =>
            {
                // 50개 처리완료는 10초이내에 끝나야함...
                for(int i=0; i<50; i++)
                {
                    string seq = (i + 1).ToString();
                    throttleActor.Tell(new Queue(new DelayMsg()
                    {
                        Delay = 0,
                        Seq = seq,
                        Message = $"초당:{elemntPerSec} 테스트-{seq}",
                        State = DelayMsgState.Reserved
                    }));
                }

                DelayMsg lastMessage = null;
                for (int i = 0; i < 50; i++)
                {
                    lastMessage =probe.ExpectMsg<DelayMsg>();
                }
                //마지막 메시지의 Seq는 50이여야함
                Assert.AreEqual("50", lastMessage.Seq);

            });
        }
        /* 위 테스트의 결과로그,블락킹없이 실시간으로 초당 5개씩 처리함
            [49:09] -  초당:5 테스트-1
            [49:09] -  초당:5 테스트-2
            [49:09] -  초당:5 테스트-3
            [49:09] -  초당:5 테스트-4
            [49:10] -  초당:5 테스트-5
            [49:10] -  초당:5 테스트-6
            [49:10] -  초당:5 테스트-7
            [49:10] -  초당:5 테스트-8
            [49:10] -  초당:5 테스트-9
            [49:11] -  초당:5 테스트-10         
        */
    }
}
