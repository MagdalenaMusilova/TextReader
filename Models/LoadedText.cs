using TextReader.Enums;
using TextReader.TextInputs;

namespace TextReader.Models;

public class LoadedText
{
    private const int charsPerLine = 30;

    public event EventHandler? FinishedLoadingEvent;
    
    private ITextInput _input;
    private SearchFeature _searchFeature;
    private List<long> _lineOffsets = new List<long>();    //todo needs to be changed into something that handles more than int.MaxValue elements 
    private bool _lineOffsetsCalculated = false;
    private int _linesCountGuess;
    private int _linesCount;
    
    private NewLineType _newLineType;
    private int _newLineSize;
    private Func<ITextInput, bool> _isNextByteNewLine;

    public long LinesCount => _lineOffsetsCalculated ? _linesCount : _linesCountGuess;
    private long CurLinesCalculatedCount => _lineOffsets.Count;
    
    
    public LoadedText(ITextInput input)
    {
        _input = input;
        _searchFeature = new SearchFeature(_input);
        GuessNumberOfLines();
        SetNewLineType();
        CalculateLineOffsets();
    }
    
    private void SetNewLineType()
    {
        //todo use this to get the first new line
        _newLineType = NewLineType.N;   // dummy value, in case there is no newline
        
        int readByte;
        while ((readByte = _input.ReadByte()) != -1)
        {
            // check if newline
            if (readByte == '\r')
            {
                int nextByte = _input.ReadByte();
                if (nextByte == '\n')
                {
                    _newLineType = NewLineType.RN;
                    _newLineSize = 2;
                    break;
                }
                else
                {
                    _newLineType = NewLineType.R;
                    _newLineSize = 1;
                    break;
                }
            }
            else if (readByte == '\n')
            {
                _newLineType = NewLineType.N;
                _newLineSize = 1;
                break;
            }
        }

        switch (_newLineType)
        {
            case NewLineType.N:
                _isNextByteNewLine = (stream) => stream.ReadByte() == '\n';
                break;
            case NewLineType.R:
                _isNextByteNewLine = (stream) => stream.ReadByte() == '\r';
                break;
            case NewLineType.RN: 
                _isNextByteNewLine = (stream) => stream.ReadByte() == '\r' && stream.ReadByte() == '\n';
                break;
        }
    }
    
    private void GuessNumberOfLines()
    {
        _linesCountGuess = (int)(_input.Length / charsPerLine);
    }
    
    private void CalculateLineOffsets()
    {
        Task.Run(() =>
        {
            var tmpInput = _input.Copy();
            _lineOffsets.Capacity = (int)LinesCount;
            long offset = 0;

            _lineOffsets.Add(0);

            while (offset < tmpInput.Length)
            {
                if (_isNextByteNewLine(tmpInput))
                {
                    offset += _newLineSize;
                    _lineOffsets.Add(offset);
                    continue;
                }
                
                offset++;
            }

            // if the file doesnt end with new line, add offset for EOF (for easier calculations)
            if (_lineOffsets.Last() != tmpInput.Length)    
            {
                _lineOffsets.Add(offset);
            }
            _linesCount = _lineOffsets.Count - 1;

            _lineOffsetsCalculated = true;
            FinishedLoadingEvent?.Invoke(this, EventArgs.Empty);
        });
    }

    public int GetLines(long startIndex, int lineCount, string[] buffer)
    {
        // wait for the whole block to be calculated
        while (!_lineOffsetsCalculated && CurLinesCalculatedCount <= startIndex + lineCount)
        {
            // todo better way to get to the correct line mby?
            Thread.Sleep(100);
        }

        if (startIndex >= LinesCount)   // index that isn't in the file
        {
            return 0;
        }

        if (startIndex + lineCount > LinesCount)    // make sure that we dont read outside the file (there are fewer lines than requested)
        {
            lineCount = (int)(LinesCount - startIndex);
        }
        
        _input.Seek(_lineOffsets[(int)startIndex]);
        for (int i = 0; i < lineCount; i++)
        {
            long size = _lineOffsets[(int)(startIndex + i + 1)] - _lineOffsets[(int)(startIndex + i)];
            buffer[i] = _input.Read(size);
        }
        
        return lineCount;
    }


    public WordPosition IndexToWordPosition(int index)
    {
        var lineOffsetsI = _lineOffsets.BinarySearch(index);
        if (lineOffsetsI < 0)
        {
            lineOffsetsI = ~lineOffsetsI;
        }
        return new WordPosition{
            lineIndex = lineOffsetsI, 
            lineOffset = index - _lineOffsets[lineOffsetsI]
        };
    }

    public List<WordPosition> Search(string word)
    {
        var indexes = _searchFeature.Search(word);
        return indexes.Select(IndexToWordPosition).ToList();
    }
}