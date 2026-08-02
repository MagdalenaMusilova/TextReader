using System.Windows.Controls;
using System.Windows.Media;

namespace TextReader;

public class RenderCanvas : Canvas
{
    private readonly VisualCollection _visuals;

    public RenderCanvas()
    {
        _visuals = new VisualCollection(this);
    }

    public void AddVisual(Visual visual)
    {
        _visuals.Add(visual);
    }

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return _visuals[index];
    }
}