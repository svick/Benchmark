using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class Program
{
    static void Main() => BenchmarkRunner.Run<Bench>();
}

//[MemoryDiagnoser]
//[DisassemblyDiagnoser(printAsm: true, printSource: true)]
public class Bench
{
    static readonly ThreadLocal<ImmutableArray<long>.Builder> _cachedBuilder = new ThreadLocal<ImmutableArray<long>.Builder>(ImmutableArray.CreateBuilder<long>);
    static readonly Func<long[], ImmutableArray<long>> _unsafeFreeze = GetUnsafeFreeze<long>();

    //[Params(100, 1000, 10000)]
    public const int N = 1000;

    [Benchmark(Baseline = true)]
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
    public void UncachedBuilder()
    {
        var builder = ImmutableArray.CreateBuilder<long>();
        builder.Capacity = N;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void MyUncachedBuilder()
    {
        var builder = MyImmutable.ImmutableArray.CreateBuilder<long>();
        builder.Capacity = N;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
    public void MyInlinedUncachedBuilder()
    {
        var builder = MyImmutableInlined.ImmutableArray.CreateBuilder<long>();
        builder.Capacity = N;
        for (long i = 0; i < builder.Capacity; i++)
        {
            builder.Add(i);
        }
        builder.MoveToImmutable();
    }
    [Benchmark]
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
