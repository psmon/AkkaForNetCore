using Akka.Actor;
using Akka.Event;

namespace AkkaNetCore.Actors.Study
{
    public class BasicActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        public BasicActor()
        {
            ReceiveAsync<string>(async msg =>
            {
                logger.Info($"{msg} 를 전송받았습니다.");
                if (Sender != null)
                {
                    Sender.Tell("ok");
                }
            });
        }
    }
}
