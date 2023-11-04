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
        /// <param name="context">the current dbcontext  for migration</param>
        /// <param name="migrationsAssembly">dbcontext assembly</param>
        /// <param name="autoMigrateName">A unique name that corresponds one-to-one to dbcontext</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static void AutoMigrate(
            this DbContext context,
            Assembly migrationsAssembly,
            string autoMigrateName
        )
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (migrationsAssembly is null)
            {
                throw new ArgumentNullException(nameof(migrationsAssembly));
            }
            if (string.IsNullOrWhiteSpace(autoMigrateName))
            {
                throw new ArgumentException(nameof(autoMigrateName));
            }

            SnapshotExtensions.CompilationAccelerator();

            context.BindVerify(autoMigrateName);

            using AutoMigrateContext autoMigrateContext = new AutoMigrateContext(context);

            Modes.AutoMigration? migration = autoMigrateContext.GetAutoMigration(autoMigrateName);

            string @namespace = $"{migrationsAssembly.GetName().Name}.Migrations";
            string className = "AutoMigrate_Snapshot";
            bool hasChanged = false;
            ModelSnapshot? modelSnapshot = null;

            if (migration != null)
            {
                ModelSnapshot? sbapshot = context.CreateSnapshot(
                    migration.Migrations!,
                    @namespace,
                    className
                );

                if (sbapshot != null && sbapshot.Model != null)
                {
                    modelSnapshot = sbapshot;
                }
            }

            context.Database.EnsureCreated();
            hasChanged = context.AutoMigrationing(modelSnapshot);

            if (hasChanged == false)
            {
                return;
            }

            byte[] codeBuffer = context.CreateSnapshotBuffer(@namespace, className);

            autoMigrateContext.AutoMigrations!.Add(new AutoMigration(codeBuffer, autoMigrateName));

            autoMigrateContext.SaveChanges();
        }

        /// <summary>
        /// auto migrate <see cref="DbContext"/>
        /// </summary>
        /// <param name="context">the current dbcontext  for migration</param>
        /// <param name="migrationsAssembly">dbcontext assembly</param>
        /// <param name="autoMigrateName">A unique name that corresponds one-to-one to dbcontext</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static async Task AutoMigrateAsync(
            this DbContext context,
            Assembly migrationsAssembly,
            string autoMigrateName
        )
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (migrationsAssembly is null)
            {
                throw new ArgumentNullException(nameof(migrationsAssembly));
            }
            if (string.IsNullOrWhiteSpace(autoMigrateName))
            {
                throw new ArgumentException(nameof(autoMigrateName));
            }

            await Task.Run(() => AutoMigrate(context, migrationsAssembly, autoMigrateName));
        }
    }
}
