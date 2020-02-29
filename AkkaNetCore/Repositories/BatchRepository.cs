using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using AkkaNetCore.Config;
using AkkaNetCore.Models.Entity;

namespace AkkaNetCore.Repositories
{
    public class BatchRepository : DbContext
    {
        private string database = "akkadb";

        private readonly AppSettings appSettings;

        public string DebugConString { get; set; }

        public DbSet<MessageCompleted> MessageCompleted { get; set; }

        public DbSet<MessageReseved> MessageReseved { get; set; }

        public BatchRepository(AppSettings _appSettings)
        {
            appSettings = _appSettings;            
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbOption = "";
                string dbConnectionString = appSettings.DBConnection + $"database={database};" + dbOption;
                optionsBuilder.UseMySql(dbConnectionString);
                base.OnConfiguring(optionsBuilder);
            }
        }
    }
}
