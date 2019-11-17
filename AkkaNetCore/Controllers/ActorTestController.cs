using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using AkkaNetCore.Models.Actor;
using Microsoft.AspNetCore.Mvc;
using static AkkaNetCore.Actors.ActorProviders;

namespace AkkaNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActorTestController : Controller
    {
        private readonly IActorRef printerActor;
        private readonly IActorRef highPassActor;
        private readonly IActorRef cashPassActor;


        public ActorTestController(PrinterActorProvider _printerActorProvider,
            HigPassGateActorProvider _higPassGateActorProvider,CashGateActorProvider _cashGateActorProvider)
        {
            printerActor = _printerActorProvider();
            highPassActor = _higPassGateActorProvider();
            cashPassActor = _cashGateActorProvider();
        }
        
        [HttpPost("/printer")]
        public void Printer([FromBody] PrintPage value)
        {
            // 프린팅을 요청한다.
            printerActor.Tell(value);
        }
        
        [HttpPost("/gate/highpassgate")]
        public void Highpassgate([FromBody] string value)
        {
            highPassActor.Tell(value);
        }
        
        [HttpPost("/gate/cashgate")]
        public void Cashgate([FromBody] string value)
        {
            cashPassActor.Tell(value);
        }
        
    }
}