using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace AutoMigrations.Extensions
{
    internal static class SnapshotExtensions
    {
        static bool compilationAcceleratorCompleted;
        static bool isRunning;

        public static void CompilationAccelerator()
        {
            if (compilationAcceleratorCompleted is true || isRunning is true)
            {
                return;
            }

            isRunning = true;

            ThreadPool.QueueUserWorkItem(
                (o) =>
                {
                    var stream = RoslynCompile.Compile(
                        "using System;namespace AutoMigrations"
                            + $"{Environment.TickCount}"
                            + "{ internal class Accelerator{}}"
                    );

                    stream?.Dispose();

                    compilationAcceleratorCompleted = true;
                }
            );
        }

        public static ModelSnapshot? CreateSnapshot(
            this DbContext _,
            byte[] assemblyBuffer,
            string nameSpace,
            string className
        )
        {
            Assembly assembly = Assembly.Load(assemblyBuffer);

            var typeName = $"{nameSpace}.{className}";

            if (assembly.CreateInstance(typeName) is ModelSnapshot modelSnapshot)
            {
                return modelSnapshot;
            }

            return null;
        }

        public static string CreateSnapshotBuffer(
            this DbContext context,
            string nameSpace,
            string className
        )
        {
#if NET6_0_OR_GREATER

            IModel mode = context.Database.GetService<IDesignTimeModel>().Model;

#elif NETSTANDARD2_0

            IModel mode = context.Model;

#endif
            DesignTimeServicesBuilder builder = new DesignTimeServicesBuilder(
                context.GetType().Assembly!,
                Assembly.GetEntryAssembly()!,
                new OperationReporter(new OperationReportHandler()),
                new string[0]
            );

            string code = builder
                .Build(context)
                .GetService<IMigrationsCodeGenerator>()!
                .GenerateSnapshot(nameSpace, context.GetType(), className, mode);

            return code;
        }
    }
}
