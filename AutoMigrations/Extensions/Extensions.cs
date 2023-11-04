using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using System.Collections.Concurrent;

namespace AutoMigrations.Extensions
{
    internal static class Extensions
    {
        static ConcurrentDictionary<string, object> autoMigrateStorages = new();

        public static void BindVerify(this DbContext context, string autoMigrateName)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrWhiteSpace(autoMigrateName))
            {
                throw new ArgumentException(nameof(autoMigrateName));
            }

            Type contextType = context.GetType();

            if (
                autoMigrateStorages.TryGetValue(autoMigrateName, out object? exist)
                && exist is Type existType
            )
            {
                if (contextType != existType)
                {
                    throw new InvalidOperationException(
                        $"name:{autoMigrateName} has been bound by type:{existType}"
                    );
                }
            }

            autoMigrateStorages[autoMigrateName] = contextType;
        }

#if NET6_0_OR_GREATER

        public static IRelationalModel InitializeMode(this DbContext context, IModel? model)
        {
            if (model is null)
            {
                return null!;
            }

            IModelRuntimeInitializer init = context.Database.GetService<IModelRuntimeInitializer>();

            IModel codeModel = init.Initialize(model, true);

            IRelationalModel finaMode = codeModel.GetRelationalModel();

            return finaMode;
        }

        public static IRelationalModel GetRelationalModel(this DbContext context)
        {
            var designTimeMode = context.GetService<IDesignTimeModel>();

            var model = designTimeMode.Model;

            IRelationalModel finaMode = model.GetRelationalModel();

            return finaMode;
        }

#endif
    }
}
