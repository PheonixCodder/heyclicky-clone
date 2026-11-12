using Windows.UI;

namespace HeyClicky.App.Ink;

public readonly record struct ClickyHomeSpreadPaper(ClickyPaperStock? Stock)
{
	public Color Ink
	{
		get
		{
			ClickyPaperStock? stock = Stock;
			if (stock.HasValue)
			{
				ClickyPaperStock valueOrDefault = stock.GetValueOrDefault();
				return valueOrDefault.Ink();
			}
			return PaperColor.Hex("#2B2620");
		}
	}

	public Color Quiet
	{
		get
		{
			ClickyPaperStock? stock = Stock;
			if (stock.HasValue)
			{
				ClickyPaperStock valueOrDefault = stock.GetValueOrDefault();
				return valueOrDefault.Ink().Opacity(0.58);
			}
			return PaperColor.Hex("#8A8F9E");
		}
	}

	public Color Deep
	{
		get
		{
			ClickyPaperStock? stock = Stock;
			if (stock.HasValue)
			{
				ClickyPaperStock valueOrDefault = stock.GetValueOrDefault();
				return valueOrDefault.Deep();
			}
			return ClickyPaperInk.ClickyBlue;
		}
	}

	public Color TitleWash => (Stock ?? ClickyPaperStock.Cream).Wash();

	public Color Bottom
	{
		get
		{
			ClickyPaperStock? stock = Stock;
			if (stock.HasValue)
			{
				ClickyPaperStock valueOrDefault = stock.GetValueOrDefault();
				return valueOrDefault.Paper();
			}
			return PaperColor.Hex("#F7F2E4");
		}
	}

	public Color Pencil
	{
		get
		{
			ClickyPaperStock? stock = Stock;
			if (stock.HasValue)
			{
				ClickyPaperStock valueOrDefault = stock.GetValueOrDefault();
				return valueOrDefault.Ink().Opacity(0.66);
			}
			return PaperColor.Hex("#6A6F7A");
		}
	}

	public PaperFill Fill
	{
		get
		{
			ClickyPaperStock? stock = Stock;
			if (stock.HasValue)
			{
				ClickyPaperStock valueOrDefault = stock.GetValueOrDefault();
				return PaperFill.Of(valueOrDefault.Paper());
			}
			return PaperFill.Vertical((0.0, PaperColor.Hex("#FFFCF3")), (0.4, PaperColor.Hex("#FBF7EC")), (1.0, PaperColor.Hex("#F7F2E4")));
		}
	}

	public static readonly ClickyHomeSpreadPaper Note = new ClickyHomeSpreadPaper(null);

	public static ClickyHomeSpreadPaper Of(ClickyPaperStock stock)
	{
		return new ClickyHomeSpreadPaper(stock);
	}
}
