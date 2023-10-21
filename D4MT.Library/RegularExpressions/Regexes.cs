
using System.Text.RegularExpressions;

namespace D4MT.Library.RegularExpressions;

public sealed partial class Regexes {
    public static readonly Regex RestrictedPathMatcher = CreateRestrictedPathMatcher();

    [GeneratedRegex(@"^[A-Za-z]:\\?$|^[A-Za-z]:\\(?:[W|w][I|i][N|n][D|d][O|o][W|w][S|s])\\?|^[A-Za-z]:\\(?:[U|u][S|s][E|e][R|r][S|s]\\?(?:[A-Za-z|\s]*?(?=\\|$)))\\?$")]
    private static partial Regex CreateRestrictedPathMatcher();
}
