// ReSharper disable UnusedMember.Global

namespace API.Services;

internal static class Logging
{
    internal static ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();

    internal static ILogger CreateLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }

    internal static ILogger CreateLogger(string categoryName)
    {
        return LoggerFactory.CreateLogger(categoryName);
    }
}
