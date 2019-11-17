using System;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class HigPassGateActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;

        public HigPassGateActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"하이패스 액터 생성:{id}");


            ReceiveAsync<string>(async msg =>
            {
                //하이패스는 그냥 지나가면됨
                logger.Debug($"{msg}");
            });
        }

    }
}
