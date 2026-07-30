using Bogus;

namespace TextReader.TextInputs;

public class RandomTextInput : ITextInput
{
    private int _maxLines;
    private int _curLineCount = 0;
    private string[] lines;
    
    public RandomTextInput(in int maxLines)
    {
        _maxLines = maxLines;
        lines = new string[_maxLines];
        GenerateLines();
    }

    public int LinesCount => _maxLines;
    
    public int GetLines(int startIndex, int lineCount, string[] buffer)
    {
        _curLineCount = startIndex;
        if (_curLineCount + lineCount >= _maxLines)
        {
            lineCount = _maxLines - _curLineCount;
        }

        for (int i = 0; i < lineCount; i++)
        {
            buffer[i] = lines[_curLineCount++];
        }
        
        return lineCount;
    }

    private void GenerateLines()
    {
        /*Faker faker = new Faker();
        for (int i = 0; i < _maxLines; i++)
        {
            lines[i] = faker.Lorem.Sentence();
        }*/
        for (int i = 0; i < _maxLines; i++)
        {
            lines[i] = i.ToString();
        }
    }
}