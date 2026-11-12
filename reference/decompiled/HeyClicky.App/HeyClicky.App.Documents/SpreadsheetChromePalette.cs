using Microsoft.UI;
using Windows.UI;

namespace HeyClicky.App.Documents;

internal static class SpreadsheetChromePalette
{
	public static readonly Color Card = Colors.White;

	public static readonly Color Line = SpreadsheetTextAttributes.Rgb(0.882, 0.89, 0.882);

	public static readonly Color BarFill = SpreadsheetTextAttributes.Rgb(0.973, 0.976, 0.98);

	public static readonly Color PrimaryText = SpreadsheetTextAttributes.Rgb(0.122, 0.122, 0.122);

	public static readonly Color SecondaryText = SpreadsheetTextAttributes.Rgb(0.267, 0.278, 0.275);

	public static readonly Color TertiaryText = SpreadsheetTextAttributes.Rgb(0.459, 0.467, 0.459);

	public static readonly Color SelectedTabFill = SpreadsheetTextAttributes.Rgb(0.882, 0.914, 0.969);

	public static readonly Color SelectedTabText = SpreadsheetTextAttributes.Rgb(0.043, 0.341, 0.816);

	public static readonly Color Link = SpreadsheetTextAttributes.Rgb(0.067, 0.333, 0.8);
}
