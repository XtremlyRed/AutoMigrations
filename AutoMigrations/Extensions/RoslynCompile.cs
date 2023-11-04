using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMigrations.Extensions
{
    public static class RoslynCompile
    {
        public static MemoryStream Compile(string scriptCode, string? assemblyName = null)
        {
            if (string.IsNullOrWhiteSpace(scriptCode))
            {
                throw new InvalidOperationException("scripts is null or empty");
            }

            CSharpCompilationOptions options = new(OutputKind.DynamicallyLinkedLibrary);

            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(i => i.IsDynamic == false)
                .Where(i => string.IsNullOrEmpty(i.Location) == false)
                .ToArray();

            PortableExecutableReference[] metadataReferences = assemblies
                .Select(i => MetadataReference.CreateFromFile(i.Location))
                .ToArray();

            CSharpCompilation compilation = CSharpCompilation
                .Create(assemblyName ?? $"script.{Environment.TickCount}")
                .WithOptions(options)
                .AddReferences(metadataReferences)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText(scriptCode));

            MemoryStream stream = new();

            Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
            if (result.Success)
            {
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }

            string message = string.Join(
                Environment.NewLine,
                result.Diagnostics.Select(i => i.ToString())
            );

            throw new CompileException(result.Diagnostics, message);
        }
    }

    public class CompileException : Exception
    {
        public CompileException(IReadOnlyList<object> diagnostics, string message)
            : base(message)
        {
            Diagnostics = diagnostics;
        }

        public IReadOnlyList<object> Diagnostics { get; private set; }
    }
}
