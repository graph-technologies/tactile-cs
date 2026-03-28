using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TactileCs.Diagnostics;

/// <summary>
/// Centralised logging facade for the TactileCs library.
/// <para>
/// By default all log calls are no-ops.  Call
/// <see cref="Configure(ILoggerFactory)"/> at application start-up to
/// wire the library into your logging pipeline.
/// </para>
/// </summary>
public static class TactileLogger
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    /// <summary>
    /// Configures TactileCs to emit structured logs through the supplied
    /// <see cref="ILoggerFactory"/>.
    /// </summary>
    public static void Configure(ILoggerFactory factory)
    {
        _factory = factory ?? NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Resets logging to the default (silent) state.
    /// </summary>
    public static void Reset()
    {
        _factory = NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Creates a logger for the specified category type.
    /// </summary>
    public static ILogger<T> CreateLogger<T>() => _factory.CreateLogger<T>();

    /// <summary>
    /// Creates a logger for the specified category name.
    /// </summary>
    public static ILogger CreateLogger(string categoryName) =>
        _factory.CreateLogger(categoryName);
}
