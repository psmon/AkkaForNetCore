using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Akka.Monitoring;
using AkkaNetCore.Models.Message;

namespace AkkaNetCore.Actors
{
    public class CashGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private Random rnd;
        private int msgCnt;
        protected IActorRef CountConsume;


        public CashGateActor()
        {
            rnd = new Random();
            id = Guid.NewGuid().ToString();
            msgCnt = 0;
            logger.Info($"Create CashGateActor:{id}");
            CountConsume = Startup.SingleToneActor;

            ReceiveAsync<DelayMsg>(async msg =>
            {
                msgCnt++;

                Context.IncrementMessagesReceived();
                Context.IncrementCounter("akka.custom.received1");

                if ((msgCnt % 10) == 0)
                    logger.Info($"{msg.Message}--{msgCnt}");

                if(null!=CountConsume) CountConsume.Tell(msg);

                //수신자가 있으면 보낸다.
                if (!Sender.IsNobody())
                {
                    if (msg.Message == "정산해주세요")
                    {
                        //정산 소요 시간
                        int auto_delay = msg.Delay == 0 ? rnd.Next(300, 1000) : msg.Delay;
                        await Task.Delay(auto_delay);
                        Sender.Tell($"정산완료 통과하세요");
                    }
                }                    
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
