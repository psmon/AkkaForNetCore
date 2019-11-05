using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using AkkaNetCore.Models.Actor;

namespace AkkaNetCore.Actors
{
    public class PrinterActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;

        public PrinterActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"프린터 액터 생성:{id}");
            
            ReceiveAsync<PrintPage>(async page =>
            {
                logger.Debug($"프린터 요청 들어옴:{page}");
                await Task.Delay(page.DelayForPrint);

                //주소로 액터를 선택하기 : 장점 생성자에 참조객체를 가질필요가 없다.
                ActorSelection tonerActor = Context.System.ActorSelection("user/toner");                
                //토너를 비동기로 소모시킴
                tonerActor.Tell(1);

                logger.Debug($"페이지 출력 완료:{page}");
            });

            ReceiveAsync<string>(async msg =>
            {
                logger.Debug($"토너 관리액터에게 받은 메시지:{msg}");
            });
        }
    }
}
