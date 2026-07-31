using System.IO;
using System.Text;
using TextReader.Enums;

namespace TextReader.TextInputs;

public class FileTextInput : ITextInput
{
    
    private FileStream _stream;
    private Encoding _encoding;

    public bool EOF => _stream.Position >= _stream.Length;
    public long Length => _stream.Length;

    public FileTextInput(string filePath)
    {
        _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        SetEncoding();
    }
    
    private FileTextInput()
    {
    }
    
    private void SetEncoding()
    {
        using var tmp = new StreamReader(_stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true); //todo better way to get encoding?
        tmp.Peek();
        _encoding = tmp.CurrentEncoding;
    }
    
    ~FileTextInput()
    {
        _stream.Close();
    }

    public ITextInput Copy()
    {
        var res = new FileTextInput();
        res._stream = new FileStream(_stream.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
        res._encoding = _encoding;
        return res;
    }
    
    public void Seek(long index)
    {
        _stream.Seek(index, SeekOrigin.Begin);
    }

    public int ReadByte()
    {
        return _stream.ReadByte();
    }

    public string Read(long size)
    {
        byte[] buffer = new byte[size];
        _stream.ReadExactly(buffer);
        return _encoding.GetString(buffer);
    }
}