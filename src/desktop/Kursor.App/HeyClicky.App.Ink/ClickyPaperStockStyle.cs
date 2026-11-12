using System;
using System.Linq;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyPaperStockStyle
{
	public static readonly ClickyPaperStock[] AllCases = Enum.GetValues<ClickyPaperStock>();

	public static string RawValue(this ClickyPaperStock stock)
	{
		return stock.ToString().ToLowerInvariant();
	}

	public static ClickyPaperStock? FromRawValue(string? raw)
	{
		return AllCases.Cast<ClickyPaperStock?>().FirstOrDefault((ClickyPaperStock? s) => s.Value.RawValue() == raw);
	}

	public static Color Paper(this ClickyPaperStock stock)
	{
		return PaperColor.Hex(stock switch
		{
			ClickyPaperStock.Sky => "#EBF1FD", 
			ClickyPaperStock.Butter => "#FCF5E3", 
			ClickyPaperStock.Mint => "#E9F6EC", 
			ClickyPaperStock.Periwinkle => "#F0EEFD", 
			ClickyPaperStock.Peach => "#FDEEE5", 
			ClickyPaperStock.Blush => "#FCE9F1", 
			ClickyPaperStock.Kraft => "#F4EEE2", 
			_ => "#FBF7EC", 
		});
	}

	public static Color Wash(this ClickyPaperStock stock)
	{
		return PaperColor.Hex(stock switch
		{
			ClickyPaperStock.Sky => "#D7E5FF", 
			ClickyPaperStock.Butter => "#FEEEC7", 
			ClickyPaperStock.Mint => "#D3F0DC", 
			ClickyPaperStock.Periwinkle => "#E3DEFF", 
			ClickyPaperStock.Peach => "#FFDDC9", 
			ClickyPaperStock.Blush => "#FFC7E1", 
			ClickyPaperStock.Kraft => "#EEE3CF", 
			_ => "#F3E9CC", 
		});
	}

	public static Color Ink(this ClickyPaperStock stock)
	{
		return PaperColor.Hex(stock switch
		{
			ClickyPaperStock.Sky => "#0E1A1F", 
			ClickyPaperStock.Butter => "#1C1606", 
			ClickyPaperStock.Mint => "#0F2417", 
			ClickyPaperStock.Periwinkle => "#141A38", 
			ClickyPaperStock.Peach => "#2D190B", 
			ClickyPaperStock.Blush => "#2C1418", 
			ClickyPaperStock.Kraft => "#2B2114", 
			_ => "#2B2620", 
		});
	}

	public static Color Deep(this ClickyPaperStock stock)
	{
		return PaperColor.Hex(stock switch
		{
			ClickyPaperStock.Sky => "#1B3A8C", 
			ClickyPaperStock.Butter => "#5A3A00", 
			ClickyPaperStock.Mint => "#1D5A33", 
			ClickyPaperStock.Periwinkle => "#2B3990", 
			ClickyPaperStock.Peach => "#8A3B0F", 
			ClickyPaperStock.Blush => "#7A1F35", 
			ClickyPaperStock.Kraft => "#5A3D16", 
			_ => "#2D5BE3", 
		});
	}

	public static Color QuietInk(this ClickyPaperStock stock)
	{
		return stock.Ink().Opacity(0.58);
	}
}
