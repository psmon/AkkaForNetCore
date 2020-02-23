using System;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class SingleToneActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private UInt64 totalCount;
        
        public SingleToneActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"싱글톤 액터 생성:{id}");

            ReceiveAsync<UInt64>(async amount =>
            {                
                totalCount += amount;

                //100개씩마다 로그찍음
                if( (totalCount % 100) == 0)
                    logger.Info($"====== 메시지 처리량:{totalCount}");
            });
        }
    }
}
