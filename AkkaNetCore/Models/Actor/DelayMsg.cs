﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaNetCore.Models.Actor
{
    public class DelayMsg
    {
        public string Message { get; set; }

        public int Delay { get; set; }
    }
}
