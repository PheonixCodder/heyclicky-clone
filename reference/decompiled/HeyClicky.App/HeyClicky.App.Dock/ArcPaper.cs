using HeyClicky.App.Design;
using Windows.UI;

namespace HeyClicky.App.Dock;

public readonly record struct ArcPaper(PaperStock Stock)
{
	public Color Fill => Stock switch
	{
		PaperStock.Sky => DS.Hex(15462909u), 
		PaperStock.Butter => DS.Hex(16578019u), 
		PaperStock.Mint => DS.Hex(15333100u), 
		PaperStock.Periwinkle => DS.Hex(15789821u), 
		PaperStock.Peach => DS.Hex(16641765u), 
		PaperStock.Blush => DS.Hex(16574961u), 
		PaperStock.Kraft => DS.Hex(16051938u), 
		_ => DS.Hex(16513004u), 
	};

	public Color TitleWash => Stock switch
	{
		PaperStock.Sky => DS.Hex(14149119u), 
		PaperStock.Butter => DS.Hex(16707271u), 
		PaperStock.Mint => DS.Hex(13889756u), 
		PaperStock.Periwinkle => DS.Hex(14933759u), 
		PaperStock.Peach => DS.Hex(16768457u), 
		PaperStock.Blush => DS.Hex(16762849u), 
		PaperStock.Kraft => DS.Hex(15655887u), 
		_ => DS.Hex(15985100u), 
	};

	public Color Ink => Stock switch
	{
		PaperStock.Sky => DS.Hex(924191u), 
		PaperStock.Butter => DS.Hex(1840646u), 
		PaperStock.Mint => DS.Hex(992279u), 
		PaperStock.Periwinkle => DS.Hex(1317432u), 
		PaperStock.Peach => DS.Hex(2955531u), 
		PaperStock.Blush => DS.Hex(2888728u), 
		PaperStock.Kraft => DS.Hex(2826516u), 
		_ => DS.Hex(2827808u), 
	};

	public Color Deep => Stock switch
	{
		PaperStock.Sky => DS.Hex(1784460u), 
		PaperStock.Butter => DS.Hex(5913088u), 
		PaperStock.Mint => DS.Hex(1923635u), 
		PaperStock.Periwinkle => DS.Hex(2832784u), 
		PaperStock.Peach => DS.Hex(9059087u), 
		PaperStock.Blush => DS.Hex(8003381u), 
		PaperStock.Kraft => DS.Hex(5913878u), 
		_ => DS.Hex(2972643u), 
	};
}
