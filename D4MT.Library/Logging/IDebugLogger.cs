namespace D4MT.Library.Logging;

public interface IDebugLogger : IEquatable<IDebugLogger> {
    IDebugLogger? Parent { get; }
    Guid Id { get; }
    string Name { get; }

    void LogIf(bool condition, string message, LogLevel logLevel = LogLevel.Information);
    void Log(string message, LogLevel logLevel = LogLevel.Information);
    IDebugLogger CreateChildWithName(string name);
    IDebugLogger CreateChildFromType(Type callingType);
}
