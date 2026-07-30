namespace TextReader.TextInputs;

public interface ITextInput
{
    public bool EndOfInput { get; }
    public string GetLine();
    public string[] GetLines(in int lineCount);
}