// 참고: 유닛테스트가 QA에 대한 커버리지를 높이는데 유용하냐? 와는 별도로
// 유닛테스트 작성은 개발코드 퀄리티를 높이는데 크게 기여한다.
// 비동기로 항상 작동하는 실시간 메시징을 검사하는 방법은 조금 더 고급방법이 요구되며
// Akka.TestKit 은 액터 메시징을 검사하는데 단순화된 방법을 제공합니다.
//
// 유닛테스트가 없는 경우:
// 1. DI에 의존한나머지, 자신이 만든 객체를 ZeroBase 에서 생성 하는방법을 알지 못한다.(DI가 나쁜게아닌,DI의 노예가되어 개발수준이 낮아지는것을 의미한다.)
// 2. 자신의 코드는, 디펀던시가 복잡한 서비스 내에서만 작동가능 하고 예측불가다.(몬스터를 계속 만들어내고 있다.)
// 3. 최초 작성되고 수정된모듈이 기대 작동에대한 정의가 없기때문에, 이것을 수정한 사람은 상상력을 동원하고,항상 다르게 작동한고 심지어 개선된 것처럼 보이기까지 한다.

// Unit Test 참고링크
// https://docs.microsoft.com/ko-kr/dotnet/core/testing/unit-testing-with-nunit
// https://getakka.net/articles/actors/testing-actor-systems.html
// https://petabridge.com/blog/how-to-unit-test-akkadotnet-actors-akka-testkit/

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit;
using AkkaNetCore.Actors.Study;
using AkkaNetCore.Models.Message;
using Xunit;
using Xunit.Abstractions;

namespace AkkaNetCoreTest.Actors
{
    // 테스트 목적 : 액터의 기본 사용법을 알수 있습니다.
    public class ActorBaseTest : TestKitXunit
    {
        TestProbe probe;

        public ActorBaseTest(ITestOutputHelper output) : base(output)
        {
            Setup();
        }
        
        public void Setup()
        {
            probe = this.CreateTestProbe();
        }

        [Theory]
        [InlineData(100)]
        public void 기본액터전송은_100ms이내에_이루어져야한다(int cutoff)
        {
            var basicActor = Sys.ActorOf(Props.Create(() => new BasicActor()));
            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                basicActor.Tell("메시지전송");
                ExpectMsg("ok");
            });
        }

        [Theory]
        [InlineData(100,500)]
        public void 정산요청을하면_정산이완료되어야한다(int delay, int cutoff)
        {
            var cashGate = Sys.ActorOf(Props.Create(() => new CashGateActor()));
            // sets a maximum allowable time for entire block to finish
            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                var msg = new DelayMsg()
                { 
                    Delay = delay,
                    Message = "정산해주세요"
                };
                cashGate.Tell(msg);
                ExpectMsg("정산완료 통과하세요");
            });
        }

        [Theory]
        [InlineData(300)]
        public async Task 프린트를하면_토너가감소해야한다(int cutoff)
        {
            // Printer -> Toner -> TestProbe
            var toner = Sys.ActorOf(Props.Create(() => new TonerActor()),"toner");            
            var printer = Sys.ActorOf(Props.Create(() => new PrinterActor(null)), "printer");

            //토너 관찰자 설정
            toner.Tell(probe.Ref);

            Within(TimeSpan.FromMilliseconds(cutoff), () =>
            {
                //프린터를 요청한다.
                printer.Tell(new PrintPage()
                {
                    SeqNo = 1,
                    DelayForPrint = 1,
                    Content = "test"
                });

                //관찰자의 메시지를 조사하여, 토너량을 알아낸다.
                probe.ExpectMsg("남은 용량은 4 입니다.");
            });
        }
    }
}