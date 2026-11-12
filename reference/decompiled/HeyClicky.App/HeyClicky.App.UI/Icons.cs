using HeyClicky.App.Design;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.UI;

public static class Icons
{
	public const string XMark = "\ue711";

	public const string Check = "\ue73e";

	public const string ArrowUp = "\ue74a";

	public const string Paperclip = "\ue723";

	public const string Copy = "\ue8c8";

	public const string Mic = "\ue720";

	public const string MicOff = "\uf781";

	public const string Speaker = "\ue767";

	public const string SpeakerMute = "\ue74f";

	public const string Play = "\ue768";

	public const string Pause = "\ue769";

	public const string Clock = "\ue823";

	public const string Link = "\ue71b";

	public const string Calendar = "\ue787";

	public const string ChevronUp = "\ue70e";

	public const string ChevronDown = "\ue70d";

	public const string ChevronLeft = "\ue76b";

	public const string ChevronRight = "\ue76c";

	public const string More = "\ue712";

	public const string Refresh = "\ue72c";

	public const string Settings = "\ue713";

	public const string Info = "\ue946";

	public const string Keyboard = "\ue765";

	public const string Video = "\ue714";

	public const string People = "\ue716";

	public const string Warning = "\ue7ba";

	public const string Send = "\ue724";

	public const string Pen = "\ue70f";

	public const string Lightbulb = "\ue82f";

	public const string OpenOut = "\ue8a7";

	public const string Stop = "\ue71a";

	public const string Window = "\ue737";

	public const string Upload = "\ue898";

	public const string AddFile = "\ue8e5";

	public const string Share = "\ue72d";

	public const string Sparkle = "\uf4a5";

	public const string Terminal = "\ue756";

	public const string Folder = "\ue8b7";

	public const string Expand = "\ue740";

	public const string Download = "\ue896";

	public const string Globe = "\ue774";

	public const string Home = "\ue80f";

	public const string Newspaper = "\ue736";

	public const string PlaySolid = "\uf5b0";

	public const string VideoSolid = "\uea0c";

	public const string MicSolid = "\uec71";

	public const string CheckCircleSolid = "\uec61";

	public const string ArrowUpToLine = "\ue898";

	public const string WindowOnRectangle = "\ue7c4";

	public const string ArrowUpRightPath = "M5,19 L19,5 M8,5 L19,5 L19,16";

	public const string SparklesPath = "M10,3 C10.6,7.6 12.4,9.4 17,10 C12.4,10.6 10.6,12.4 10,17 C9.4,12.4 7.6,10.6 3,10 C7.6,9.4 9.4,7.6 10,3 Z M18,13 C18.3,15.2 18.8,15.7 21,16 C18.8,16.3 18.3,16.8 18,19 C17.7,16.8 17.2,16.3 15,16 C17.2,15.7 17.7,15.2 18,13 Z M17,2 C17.2,3.4 17.6,3.8 19,4 C17.6,4.2 17.2,4.6 17,6 C16.8,4.6 16.4,4.2 15,4 C16.4,3.8 16.8,3.4 17,2 Z";

	public const string WaveformPath = "M3,10 h2 v4 h-2 z M7,7 h2 v10 h-2 z M11,3 h2 v18 h-2 z M15,6 h2 v12 h-2 z M19,9 h2 v6 h-2 z";

	public static Path PathIcon(string data, double size, Color color)
	{
		Geometry data2 = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), data);
		return new Path
		{
			Data = data2,
			Fill = Ui.Brush(color),
			Width = size,
			Height = size,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
	}

	public static Viewbox Vector(double size, Color color, string? fill, string? stroke = null, double strokeWidth = 2.0)
	{
		Grid grid = new Grid
		{
			Width = 24.0,
			Height = 24.0
		};
		if (fill != null)
		{
			grid.Children.Add(new Path
			{
				Data = Parse(fill),
				Fill = Ui.Brush(color)
			});
		}
		if (stroke != null)
		{
			grid.Children.Add(new Path
			{
				Data = Parse(stroke),
				Stroke = Ui.Brush(color),
				StrokeThickness = strokeWidth,
				StrokeStartLineCap = PenLineCap.Round,
				StrokeEndLineCap = PenLineCap.Round,
				StrokeLineJoin = PenLineJoin.Round
			});
		}
		return new Viewbox
		{
			Width = size,
			Height = size,
			Child = grid,
			Stretch = Stretch.Uniform,
			IsHitTestVisible = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		static Geometry Parse(string d)
		{
			return (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), d);
		}
	}

	public static Path StrokeIcon(string data, double size, Color color, double strokeThickness)
	{
		Geometry data2 = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), data);
		return new Path
		{
			Data = data2,
			Stroke = Ui.Brush(color),
			StrokeThickness = strokeThickness,
			StrokeStartLineCap = PenLineCap.Round,
			StrokeEndLineCap = PenLineCap.Round,
			StrokeLineJoin = PenLineJoin.Round,
			Width = size,
			Height = size,
			Stretch = Stretch.Uniform,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
	}

	public static TextBlock Glyph(string glyph, double size, Color color)
	{
		return new TextBlock
		{
			Text = glyph,
			FontFamily = DS.Fonts.Symbols,
			FontSize = size,
			Foreground = Ui.Brush(color),
			IsTextScaleFactorEnabled = false,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center
		};
	}
}
