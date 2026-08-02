namespace TextReader.TextInputs;

public interface ITextInput
{
    public event EventHandler? DataReadyEvent;
    
    public bool EOF { get; }
    public long Length { get; }
    public long Position { get; }

    public ITextInput Copy();
    public void Seek(long index);
    public int ReadByte();
    public string Read(long size);
    public Task SaveToFileAsync(string destFileName);
}