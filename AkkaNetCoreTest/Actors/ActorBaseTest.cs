// 참고:
//https://petabridge.com/blog/how-to-unit-test-akkadotnet-actors-akka-testkit/
//https://getakka.net/articles/actors/testing-actor-systems.html

using System;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.NUnit3;
using AkkaNetCore.Actors;
using NUnit.Framework;

namespace AkkaNetCoreTest.Actors
{
    class ActorBaseTest : TestKit
    {
        TestProbe probe;

        [SetUp]
        public void Setup()
        {
            probe = this.CreateTestProbe();
        }
        
        [TestCase(100, 300)]
        public void Actor_should_respond_within_max_allowable_time(int delay, int cutoff)
        {
            var cashGate = Sys.ActorOf(Props.Create(() => new CashGateActor(delay)));
            // sets a maximum allowable time for entire block to finish
            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                cashGate.Tell("정산게이트 통과요청");
                ExpectMsg("정산완료 통과하세요");
            });
        }
    }
}