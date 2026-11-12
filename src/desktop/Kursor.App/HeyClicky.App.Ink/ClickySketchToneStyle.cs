using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickySketchToneStyle
{
	public static Color Fill(this ClickySketchTone tone)
	{
		return PaperColor.Hex(tone switch
		{
			ClickySketchTone.Blue1 => "#D3E5FA", 
			ClickySketchTone.Blue2 => "#EEF5FD", 
			ClickySketchTone.Blue3 => "#ECF0FC", 
			ClickySketchTone.Blue4 => "#ECF6FB", 
			_ => "#E7EEF8", 
		});
	}

	public static Color Pencil(this ClickySketchTone tone)
	{
		return PaperColor.Hex(tone switch
		{
			ClickySketchTone.Blue1 => "#5F84B4", 
			ClickySketchTone.Blue2 => "#86A2C6", 
			ClickySketchTone.Blue3 => "#8592C4", 
			ClickySketchTone.Blue4 => "#83A6C1", 
			_ => "#9AB0CB", 
		});
	}

	public static Color Words(this ClickySketchTone tone)
	{
		return PaperColor.Hex(tone switch
		{
			ClickySketchTone.Blue1 => "#1F3657", 
			ClickySketchTone.Blue2 => "#29436A", 
			ClickySketchTone.Blue3 => "#2C3A68", 
			ClickySketchTone.Blue4 => "#264660", 
			_ => "#33507A", 
		});
	}

	public static Color SoftWords(this ClickySketchTone tone)
	{
		return PaperColor.Hex(tone switch
		{
			ClickySketchTone.Blue1 => "#52729A", 
			ClickySketchTone.Blue2 => "#62809F", 
			ClickySketchTone.Blue3 => "#64729F", 
			ClickySketchTone.Blue4 => "#5B7E97", 
			_ => "#6A84A6", 
		});
	}
}
