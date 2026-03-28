using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TactileCs.Diagnostics;

namespace TactileCs.Tests.Diagnostics;

public class TactileLoggerTests
{
    [Fact]
    public void CreateLogger_WithoutConfigure_ReturnsNullLogger()
    {
        TactileLogger.Reset();
        try
        {
            var logger = TactileLogger.CreateLogger<TactileLoggerTests>();

            Assert.NotNull(logger);
            // NullLogger is a no-op; logging should not throw.
            logger.LogInformation("This should be a no-op");
        }
        finally
        {
            TactileLogger.Reset();
        }
    }

    [Fact]
    public void Configure_AllowsLoggingThroughFactory()
    {
        TactileLogger.Reset();
        try
        {
            var provider = new FakeLoggerProvider();
            using var factory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(provider);
            });

            TactileLogger.Configure(factory);

            var logger = TactileLogger.CreateLogger("TestCategory");
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Debug));
        }
        finally
        {
            TactileLogger.Reset();
        }
    }

    [Fact]
    public void Reset_ReturnsToDefaultState()
    {
        TactileLogger.Reset();
        try
        {
            var provider = new FakeLoggerProvider();
            using var factory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(provider);
            });

            TactileLogger.Configure(factory);
            var loggerBefore = TactileLogger.CreateLogger("ResetTest");
            Assert.True(loggerBefore.IsEnabled(LogLevel.Debug));

            TactileLogger.Reset();

            var loggerAfter = TactileLogger.CreateLogger("ResetTest");
            Assert.False(loggerAfter.IsEnabled(LogLevel.Debug));
        }
        finally
        {
            TactileLogger.Reset();
        }
    }

    /// <summary>
    /// Minimal logger provider for test purposes.
    /// </summary>
    private sealed class FakeLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new FakeLogger();
        public void Dispose() { }

        private sealed class FakeLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
