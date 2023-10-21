using System.Diagnostics;

namespace D4MT.Library.Logging;

public sealed class DebugLogger(string name) : IDebugLogger, IDebugLoggerCreator {
    public static readonly IDebugLogger Shared = new DebugLogger("D4MT");

    public IDebugLogger? Parent { get; }
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; } = name;

    private DebugLogger(IDebugLogger parent, string name) : this(name) {
        Parent = parent;
    }

    public void Log(string message, LogLevel logLevel = LogLevel.Information) {
        string prefix = logLevel switch {
            LogLevel.Trace or LogLevel.Information => "",
            LogLevel.Warning => "Warning: ",
            LogLevel.Error => "ERROR: ",
            _ => throw new UnreachableException("")
        };
        string dateAndTime = $"<{DateTime.Now:dd/MM/yyyy HH:mm:ss}>";
        string formatted = $"{dateAndTime} [{Name}] {prefix}{message}";
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

    public IDebugLogger CreateChildWithName(string name) {
        return new DebugLogger(this, name);
    }

    public IDebugLogger CreateChildFromType(Type callingType) {
        return CreateChildWithName(callingType.Name);
    }

    public static IDebugLogger CreateFromType(Type callingType) {
        return new DebugLogger(callingType.Name);
    }

    public override bool Equals(object? obj) {
        return obj is IDebugLogger other && Equals(other);
    }

    public override int GetHashCode() {
        return Id.GetHashCode() & Name.GetHashCode();
    }

    public bool Equals(IDebugLogger? other) {
        return other is not null &&
            GetHashCode().Equals(other.GetHashCode());
    }

    public override string ToString() {
        return $"DebugLogger {Name} ({Id})";
    }
}
