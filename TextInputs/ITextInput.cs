namespace TextReader.TextInputs;

public interface ITextInput
{
    public bool EOF { get; }
    public long Length { get; }

    public ITextInput Copy();
    public void Seek(long index);
    public int ReadByte();
    public string Read(long size);
}