using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyPaperStickerKindStyle
{
	public static Color Fill(this ClickyPaperStickerKind kind)
	{
		return PaperColor.Hex(kind switch
		{
			ClickyPaperStickerKind.Live => "#2F7CF6", 
			ClickyPaperStickerKind.New => "#1F9D57", 
			_ => "#F5A524", 
		});
	}

	public static Color Words(this ClickyPaperStickerKind kind)
	{
		if (kind != ClickyPaperStickerKind.NeedsYou)
		{
			return PaperColor.White;
		}
		return PaperColor.Hex("#3A2300");
	}
}
