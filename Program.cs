using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

public class Program
{
    //static void Main() => BenchmarkRunner.Run<Program>();
    static void Main() => new Program().Benchmark();

    const string BaseDir = @"E:\Users\Svick\git\tunnelvisionlabs-dotnet-threading\Rackspace.Threading";
    static readonly string UserDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    List<string> files = Directory.GetFiles(BaseDir, "*.cs", SearchOption.AllDirectories)
        .ToList();
        
    List<MetadataReference> references = new[] 
    {
        $"{UserDir}/.nuget/packages/system.collections.concurrent/4.3.0/ref/netstandard1.3/System.Collections.Concurrent.dll",
        $"{UserDir}/.nuget/packages/system.collections/4.3.0/ref/netstandard1.3/System.Collections.dll",
        $"{UserDir}/.nuget/packages/system.collections.immutable/1.3.1/lib/netstandard1.0/System.Collections.Immutable.dll",
        $"{UserDir}/.nuget/packages/system.diagnostics.debug/4.3.0/ref/netstandard1.3/System.Diagnostics.Debug.dll",
        $"{UserDir}/.nuget/packages/system.diagnostics.fileversioninfo/4.3.0/ref/netstandard1.3/System.Diagnostics.FileVersionInfo.dll",
        $"{UserDir}/.nuget/packages/system.diagnostics.tools/4.3.0/ref/netstandard1.0/System.Diagnostics.Tools.dll",
        $"{UserDir}/.nuget/packages/system.globalization/4.3.0/ref/netstandard1.3/System.Globalization.dll",
        $"{UserDir}/.nuget/packages/system.io.compression/4.3.0/ref/netstandard1.3/System.IO.Compression.dll",
        $"{UserDir}/.nuget/packages/system.io/4.3.0/ref/netstandard1.3/System.IO.dll",
        $"{UserDir}/.nuget/packages/system.io.filesystem/4.3.0/ref/netstandard1.3/System.IO.FileSystem.dll",
        $"{UserDir}/.nuget/packages/system.io.filesystem.primitives/4.3.0/ref/netstandard1.3/System.IO.FileSystem.Primitives.dll",
        $"{UserDir}/.nuget/packages/system.linq/4.3.0/ref/netstandard1.0/System.Linq.dll",
        $"{UserDir}/.nuget/packages/system.linq.expressions/4.3.0/ref/netstandard1.3/System.Linq.Expressions.dll",
        $"{UserDir}/.nuget/packages/system.reflection/4.3.0/ref/netstandard1.3/System.Reflection.dll",
        $"{UserDir}/.nuget/packages/system.reflection.extensions/4.3.0/ref/netstandard1.0/System.Reflection.Extensions.dll",
        $"{UserDir}/.nuget/packages/system.reflection.metadata/1.4.2/lib/netstandard1.1/System.Reflection.Metadata.dll",
        $"{UserDir}/.nuget/packages/system.reflection.primitives/4.3.0/ref/netstandard1.0/System.Reflection.Primitives.dll",
        $"{UserDir}/.nuget/packages/system.resources.resourcemanager/4.3.0/ref/netstandard1.0/System.Resources.ResourceManager.dll",
        $"{UserDir}/.nuget/packages/system.runtime/4.3.0/ref/netstandard1.3/System.Runtime.dll",
        $"{UserDir}/.nuget/packages/system.runtime.extensions/4.3.0/ref/netstandard1.3/System.Runtime.Extensions.dll",
        $"{UserDir}/.nuget/packages/system.runtime.handles/4.3.0/ref/netstandard1.3/System.Runtime.Handles.dll",
        $"{UserDir}/.nuget/packages/system.runtime.interopservices/4.3.0/ref/netstandard1.3/System.Runtime.InteropServices.dll",
        $"{UserDir}/.nuget/packages/system.runtime.numerics/4.3.0/ref/netstandard1.1/System.Runtime.Numerics.dll",
        $"{UserDir}/.nuget/packages/system.security.cryptography.algorithms/4.3.0/ref/netstandard1.3/System.Security.Cryptography.Algorithms.dll",
        $"{UserDir}/.nuget/packages/system.security.cryptography.encoding/4.3.0/ref/netstandard1.3/System.Security.Cryptography.Encoding.dll",
        $"{UserDir}/.nuget/packages/system.security.cryptography.primitives/4.3.0/ref/netstandard1.3/System.Security.Cryptography.Primitives.dll",
        $"{UserDir}/.nuget/packages/system.security.cryptography.x509certificates/4.3.0/ref/netstandard1.3/System.Security.Cryptography.X509Certificates.dll",
        $"{UserDir}/.nuget/packages/system.text.encoding.codepages/4.3.0/ref/netstandard1.3/System.Text.Encoding.CodePages.dll",
        $"{UserDir}/.nuget/packages/system.text.encoding/4.3.0/ref/netstandard1.3/System.Text.Encoding.dll",
        $"{UserDir}/.nuget/packages/system.text.encoding.extensions/4.3.0/ref/netstandard1.3/System.Text.Encoding.Extensions.dll",
        $"{UserDir}/.nuget/packages/system.threading/4.3.0/ref/netstandard1.3/System.Threading.dll",
        $"{UserDir}/.nuget/packages/system.threading.tasks/4.3.0/ref/netstandard1.3/System.Threading.Tasks.dll",
        $"{UserDir}/.nuget/packages/system.threading.tasks.parallel/4.3.0/ref/netstandard1.1/System.Threading.Tasks.Parallel.dll",
        $"{UserDir}/.nuget/packages/system.threading.timer/4.3.0/ref/netstandard1.2/System.Threading.Timer.dll",
        $"{UserDir}/.nuget/packages/system.valuetuple/4.3.0/lib/netstandard1.0/System.ValueTuple.dll",
        $"{UserDir}/.nuget/packages/system.xml.readerwriter/4.3.0/ref/netstandard1.3/System.Xml.ReaderWriter.dll",
        $"{UserDir}/.nuget/packages/system.xml.xdocument/4.3.0/ref/netstandard1.3/System.Xml.XDocument.dll",
        $"{UserDir}/.nuget/packages/system.xml.xmldocument/4.3.0/ref/netstandard1.3/System.Xml.XmlDocument.dll",
        $"{UserDir}/.nuget/packages/system.xml.xpath/4.3.0/ref/netstandard1.3/System.Xml.XPath.dll",
        $"{UserDir}/.nuget/packages/system.xml.xpath.xdocument/4.3.0/ref/netstandard1.3/System.Xml.XPath.XDocument.dll"
    }.Select(f => MetadataReference.CreateFromFile(f)).ToList<MetadataReference>();

    CSharpCompilation CreateCompilation(DocumentationMode documentationMode)
    {
        var trees = files.Select(f =>
        {
            using (var stream = File.OpenRead(f))
            {
                return SyntaxFactory.ParseSyntaxTree(
                    SourceText.From(stream), new CSharpParseOptions(LanguageVersion.Latest, documentationMode,
                    preprocessorSymbols: new[] { "NETSTANDARD1_3" }), path: f.Substring(BaseDir.Length + 1));
            }
        }).ToList();

        return CSharpCompilation.Create(
            null, trees, references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [Params(EmitMode.None, EmitMode.Null, EmitMode.NullStream)]
    public EmitMode EmitMode { get; set; } = EmitMode.NullStream;

    [Params(DocumentationMode.None, DocumentationMode.Parse, DocumentationMode.Diagnose)]
    public DocumentationMode DocumentationMode { get; set; } = DocumentationMode.Parse;

    [Benchmark]
    public void Benchmark()
    {
        var compilation = CreateCompilation(DocumentationMode);

        foreach (var diag in compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Error).Take(10))
            Console.WriteLine(diag);

        Stream xmlStream;

        switch (EmitMode)
        {
            case EmitMode.None:
                return;
            case EmitMode.Null:
                xmlStream = null;
                break;
            case EmitMode.NullStream:
                xmlStream = Stream.Null;
                break;
            default:
                throw new Exception();
        }

        var result = compilation.Emit(Stream.Null, xmlDocumentationStream: xmlStream);

        if (!result.Success)
            throw new Exception();
    }
}

public enum EmitMode
{
    None,
    Null,
    NullStream
}