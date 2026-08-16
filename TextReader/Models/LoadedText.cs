using TextReader.Enums;
using TextReader.TextInputs;

namespace TextReader.Models;

/// <summary>
/// Manages loaded text content with line-based indexing and search functionality
/// </summary>
public class LoadedText
{
    private const int CharsPerLine = 30; // Used for initial line count estimation

    /// <summary>
    /// Event raised when line offset calculation is complete
    /// </summary>
    public event EventHandler? FinishedLoadingEvent;

    private readonly ITextInput _input;
    private readonly SearchFeature _searchFeature;
    private readonly List<int> _lineOffsets = new();
    private bool _lineOffsetsCalculated;
    private int _estimatedLineCount;
    private int _actualLineCount;

    private NewLineType _newLineType;
    private int _newLineSize;
    private Func<ITextInput, bool> _isNextByteNewLine;

    /// <summary>
    /// Total number of lines in the text (estimated until fully loaded)
    /// </summary>
    public int LinesCount => _lineOffsetsCalculated ? _actualLineCount : _estimatedLineCount;

    /// <summary>
    /// Number of lines that have been calculated so far
    /// </summary>
    private int CurrentLinesCalculatedCount => _lineOffsets.Count;
    
    
    public LoadedText(ITextInput input)
    {
        _input = input;
        _searchFeature = new SearchFeature(_input);
        EstimateLineCount();
        DetectNewLineType();
        CalculateLineOffsets();
    }

    /// <summary>
    /// Detects the newline character type used in the text (CR, LF, or CRLF)
    /// </summary>
    private void DetectNewLineType()
    {
        // Default to LF (\n) if no newline is found
        _newLineType = NewLineType.N;

        int readByte;
        while ((readByte = _input.Read()) != -1)
        {
            // Check for carriage return
            if (readByte == '\r')
            {
                int nextByte = _input.Read();
                if (nextByte == '\n')
                {
                    // Windows-style CRLF
                    _newLineType = NewLineType.RN;
                    _newLineSize = 2;
                    break;
                }
                else
                {
                    // Mac-style CR
                    _newLineType = NewLineType.R;
                    _newLineSize = 1;
                    break;
                }
            }
            else if (readByte == '\n')
            {
                // Unix-style LF
                _newLineType = NewLineType.N;
                _newLineSize = 1;
                break;
            }
        }

        // Reset position to beginning after detecting newline type
        _input.Seek(0);

        // Set up the newline detection function based on detected type
        switch (_newLineType)
        {
            case NewLineType.N:
                _isNextByteNewLine = (stream) => stream.Read() == '\n';
                break;
            case NewLineType.R:
                _isNextByteNewLine = (stream) => stream.Read() == '\r';
                break;
            case NewLineType.RN:
                _isNextByteNewLine = (stream) =>
                {
                    if (stream.Read() == '\r')
                    {
                        if (stream.Peek() == '\n')
                        {
                            stream.Read(); // Consume \n
                            return true;
                        }
                    }
                    return false;
                };
                break;
        }
    }

    /// <summary>
    /// Estimates the number of lines based on file size and average characters per line
    /// </summary>
    private void EstimateLineCount()
    {
        _estimatedLineCount = (int)Math.Ceiling(_input.ByteLength / (double)CharsPerLine);
    }
    
    /// <summary>
    /// Asynchronously calculates byte offsets for all lines in the text
    /// </summary>
    private void CalculateLineOffsets()
    {
        Task.Run(() =>
        {
            using var tmpInput = _input.Copy();
            _lineOffsets.Capacity = LinesCount;

            // First line starts at position 0
            _lineOffsets.Add(0);

            // Find all newline positions
            while (tmpInput.Position < tmpInput.ByteLength)
            {
                if (_isNextByteNewLine(tmpInput))
                {
                    _lineOffsets.Add((int)tmpInput.Position);
                }
            }

            // If file doesn't end with newline, add EOF offset for easier calculations
            if (_lineOffsets.Last() != tmpInput.ByteLength)
            {
                _lineOffsets.Add((int)tmpInput.ByteLength);
            }

            _actualLineCount = _lineOffsets.Count - 1;
            _lineOffsetsCalculated = true;
            FinishedLoadingEvent?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    /// Retrieves a range of lines from the text
    /// </summary>
    /// <param name="startLineIndex">Zero-based index of the first line to retrieve</param>
    /// <param name="lineCount">Number of lines to retrieve</param>
    /// <param name="buffer">Buffer to store the retrieved lines</param>
    /// <returns>Actual number of lines retrieved</returns>
    public int GetLines(int startLineIndex, int lineCount, string[] buffer)
    {
        // Wait for the requested lines to be calculated
        while (!_lineOffsetsCalculated && CurrentLinesCalculatedCount <= startLineIndex + lineCount)
        {
            Thread.Sleep(100);
        }

        // Return early if start index is out of bounds
        if (startLineIndex >= LinesCount)
        {
            return 0;
        }

        // Adjust line count if it extends beyond available lines
        if (startLineIndex + lineCount > LinesCount)
        {
            lineCount = LinesCount - startLineIndex;
        }

        // Read each line
        _input.Seek(_lineOffsets[startLineIndex]);
        for (int i = 0; i < lineCount; i++)
        {
            int lineSize = _lineOffsets[startLineIndex + i + 1] - _lineOffsets[startLineIndex + i];
            buffer[i] = _input.Read(lineSize);
        }

        return lineCount;
    }


    /// <summary>
    /// Converts a byte index to a line and character position
    /// </summary>
    /// <param name="byteIndex">Absolute byte index in the text</param>
    /// <returns>WordPosition containing line index and character offset</returns>
    public WordPosition IndexToWordPosition(int byteIndex)
    {
        var lineIndex = _lineOffsets.BinarySearch(byteIndex);
        if (lineIndex < 0)
        {
            // If not found, BinarySearch returns bitwise complement of next larger element
            lineIndex = ~lineIndex;
            lineIndex--;
        }

        return new WordPosition
        {
            LineIndex = lineIndex,
            CharacterOffset = byteIndex - _lineOffsets[lineIndex]
        };
    }

    /// <summary>
    /// Searches for all occurrences of a word in the text
    /// </summary>
    /// <param name="searchWord">Word to search for (case-insensitive)</param>
    /// <returns>List of positions where the word was found</returns>
    public List<WordPosition> Search(string searchWord)
    {
        var byteIndexes = _searchFeature.Search(searchWord);
        return byteIndexes.Select(IndexToWordPosition).ToList();
    }
}