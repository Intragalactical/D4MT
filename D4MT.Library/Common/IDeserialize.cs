namespace D4MT.Library.Common;

public interface IDeserialize<T> {
    static abstract T? Deserialize(string filePath);
    static abstract Task<T?> DeserializeAsync(string filePath, CancellationToken cancellationToken);
}
