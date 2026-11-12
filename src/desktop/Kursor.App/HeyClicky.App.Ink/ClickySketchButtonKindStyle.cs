using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickySketchButtonKindStyle
{
	public static Color Fill(this ClickySketchButtonKind kind)
	{
		return kind switch
		{
			ClickySketchButtonKind.Teal => ClickySketchInk.Teal, 
			ClickySketchButtonKind.Plain => PaperColor.White.Opacity(0.92), 
			_ => PaperColor.White.Opacity(0.55), 
		};
	}

	public static Color Pencil(this ClickySketchButtonKind kind)
	{
		return kind switch
		{
			ClickySketchButtonKind.Teal => ClickySketchInk.TealLine, 
			ClickySketchButtonKind.Plain => PaperColor.Hex("#8FA9C9"), 
			_ => PaperColor.Hex("#9AA3B5"), 
		};
	}

	public static Color Words(this ClickySketchButtonKind kind)
	{
		return kind switch
		{
			ClickySketchButtonKind.Teal => ClickySketchInk.TealWords, 
			ClickySketchButtonKind.Plain => PaperColor.Hex("#2E4A72"), 
			_ => PaperColor.Hex("#4A5570"), 
		};
	}
}
