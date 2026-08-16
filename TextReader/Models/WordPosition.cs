namespace TextReader.Models;

/// <summary>
/// Represents a position in the text by line and character offset
/// </summary>
public struct WordPosition
{
    /// <summary>
    /// Zero-based line index
    /// </summary>
    public int LineIndex;

    /// <summary>
    /// Character offset within the line
    /// </summary>
    public int CharacterOffset;
}