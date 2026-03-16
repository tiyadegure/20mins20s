using ProjectEye.Core.Models.Statistic;
using System;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;

namespace ProjectEye.Database
{
    public class StatisticContext : DbContext
    {
        /// <summary>
        /// 统计数据
        /// </summary>
        public DbSet<StatisticModel> Statistics { get; set; }

        /// <summary>
        /// 番茄数据
        /// </summary>
        public DbSet<Core.Models.Statistic.TomatoModel> Tomatos { get; set; }

        public StatisticContext()
            : base(new SQLiteConnection
            {
                ConnectionString = $"Data Source={GetDatabasePath()}"
            }, true)
        {
            DbConfiguration.SetConfiguration(new SQLiteConfiguration());
        }

        private static string GetDatabasePath()
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir);
            return Path.Combine(dataDir, "data.db");
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var model = modelBuilder.Build(Database.Connection);
            new SQLiteBuilder(model).Handle();
        }
    }
}
