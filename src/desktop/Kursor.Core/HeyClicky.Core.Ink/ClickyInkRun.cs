using Windows.UI;

namespace HeyClicky.Core.Ink;

public readonly record struct ClickyInkRun(string Text, Color? Foreground = null, bool IsBold = false, Color? Background = null, Color? Strike = null, bool IsLink = false, Color? LinkTint = null, string? Symbol = null)
{
	public bool IsClear
	{
		get
		{
			Color? foreground = Foreground;
			if (foreground.HasValue)
			{
				return foreground.GetValueOrDefault().A == 0;
			}
			return false;
		}
	}

	public static readonly Color Clear = Color.FromArgb(0, 0, 0, 0);
}
