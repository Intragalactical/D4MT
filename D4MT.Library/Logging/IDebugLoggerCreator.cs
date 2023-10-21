namespace D4MT.Library.Logging;

public interface IDebugLoggerCreator {
    static abstract IDebugLogger CreateFromType(Type callingType);
}
