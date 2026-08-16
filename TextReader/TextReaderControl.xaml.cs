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

/// <summary>
/// Custom text reader control with search, selection, and line numbering capabilities
/// </summary>
public partial class TextReaderControl : UserControl
{
    // Layout Constants
    private const double DefaultFontSize = 14;
    private const double LineNumberMargin = 50;
    private const double TextOffsetWithLineNumbers = 60;
    private const double TextOffsetWithoutLineNumbers = 10;
    private const double TopMargin = 10;
    private const int BufferCapacity = 128; // Number of lines to buffer for smooth scrolling

    // UI State
    private bool _searchBoxVisible;
    private bool _showLineNumbers;

    // Rendering
    private double _lineHeight;
    private int _linesPerPage;
    private readonly DrawingVisual _drawingVisual;

    // Search Highlighting Colors
    private static readonly SolidColorBrush ActiveSearchHighlightColor = Brushes.Yellow;
    private static readonly SolidColorBrush InactiveSearchHighlightColor = Brushes.LightGray;
    private static readonly SolidColorBrush SelectionColor = new SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 0, 120, 215));

    // Text Data
    private LoadedText _loadedText;
    private bool _isTextLoaded;
    private readonly string[] _lineBuffer = new string[BufferCapacity];
    private int _currentBufferLineCount;
    private int _bufferStartLineIndex;
    private int _currentTopLineIndex;

    // Search State
    private List<WordPosition> _searchResults = new();
    private int _currentSearchResultIndex;
    private Dictionary<int, List<int>> _searchHighlightsByLine = new();
    private double _searchHighlightWidth;

    // Selection State
    private bool _isSelecting;
    private Point? _selectionStart;
    private Point? _selectionEnd;
    private (int line, int offset)? _selectionStartPos;
    private (int line, int offset)? _selectionEndPos;

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
        _linesPerPage = (int)Math.Floor((ActualHeight - TopMargin) / _lineHeight);
        RerenderUCElements();
    }

    public void Load(ITextInput textInput)
    {
        Clear();
        _loadedText = new LoadedText(textInput);
        _loadedText.FinishedLoadingEvent += (sender, args) => Dispatcher.Invoke(RerenderUCElements);
        _isTextLoaded = true;

        LoadBuffer(0);
        RerenderUCElements();
    }

    public void Clear()
    {
        RenderVisibleLines();
    }

    private void RerenderUCElements()
    {
        if (_isTextLoaded)
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

            double xOffset = _showLineNumbers ? TextOffsetWithLineNumbers : TextOffsetWithoutLineNumbers;

            for (int i = 0, bufferI = _currentTopLineIndex - _bufferStartLineIndex;
                 i < _linesPerPage && bufferI < _currentBufferLineCount;
                 i++, bufferI++)
            {
                double yPos = TopMargin + i * _lineHeight;

                // Render line number
                if (_showLineNumbers)
                {
                    int lineNumber = _currentTopLineIndex + i + 1;
                    var lineNumberText = CreateFormattedText(lineNumber.ToString());
                    lineNumberText.SetForegroundBrush(Brushes.Gray);
                    dc.DrawText(lineNumberText, new Point(10, yPos));
                }

                // Render line content
                var formattedText = CreateFormattedText(_lineBuffer[bufferI]);
                dc.DrawText(formattedText, new Point(xOffset, yPos));
            }
        }
    }

    /// <summary>
    /// Renders selection and search highlights on the canvas
    /// </summary>
    private void RenderHighlights(DrawingContext dc)
    {
        // Render text selection
        RenderSelection(dc);

        double xOffset = _showLineNumbers ? TextOffsetWithLineNumbers : TextOffsetWithoutLineNumbers;

        // Render search result highlights
        foreach ((int lineIndex, List<int> characterOffsets) in _searchHighlightsByLine)
        {
            int relativeIndex = lineIndex - _currentTopLineIndex;
            if (relativeIndex < 0 || relativeIndex >= _linesPerPage)
            {
                continue;
            }

            int bufferIndex = lineIndex - _bufferStartLineIndex;
            string line = _lineBuffer[bufferIndex];
            foreach (var characterOffset in characterOffsets)
            {
                string textBefore = line.Substring(0, characterOffset);
                var leftPadding = CreateFormattedText(textBefore).WidthIncludingTrailingWhitespace;
                var rect = new Rect(
                    xOffset + leftPadding,
                    TopMargin + relativeIndex * _lineHeight,
                    _searchHighlightWidth,
                    _lineHeight);
                var color = _searchResults[_currentSearchResultIndex].LineIndex == lineIndex &&
                            _searchResults[_currentSearchResultIndex].CharacterOffset == characterOffset
                    ? ActiveSearchHighlightColor
                    : InactiveSearchHighlightColor;
                dc.DrawRectangle(color, null, rect);
            }
        }
    }

    private void RenderSelection(DrawingContext dc)
    {
        if (_selectionStartPos == null || _selectionEndPos == null)
            return;

        double xOffset = _showLineNumbers ? TextOffsetWithLineNumbers : TextOffsetWithoutLineNumbers;

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
            int relativeIndex = lineIndex - _currentTopLineIndex;
            if (relativeIndex < 0 || relativeIndex >= _linesPerPage)
                continue;

            int bufferIndex = lineIndex - _bufferStartLineIndex;
            if (bufferIndex < 0 || bufferIndex >= _currentBufferLineCount)
                continue;

            string line = _lineBuffer[bufferIndex];

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
                xOffset + leftPadding,
                TopMargin + relativeIndex * _lineHeight,
                selectionWidth,
                _lineHeight);

            dc.DrawRectangle(SelectionColor, null, rect);
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
        _currentTopLineIndex = index;
        ScrollBar.Value = _currentTopLineIndex;
        if (index <= _bufferStartLineIndex || index + _linesPerPage >= _bufferStartLineIndex + _currentBufferLineCount)
        {
            LoadBuffer(index);
        }
        RenderVisibleLines();
    }

    private void LoadBuffer(int startIndex)
    {
        _bufferStartLineIndex = startIndex;
        _currentBufferLineCount = _loadedText.GetLines(_bufferStartLineIndex, BufferCapacity, _lineBuffer);
    }
    
    /// <summary>
    /// Loads and indexes search results for a given word
    /// </summary>
    private void LoadWordSearchData(string word)
    {
        _searchResults.Clear();
        _searchHighlightsByLine.Clear();

        _searchResults = _loadedText.Search(word);
        _searchHighlightWidth = CreateFormattedText(word).WidthIncludingTrailingWhitespace;
        _currentSearchResultIndex = 0;

        // Group search results by line for efficient rendering
        foreach (var searchResult in _searchResults)
        {
            if (!_searchHighlightsByLine.ContainsKey(searchResult.LineIndex))
            {
                _searchHighlightsByLine[searchResult.LineIndex] = new List<int>();
            }
            _searchHighlightsByLine[searchResult.LineIndex].Add(searchResult.CharacterOffset);
        }
    }
    
    private void ClearSearchResults()
    {
        _searchResults.Clear();
        _searchHighlightsByLine.Clear();
        RerenderUCElements();
    }

    /// <summary>
    /// Scrolls to and highlights the current search result
    /// </summary>
    private void GoToCurSearchResult()
    {
        SearchResultsIndexes.Text = $"{_currentSearchResultIndex + 1}/{_searchResults.Count}";
        if (_searchResults.Count > 0)
        {
            ScrollToIndex(_searchResults[_currentSearchResultIndex].LineIndex);
        }
        RerenderUCElements();
    }
    
    private void GoToNextSearchResult()
    {
        _currentSearchResultIndex++;
        if (_currentSearchResultIndex == _searchResults.Count)
        {
            _currentSearchResultIndex = 0;
        }
        GoToCurSearchResult();
    }
    
    private void GoToPrevSearchResult()
    {
        _currentSearchResultIndex--;
        if (_currentSearchResultIndex == -1)
        {
            _currentSearchResultIndex = _searchResults.Count - 1;
        }        
        GoToCurSearchResult();
    }

    public void ToggleSearchBox()
    {
        _searchBoxVisible = !_searchBoxVisible;
        if (_searchBoxVisible)
        {
            ShowSearchBox();
        }
        else
        {
            HideSearchBox();
        }
    }

    public void ToggleLineNumbers()
    {
        _showLineNumbers = !_showLineNumbers;
        RenderVisibleLines();
    }
    
    public void ShowSearchBox()
    {
        SearchBar.Visibility = Visibility.Visible;
        _searchBoxVisible = true;
    }
    
    public void HideSearchBox()
    {
        SearchBar.Visibility = Visibility.Collapsed;
        _searchBoxVisible = false;
    }


    
    // Scroll
    
    private void Scroll_OnScroll(object sender, ScrollEventArgs e)
    {
        if ((int)ScrollBar.Value == _currentTopLineIndex)
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

    private void SearchTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            string word = SearchTextBox.Text;
            LoadWordSearchData(word);
            GoToCurSearchResult();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            HideSearchBox();
            ClearSearchResults();
            TextReaderCanvas.Focus();
            e.Handled = true;
        }
    }


    // Mouse Selection

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Check for double-click on the left side to toggle line numbers
        if (e.ClickCount == 2)
        {
            Point position = e.GetPosition(TextReaderCanvas);
            double xOffset = _showLineNumbers ? TextOffsetWithLineNumbers : TextOffsetWithoutLineNumbers;

            if (position.X < xOffset)
            {
                ToggleLineNumbers();
                e.Handled = true;
                return;
            }
        }

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
        int relativeLineIndex = (int)Math.Floor((point.Y - TopMargin) / _lineHeight);
        relativeLineIndex = Math.Max(0, Math.Min(relativeLineIndex, _linesPerPage - 1));
        int lineIndex = _currentTopLineIndex + relativeLineIndex;

        int bufferIndex = lineIndex - _bufferStartLineIndex;
        if (bufferIndex < 0 || bufferIndex >= _currentBufferLineCount)
            return (lineIndex, 0);

        string line = _lineBuffer[bufferIndex];

        // Calculate line offset from X position
        double xOffset = _showLineNumbers ? TextOffsetWithLineNumbers : TextOffsetWithoutLineNumbers;
        double targetX = point.X - xOffset;
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
            int bufferIndex = lineIndex - _bufferStartLineIndex;
            if (bufferIndex < 0 || bufferIndex >= _currentBufferLineCount)
                continue;

            string line = _lineBuffer[bufferIndex];

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

    private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        int linesToScroll = -e.Delta / 40; // Delta is typically 120 per notch, scroll ~3 lines per notch
        int newIndex = _currentTopLineIndex + linesToScroll;

        newIndex = Math.Max(0, Math.Min(newIndex, (int)(_loadedText.LinesCount - _linesPerPage)));

        if (newIndex != _currentTopLineIndex)
        {
            ScrollToIndex(newIndex);
        }

        e.Handled = true;
    }

    private async void ShowCopyNotification()
    {
        CopyNotification.Visibility = Visibility.Visible;
        await Task.Delay(1500);
        CopyNotification.Visibility = Visibility.Collapsed;
    }

    private void CopySelectedText()
    {
        string selectedText = GetSelectedText();
        if (!string.IsNullOrEmpty(selectedText))
        {
            Clipboard.SetText(selectedText);
            ShowCopyNotification();
        }
    }

    private void Canvas_KeyDown(object sender, KeyEventArgs e)
    {
        //copy
        if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            CopySelectedText();
            e.Handled = true;
        }
        // search
        else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ToggleSearchBox();
            e.Handled = true;
        }
        // toggle line numbers
        else if (e.Key == Key.L && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ToggleLineNumbers();
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
            e.Handled = true;
        }
        // end of document
        else if (e.Key == Key.End)
        {
            ScrollToIndex((int)(_loadedText.LinesCount - _linesPerPage));
            e.Handled = true;
        }
        // prev page
        else if (e.Key == Key.PageUp)
        {
            int index = _currentTopLineIndex - _linesPerPage;
            if (index < 0)
                index = 0;
            ScrollToIndex(index);
            e.Handled = true;
        }
        // next page
        else if (e.Key == Key.PageDown)
        {
            int index = _currentTopLineIndex + _linesPerPage;
            if (index > _loadedText.LinesCount - _linesPerPage)
                index = (int)(_loadedText.LinesCount - _linesPerPage);
            ScrollToIndex(index);
            e.Handled = true;
        }
    }
}