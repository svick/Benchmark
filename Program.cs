using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class Program
{
    static void Main() => BenchmarkRunner.Run<Benchmark>();
    //static async Task Main() { await new Benchmark{N = 2}.DelayTaskSource(); }
}

public class MainConfig : ManualConfig
{
    public MainConfig()
    {
        //Add(Job.ShortRun.With(Platform.X64).With(CsProjClassicNetToolchain.Net471));
        //Add(Job.Default.With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp11));
        //Add(Job.Default.With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp20));
        //Add(Job.ShortRun.With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp21));
        //Add(MemoryDiagnoser.Default);
    }
}

//[Config(typeof(MainConfig))]
//[DisassemblyDiagnoser(printAsm: true, printSource: true)]
//[ShortRunJob]
//[MemoryDiagnoser]
public class Benchmark
{
    [Params(1, 2, 3, 4)]
    public int Stage;

    CSharpCompilation baseCompilation = CSharpCompilation.Create(null)
        .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

    [Benchmark]
    public void GetAttributes()
    {
        string code = @"
using System;

[A]
class Foo {}

[A]
class Bar {}

class A : Attribute {}
";

        var tree = SyntaxFactory.ParseSyntaxTree(code);

        var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();

        foreach (var c in classes)
        {
            foreach (var a in c.AttributeLists.Select(al => al.Attributes))
            { }
        }

        if (Stage == 0) return;

        var compilation = baseCompilation.AddSyntaxTrees(tree);

        if (Stage == 1) return;

        var model = compilation.GetSemanticModel(tree);

        if (Stage == 2) return;

        var fooClass = classes[0];

        var fooAttribute = fooClass.AttributeLists.Single().Attributes.Single();

        var fooSymbol = model.GetSymbolInfo(fooAttribute).Symbol;

        if (fooSymbol == null) throw new Exception();

        if (Stage == 3) return;

        var barClass = classes[1];

        var barAttribute = barClass.AttributeLists.Single().Attributes.Single();

        var barSymbol = model.GetSymbolInfo(barAttribute).Symbol;

        if (barSymbol == null) throw new Exception();
    }
}