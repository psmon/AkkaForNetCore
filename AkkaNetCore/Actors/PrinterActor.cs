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
                logger.Debug($"페이지 출력 완료:{page}");
            });            
        }
    }
}
