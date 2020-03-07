using System;
using Akka.Actor;
using AkkaNetCore.Config;
using AkkaNetCore.Models.Message;
using Microsoft.AspNetCore.Mvc;


namespace AkkaNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActorTestController : Controller
    {
        private readonly IActorRef printerActor;
        private readonly IActorRef highPassActor;
        private readonly IActorRef cashPassActor;
        private readonly IActorRef clusterRoundbin1;
        
        private readonly IActorRef basicActor;
        private readonly IActorRef singleToneActor;


        public ActorTestController()
        {
            basicActor = AkkaLoad.ActorSelect("basic");
            printerActor = AkkaLoad.ActorSelect("printer");
            highPassActor = AkkaLoad.ActorSelect("highpass");
            cashPassActor = AkkaLoad.ActorSelect("cashpass");
            clusterRoundbin1 = AkkaLoad.ActorSelect("clusterRoundRobin");
            singleToneActor = AkkaLoad.ActorSelect("SingleToneActor");
        }

        [HttpPost("/Single/Basic/tell")]
        public void Printer(string message)
        {
            // basicActor 요청한다.
            basicActor.Tell(message);
        }

        /// <summary>
        /// 프린트 액터
        /// </summary>
        /// <response code="200">성공</response>
        /// <response code="412">
        /// ....         
        /// </response>
        [HttpPost("/Single/Printer/tell")]
        public void Printer([FromBody] PrintPage value)
        {
            // 프린팅을 요청한다.
            printerActor.Tell(value);
        }

        /// <summary>
        /// 단일노드 - HighPassGate
        /// </summary>
        /// <response code="200">성공</response>
        /// <response code="412">
        /// ....         
        /// </response>
        [HttpPost("/Single/Highpassgate/tell")]
        public void Highpassgate(string value,int count, int delay)
        {
            for(int i = 0; i < count; i++)
            {
                var delayMsg = new DelayMsg
                {
                    Seq = Guid.NewGuid().ToString(),
                    Delay = delay,
                    Message = value,
                    State = DelayMsgState.Reserved,                    
                };
                
                singleToneActor.Tell(delayMsg);
                highPassActor.Tell(delayMsg);
            }
        }

        /// <summary>
        /// 단일노드 - CashGate
        /// </summary>
        /// <response code="200">성공</response>
        /// <response code="412">
        /// ....         
        /// </response>
        [HttpPost("/Single/Cashgate/tell")]
        public void Cashgate(string value, int count, int delay)
        {
            for (int i = 0; i < count; i++)
            {
                var delayMsg = new DelayMsg
                {
                    Seq = Guid.NewGuid().ToString(),
                    Delay = delay,
                    Message = value
                };
                cashPassActor.Tell(delayMsg);
            }
        }

        /// <summary>
        /// 단일노드 - CashGate Ask 패턴
        /// </summary>
        /// <response code="200">성공</response>
        /// <response code="412">
        /// ....         
        /// </response>
        [HttpPost("/Single/Cashgate/ask")]
        public string CashgateAsk(string value)
        {
            var delayMsg = new DelayMsg
            {
                Delay = 0,
                Message = value
            };
            var result = cashPassActor.Ask<string>(delayMsg).Result;
            return result;
        }

        /// <summary>
        /// 클러스터노드 - 메시지전송
        /// role : akkanet
        /// </summary>
        /// <response code="200">성공</response>
        /// <response code="412">
        /// ....         
        /// </response>
        [HttpPost("/Cluster/msg/akkanet/tell")]
        public void Cluster(string value, int count, int delay)
        {
            for (int i = 0; i < count; i++)
            {
                var delayMsg = new DelayMsg
                {
                    Seq = Guid.NewGuid().ToString(),
                    Delay = delay,
                    Message = value,
                    State = DelayMsgState.Reserved
                };
                singleToneActor.Tell(delayMsg);
                clusterRoundbin1.Tell(delayMsg);
            }                
        }

    }
}