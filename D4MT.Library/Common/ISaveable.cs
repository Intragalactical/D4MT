namespace D4MT.Library.Common;

public interface ISaveable {
    bool TrySave();
    Task<bool> TrySaveAsync(CancellationToken cancellationToken);
}
