using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkEdgeCardLayout
{
	public const double FooterHeight = 46.0;

	public const double TitleBarHeight = 34.0;

	public const double Bezel = 6.0;

	public const double ScreenCornerRadius = 12.0;

	public const double OpenedFooterHeight = 60.0;

	public static Size SheetSize => ClickyInkLayout.PageSize;

	public static Size Size => SizeFor(SheetSize);

	public static Size SizeFor(Size sheet, double footerHeight = 46.0)
	{
		return new Size(sheet.Width + 12.0, 34.0 + sheet.Height + footerHeight + 12.0);
	}

	public static Size SheetFor(Size card, double footerHeight = 46.0)
	{
		return new Size(card.Width - 12.0, card.Height - 34.0 - footerHeight - 12.0);
	}
}
