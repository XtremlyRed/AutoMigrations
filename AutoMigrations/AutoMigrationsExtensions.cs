using AutoMigrations.Context;
using AutoMigrations.Extensions;
using AutoMigrations.Modes;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

using System.Reflection;

namespace AutoMigrations
{
    /// <summary>
    /// <see cref="DbContext"/> auto migrate
    /// </summary>
    public static class AutoMigrationsExtensions
    {
        /// <summary>
        /// auto migrate <see cref="DbContext"/>
        /// </summary>
        /// <param name="dbContext">the current dbcontext  for migration</param>
        /// <param name="migrationsAssembly">dbcontext assembly</param>
        /// <param name="dbContextOptions"><paramref name="dbContext"/> DbContextOptions </param>
        /// <param name="removeColumnWhenDrop">Remove database columns when deleting entity attributes,true : remove, otherwise not</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static void AutoMigrate(
            this DbContext dbContext,
            Assembly migrationsAssembly,
            DbContextOptions? dbContextOptions = null,
            bool removeColumnWhenDrop = false
        )
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }
            if (migrationsAssembly is null)
            {
                throw new ArgumentNullException(nameof(migrationsAssembly));
            }

            var contextType = dbContext.GetType();

            var autoMigrateName = $"{contextType.Namespace}.{contextType.Name} - Migrations";

            using AutoMigrateContext autoMigrateContext = new AutoMigrateContext(
                dbContext,
                dbContextOptions
            );

            Modes.AutoMigration? migration = autoMigrateContext.GetAutoMigration(autoMigrateName);

            string @namespace = $"{migrationsAssembly.GetName().Name}.Migrations";

            string className = $"{dbContext.GetType().Name}_AutoMigrate_Snapshot";

            ModelSnapshot? modelSnapshot = null;

            if (migration != null)
            {
                if (migration.Migrations!.Verify(migration.Key!) == false)
                {
                    throw new InvalidOperationException(
                        "snapshot has been abnormally modified and cannot be migrated"
                    );
                }

                ModelSnapshot? sbapshot = null;

                sbapshot = dbContext.CreateSnapshot(migration.Migrations!, @namespace, className);

                if (sbapshot != null && sbapshot.Model != null)
                {
                    modelSnapshot = sbapshot;
                }
            }

            var opts = dbContext.GetDifferences(modelSnapshot);

            if (opts is null || opts.Count == 0)
            {
                return;
            }

            dbContext.ExecuteMigrating(opts, removeColumnWhenDrop);

            string code = dbContext.CreateSnapshotBuffer(@namespace, className);

            if (code is null)
            {
                return;
            }

            using MemoryStream stream = RoslynCompile.Compile(code, @namespace);

            byte[] buffer = stream.ToArray();

            autoMigrateContext.AutoMigrations!.Add(
                new AutoMigration(buffer, autoMigrateName, buffer.Encrypt())
            );

            autoMigrateContext.SaveChanges();
        }

        /// <summary>
        /// auto migrate <see cref="DbContext"/>
        /// </summary>
        /// <param name="dbContext">the current dbcontext  for migration</param>
        /// <param name="migrationsAssembly">dbcontext assembly</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static async Task AutoMigrateAsync(
            this DbContext dbContext,
            Assembly migrationsAssembly,
            DbContextOptions? dbContextOptions = null
        )
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }
            if (migrationsAssembly is null)
            {
                throw new ArgumentNullException(nameof(migrationsAssembly));
            }

            await Task.Run(() => AutoMigrate(dbContext, migrationsAssembly, dbContextOptions));
        }
    }
}
