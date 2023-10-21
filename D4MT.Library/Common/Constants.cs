using IOEnumerationOptions = System.IO.EnumerationOptions;

namespace D4MT.Library.Common;

public static class Constants {
    public static class Strings {
        public static class Patterns {
            public const string ProjectFileName = "Project.json";
            public const string ExplorerFileName = "explorer.exe";
            public const string All = "*";
        }

        public static class Paths {
            public const string ConfigurationFile = @".\Configuration.json";
            public static readonly string WindowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        }

        public static class FormatLiterals {
            public const string RelativeProjectPath = @".\{0}\" + Patterns.ProjectFileName;
            public const string EditorWindowTitle = @"Project {0} - Democracy 4 Modding Tool";
        }
    }

    public static class EnumerationOptions {
        public static readonly IOEnumerationOptions NoRecursionNoInaccessibleSpecialSystemOrHidden = new() {
            RecurseSubdirectories = false,
            MaxRecursionDepth = 1,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
        };

        public static readonly IOEnumerationOptions FirstLevelRecursionNoInaccessibleSpecialSystemOrHidden = new() {
            RecurseSubdirectories = true,
            MaxRecursionDepth = 1,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden
        };
    }
}
