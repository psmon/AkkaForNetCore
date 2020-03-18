using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaNetCore.Config;
using AkkaNetCore.Models.LoadTest;
using Microsoft.AspNetCore.Mvc;

namespace AkkaNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoadTestController : Controller
    {
        IActorRef TPSCommandActor;

        public LoadTestController()
        {
            TPSCommandActor = AkkaLoad.ActorSelect("TPSCommandActor");
        }


        /// <summary>
        /// 로드테스트
        /// </summary>
        /// <response code="200">성공</response>
        /// <response code="412">
        /// ....         
        /// </response>
        [HttpPost("/ApiCall/SomeOne")]
        public void SomeOne(int tps, int count)
        {
            //TPS 조정
            TPSCommandActor.Tell(tps);

            //Load Test
            for (int i = 0; i < count; i++)
            {
                ApiCallSpec apiCallSpec = new ApiCallSpec()
                {
                    Arg = new ApiArguMent()
                    {
                        SomeArg1 = i,
                        SomeArg2 = i * 10,
                        SomeArg3 = "ApiTest:" + i
                    }
                };
                TPSCommandActor.Tell(apiCallSpec);
            }
        }
    }
}