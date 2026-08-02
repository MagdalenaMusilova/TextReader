using System.IO;
using System.Text;
using TextReader.Enums;

namespace TextReader.TextInputs;

public class FileTextInput : ITextInput, IDisposable
{
    private FileStream _stream;
    private Encoding _encoding;
    private long _textLength;
    private long _position;

    public event EventHandler? DataReadyEvent;
    public bool EOF => _stream.Position >= _stream.Length;
    public long Length => _textLength;
    public long Position => _position;

    public FileTextInput(string filePath)
    {
        _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        SetEncoding();
        CalculateTextLength();
        _stream.Seek(0, SeekOrigin.Begin);
        DataReadyEvent?.Invoke(this, EventArgs.Empty);
    }
    
    private FileTextInput()
    {
    }
    
    private void SetEncoding()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        using var tmp = new StreamReader(_stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true); //todo better way to get encoding?
        tmp.Peek();
        _encoding = tmp.CurrentEncoding;
        _stream.Seek(0, SeekOrigin.Begin);
    }

    private void CalculateTextLength()
    {
        _stream.Seek(0, SeekOrigin.Begin);

        byte[] buffer = new byte[_stream.Length];
        _stream.ReadExactly(buffer);
        _textLength = _encoding.GetCharCount(buffer);

        _stream.Seek(0, SeekOrigin.Begin);
    }
    
    ~FileTextInput()
    {
        Dispose(false);
    }

    public ITextInput Copy()
    {
        var res = new FileTextInput();
        res._stream = new FileStream(_stream.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
        res._encoding = _encoding;
        res._textLength = _textLength;
        return res;
    }

    public void Seek(long index)
    {
        _stream.Seek(0, SeekOrigin.Begin);
        _position = 0;

        if (index > 0)
        {
            Read(index);
        }
    }

    public int ReadByte()
    {
        int result = _stream.ReadByte();
        if (result != -1)
        {
            _position++;
        }
        return result;
    }

    public string Read(long size)
    {
        // Need to read enough bytes to get 'size' characters
        List<byte> bytes = new List<byte>();
        long charsRead = 0;

        while (charsRead < size && !EOF)
        {
            int b = _stream.ReadByte();
            if (b == -1) break;

            bytes.Add((byte)b);

            // Check how many characters we have so far
            charsRead = _encoding.GetCharCount(bytes.ToArray());
        }

        _position += size;
        return _encoding.GetString(bytes.ToArray());
    }

    public void Close()
    {
        _stream.Close();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stream?.Dispose();
        }
    }
}