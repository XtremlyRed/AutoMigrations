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
            byte[] codeBinary,
            string nameSpace,
            string className
        )
        {
            string core = Encoding.ASCII.GetString(codeBinary);

            Stopwatch stopwatch = Stopwatch.StartNew();

            using MemoryStream stream = RoslynCompile.Compile(core, nameSpace);

            stopwatch.Stop();

            Assembly assembly = Assembly.Load(stream.GetBuffer());

            var typeName = $"{nameSpace}.{className}";

            if (assembly.CreateInstance(typeName) is ModelSnapshot modelSnapshot)
            {
                return modelSnapshot;
            }

            return null;
        }

        public static byte[] CreateSnapshotBuffer(
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

            //model SnapshotNamespace:
            //add a nameplace to the dynamically generated class (must be under the same named control as the current code)
            //modelSnapshotName: Name of the dynamically generated class
            string code = builder
                .Build(context)
                .GetService<IMigrationsCodeGenerator>()!
                .GenerateSnapshot(nameSpace, context.GetType(), className, mode);

            byte[] codeBuffer = Encoding.ASCII.GetBytes(code);

            return codeBuffer;
        }
    }
}
