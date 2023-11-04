﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

using System.IO;

namespace AutoMigrations.Extensions
{
    internal static class MigrateExtensions
    {
        public static bool AutoMigrationing(this DbContext context, ModelSnapshot? modelSnapshot)
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
            Migrationing(context, opts);

            return true;
        }

        /// <summary>
        /// migrate database table structure
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="context"></param>
        private static void Migrationing(
            DbContext context,
            IReadOnlyList<MigrationOperation> operations
        )
        {
            //migrate column name
            List<string> columnNames = new();

            //other migrate
            List<MigrationOperation> migrations = new();

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
                //generate sql scripts
                var commandTexts = context.Database
                    .GetService<IMigrationsSqlGenerator>()
                    .Generate(migrations, context.Model)
                    .Select(p => p.CommandText)
                    .ToArray();

                //execute sql scripts
                int changeCount = context.ExecuteSQL(commandTexts);
            }
        }
    }
}