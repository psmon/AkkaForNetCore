using System;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.NUnit3;
using AkkaNetCore.Actors.Utils;
using AkkaNetCore.Models.Actor;
using NUnit.Framework;

namespace AkkaNetCoreTest.Actors
{
    public class TestBatchWriterActor : ReceiveActor
    {
        IActorRef probe;
        public TestBatchWriterActor(IActorRef _probe)
        {
            probe = _probe;
            ReceiveAsync<object>(async message =>
            {
                if (message is Batch batchMessage)
                {
                    //TODO : 배치처리를 수행..
                    probe.Tell(batchMessage);
                }
            });
        }
    }

    class BatchActorTest : TestKit
    {
        TestProbe BatchWriterProbe;

        [SetUp]
        public void Setup()
        {
            //배치가 컬렉션단위로 잘 수행하는지 관찰자 셋팅
            BatchWriterProbe = this.CreateTestProbe("iamprobe");
        }

        // 테스트목적 : 이벤트가 발생할때마다 DB저장이 아닌, 특정시간 수집된 구간의 데이터 벌크인서트처리목적(벌크인서트는 건바이건보다 빠르다)
        // 3초(collectSec) 이내에는 배치처리를 위한 컬렉션이 생성되면 안됨..        
        [TestCase(1)]
        public void LazyBatchAreOK(int collectSec)
        {
            var batchActor = Sys.ActorOf(Props.Create(() => new BatchActor(collectSec)));
            IActorRef batchWriterActor = Sys.ActorOf(Props.Create(() => new TestBatchWriterActor(BatchWriterProbe)));
            batchActor.Tell(new SetTarget(batchWriterActor));   //배치저장담당자 지정 : 여기서는 관찰자만 등록,실제 DB저장없음

            //이벤트는 실시간적으로 발생한다.
            batchActor.Tell(new Queue("오브젝트1"));
            batchActor.Tell(new Queue("오브젝트2"));
            batchActor.Tell(new Queue("오브젝트3"));

            ExpectNoMsg();  //실시간에 대응하여, 배치 컬렉션에 쌓이지 않는다.

            var batchList2 =  ExpectMsg<Batch>();

            //배치 항목을 검사
            var batchList = ExpectMsg<Batch>(TimeSpan.FromSeconds(collectSec+2)).Obj;
            
            var firstItem = batchList[0] as string;
            Assert.AreEqual("오브젝트1", firstItem);

            //이벤트는 실시간적으로 발생한다.
            batchActor.Tell(new Queue("오브젝트4"));
            batchActor.Tell(new Queue("오브젝트5"));
            batchActor.Tell(new Queue("오브젝트6"));
            batchActor.Tell(new Flush()); //강제 벌크요청

            //배치 항목을 검사
            batchList = ExpectMsg<Batch>().Obj;
            firstItem = batchList[0] as string;
            Assert.AreEqual("오브젝트4", firstItem);
        }

    }
}
