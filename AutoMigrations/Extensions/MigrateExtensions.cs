using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace AutoMigrations.Extensions
{
    internal static class MigrateExtensions
    {
        public static IReadOnlyList<MigrationOperation> GetDifferences(
            this DbContext context,
            ModelSnapshot? modelSnapshot
        )
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

            IReadOnlyList<MigrationOperation> opts = null!;

            // has changed
            if (modelDiffer.HasDifferences(codeModel, finaMode) == false)
            {
                return opts;
            }

            //get migration operation
            opts = modelDiffer.GetDifferences(codeModel, finaMode);

            return opts;
        }

        /// <summary>
        /// migrate database table structure
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="context"></param>
        public static int ExecuteMigrating(
            this DbContext context,
            IReadOnlyList<MigrationOperation> operations,
            bool removeColumnWhenDrop = false
        )
        {
            if (operations is null || operations.Count == 0)
            {
                return -1;
            }

            //migrate column name
            List<string> allCommandTexts = new();

            //other migrate
            List<MigrationOperation> migrations = new();

            Type[] types = operations.Select(i => i.GetType()).ToArray();

            foreach (MigrationOperation operation in operations)
            {
                if (operation is RenameColumnOperation renameColumn)
                {
                    allCommandTexts.Add(context.GetRenameSQL(renameColumn));
                    continue;
                }

                if (operation is DropColumnOperation dropColumn && removeColumnWhenDrop == false)
                {
                    continue;
                }

                migrations.Add(operation);
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
                string[] commandTexts = context.Database
                    .GetService<IMigrationsSqlGenerator>()
                    .Generate(migrations, mode)
                    .Select(p => p.CommandText)
                    .ToArray();

                allCommandTexts.AddRange(commandTexts);
            }
            int changeCount = 0;
            if (allCommandTexts.Count > 0)
            {
                //execute sql scripts
                changeCount = context.ExecuteSQL(allCommandTexts);
            }

            return changeCount;
        }
    }
}
