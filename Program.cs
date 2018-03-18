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

[MemoryDiagnoser]
[DisassemblyDiagnoser(printAsm: true, printSource: true)]
public class Bench
{
    static readonly ThreadLocal<ImmutableArray<long>.Builder> _cachedBuilder = new ThreadLocal<ImmutableArray<long>.Builder>(ImmutableArray.CreateBuilder<long>);
    static readonly ThreadLocal<MyImmutable.ImmutableArray<long>.Builder> _myCachedBuilder = new ThreadLocal<MyImmutable.ImmutableArray<long>.Builder>(MyImmutable.ImmutableArray.CreateBuilder<long>);
    static readonly ThreadLocal<MyImmutableInlined.ImmutableArray<long>.Builder> _myInlinedCachedBuilder = new ThreadLocal<MyImmutableInlined.ImmutableArray<long>.Builder>(MyImmutableInlined.ImmutableArray.CreateBuilder<long>);
    static readonly Func<long[], ImmutableArray<long>> _unsafeFreeze = GetUnsafeFreeze<long>();

    public const int N = 1000;

    [Benchmark]
    public void CachedBuilder()
    {
        var builder = _cachedBuilder.Value;
        builder.Capacity = 1000;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void CachedBuilderIndexer()
    {
        var builder = _cachedBuilder.Value;
        builder.Count = 1000;
        for (int i = 0; i < builder.Count; i++)
        {
            builder[i] = i;
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void MyCachedBuilder()
    {
        var builder = _myCachedBuilder.Value;
        builder.Capacity = N;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void MyInlinedCachedBuilder()
    {
        var builder = _myInlinedCachedBuilder.Value;
        builder.Capacity = N;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void MyCachedBuilderIndexer()
    {
        var builder = _myCachedBuilder.Value;
        builder.Count = 1000;
        for (int i = 0; i < builder.Count; i++)
        {
            builder[i] = i;
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void MyInlinedCachedBuilderIndexer()
    {
        var builder = _myInlinedCachedBuilder.Value;
        builder.Count = 1000;
        for (int i = 0; i < builder.Count; i++)
        {
            builder[i] = i;
        }
        builder.MoveToImmutable();
    }

    [Benchmark(Baseline = true)]
    public void UnsafeFreeze()
    {
        var array = new long[N];
        for (long i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }
        _unsafeFreeze(array);
    }

    static Func<T[], ImmutableArray<T>> GetUnsafeFreeze<T>()
    {
        var ctor = typeof(ImmutableArray<T>)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(c => c.GetParameters().Count() == 1 && c.GetParameters().Single().ParameterType.Equals(typeof(T[])));
        var param = Expression.Parameter(typeof(T[]));
        var body = Expression.New(ctor, param);
        var func = Expression.Lambda<Func<T[], ImmutableArray<T>>>(body, param);
        return func.Compile();
    }
}
