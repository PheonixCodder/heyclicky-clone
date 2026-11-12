using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyPaperGelStyle
{
	public static (double Offset, Color Color)[] Stops(this ClickyPaperGel gel)
	{
		return gel switch
		{
			ClickyPaperGel.Blue => new(double, Color)[5]
			{
				(0.0, PaperColor.Hex("#EDF4FF")),
				(0.18, PaperColor.Hex("#D6E7FF")),
				(0.5, PaperColor.Hex("#A2C7FF")),
				(0.8, PaperColor.Hex("#99CBF8")),
				(1.0, PaperColor.Hex("#A2D2F7"))
			}, 
			ClickyPaperGel.White => new(double, Color)[3]
			{
				(0.0, PaperColor.Hex("#FBFBFB")),
				(0.42, PaperColor.Hex("#EDEDED")),
				(1.0, PaperColor.Hex("#E4E4E4"))
			}, 
			ClickyPaperGel.Butter => new(double, Color)[3]
			{
				(0.0, PaperColor.Hex("#FFF6D2")),
				(0.4, PaperColor.Hex("#FFE48A")),
				(1.0, PaperColor.Hex("#F8CB4A"))
			}, 
			_ => new(double, Color)[3]
			{
				(0.0, PaperColor.Hex("#ECFBEE")),
				(0.45, PaperColor.Hex("#C6EECB")),
				(1.0, PaperColor.Hex("#9BD9A4"))
			}, 
		};
	}

	public static Color Rim(this ClickyPaperGel gel)
	{
		return PaperColor.Hex(gel switch
		{
			ClickyPaperGel.Blue => "#221898", 
			ClickyPaperGel.White => "#626262", 
			ClickyPaperGel.Butter => "#7A4D00", 
			_ => "#1E5E2D", 
		});
	}

	public static Color Words(this ClickyPaperGel gel)
	{
		switch (gel)
		{
		case ClickyPaperGel.Blue:
		case ClickyPaperGel.White:
			return PaperColor.Black.Opacity(0.86);
		case ClickyPaperGel.Butter:
			return PaperColor.Hex("#3A2600");
		default:
			return PaperColor.Hex("#0F2E17");
		}
	}
}
