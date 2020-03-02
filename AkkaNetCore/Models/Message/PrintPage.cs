namespace AkkaNetCore.Models.Message
{
    public class PrintPage : BaseMessage
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
