using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TextReader.Models;
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
    
    private LoadedText _loadedText;
    private bool _textInputAssigned = false;
    private readonly string[] _buffer = new string[bufferMaxSize];
    private int _curBufferSize;
    private int _bufferStartIndex = 0;
    private int _curLineCount = 0;
    
    private List<WordPosition> _searchResults = new();
    private int _searchIndex = 0;
    
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

        ScrollToIndex((int)ScrollBar.Value);
        RenderVisibleLines();
    }

    private void ScrollToIndex(int index)
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
        _curBufferSize = _loadedText.GetLines(_bufferStartIndex, bufferMaxSize, _buffer);
    }

    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        string word = SearchTextBox.Text;
        _searchIndex = 0;
        _searchResults = _loadedText.Search(word);
        LoadSearchResult();
    }

    private void SearchUpButton_OnClick(object sender, RoutedEventArgs e)
    {
        //todo handle 0 results
        _searchIndex = 
            _searchIndex == 0 
                ? _searchResults.Count - 1 
                : _searchIndex - 1;
        LoadSearchResult();
    }

    private void SearchDownButton_OnClick(object sender, RoutedEventArgs e)
    {
        _searchIndex = 
            _searchIndex == _searchResults.Count - 1 
                ? 0 
                : _searchIndex + 1;
        LoadSearchResult();
    }
    
    private void LoadSearchResult()
    {
        SearchResultsIndexes.Text = $"{_searchIndex + 1}/{_searchResults.Count}";
        if (_searchResults.Count > 0)
        {
            ScrollToIndex(_searchResults[_searchIndex].lineIndex);
        }
    }

    public void ShowSearchBox() => SearchBar.Visibility = Visibility.Visible;
    public void HideSearchBox() => SearchBar.Visibility = Visibility.Collapsed;

    private void SearchCloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        HideSearchBox();
    }
}