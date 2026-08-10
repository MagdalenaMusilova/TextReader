using System.IO;
using Bogus;

namespace TextReader.TextInputs;

public class RandomTextInput : ITextInput
{
    public event EventHandler? DataReadyEvent;
    
    private string _fullText;
    private long _curBytePosition = 0;

    public bool EOF => _curBytePosition >= _fullText.Length;
    public long Position => _curBytePosition;
    public long ByteLength => _fullText.Length; //should be only 1B chars => string len == byte lenght 

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

    public void Seek(long byteIndex)
    {
        _curBytePosition = byteIndex;
    }

    public int Read()
    {
        if (EOF)
        {
            return -1;
        }
        return _fullText[(int)_curBytePosition++];
    }

    public int Peak()
    {
        return _fullText[(int)_curBytePosition];
    }

    public string Read(long size)
    {
        var res = _fullText.Substring((int)_curBytePosition, (int)size);
        _curBytePosition += size;
        return res;
    }

    public async Task SaveToFileAsync(string destFileName)
    {
        await File.WriteAllTextAsync(destFileName, _fullText);
    }

    private void GenerateLines(in int numOfSentences)
    {
        Faker faker = new Faker();
        var sentences = faker.Lorem.Sentences(numOfSentences);
        _fullText = string.Join(Environment.NewLine, sentences);
    }
    
    public void Dispose()
    {
    }
}