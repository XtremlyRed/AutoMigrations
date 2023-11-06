using AutoMigrations.Extensions;
using AutoMigrations.Modes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMigrations.Context
{
    /// <summary>
    /// context for creating migration records
    /// </summary>
    public class AutoMigrateContext : DbContext
    {
        /// <summary>
        /// get <see cref="DbContextOptions"/>
        /// </summary>
        /// <param name="otherContext"></param>
        /// <returns></returns>
        private static DbContextOptions OptsBuilder(
            DbContext otherContext,
            DbContextOptions? dbContextOptions = null
        )
        {
            var existOpts =
                dbContextOptions ?? otherContext.Database.GetService<IDbContextOptions>();

            var optionsBuilder = new DbContextOptionsBuilder<AutoMigrateContext>();

            DbContextOptions opts = optionsBuilder.Options;

            foreach (var item in existOpts.Extensions)
            {
                opts = opts.WithExtension(item);
            }

            return opts;
        }

        /// <summary>
        /// create from user context  <see cref="DbContextOptions"/>
        /// </summary>
        /// <param name="otherContext"></param>
        public AutoMigrateContext(DbContext otherContext, DbContextOptions? dbContextOptions = null)
            : base(OptsBuilder(otherContext, dbContextOptions)) { }

        /// <summary>
        /// migration records
        /// </summary>
        public virtual DbSet<AutoMigration>? AutoMigrations { get; set; }

        /// <summary>
        /// Obtain previous migration records through a unique migration name
        /// </summary>
        /// <param name="autoMigrateName"></param>
        /// <returns></returns>
        public AutoMigration? GetAutoMigration(string autoMigrateName)
        {
            // Database.EnsureCreated();

            try
            {
                var exist = AutoMigrations!
                    .Where(i => i.Name == autoMigrateName)
                    .OrderByDescending(i => i.MigrationTime)
                    .FirstOrDefault();

                return exist;
            }
            catch (Exception)
            {
                var opts = this.GetDifferences(null);
                this.ExecuteMigrating(opts);
                return null;
            }
        }
    }
}
