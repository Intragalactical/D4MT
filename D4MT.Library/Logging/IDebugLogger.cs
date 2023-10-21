namespace D4MT.Library.Logging;

public interface IDebugLogger {
    Guid Id { get; }

    void LogIf(bool condition, string message, LogLevel logLevel = LogLevel.Information);
    void Log(string message, LogLevel logLevel = LogLevel.Information);

    static abstract IDebugLogger CreateFromType(Type callingType);
}
