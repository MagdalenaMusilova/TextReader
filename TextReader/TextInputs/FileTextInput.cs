using System.IO;
using System.Text;
using TextReader.Enums;

namespace TextReader.TextInputs;

public class FileTextInput : ITextInput
{
    private string _filePath;
    private FileStream _fileStream;

    private long _length;

    public event EventHandler? DataReadyEvent;
    public bool EOF => Position >= _length;
    public long Position => _fileStream.Position;
    public long ByteLength => _length;

    public FileTextInput(string filePath, long bytePosition = 0)
    {
        _filePath = filePath;
        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        _length = _fileStream.Length;
        Seek(bytePosition);
        DataReadyEvent?.Invoke(this, EventArgs.Empty);
    }

    ~FileTextInput()
    {
        Dispose(false);
    }

    public ITextInput Copy()
    {
        return new FileTextInput(_filePath, 0);
    }

    public void Seek(long byteIndex)
    {
        _fileStream.Seek(byteIndex, SeekOrigin.Begin);
    }

    public int Read()
    {
        return _fileStream.ReadByte();
    }

    public int Peak()
    {
        int b = _fileStream.ReadByte();
        if (b != -1)
        {
            _fileStream.Seek(-1, SeekOrigin.Current);
        }
        return b;
    }


    public string Read(long size)
    {
        byte[] buffer = new byte[size];
        int bytesRead = _fileStream.Read(buffer, 0, (int)size);

        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    public async Task SaveToFileAsync(string destFileName)
    {
        await using var source = File.OpenRead(_filePath);
        await using var destination = File.Create(destFileName);

        await source.CopyToAsync(destination);
    }

    public void Close()
    {
        _fileStream.Close();
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
            _fileStream.Close();
            _fileStream.Dispose();
        }
    }
}