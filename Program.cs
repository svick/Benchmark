using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;

public class Program
{
    static void Main() => BenchmarkRunner.Run<Bench>();
    //static void Main() => new Bench().Indexer();
}

[InProcess]
public class Bench
{
    public const int N = 1000;

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Add()
    {
        var builder = ImmutableArray.CreateBuilder<long>();
        builder.Capacity = 1000;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Indexer()
    {
        var builder = ImmutableArray.CreateBuilder<long>();
        builder.Count = 1000;
        for (int i = 0; i < builder.Count; i++)
        {
            builder[i] = i;
        }
        builder.MoveToImmutable();
    }
}
