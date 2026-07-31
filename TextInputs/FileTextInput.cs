using System.IO;
using System.Text;
using TextReader.Enums;

namespace TextReader.TextInputs;

public class FileTextInput : ITextInput
{
    private const int charsPerLine = 30;
    
    private FileStream _stream;
    private Encoding _encoding;
    private NewLineType _newLineType;
    private int _newLineSize;
    private List<long> _lineOffsets = new List<long>();    //todo needs to be changed into something that handles more than int.MaxValue elements 
    private bool _lineOffsetsCalculated = false;
    private int _linesCountGuess;
    private int _linesCount;

    public event EventHandler? FinishedLoadingEvent;
    private Func<FileStream, bool> _isNextByteNewLine;
    
    public long LinesCount => _lineOffsetsCalculated ? _linesCount : _linesCountGuess; 
    private long CurLinesCalculatedCount => _lineOffsets.Count;
    
    public FileTextInput(string filePath)
    {
        _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        SetEncoding();
        SetNewLineType();
        
        GuessNumberOfLines();
        CalculateLineOffsets();
    }

    private void GuessNumberOfLines()
    {
        _linesCountGuess = (int)(_stream.Length / charsPerLine);
    }

    private void SetEncoding()
    {
        using var tmp = new StreamReader(_stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true); //todo better way to get encoding?
        tmp.Peek();
        _encoding = tmp.CurrentEncoding;
    }
    
    private void SetNewLineType()
    {
        using FileStream tmpStream = new FileStream(_stream.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
        _newLineType = NewLineType.N;   // dummy value, in case there is no newline
        
        int readByte;
        while ((readByte = tmpStream.ReadByte()) != -1)
        {
            // check if newline
            if (readByte == '\r')
            {
                int nextByte = tmpStream.ReadByte();
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
    
    private void CalculateLineOffsets()
    {
        Task.Run(() =>
        {
            using FileStream tmpStream = new FileStream(_stream.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
            _lineOffsets.Capacity = (int)LinesCount;
            long offset = 0;

            _lineOffsets.Add(0);

            int readByte;
            while (offset < tmpStream.Length)
            {
                if (_isNextByteNewLine(tmpStream))
                {
                    offset += _newLineSize;
                    _lineOffsets.Add(offset);
                    continue;
                }
                
                offset++;
            }

            // if the file doesnt end with new line, add offset for EOF (for easier calculations)
            if (_lineOffsets.Last() != tmpStream.Length)    
            {
                _lineOffsets.Add(offset);
            }
            _linesCount = _lineOffsets.Count - 1;

            _lineOffsetsCalculated = true;
            FinishedLoadingEvent?.Invoke(this, EventArgs.Empty);
        });
    }
    
    ~FileTextInput()
    {
        _stream.Close();
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
        
        _stream.Seek(_lineOffsets[(int)startIndex], SeekOrigin.Begin);
        for (int i = 0; i < lineCount; i++)
        {
            long size = _lineOffsets[(int)(startIndex + i + 1)] - _lineOffsets[(int)(startIndex + i)] - _newLineSize;
            buffer[i] = ReadStringFromStream(size);
            for (int j = 0; j < _newLineSize; j++)
            {
                _stream.ReadByte();
            }
        }
        
        return lineCount;
    }

    private string ReadStringFromStream(long size)
    {
        byte[] buffer = new byte[size];
        _stream.ReadExactly(buffer);
        return _encoding.GetString(buffer);
    }
}