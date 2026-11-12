using HeyClicky.App.Design;
using HeyClicky.Core.Onboarding;
using Windows.UI;

namespace HeyClicky.App.OneBrain;

public static class TourPaperStocks
{
	public static TourPaperStock? Parse(string? raw)
	{
		return raw?.Trim().ToLowerInvariant() switch
		{
			"sky" => TourPaperStock.Sky, 
			"butter" => TourPaperStock.Butter, 
			"mint" => TourPaperStock.Mint, 
			"periwinkle" => TourPaperStock.Periwinkle, 
			"peach" => TourPaperStock.Peach, 
			"blush" => TourPaperStock.Blush, 
			"kraft" => TourPaperStock.Kraft, 
			"cream" => TourPaperStock.Cream, 
			_ => null, 
		};
	}

	public static TourPaperStock ForTitle(string title, string summary = "")
	{
		return Parse(DrawnTourBriefs.PaperForTitle(title, summary)).GetValueOrDefault();
	}

	public static Color Paper(this TourPaperStock s)
	{
		return s switch
		{
			TourPaperStock.Sky => DS.Hex(15462909u), 
			TourPaperStock.Butter => DS.Hex(16578019u), 
			TourPaperStock.Mint => DS.Hex(15333100u), 
			TourPaperStock.Periwinkle => DS.Hex(15789821u), 
			TourPaperStock.Peach => DS.Hex(16641765u), 
			TourPaperStock.Blush => DS.Hex(16574961u), 
			TourPaperStock.Kraft => DS.Hex(16051938u), 
			_ => DS.Hex(16513004u), 
		};
	}

	public static Color Ink(this TourPaperStock s)
	{
		return s switch
		{
			TourPaperStock.Sky => DS.Hex(924191u), 
			TourPaperStock.Butter => DS.Hex(1840646u), 
			TourPaperStock.Mint => DS.Hex(992279u), 
			TourPaperStock.Periwinkle => DS.Hex(1317432u), 
			TourPaperStock.Peach => DS.Hex(2955531u), 
			TourPaperStock.Blush => DS.Hex(2888728u), 
			TourPaperStock.Kraft => DS.Hex(2826516u), 
			_ => DS.Hex(2827808u), 
		};
	}
}
