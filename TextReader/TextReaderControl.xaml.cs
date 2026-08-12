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
    const double DefaultFontSize = 14;
    const double XOffset = 10;
    const double YOffsetTop = 10;
    private double _lineHeight;
    private int _visibleLinesCount;
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
            ScrollBar.Maximum = _loadedText.LinesCount - _visibleLinesCount + 1;
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
                 i < _visibleLinesCount && bufferI < _curBufferSize;
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
        foreach ((int lineIndex, List<long> lineOffsets) in _searchHighlights)
        {
            int relativeIndex = lineIndex - _curLine;
            if (relativeIndex < 0 || relativeIndex >= _visibleLinesCount)
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
        if (index <= _bufferStartIndex || index + _visibleLinesCount >= _bufferStartIndex + _curBufferSize)
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
    
    public void ShowSearchBox() => SearchBar.Visibility = Visibility.Visible;
    public void HideSearchBox() => SearchBar.Visibility = Visibility.Collapsed;


    
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
}