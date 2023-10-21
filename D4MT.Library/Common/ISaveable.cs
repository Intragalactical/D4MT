namespace D4MT.Library.Common;

public interface ISaveable {
    void Save();
    Task SaveAsync(CancellationToken cancellationToken);
}
