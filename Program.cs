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

public class Program
{
    static void Main() => BenchmarkRunner.Run<Benchmark>();
    //static async Task Main() { await new Benchmark{N = 2}.DelayTaskSource(); }
}

public class MainConfig : ManualConfig
{
    public MainConfig()
    {
        Add(Job.ShortRun.With(Platform.X64).With(CsProjClassicNetToolchain.Net471));
        //Add(Job.Default.With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp11));
        //Add(Job.Default.With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp20));
        Add(Job.ShortRun.With(Platform.X64).With(CsProjCoreToolchain.NetCoreApp21));
        //Add(MemoryDiagnoser.Default);
    }
}

//[Config(typeof(MainConfig))]
//[DisassemblyDiagnoser(printAsm: true, printSource: true)]
//[ShortRunJob]
[MemoryDiagnoser]
public class Benchmark
{
    [Params(1, 10/*, 100*/)]
    public int N;


    [Benchmark]
    public async Task TaskDelay()
    {
        for (int i = 0; i < N; i++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }
    }

    /*[Benchmark]
    public async Task DelayTaskSource()
    {
        var delayTaskSource = new DelayTaskSource();

        for (int i = 0; i < N; i++)
        {
            await delayTaskSource.Delay(TimeSpan.FromMilliseconds(1));
        }
    }*/
}

class DelayTaskSource
{
    readonly ValueTaskSource valueTaskSource;
    readonly object l = new object();
    readonly List<(Action<object>, object, short)> continuations = new List<(Action<object>, object, short)>();
    readonly Dictionary<short, long> timestamps = new Dictionary<short, long>();
    readonly List<short> tokenCache = new List<short>();
    readonly Timer timer;
    long? timerTimestamp;
    short nextToken;

    public DelayTaskSource()
    {
        valueTaskSource = new ValueTaskSource(this);
        timer = new Timer(TimerCallback);
    }

    private static long ToTicks(TimeSpan timeSpan) => (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);
    private static TimeSpan ToTimeSpan(long ticks) => TimeSpan.FromSeconds((double)ticks / Stopwatch.Frequency);

    public ValueTask Delay(TimeSpan delay)
    {
        long nowTimestamp = Stopwatch.GetTimestamp();
        long delayTimestamp = nowTimestamp + ToTicks(delay);

        lock (l)
        {
            if (nextToken < 0)
                throw new Exception("token overflow");

            short token = nextToken++;

            timestamps.Add(token, delayTimestamp);

            if (timerTimestamp == null || timerTimestamp > delayTimestamp)
            {
                timer.Change(delay, Timeout.InfiniteTimeSpan);
                timerTimestamp = delayTimestamp;
            }

            return new ValueTask(valueTaskSource, token);
        }
    }

    private void TimerCallback(object _)
    {
        long nowTimestamp = Stopwatch.GetTimestamp();
        Action<object> continuation = null;
        object state = null;

        long? nextTimestamp = null;

        lock (l)
        {
            tokenCache.Clear();

            foreach (var (token, timestamp) in timestamps)
            {
                if (timestamp <= nowTimestamp) 
                {
                    tokenCache.Add(token);
                }
                else if (nextTimestamp == null || timestamp < nextTimestamp)
                {
                    nextTimestamp = timestamp;
                }
            }

            if (nextTimestamp != null)
                timer.Change(ToTimeSpan(nextTimestamp.Value - nowTimestamp), Timeout.InfiniteTimeSpan);
            timerTimestamp = nextTimestamp;

            foreach (var token in tokenCache)
            {
                timestamps.Remove(token);
            }

            foreach (var (c, s, token) in continuations)
            {
                if (!timestamps.ContainsKey(token))
                {
                    continuation = c;
                    state = s;
                }
            }
        }

        continuation?.Invoke(state);
    }

    class ValueTaskSource : IValueTaskSource
    {
        readonly DelayTaskSource parent;

        public ValueTaskSource(DelayTaskSource parent) => this.parent = parent;

        public void GetResult(short token)
        {
            lock (parent.l)
            {
                if (parent.timestamps.ContainsKey(token))
                    throw new InvalidOperationException("not yet completed");
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            lock (parent.l)
            {
                if (parent.timestamps.ContainsKey(token))
                    return ValueTaskSourceStatus.Pending;

                return ValueTaskSourceStatus.Succeeded;
            }
        }

        public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            lock (parent.l)
            {
                if (parent.timestamps.ContainsKey(token))
                {
                    parent.continuations.Add((continuation, state, token));
                    return;
                }
            }

            // already completed, invoke inline
            continuation(state);
        }
    }
}
