using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AkkaNetCore.Config;
using AkkaNetCore.Models.Entity;
using AkkaNetCore.Repositories;
using Xunit;
using Xunit.Abstractions;
using Z.EntityFramework.Extensions;

namespace AkkaNetCoreTest.Repositories
{
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
        [InlineData(10000, 100, 10)]
        public void BulkSppedTest(int daatSize,int batchSize, int cutoff)
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
