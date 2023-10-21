namespace D4MT.Library.Extensions;

public static class IOExtensions {
    #region DirectoryInfo
    public static DirectoryInfo? ToDirectoryInfo(this string path) {
        return string.IsNullOrWhiteSpace(path) is false ?
            new(path) :
            null;
    }
    #endregion
}
