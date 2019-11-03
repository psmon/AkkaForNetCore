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
    public class PrinterController : Controller
    {
        private IActorRef printerActor;

        public PrinterController(PrinterActorProvider _printerActorProvider)
        {
            printerActor = _printerActorProvider();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] PrintPage value)
        {
            // 프린팅을 요청한다.
            printerActor.Tell(value);
        }        
    }
}