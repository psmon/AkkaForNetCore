using System;
using Akka.Actor;
using AkkaNetCore.Actors.Study;
using Xunit;
using Xunit.Abstractions;

namespace AkkaNetCoreTest.Actors
{
    public class SupervisorTest : TestKitXunit
    {
        IActorRef supervisor;

        public SupervisorTest(ITestOutputHelper output) : base(output)
        {
            Setup();
        }

        public void Setup()
        {            
            supervisor = Sys.ActorOf<Supervisor>("supervisor");
        }

        [Fact(DisplayName = "ArithmeticException 무시하며,NullReferenceException 재시작,나머지 예외는 중단한다.")]
        public void Test1()
        {
            supervisor.Tell(Props.Create<Child>());
            var child = ExpectMsg<IActorRef>();

            //특정상태로 셋팅 하고 확인
            child.Tell(42); // set state to 42
            child.Tell("get");
            ExpectMsg(42);
            
            child.Tell(new ArithmeticException());      // Directive.Resume
            child.Tell("get");
            ExpectMsg(42);

            //재시작 되었기때문에,Child의 상태값이 0이되었다.
            child.Tell(new NullReferenceException());   //Directive.Restart
            child.Tell("get");
            ExpectMsg(0);

            //Watch를 하면 액터 종료메시지를 받을수 있다.(아카 모니터링기능)
            Watch(child);
            child.Tell(new ArgumentException());        //Directive.Stop
            var message1 = ExpectMsg<Terminated>();
            Assert.Equal(message1.ActorRef,child);

            supervisor.Tell(Props.Create<Child>()); // create new child
            var child2 = ExpectMsg<IActorRef>();
            Watch(child2);
            child2.Tell("get"); // verify it is alive
            ExpectMsg(0);

            child2.Tell(new Exception("CRASH"));
            var message2 = ExpectMsg<Terminated>();            
            Assert.Equal(message2.ActorRef, child2);
            Assert.Equal(true, message2.ExistenceConfirmed);

        }
    }
}
