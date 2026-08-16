using System.Windows.Controls;
using System.Windows.Media;

namespace TextReader;

/// <summary>
/// Custom canvas control that supports direct rendering using DrawingVisual objects
/// Provides low-level rendering capabilities for high-performance text display
/// </summary>
public class RenderCanvas : Canvas
{
    private readonly VisualCollection _visuals;

    public RenderCanvas()
    {
        _visuals = new VisualCollection(this);
    }

    /// <summary>
    /// Adds a visual element to the canvas for rendering
    /// </summary>
    /// <param name="visual">Visual object to add</param>
    public void AddVisual(Visual visual)
    {
        _visuals.Add(visual);
    }

    /// <summary>
    /// Gets the number of visual children in this canvas
    /// </summary>
    protected override int VisualChildrenCount => _visuals.Count;

    /// <summary>
    /// Retrieves a visual child by index
    /// </summary>
    /// <param name="index">Zero-based index of the visual child</param>
    /// <returns>The visual child at the specified index</returns>
    protected override Visual GetVisualChild(int index)
    {
        return _visuals[index];
    }
}