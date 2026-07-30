using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace TextReader;

public partial class TextReaderControl : UserControl
{
    const double DefaultFontSize = 14;
    const double XOffset = 10;
    const double YOffset = 10;
    private double _lineHeight;
    
    private readonly DrawingVisual _drawingVisual;
    
    private readonly List<string> _lines = new();
    private int _curLineCount = 0;
    private int _linesShownCount = 15;
    
    public TextReaderControl()
    {
        InitializeComponent();
        _drawingVisual = new DrawingVisual();
        TextReaderCanvas.AddVisual(_drawingVisual);
        
        // Calculate line height once
        _lineHeight = CreateFormattedText("Sample").Height;
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
    
    public void AppendLines(IEnumerable<string> lines)
    {
        _lines.AddRange(lines);
        RenderVisibleLines();
    }
    
    private void RenderVisibleLines()
    {
        using (DrawingContext dc = _drawingVisual.RenderOpen())
        {
            // lineI = index of which line is being printed, basically _curLineCount + i
            // renderI = index of which line in the reader is being printed. 0 means at the top of the window
            for (int lineI = _curLineCount, renderI = 0;
                 renderI < _linesShownCount && lineI < _lines.Count;
                 lineI++, renderI++)
            {
                var formattedText = CreateFormattedText(_lines[lineI]);
                double yPos = YOffset + renderI * _lineHeight;
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

        _curLineCount = (int)ScrollBar.Value;
        RenderVisibleLines();
    }
}