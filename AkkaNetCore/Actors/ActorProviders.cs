﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;

namespace AkkaNetCore.Actors
{
    public class ActorProviders
    {
        public delegate IActorRef PrinterActorProvider();
    }
}