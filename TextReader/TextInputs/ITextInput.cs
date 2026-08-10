namespace TextReader.TextInputs;

public interface ITextInput : IDisposable
{
    public event EventHandler? DataReadyEvent;
    
    public bool EOF { get; }
    public long Position { get; }
    public long ByteLength { get; }
    
    public ITextInput Copy();
    public void Seek(long byteIndex);
    public int Read();
    public int Peak();
    public string Read(long size);
    public Task SaveToFileAsync(string destFileName);
}