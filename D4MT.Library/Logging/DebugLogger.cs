using System.Diagnostics;

namespace D4MT.Library.Logging;

public sealed class DebugLogger(string name) : IDebugLogger {
    public Guid Id { get; } = Guid.NewGuid();

    private readonly string _name = name;

    public void Log(string message, LogLevel logLevel = LogLevel.Information) {
        string prefix = logLevel switch {
            LogLevel.Trace or LogLevel.Information => "",
            LogLevel.Warning => "Warning: ",
            LogLevel.Error => "ERROR: ",
            _ => throw new UnreachableException("")
        };
        string dateAndTime = $"<{DateTime.Now:dd/MM/yyyy HH:mm:ss}>";
        string formatted = $"{dateAndTime} [{_name}] {prefix}{message}";
#if DEBUG
        Debug.WriteLine(formatted);
#endif
    }

    public void LogIf(bool condition, string message, LogLevel logLevel = LogLevel.Information) {
        if (condition is false) {
            return;
        }

        Log(message, logLevel);
    }

    public static IDebugLogger CreateFromType(Type callingType) {
        return new DebugLogger(callingType.Name);
    }
}
