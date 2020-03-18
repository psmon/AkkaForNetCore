using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Actors.Utils;
using AkkaNetCore.Config;
using AkkaNetCore.Models.LoadTest;
using AkkaNetCore.Models.Message;
using AkkaNetCore.Service;


namespace AkkaNetCore.Actors.LoadTest
{
    //클러스터 내에 하나만 존재하여, 호출 TPS조절을 합니다.
    public class TPSCommandActor : ReceiveActor
    {
        IActorRef batchActor;
        IActorRef throttleLimitWork;
        int seqNo;

        public TPSCommandActor(IActorRef _worker)
        {
            seqNo = 0;

            // 1초에한번씩, 큐에쌓인 데이터를 처리합니다.
            batchActor = Context.ActorOf(Props.Create(() => new BatchActor(1)));
            throttleLimitWork = Context.ActorOf(Props.Create(() => new ThrottleLimitWork()));

            //배치액터와 조절기를 연결합니다.
            batchActor.Tell(new SetTarget(throttleLimitWork));
            //조절기에 워커를 연결합니다.
            throttleLimitWork.Tell(new SetTarget(_worker));

            //기본 능력치를 5로 설정합니다.
            throttleLimitWork.Tell(5);

            //초당 처리능력을 설정합니다.
            ReceiveAsync<int>(async limit =>
            {                
                throttleLimitWork.Tell(limit);
            });

            // LoadTest 명령은 이곳으로부터 출발합니다.
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
