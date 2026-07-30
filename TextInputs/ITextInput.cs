namespace TextReader.TextInputs;

public interface ITextInput
{
    public int LinesCount { get; }
    public int GetLines(int startIndex, int lineCount, string[] buffer);
}