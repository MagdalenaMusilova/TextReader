using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TextReader.Models;
using TextReader.TextInputs;
using Color = System.Drawing.Color;

namespace TextReader;

public partial class TextReaderControl : UserControl
{
    public bool searchBoxVisible = false;
    
    const double DefaultFontSize = 14;
    const double XOffset = 10;
    const double YOffsetTop = 10;
    private double _lineHeight;
    private int _linesPerPage;
    private const int bufferMaxSize = 128;

    private static readonly SolidColorBrush _search_highlight_color = Brushes.Yellow;
    private static readonly SolidColorBrush _search_results_color = Brushes.LightGray;

    private readonly DrawingVisual _drawingVisual;
    
    private LoadedText _loadedText;
    private bool _textInputAssigned = false;
    private readonly string[] _buffer = new string[bufferMaxSize];
    private int _curBufferSize;
    private int _bufferStartIndex = 0;
    private int _curLine = 0;
    
    private List<WordPosition> _searchResults = new();
    private int _searchIndex = 0;
    
    private Dictionary<int, List<long>> _searchHighlights = new();
    private double _searchHighlightWidth = 0;

    // Selection
    private bool _isSelecting = false;
    private Point? _selectionStart = null;
    private Point? _selectionEnd = null;
    private (int line, int offset)? _selectionStartPos = null;
    private (int line, int offset)? _selectionEndPos = null;

    private static readonly SolidColorBrush _selection_color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 120, 215));

    public TextReaderControl()
    {
        InitializeComponent();
        _drawingVisual = new DrawingVisual();
        TextReaderCanvas.AddVisual(_drawingVisual);

        // Calculate line height once
        _lineHeight = CreateFormattedText("Sample").Height;

        // Setup mouse events for selection
        TextReaderCanvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
        TextReaderCanvas.MouseMove += Canvas_MouseMove;
        TextReaderCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;

        // Setup keyboard events for copying
        TextReaderCanvas.KeyDown += Canvas_KeyDown;

        // Setup mouse wheel scrolling
        TextReaderCanvas.MouseWheel += Canvas_MouseWheel;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _linesPerPage = (int)Math.Floor((ActualHeight - YOffsetTop) / _lineHeight);
        RerenderUCElements();
    }

    public void Load(ITextInput textInput)
    {
        Clear();
        _loadedText = new LoadedText(textInput);
        _loadedText.FinishedLoadingEvent += (sender, args) => RerenderUCElements();
        _textInputAssigned = true;
        
        LoadBuffer(0);
        RerenderUCElements();
    }

    public void Clear()
    {
        RenderVisibleLines();
    }

    private void RerenderUCElements()
    {
        if (_textInputAssigned)
        {
            ScrollBar.Maximum = _loadedText.LinesCount - _linesPerPage + 1;
            ScrollBar.UpdateLayout();
            RenderVisibleLines();
        }
    }
    
    private void RenderVisibleLines()
    {
        using (DrawingContext dc = _drawingVisual.RenderOpen())
        {
            RenderHighlights(dc);

            for (int i = 0, bufferI = _curLine - _bufferStartIndex;
                 i < _linesPerPage && bufferI < _curBufferSize;
                 i++, bufferI++)
            {
                var formattedText = CreateFormattedText(_buffer[bufferI]);
                double yPos = YOffsetTop + i * _lineHeight;
                dc.DrawText(formattedText, new Point(XOffset, yPos));
            }
        }
    }

    private void RenderHighlights(DrawingContext dc)
    {
        // Render selection
        RenderSelection(dc);

        // Render search highlights
        foreach ((int lineIndex, List<long> lineOffsets) in _searchHighlights)
        {
            int relativeIndex = lineIndex - _curLine;
            if (relativeIndex < 0 || relativeIndex >= _linesPerPage)
            {
                continue;
            }

            int bufferIndex = lineIndex - _bufferStartIndex;
            string line = _buffer[bufferIndex];
            foreach (var lineOffset in lineOffsets)
            {
                string textBefore = line.Substring(0, (int)lineOffset);
                var leftPadding = CreateFormattedText(textBefore).WidthIncludingTrailingWhitespace;
                var rect = new Rect(
                    XOffset + leftPadding,
                    YOffsetTop + relativeIndex * _lineHeight,
                    _searchHighlightWidth,
                    _lineHeight);
                var color = _searchResults[_searchIndex].lineIndex == lineIndex &&
                            _searchResults[_searchIndex].lineOffset == lineOffset
                    ? _search_highlight_color
                    : _search_results_color;
                dc.DrawRectangle(color, null, rect);
            }
        }
    }

    private void RenderSelection(DrawingContext dc)
    {
        if (_selectionStartPos == null || _selectionEndPos == null)
            return;

        var start = _selectionStartPos.Value;
        var end = _selectionEndPos.Value;

        // Ensure start is before end
        if (start.line > end.line || (start.line == end.line && start.offset > end.offset))
        {
            (start, end) = (end, start);
        }

        // Render selection for each visible line
        for (int lineIndex = start.line; lineIndex <= end.line; lineIndex++)
        {
            int relativeIndex = lineIndex - _curLine;
            if (relativeIndex < 0 || relativeIndex >= _linesPerPage)
                continue;

            int bufferIndex = lineIndex - _bufferStartIndex;
            if (bufferIndex < 0 || bufferIndex >= _curBufferSize)
                continue;

            string line = _buffer[bufferIndex];

            int startOffset = (lineIndex == start.line) ? start.offset : 0;
            int endOffset = (lineIndex == end.line) ? end.offset : line.Length;

            if (startOffset >= line.Length)
                continue;

            endOffset = Math.Min(endOffset, line.Length);

            string textBefore = line.Substring(0, startOffset);
            string selectedText = line.Substring(startOffset, endOffset - startOffset);

            var leftPadding = CreateFormattedText(textBefore).WidthIncludingTrailingWhitespace;
            var selectionWidth = CreateFormattedText(selectedText).WidthIncludingTrailingWhitespace;

            var rect = new Rect(
                XOffset + leftPadding,
                YOffsetTop + relativeIndex * _lineHeight,
                selectionWidth,
                _lineHeight);

            dc.DrawRectangle(_selection_color, null, rect);
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

    private void ScrollToIndex(int index)
    {
        _curLine = index;
        ScrollBar.Value = _curLine;
        if (index <= _bufferStartIndex || index + _linesPerPage >= _bufferStartIndex + _curBufferSize)
        {
            LoadBuffer(index);
        }
        RenderVisibleLines();
    }

    private void LoadBuffer(int startIndex)
    {
        _bufferStartIndex = startIndex;
        _curBufferSize = _loadedText.GetLines(_bufferStartIndex, bufferMaxSize, _buffer);
    }
    
    private void LoadWordSearchData(string word)
    {
        _searchResults.Clear();
        _searchHighlights.Clear();
        
        _searchResults = _loadedText.Search(word);
        _searchHighlightWidth = CreateFormattedText(word).WidthIncludingTrailingWhitespace;
        _searchIndex = 0;
        
        // add higlights
        foreach (var searchResult in _searchResults)
        {
            if (!_searchHighlights.ContainsKey(searchResult.lineIndex))
            {
                _searchHighlights[searchResult.lineIndex] = new List<long>();
            }
            _searchHighlights[searchResult.lineIndex].Add(searchResult.lineOffset);
        }
    }
    
    private void ClearSearchResults()
    {
        _searchResults.Clear();
        _searchHighlights.Clear();
        RerenderUCElements();
    }

    private void GoToCurSearchResult()
    {
        SearchResultsIndexes.Text = $"{_searchIndex + 1}/{_searchResults.Count}";
        if (_searchResults.Count > 0)
        {
            ScrollToIndex(_searchResults[_searchIndex].lineIndex);
        }
        RerenderUCElements();
    }
    
    private void GoToNextSearchResult()
    {
        _searchIndex++;
        if (_searchIndex == _searchResults.Count)
        {
            _searchIndex = 0;
        }
        GoToCurSearchResult();
    }
    
    private void GoToPrevSearchResult()
    {
        _searchIndex--;
        if (_searchIndex == -1)
        {
            _searchIndex = _searchResults.Count - 1;
        }        
        GoToCurSearchResult();
    }

    public void ToggleSearchBox()
    {
        searchBoxVisible = !searchBoxVisible;
        if (searchBoxVisible)
        {
            ShowSearchBox();
        }
        else
        {
            HideSearchBox();
        }
    }
    
    public void ShowSearchBox()
    {
        SearchBar.Visibility = Visibility.Visible;
        searchBoxVisible = true;
    }
    
    public void HideSearchBox()
    {
        SearchBar.Visibility = Visibility.Collapsed;
        searchBoxVisible = false;
    }


    
    // Scroll
    
    private void Scroll_OnScroll(object sender, ScrollEventArgs e)
    {
        if ((int)ScrollBar.Value == _curLine)
        {
            return;
        }

        ScrollToIndex((int)ScrollBar.Value);
        RenderVisibleLines();
    }
    
    
    // Search button
    
    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        string word = SearchTextBox.Text;
        LoadWordSearchData(word);
        GoToCurSearchResult();
    }
    
    private void SearchCloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        HideSearchBox();
        ClearSearchResults();
    }
    
    private void SearchUpButton_OnClick(object sender, RoutedEventArgs e)
    {
        GoToPrevSearchResult();
    }

    private void SearchDownButton_OnClick(object sender, RoutedEventArgs e)
    {
        GoToNextSearchResult();
    }


    // Mouse Selection

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        TextReaderCanvas.Focus();
        _isSelecting = true;
        _selectionStart = e.GetPosition(TextReaderCanvas);
        _selectionEnd = _selectionStart;
        _selectionStartPos = GetTextPositionFromPoint(_selectionStart.Value);
        _selectionEndPos = _selectionStartPos;
        TextReaderCanvas.CaptureMouse();
        RenderVisibleLines();
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isSelecting && e.LeftButton == MouseButtonState.Pressed)
        {
            _selectionEnd = e.GetPosition(TextReaderCanvas);
            _selectionEndPos = GetTextPositionFromPoint(_selectionEnd.Value);
            RenderVisibleLines();
        }
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;
            TextReaderCanvas.ReleaseMouseCapture();
        }
    }

    private (int line, int offset) GetTextPositionFromPoint(Point point)
    {
        // Calculate line from Y position
        int relativeLineIndex = (int)Math.Floor((point.Y - YOffsetTop) / _lineHeight);
        relativeLineIndex = Math.Max(0, Math.Min(relativeLineIndex, _linesPerPage - 1));
        int lineIndex = _curLine + relativeLineIndex;

        int bufferIndex = lineIndex - _bufferStartIndex;
        if (bufferIndex < 0 || bufferIndex >= _curBufferSize)
            return (lineIndex, 0);

        string line = _buffer[bufferIndex];

        // Calculate line offset from X position
        double targetX = point.X - XOffset;
        int offset = 0;

        for (int i = 0; i <= line.Length; i++)
        {
            string substr = line.Substring(0, i);
            double width = CreateFormattedText(substr).WidthIncludingTrailingWhitespace;

            if (width >= targetX)
            {
                offset = i;
                break;
            }
            offset = i;
        }

        return (lineIndex, offset);
    }

    private string GetSelectedText()
    {
        if (_selectionStartPos == null || _selectionEndPos == null)
            return string.Empty;

        var start = _selectionStartPos.Value;
        var end = _selectionEndPos.Value;

        // Ensure start is before end
        if (start.line > end.line || (start.line == end.line && start.offset > end.offset))
        {
            (start, end) = (end, start);
        }

        if (start.line == end.line && start.offset == end.offset)
            return string.Empty;

        var result = new System.Text.StringBuilder();

        for (int lineIndex = start.line; lineIndex <= end.line; lineIndex++)
        {
            int bufferIndex = lineIndex - _bufferStartIndex;
            if (bufferIndex < 0 || bufferIndex >= _curBufferSize)
                continue;

            string line = _buffer[bufferIndex];

            int startOffset = (lineIndex == start.line) ? start.offset : 0;
            int endOffset = (lineIndex == end.line) ? end.offset : line.Length;

            if (startOffset >= line.Length)
                continue;

            endOffset = Math.Min(endOffset, line.Length);

            string selectedPart = line.Substring(startOffset, endOffset - startOffset);
            result.Append(selectedPart);

            if (lineIndex < end.line)
                result.AppendLine();
        }

        return result.ToString();
    }

    private void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        //copy
        if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            string selectedText = GetSelectedText();
            if (!string.IsNullOrEmpty(selectedText))
            {
                Clipboard.SetText(selectedText);
            }
            e.Handled = true;
        }
        // search
        else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ToggleSearchBox();
            e.Handled = true;
        }
        // prev/next search res
        else if (e.Key == Key.F3)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                GoToPrevSearchResult();
            }
            else
            {
                GoToNextSearchResult();
            }
            e.Handled = true;
        }
        // top of document
        else if (e.Key == Key.Home)
        {
            ScrollToIndex(0);
        } 
        // end of document
        else if (e.Key == Key.End)
        {
            ScrollToIndex((int)(_loadedText.LinesCount - _linesPerPage));
        } 
        // prev page
        else if (e.Key == Key.PageUp)
        {
            int index = _curLine - _linesPerPage;
            if (index < 0)
                index = 0;
            ScrollToIndex(index);
        } 
        // next page
        else if (e.Key == Key.PageDown)
        {
            int index = _curLine + _linesPerPage;
            if (index > _loadedText.LinesCount - _linesPerPage)
                index = (int)(_loadedText.LinesCount - _linesPerPage);
            ScrollToIndex(index);
        }
    }
}