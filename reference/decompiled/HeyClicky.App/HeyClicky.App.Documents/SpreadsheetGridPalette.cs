using Microsoft.UI;
using Windows.UI;

namespace HeyClicky.App.Documents;

public static class SpreadsheetGridPalette
{
	public static readonly Color Paper = Colors.White;

	public static readonly Color GridLine = SpreadsheetTextAttributes.Rgb(0.886, 0.89, 0.89);

	public static readonly Color HeaderFill = SpreadsheetTextAttributes.Rgb(0.973, 0.976, 0.98);

	public static readonly Color HeaderText = SpreadsheetTextAttributes.Rgb(0.267, 0.278, 0.275);

	public static readonly Color HeaderLine = SpreadsheetTextAttributes.Rgb(0.769, 0.78, 0.773);

	public static readonly Color HeaderSelectedFill = SpreadsheetTextAttributes.Rgb(0.827, 0.89, 0.992);

	public static readonly Color HeaderSelectedText = SpreadsheetTextAttributes.Rgb(0.016, 0.118, 0.286);

	public static readonly Color HeaderFullySelectedFill = SpreadsheetTextAttributes.Rgb(0.043, 0.341, 0.816);

	public static readonly Color SelectionBorder = SpreadsheetTextAttributes.Rgb(0.102, 0.451, 0.91);

	public static readonly Color SelectionFill = SpreadsheetTextAttributes.Rgb(0.055, 0.396, 0.922, 0.09);

	public static readonly Color FrozenDivider = SpreadsheetTextAttributes.Rgb(0.737, 0.757, 0.776);
}
