using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class TonerActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int tonerAmount = 5;
        IActorRef probe;

        public TonerActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"토너 액터 생성:{id}");
            ReceiveAsync<int>(async usageAmount =>
            {
                logger.Info($"토너 소모 :{usageAmount}");

                if (tonerAmount < 1)
                {
                    //송신자 에게 이야기하기
                    Sender.Tell("토너가 모두 소모되었습니다.");
                    logger.Warning("토너 소진,토너를 충전하라");
                }
                tonerAmount -= usageAmount;                
            });

            ReceiveAsync<string>(async msg =>
            {
                if (msg == "남은용량?")
                {
                    string response = $"남은 용량은 {tonerAmount} 입니다.";
                    Sender.Tell(response);

                    // Forward는 메시지가 중재자를 통과하더라도 원래 발신자 주소가 유지됨,라우터,복제기등에 유용
                    if (probe != null) probe.Forward(response);
                }
            });

            ReceiveAsync<IActorRef>(async msg =>
            {
                probe = msg;
            });

        }
    }
}
