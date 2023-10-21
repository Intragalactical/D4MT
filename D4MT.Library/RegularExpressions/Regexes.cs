
using System.Text.RegularExpressions;

namespace D4MT.Library.RegularExpressions;

public sealed partial class Regexes {
    public static readonly Regex RestrictedPathMatcher = CreateRestrictedPathMatcher();

    [GeneratedRegex(@"^[A-Za-z]:\\?$|^[A-Za-z]:\\(?:[W|w]indows)\\?|^[A-Za-z]:\\(?:[U|u]sers\\?(?:[A-Za-z|\s]*?(?=\\|$)))\\?$")]
    private static partial Regex CreateRestrictedPathMatcher();
}
