namespace TextReader.TextInputs;

/// <summary>
/// Interface for text input sources that can be read sequentially or randomly accessed
/// </summary>
public interface ITextInput : IDisposable
{
    /// <summary>
    /// Event raised when data is ready to be read
    /// </summary>
    event EventHandler? DataReadyEvent;

    /// <summary>
    /// Gets whether the end of the input has been reached
    /// </summary>
    bool IsEndOfFile { get; }

    /// <summary>
    /// Gets the current byte position in the input
    /// </summary>
    long Position { get; }

    /// <summary>
    /// Gets the total length of the input in bytes
    /// </summary>
    long ByteLength { get; }

    /// <summary>
    /// Creates a copy of this text input for independent reading
    /// </summary>
    ITextInput Copy();

    /// <summary>
    /// Seeks to the specified byte position
    /// </summary>
    void Seek(long byteIndex);

    /// <summary>
    /// Reads a single byte from the current position
    /// </summary>
    /// <returns>The byte value, or -1 if end of file</returns>
    int Read();

    /// <summary>
    /// Peeks at the next byte without advancing the position
    /// </summary>
    /// <returns>The byte value, or -1 if end of file</returns>
    int Peek();

    /// <summary>
    /// Reads a string of the specified byte length from the current position
    /// </summary>
    string Read(long size);

    /// <summary>
    /// Saves the input content to a file asynchronously
    /// </summary>
    Task SaveToFileAsync(string destFileName);
}