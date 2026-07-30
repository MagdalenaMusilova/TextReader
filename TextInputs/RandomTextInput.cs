using Bogus;

namespace TextReader.TextInputs;

public class RandomTextInput : ITextInput
{
    private Faker _faker = new Faker();
    private int _maxLines;
    private int _curLineCount = 0;
    
    public RandomTextInput(in int maxLines)
    {
        _maxLines = maxLines;
    }

    public bool EndOfInput => _curLineCount >= _maxLines;

    public string GetLine()
    {
        _curLineCount++;
        return _faker.Lorem.Sentence();
    }

    public string[] GetLines(in int lineCount)
    {
        string[] res = new string[lineCount];
        for (int i = 0; i < lineCount; i++)
        {
            res[i] = GetLine();
        }
        return res;
    }
}