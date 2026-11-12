using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.Drawing.Painter;

internal sealed record WindowChromeInfo(double X, double Y, double Width, double Height, double TitleBarHeight, string Title, string Board, Color BoardColor, Color TintColor, double UnitsPerPoint)
{
	public ContainerVisual? Inside { get; set; }
}
