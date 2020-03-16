using System;
using Akka.Actor;
using Akka.TestKit;
using AkkaNetCore.Models.Message;
using Xunit;
using Xunit.Abstractions;

namespace AkkaNetCoreTest.Actors
{
    public class MailBoxTest : TestKitXunit
    {
        protected TestProbe probe;
        protected IActorRef mailBoxActor;

        public MailBoxTest(ITestOutputHelper output) : base(output)
        {
            // 이 테스트를 위한 akkaconfig는 다음에 설정되어있습니다.
            // TestKitXunit
            // TestKitXunit.akkaConfig  : 메일박스 설정
            Setup();
        }

        public void Setup()
        {
            //여기서 관찰자는 Qa 알림 역활을 받습니다.
            probe = this.CreateTestProbe();            
            var mailboxOpt = Props.Create<MailBoxActor>(probe).WithMailbox("my-custom-mailbox");
            mailBoxActor = Sys.ActorOf(mailboxOpt, "mymailbox");

        }

        [Fact(DisplayName = "보안결함_메시지가_먼저처리_되어야한다")]
        public void Test1()
        {
            // isBug가 false인 보안결함 메시지가 먼저 처리되어야합니다.
            Issue msg1 = new Issue("test1", true);
            Issue msg2 = new Issue("test2", false);
            Issue msg3 = new Issue("test3", true);
            Issue msg4 = new Issue("test4", true);
            Issue msg5 = new Issue("test5", false);
            mailBoxActor.Tell(msg1);
            mailBoxActor.Tell(msg2);
            mailBoxActor.Tell(msg3);
            mailBoxActor.Tell(msg4);
            mailBoxActor.Tell(msg5);

            for(int i = 0; i < 5; i++)
            {
                probe.ExpectMsg<Issue>(issue =>
                {
                    if (i < 2)
                        Assert.Equal(true, issue.IsSecurityFlaw);
                    else
                        Assert.Equal(true, issue.IsBug);

                    Console.WriteLine($"IssueInfo : Message:{issue.Message} " +
                        $"IsSecurityFlaw:{issue.IsSecurityFlaw} IsBug:{issue.IsBug} ");
                });
            }
            /* 기대결과
            IssueInfo: Message: test2 IsSecurityFlaw:True IsBug:False
            IssueInfo : Message: test5 IsSecurityFlaw:True IsBug:False
            IssueInfo : Message: test4 IsSecurityFlaw:False IsBug:True
            IssueInfo : Message: test3 IsSecurityFlaw:False IsBug:True
            IssueInfo : Message: test1 IsSecurityFlaw:False IsBug:True
            */
        }
    }

    public class MailBoxActor : ReceiveActor
    {
        IActorRef notifyQa;
        public MailBoxActor(IActorRef _notifyQa)
        {
            notifyQa = _notifyQa;
            //MailBoxTest
            Receive<Issue>(issue => {                
                //Console.WriteLine($"IssueInfo : Message:{issue.Message} IsSecurityFlaw:{issue.IsSecurityFlaw} IsBug:{issue.IsBug} ");
                notifyQa.Tell(issue);
            });
            //InboxTest
            Receive<string>(msg =>
            {
                if (msg == "hello")
                    Sender.Tell("world");
            });
        }
    }
}
