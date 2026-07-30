using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TextReader.TextInputs;

namespace TextReader;

public partial class TextReaderControl : UserControl
{
    const double DefaultFontSize = 14;
    const double XOffset = 10;
    const double YOffsetTop = 10;
    private double _lineHeight;
    private int _visibleLinesCount;
    private const int bufferMaxSize = 128;

    private readonly DrawingVisual _drawingVisual;
    
    private ITextInput _textInput;
    private readonly string[] _buffer = new string[bufferMaxSize];
    private int _curBufferSize;
    private int _bufferStartIndex = 0;
    private int _curLineCount = 0;
    private int _linesShownCount;
    
    public TextReaderControl()
    {
        InitializeComponent();
        _drawingVisual = new DrawingVisual();
        TextReaderCanvas.AddVisual(_drawingVisual);
        
        // Calculate line height once
        _lineHeight = CreateFormattedText("Sample").Height;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _visibleLinesCount = (int)Math.Floor((ActualHeight - YOffsetTop) / _lineHeight);
        if (_textInput != null)
        {
            ScrollBar.Maximum = _textInput.LinesCount - _visibleLinesCount;
            ScrollBar.UpdateLayout();
            RenderVisibleLines();
        }
    }

    public FormattedText CreateFormattedText(in string text)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            DefaultFontSize,
            Brushes.Black,
            1.0);
    }

    public void Load(ITextInput textInput)
    {
        Clear();
        _textInput = textInput;
        ScrollBar.Maximum = _textInput.LinesCount - _visibleLinesCount;
        LoadBuffer(0);
        RenderVisibleLines();
    }

    public void Clear()
    {
        RenderVisibleLines();
    }
    
    private void RenderVisibleLines()
    {
        using (DrawingContext dc = _drawingVisual.RenderOpen())
        {
            for (int i = 0, bufferI = _curLineCount - _bufferStartIndex;
                 i < _visibleLinesCount && bufferI < _curBufferSize;
                 i++, bufferI++)
            {
                var formattedText = CreateFormattedText(_buffer[bufferI]);
                double yPos = YOffsetTop + i * _lineHeight;
                dc.DrawText(formattedText, new Point(XOffset, yPos));
            }
        }
    }

    private void Scroll_OnScroll(object sender, ScrollEventArgs e)
    {
        if ((int)ScrollBar.Value == _curLineCount)
        {
            return;
        }

        MoveToIndex((int)ScrollBar.Value);
        RenderVisibleLines();
    }

    private void MoveToIndex(int index)
    {
        _curLineCount = index;
        ScrollBar.Value = _curLineCount;
        if (index <= _bufferStartIndex || index + _visibleLinesCount >= _bufferStartIndex + _curBufferSize)
        {
            LoadBuffer(index);
        }
        RenderVisibleLines();
    }

    private void LoadBuffer(int startIndex)
    {
        _bufferStartIndex = startIndex;
        _curBufferSize = _textInput.GetLines(_bufferStartIndex, bufferMaxSize, _buffer);
    }
}