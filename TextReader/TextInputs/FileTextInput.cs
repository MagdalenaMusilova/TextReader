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
    public bool IsEndOfFile => Position >= _length;
    public long Position => _fileStream.Position;
    public long ByteLength => _length;

    public FileTextInput(string filePath, long bytePosition = 0)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"The file '{filePath}' does not exist.", filePath);

        _filePath = filePath;

        try
        {
            _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _length = _fileStream.Length;

            if (_length == 0)
                throw new InvalidDataException($"The file '{filePath}' is empty.");

            Seek(bytePosition);
            DataReadyEvent?.Invoke(this, EventArgs.Empty);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied to file '{filePath}'. Check file permissions.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Unable to read file '{filePath}'. The file may be corrupted or in use by another process.", ex);
        }
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

    public int Peek()
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
        if (string.IsNullOrWhiteSpace(destFileName))
            throw new ArgumentException("Destination file path cannot be null or empty.", nameof(destFileName));

        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(destFileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var source = File.OpenRead(_filePath);
            await using var destination = File.Create(destFileName);

            await source.CopyToAsync(destination);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied when saving to '{destFileName}'. Check file permissions.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Unable to save file to '{destFileName}'. The path may be invalid or the disk may be full.", ex);
        }
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