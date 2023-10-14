using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace JoinRpg.TestHelpers;
public class XUnitLogger<T> : ILogger<T>, IDisposable
{
    private ITestOutputHelper _output;

    public XUnitLogger(ITestOutputHelper output)
    {
        _output = output;
    }
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        _output.WriteLine(state?.ToString());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return this;
    }

    public void Dispose()
    {
    }
}
