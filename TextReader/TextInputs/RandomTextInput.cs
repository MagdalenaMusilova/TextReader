using Bogus;

namespace TextReader.TextInputs;

public class RandomTextInput : ITextInput
{
    public event EventHandler? DataReadyEvent;
    
    private string _fullText;
    private long _curBytePosition = 0;

    public bool EOF => _curBytePosition >= _fullText.Length;
    public long Length => _fullText.Length;
    public long Position => _curBytePosition;

    public RandomTextInput(in int numOfSentences)
    {
        GenerateLines(numOfSentences);
        DataReadyEvent?.Invoke(this, EventArgs.Empty);
    }

    private RandomTextInput()
    {
    }

    public ITextInput Copy()
    {
        var res = new RandomTextInput();
        res._fullText = _fullText;
        return res;
    }

    public void Seek(long index)
    {
        _curBytePosition = index;
    }

    public int ReadByte()
    {
        if (EOF)
        {
            return -1;
        }
        return _fullText[(int)_curBytePosition++];
    }

    public string Read(long size)
    {
        var res = _fullText.Substring((int)_curBytePosition, (int)size);
        _curBytePosition += size;
        return res;
    }

    private void GenerateLines(in int numOfSentences)
    {
        Faker faker = new Faker();
        var sentences = faker.Lorem.Sentences(numOfSentences);
        _fullText = string.Join(Environment.NewLine, sentences);
    }
}