using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Models.Message;

namespace AkkaNetCore.Actors
{
    public class PrinterActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private ActorSelection tonerActor;

        public PrinterActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"프린터 액터 생성:{id}");

            //주소로 액터를 선택하기 : 위치투명성,참조객체를 얻기위해 DI가 복잡해질필요 없다,그리고 이것은 리모트로확장시 코드변화가없다.
            tonerActor = Context.System.ActorSelection("user/toner");

            ReceiveAsync<PrintPage>(async page =>
            {
                logger.Debug($"프린터 요청 들어옴:{page}");
                await Task.Delay(page.DelayForPrint);

                
                //토너를 비동기로 소모시킴
                tonerActor.Tell(1);

                //남은 토너 용량 물어봄
                var msg =await tonerActor.Ask("남은용량?");
                if (null != Sender) Sender.Tell(msg);

                logger.Debug($"ASK결과:{msg}");
                logger.Debug($"페이지 출력 완료:{page}");
            });

            ReceiveAsync<string>(async msg =>
            {
                logger.Debug($"토너 관리액터에게 받은 메시지:{msg}");
            });
        }
    }
}
