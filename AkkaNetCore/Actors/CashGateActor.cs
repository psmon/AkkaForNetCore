using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;

namespace AkkaNetCore.Actors
{
    public class CashGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private Random rnd;
        private int msgCnt;

        public CashGateActor(int delay)
        {
            rnd = new Random();
            id = Guid.NewGuid().ToString();
            msgCnt = 0;
            logger.Info($"현금정산게이트 액터 생성:{id} {delay}");

            ReceiveAsync<string>(async msg =>
            {
                msgCnt++;

                Context.IncrementMessagesReceived();

                Context.IncrementCounter("akka.custom.metric2");

                //랜덤 Delay를 줌
                int auto_delay = delay==0 ? rnd.Next(300,1000) : delay;
                await Task.Delay(auto_delay);

                if( (msgCnt % 10)==0)
                    logger.Info($"{msg}-{auto_delay}-{msgCnt}");
                //Sender.Tell($"정산완료 통과하세요");                
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
