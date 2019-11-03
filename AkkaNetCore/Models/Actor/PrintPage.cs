using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaNetCore.Models.Actor
{
    public class PrintPage
    {
        public int SeqNo { get; set; }
        
        public int DelayForPrint { get; set; }  //프리팅에 걸리는 시간 조작

        public string Content { get; set; }

        public override string ToString()
        {
            return $"SeqNo:{SeqNo} Content:{Content}";
        }
    }
}
