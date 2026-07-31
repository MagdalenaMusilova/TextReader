namespace TextReader.TextInputs;

public interface ITextInput
{
    public event EventHandler FinishedLoadingEvent;
    
    public long LinesCount { get; }
    public int GetLines(long startIndex, int lineCount, string[] buffer);
}