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

                int addCount = 1;
                CountConsume.Tell(addCount);

                //수신자가 있으면 보낸다.
                if (!Sender.IsNobody())
                    Sender.Tell($"정산완료 통과하세요");

            });

            ReceiveAsync<DelayMsg>(async msg =>
            {
                msgCnt++;

                Context.IncrementMessagesReceived();

                Context.IncrementCounter("akka.custom.received1");

                //랜덤 Delay를 줌
                int auto_delay = delay == 0 ? rnd.Next(300, 1000) : delay;
                await Task.Delay(auto_delay);

                Context.IncrementCounter("akka.custom.received2");

                if ((msgCnt % 10) == 0)
                    logger.Info($"{msg}-{auto_delay}-{msgCnt}");

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
