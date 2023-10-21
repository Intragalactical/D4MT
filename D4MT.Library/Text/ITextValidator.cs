namespace D4MT.Library.Text;

public interface ITextValidator {
    bool IsValid(string text);
    bool IsInvalid(string text);
}
