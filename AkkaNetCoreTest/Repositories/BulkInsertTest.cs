using System;
using System.Collections.Generic;
using AkkaNetCore.Config;
using AkkaNetCore.Models.Entity;
using AkkaNetCore.Repositories;
using Xunit;
using Xunit.Abstractions;
using Z.EntityFramework.Extensions;

namespace AkkaNetCoreTest.Repositories
{
    // 테스트 목적 : 대용량 이벤트처리를 저장하기위해서 벌크인서트가 사용됩니다.
    public class BulkInsertTest : TestKitXunit
    {
        private AppSettings appSettings;

        public BulkInsertTest(ITestOutputHelper output) : base(output)
        {
            Setup();
        }

        public void Setup()
        {
            appSettings = new AppSettings()
            {
                DBConnection = "server=localhost;port=33061;database=showa_search;user=root;password=root;"
            };
        }

        [Theory]
        [InlineData(50000, 100, 10)]    //5만개의 데이터 인입에 소요시간은 10초이내여야한다.
        public void 오만개의데이터는_10초이내에_DB에_인입되어야한다(int daatSize,int batchSize, int cutoff)
        {
            var bulkItems_reseverd = new List<MessageReseved>();
            for(int i = 0; i < daatSize; i++)
            {
                var addData = new MessageReseved()
                {
                    Seq = i.ToString(),
                    no = i,
                    Message = "TestMessage" + i,
                    updateTime = DateTime.Now
                };
                bulkItems_reseverd.Add(addData);
            }

            Within(TimeSpan.FromSeconds(cutoff), () => {
                EntityFrameworkManager.ContextFactory = context => new BatchRepository(appSettings);
                using (var context = new BatchRepository(appSettings))
                {
                    context.BulkInsertAsync(bulkItems_reseverd, options => {
                        options.BatchSize = batchSize;
                    }).Wait();
                }
            });
        }
    }
}
