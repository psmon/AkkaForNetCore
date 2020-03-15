using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using AkkaNetCore.Models.Message;

namespace AkkaNetCore.Actors.Study
{
    public class CustomActor : ReceiveActor
    {
        private readonly ILoggingAdapter logger = Context.GetLogger();

        protected int messageCnt = 0;

        public CustomActor()
        {
            Receive<Msg>(msg =>
            {
                messageCnt++;
                if (!msg.Message.Contains("고급"))
                {
                    //일반택배는 자주 분실된다.
                    if (messageCnt % 2 == 0)
                    {
                        logger.Warning("택배를 찾을수 없습니다.");
                        return;
                    }
                }

                //택배를 받았기때문에 완료처리되었다.
                logger.Debug("네, 받았습니다.");
                Sender.Tell(new Confirm(msg.DeliveryId), Self);
            });
        }
    }

    public class DeliveryManActor : AtLeastOnceDeliveryReceiveActor
    {
        private readonly IActorRef _probe;

        private readonly IActorRef _destionationActor;

        private readonly ILoggingAdapter logger = Context.GetLogger();

        public override string PersistenceId { get; } = "persistence-id";

        public DeliveryManActor(IActorRef probe)
        {
            _probe = probe;

            _destionationActor = Context.ActorOf(Props.Create(() => new CustomActor()),"customer");

            Recover<MsgSent>(msgSent => Handler(msgSent));

            Recover<MsgConfirmed>(msgConfirmed => Handler(msgConfirmed));

            Command<string>(str =>
            {
                logger.Debug("received:" + str);
                Persist(new MsgSent(str), Handler);
            });

            Command<Confirm>(confirm =>
            {
                //메시지 받음을 확인하고, 해당 메시지를 더이상 안보낸다.
                logger.Debug("received confirm:" + confirm.DeliveryId);
                _probe.Tell("OK");
                Persist(new MsgConfirmed(confirm.DeliveryId), Handler);
            });

        }

        // 목적지에 메시지보냄 - 재전송포함
        private void Handler(MsgSent msgSent)
        {
            logger.Debug("택배발송되었습니다.:" + msgSent.Message);
            Deliver(_destionationActor.Path, l => new Msg(l, msgSent.Message));
        }

        // 메시지가 확인됨
        private void Handler(MsgConfirmed msgConfirmed)
        {
            ConfirmDelivery(msgConfirmed.DeliveryId);
        }
    }
}
