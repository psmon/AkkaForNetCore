using System;
using Akka.Actor;
using Akka.TestKit;
using AkkaNetCore.Actors.Study;
using Xunit;
using Xunit.Abstractions;

namespace AkkaNetCoreTest.Actors
{
    public class AtLeastOnceDeliveryTest : TestKitXunit
    {
        protected TestProbe probe;

        protected IActorRef deliveryManActor;

        public AtLeastOnceDeliveryTest(ITestOutputHelper output) : base(output)
        {            
            Setup();
        }

        public void Setup()
        {
            //여기서 관찰자는 Qa 알림 역활을 받습니다.
            probe = this.CreateTestProbe();

            deliveryManActor = Sys.ActorOf(Props.Create(() => new DeliveryManActor(probe)));

        }

        [Theory]
        [InlineData(3,13000)]
        public void 일반택배는_일정한확률로_분실되며_분실시_재전송된다(int repeat, int cutoff)
        {
            for (int i = 0; i < repeat; i++)
            {
                deliveryManActor.Tell("일반택배발송:" + i);
            }

            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                for (int i = 0; i < repeat; i++)
                {
                    probe.ExpectMsg<string>(TimeSpan.FromMilliseconds(cutoff));
                }
            });
        }

        [Theory]
        [InlineData(100,300)]
        public void 고급택배는_항상성공하여_빠르게모두처리된다(int repeat,int cutoff)
        {
            for (int i = 0; i < repeat; i++)
            {
                deliveryManActor.Tell("고급택배발송:" + i);
            }

            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                for (int i = 0; i < repeat; i++)
                {
                    probe.ExpectMsg<string>(TimeSpan.FromMilliseconds(cutoff));
                }
            });
        }

    }
}
