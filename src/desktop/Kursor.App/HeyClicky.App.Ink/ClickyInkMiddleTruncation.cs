using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;

namespace HeyClicky.App.Ink;

public static class ClickyInkMiddleTruncation
{
	public static string Fit(string text, PaperFont font, double width)
	{
		if (text.Length == 0 || Width(text, font) <= width)
		{
			return text;
		}
		string[] array = text.Characters();
		int num = 0;
		int num2 = array.Length;
		string result = "…";
		while (num <= num2)
		{
			int num3 = (num + num2) / 2;
			int count = (num3 + 1) / 2;
			int num4 = num3 / 2;
			string text2 = string.Concat(string.Concat(array.Take(count)), "…", string.Concat(array.Skip(array.Length - num4)));
			if (Width(text2, font) <= width)
			{
				result = text2;
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return result;
	}

	private static double Width(string text, PaperFont font)
	{
		using CanvasTextFormat textFormat = font.TextFormat();
		using CanvasTextLayout canvasTextLayout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, textFormat, 100000f, 100000f);
		return canvasTextLayout.LayoutBoundsIncludingTrailingWhitespace.Width;
	}
}
