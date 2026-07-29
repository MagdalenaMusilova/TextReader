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
            for (int i = 0; i < _lines.Count; i++)
            {
                var formattedText = CreateFormattedText(_lines[i]);
                double yPos = YOffset + i * _lineHeight;
                dc.DrawText(formattedText, new Point(XOffset, yPos));
            }
        }
    }
}