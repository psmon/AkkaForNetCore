using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Models.Actor;

namespace AkkaNetCore.Actors
{
    public class CashGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private Random rnd;
        private int msgCnt;
        protected IActorRef CountConsume;

        public CashGateActor(int delay)
        {
            rnd = new Random();
            id = Guid.NewGuid().ToString();
            msgCnt = 0;
            logger.Info($"Create CashGateActor:{id} {delay}");
            CountConsume = Startup.SingleToneActor;

            ReceiveAsync<DelayMsg>(async msg =>
            {
                msgCnt++;

                Context.IncrementMessagesReceived();
                
                //랜덤 Delay를 줌
                int auto_delay = msg.delay == 0 ? rnd.Next(300, 1000) : msg.delay;
                await Task.Delay(auto_delay);

                Context.IncrementCounter("akka.custom.received1");

                if ((msgCnt % 10) == 0)
                    logger.Info($"{msg.message}-{auto_delay}-{msgCnt}");

                CountConsume.Tell(msg);

                //수신자가 있으면 보낸다.
                if (!Sender.IsNobody())
                    Sender.Tell($"정산완료 통과하세요");

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
