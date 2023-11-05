using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using System.IO;

namespace AutoMigrations.Extensions
{
    internal static class MigrateExtensions
    {
        public static bool ExecuteMigrate(this DbContext context, ModelSnapshot? modelSnapshot)
        {
            IMigrationsModelDiffer modelDiffer =
                context.Database.GetService<IMigrationsModelDiffer>();

#if NET6_0_OR_GREATER

            IRelationalModel codeModel = context.InitializeMode(modelSnapshot?.Model);
            IRelationalModel finaMode = context.GetRelationalModel();

#elif NETSTANDARD2_0

            IModel codeModel = modelSnapshot?.Model;
            IModel finaMode = context.Model;

#endif

            // has changed
            if (modelDiffer.HasDifferences(codeModel, finaMode) == false)
            {
                return false;
            }

            IReadOnlyList<MigrationOperation> opts = null!;

            //get migration operation
            opts = modelDiffer.GetDifferences(codeModel, finaMode);

            //migrate
            Migrating(context, opts);

            return true;
        }

        /// <summary>
        /// migrate database table structure
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="context"></param>
        private static void Migrating(
            DbContext context,
            IReadOnlyList<MigrationOperation> operations
        )
        {
            //migrate column name
            List<string> columnNames = new();

            //other migrate
            List<MigrationOperation> migrations = new();

            var types = operations.Select(i => i.GetType()).ToArray();

            foreach (MigrationOperation operation in operations)
            {
                if (operation is RenameColumnOperation renameColumn)
                {
                    columnNames.Add(context.GetRenameSQL(renameColumn));
                    continue;
                }

                migrations.Add(operation);
            }

            if (columnNames.Count > 0)
            {
                context.ExecuteSQL(columnNames);
            }

            //other migrate
            if (migrations.Count > 0)
            {
#if NET6_0_OR_GREATER

                IModel mode = context.Database.GetService<IDesignTimeModel>().Model;

#elif NETSTANDARD2_0

                IModel mode = context.Model;

#endif

                //generate sql scripts
                var commandTexts = context.Database
                    .GetService<IMigrationsSqlGenerator>()
                    .Generate(migrations, mode)
                    .Select(p => p.CommandText)
                    .ToArray();

                //execute sql scripts
                int changeCount = context.ExecuteSQL(commandTexts);
            }
        }
    }
}
