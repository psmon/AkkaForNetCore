using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class CashGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private Random rnd;

        public CashGateActor(int delay)
        {
            rnd = new Random();
            id = Guid.NewGuid().ToString();
            logger.Info($"현금정산게이트 액터 생성:{id} {delay}");

            ReceiveAsync<string>(async msg =>
            {
                //현금정산에 걸리는시간 1~10초, 0일때는 랜덤, 값이 주어질땐 주어진만큼                 
                int auto_delay = delay==0 ? rnd.Next(1000, 10000) : delay;
                await Task.Delay(auto_delay);
                logger.Info($"{msg}-{auto_delay}");
                Sender.Tell($"정산완료 통과하세요");
            });
        }        
    }
}
