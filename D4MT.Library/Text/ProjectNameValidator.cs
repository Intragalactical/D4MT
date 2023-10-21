namespace D4MT.Library.Text;

public sealed class ProjectNameValidator : ITextValidator {
    private const byte UppercaseA = (byte)'A';
    private const byte UppercaseZ = (byte)'Z';
    private const byte LowercaseA = (byte)'a';
    private const byte LowercaseZ = (byte)'z';
    private const byte Zero = (byte)'0';
    private const byte Nine = (byte)'9';

    public static readonly ITextValidator Shared = new ProjectNameValidator();

    private static readonly (byte Start, byte End)[] AllowedCharacterRanges = new[] {
        (UppercaseA, UppercaseZ), (LowercaseA, LowercaseZ), (Zero, Nine)
    };
    private static readonly byte[] AllowedCharacters = "_"u8.ToArray();

    private static bool IsValidCharacter(char c) {
        bool inRange((byte Start, byte End) range) {
            return c >= range.Start && c <= range.End;
        }

        bool equals(byte b) {
            return c == b;
        }

        return AllowedCharacterRanges.Any(inRange) || AllowedCharacters.Any(equals);
    }

    public bool IsValid(string text) {
        bool allValid = true;
        foreach (char c in text) {
            allValid = allValid && IsValidCharacter(c);
        }
        return allValid;
    }

    public bool IsInvalid(string text) {
        return IsValid(text) is false;
    }
}
