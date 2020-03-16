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
            //여기서 관찰자는 고객이 받은 택배를 카운팅합니다.
            probe = this.CreateTestProbe();

            deliveryManActor = Sys.ActorOf(Props.Create(() => new DeliveryManActor(probe)),"deliveryman");

        }

        [Theory(DisplayName = "일반택배는 일정한확률로 분실되며 분실시 재전송된다")]
        [InlineData(3,13000)]
        public void Test1(int repeat, int cutoff)
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

        [Theory(DisplayName = "고급택배는_항상성공하여_빠르게모두처리된다")]
        [InlineData(100,300)]
        public void Test2(int repeat,int cutoff)
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
