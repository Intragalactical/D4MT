namespace D4MT.Library.Text;

public sealed class ProjectNameTransformer : ITextTransformer {
    private const byte UppercaseA = (byte)'A';
    private const byte UppercaseZ = (byte)'Z';
    private const byte LowercaseA = (byte)'a';
    private const byte LowercaseZ = (byte)'z';
    private const byte Zero = (byte)'0';
    private const byte Nine = (byte)'9';

    private sealed record class Replacing {
        public readonly char ToReplace;
        public readonly char ReplaceWith;

        public Replacing(char toReplace, char replaceWith) {
            ToReplace = toReplace;
            ReplaceWith = replaceWith;
        }
    }

    private static readonly IEnumerable<Replacing> Replacings = new Replacing[] {
        new(' ', '_')
    };
    private static readonly (byte Start, byte End)[] AllowedCharacterRanges = new[] {
        (UppercaseA, UppercaseZ), (LowercaseA, LowercaseZ), (Zero, Nine)
    };
    private static readonly byte[] AllowedCharacters = "_"u8.ToArray();

    public static readonly ITextTransformer Shared = new ProjectNameTransformer();

    private static bool IsValidCharacter(char c) {
        bool inRange((byte Start, byte End) range) {
            return c >= range.Start && c <= range.End;
        }

        bool equals(byte b) {
            return c == b;
        }

        return AllowedCharacterRanges.Any(inRange) || AllowedCharacters.Any(equals);
    }

    private static bool TryGetReplacing(char c, out char replacingCharacter) {
        Replacing? replacingOrNull = Replacings
            .FirstOrDefault(replacing => { return replacing.ToReplace.Equals(c); });

        if (replacingOrNull is not Replacing replacing) {
            replacingCharacter = '\0';
            return false;
        }

        replacingCharacter = replacing.ReplaceWith;
        return true;
    }

    public string Transform(string text) {
        Span<char> newText = new char[text.Length];
        int addedCharacters = 0;
        for (int charIndex = 0; charIndex < text.Length; charIndex++) {
            char currentChar = text[charIndex];
            currentChar = TryGetReplacing(currentChar, out char replacingCharacter) && replacingCharacter is not '\0' ?
                replacingCharacter :
                currentChar;

            if (!IsValidCharacter(currentChar)) {
                continue;
            }

            newText[addedCharacters] = currentChar;

            addedCharacters++;
        }
        return new string(newText[0..addedCharacters]);
    }
}
