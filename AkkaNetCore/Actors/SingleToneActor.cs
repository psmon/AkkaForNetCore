using System;
using System.Diagnostics;
using Akka.Actor;
using Akka.Cluster;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class SingleToneActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int totalCount;        
        private DateTime startTime;
                
        public SingleToneActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"싱글톤 액터 생성:{id}");
            startTime = DateTime.Now;            
            totalCount = 0;

            ReceiveAsync<int>(async amount =>
            {
                totalCount += amount;                                
                //100개씩마다 로그찍음
                if ( (totalCount % 100) == 0)
                {
                    DateTime endTime = DateTime.Now;
                    TimeSpan timeSpan = endTime - startTime;                    
                    logger.Info($"====== 메시지 처리량:{totalCount} TPS:{endTime.Second}");
                    startTime = endTime;
                }
            });
        }
    }
}
