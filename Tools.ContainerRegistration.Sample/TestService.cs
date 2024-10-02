using System.Timers;
using Tools.ContainerRegistration.Attributes;
using Timer = System.Timers.Timer;

namespace Tools.ContainerRegistration.Sample;

/// <summary>
/// TestService will be registered as Self (singleton), ITestService and ITestService2. Scope is ignored due to
/// existing Singleton attribute. ITestService3 is ignored as there is no typeof(ITestService3) in ServiceRegistration
/// </summary>
[Singleton(true)]
[Scoped]
[ServiceRegistration([typeof(ITestService), typeof(ITestService2)])]
public class TestService : ITestService, ITestService2, ITestService3
{
    private readonly Timer _timer;

    public TestService()
    {
        _timer = new Timer();
        _timer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
        _timer.Elapsed += TimerOnElapsed;
        _timer.Start();
    }

    private void TimerOnElapsed(
        object? sender,
        ElapsedEventArgs e)
    {
        Console.WriteLine($"Tick, tick: {e.SignalTime}");
    }
}

public interface ITestService
{
}

public interface ITestService2
{
}

public interface ITestService3
{
}