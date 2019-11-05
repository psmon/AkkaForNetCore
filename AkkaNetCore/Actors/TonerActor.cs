﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors
{
    public class TonerActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();
        private readonly string id;
        private int tonerAmount = 5;

        public TonerActor()
        {
            id = Guid.NewGuid().ToString();
            logger.Info($"토너 액터 생성:{id}");
            ReceiveAsync<int>(async usageAmount =>
            {
                logger.Info($"토너 소모 :{usageAmount}");

                if (tonerAmount < 1)
                {
                    //송신자 에게 이야기하기
                    Sender.Tell("토너가 모두 소모되었습니다.");
                }
                tonerAmount -= usageAmount;                
            });
        }
    }
}
