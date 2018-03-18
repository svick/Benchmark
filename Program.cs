using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class Program
{
    static void Main() => BenchmarkRunner.Run<Bench>();
    //static void Main() => new Bench().MyInlinedCachedBuilderIndexer();
}

public class Bench
{
    [Benchmark]
    public void SimpleAdd()
    {
        var builder = MyImmutable.ImmutableArray.CreateBuilder<long>();
        builder.Capacity = 1000;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.SimpleAdd(i);
        }
        builder.MoveToImmutable();
    }

    [Benchmark]
    public void TweakedAdd()
    {
        var builder = MyImmutable.ImmutableArray.CreateBuilder<long>();
        builder.Capacity = 1000;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.TweakedAdd(i);
        }
        builder.MoveToImmutable();
    }

    [Benchmark]
    public void SplitAdd()
    {
        var builder = MyImmutable.ImmutableArray.CreateBuilder<long>();
        builder.Capacity = 1000;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.SplitAdd(i);
        }
        builder.MoveToImmutable();
    }
}
