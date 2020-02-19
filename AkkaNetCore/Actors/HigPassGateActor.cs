using System;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class HigPassGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int msgCnt;

        public HigPassGateActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"하이패스 액터 생성:{id}");
            msgCnt = 0;

            ReceiveAsync<string>(async msg =>
            {
                //하이패스는 그냥 지나가면됨
                if( (msgCnt % 100) == 0)
                    logger.Debug($"{msg}-{msgCnt}");
                msgCnt++;
            });
        }

    }
}
