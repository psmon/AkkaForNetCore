using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaNetCore.Models.Actor
{
    public class DelayMsg
    {
        public string message { get; set; }

        public int delay { get; set; }
    }
}
