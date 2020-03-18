using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaNetCore.Models.LoadTest
{
    public class ApiArguMent
    {        
        public int SomeArg1 { get; set; }

        public int SomeArg2 { get; set; }

        public string SomeArg3 { get; set; }
    }

    //API 호출에 필요한 스펙을 정의합니다.( 이 메시지가, 분산전달됩니다.)
    public class ApiCallSpec
    {
        public int SeqNo {get;set;}             //자동증가 됩니다. 값 지정할필요없음

        public ApiArguMent Arg { get; set; }    //API호출을 위한 옵션을 지정합니다.   

        public override string ToString()
        {
            return $"ApiCallSpec:{SeqNo} {Arg.SomeArg3}";
        }
    }
}
