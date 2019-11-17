using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using AkkaNetCore.Models.Actor;

namespace AkkaNetCore.Actors
{
    public class CashGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private Random rnd;

        public CashGateActor()
        {
            rnd = new Random();

            id = Guid.NewGuid().ToString();
            logger.Info($"현금정산게이트 액터 생성:{id}");

            ReceiveAsync<string>(async msg =>
            {
                //현금정산에 걸리는시간 1~10초                 
                int delay = rnd.Next(1000, 10000);
                await Task.Delay(delay);
                logger.Debug($"{msg}-{delay}");
            });
        }
        
    }
}
