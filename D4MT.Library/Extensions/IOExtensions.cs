namespace D4MT.Library.Extensions;

public static class IOExtensions {
    #region FileInfo
    #endregion

    #region DirectoryInfo
    public static FileInfo? GetFileOrNull(this DirectoryInfo directoryInfo, string pattern, EnumerationOptions enumerationOptions) {
        return directoryInfo
            .GetFiles(pattern, enumerationOptions)
            .ElementAtOrDefault(0);
    }

    public static DirectoryInfo? ToDirectoryInfo(this string path) {
        return string.IsNullOrWhiteSpace(path) is false ?
            new(path) :
            null;
    }
    #endregion
}
