using Akka.Actor;
using Akka.Monitoring;
using AkkaNetCore.Actors.Utils;
using AkkaNetCore.Models.LoadTest;
using AkkaNetCore.Models.Message;


namespace AkkaNetCore.Actors.LoadTest
{
    //한꺼번에 명령가능한 액터로,TPS조절기가 빌트인 되어있습니다.
    public class TPSCommandActor : ReceiveActor
    {
        IActorRef batchActor;
        IActorRef throttleLimitWork;
        int seqNo;

        public TPSCommandActor(IActorRef workerPool)
        {
            seqNo = 0;

            // 1초에 한번씩 데이터를 방류하는 배치액터를 생성합니다. -실시간 비동기 FSM 패턴이 사용됨
            batchActor = Context.ActorOf(Props.Create(() => new BatchActor(1)) );
            // TPS를 조절하는 조절기를 생성합니다.
            throttleLimitWork = Context.ActorOf(Props.Create(() => new ThrottleLimitWork()));

            //배치액터와 조절기를 연결합니다.
            batchActor.Tell(new SetTarget(throttleLimitWork));
            //조절기에 워커풀을 연결합니다.(라운드로빈풀)
            throttleLimitWork.Tell(new SetTarget(workerPool));

            //기본 능력치를 5로 설정합니다.
            throttleLimitWork.Tell(5);

            //초당 처리능력 실시간 변경가능합니다.
            ReceiveAsync<int>(async limit =>
            {                
                throttleLimitWork.Tell(limit);
            });

            // LoadTest 시작 명령은 이곳으로부터 출발합니다.
            // batchActor -> throttleLimitWork -> _worker
            ReceiveAsync<ApiCallSpec>(async msg =>
            {
                msg.SeqNo = seqNo;  //자동증가번호사용
                //배치처리 Queue에 적재합니다.
                batchActor.Tell(new Queue(msg));
                seqNo++;
            });
        }

        protected override void PreStart()
        {
            Context.IncrementActorCreated();
        }

        protected override void PostStop()
        {
            Context.IncrementActorStopped();
        }
    }
}
