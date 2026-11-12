using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkLayout
{
	public static readonly Size PageSize = new Size(450f, 560f);

	public const double ContentLeft = 36.0;

	public const double WindowPreviewContentLeft = 22.0;

	public const double ContentTop = 78.0;

	public const double ContentWidth = 378.0;

	public const double ContentBottom = 26.0;

	public static double ContentTopFor(bool carriesTitle, bool isContinuation)
	{
		if (!carriesTitle)
		{
			if (!isContinuation)
			{
				return 78.0;
			}
			return 60.0;
		}
		return 92.0;
	}
}
