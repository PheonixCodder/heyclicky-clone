using System.IO;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyPaperFileKinds
{
	public static Color[] FootColors(this ClickyPaperFileKind kind)
	{
		return kind switch
		{
			ClickyPaperFileKind.Slides => new Color[2]
			{
				PaperColor.Hex("#5AA2FA"),
				PaperColor.Hex("#2368DA")
			}, 
			ClickyPaperFileKind.Pdf => new Color[2]
			{
				PaperColor.Hex("#FF6B5E"),
				PaperColor.Hex("#E0392B")
			}, 
			ClickyPaperFileKind.Sheet => new Color[2]
			{
				PaperColor.Hex("#43C46B"),
				PaperColor.Hex("#23994A")
			}, 
			ClickyPaperFileKind.Image => new Color[2]
			{
				PaperColor.Hex("#B48BF5"),
				PaperColor.Hex("#7E57D9")
			}, 
			ClickyPaperFileKind.Doc => new Color[2]
			{
				PaperColor.Hex("#7FA6E8"),
				PaperColor.Hex("#4A73C9")
			}, 
			ClickyPaperFileKind.Page => new Color[2]
			{
				PaperColor.Hex("#F5A524"),
				PaperColor.Hex("#D9850B")
			}, 
			_ => new Color[2]
			{
				PaperColor.Hex("#B6BBC6"),
				PaperColor.Hex("#8F95A3")
			}, 
		};
	}

	private static string Extension(string name)
	{
		string extension = Path.GetExtension(name);
		if (extension.Length <= 1)
		{
			return "";
		}
		string text = extension;
		return text.Substring(1, text.Length - 1).ToLowerInvariant();
	}

	public static string ShortLabel(string fileName)
	{
		string text = Extension(fileName);
		switch (text)
		{
		case "key":
			return "Keynote";
		case "ppt":
		case "pptx":
			return "Slides";
		case "pdf":
			return "PDF";
		case "numbers":
			return "Numbers";
		case "xls":
		case "xlsx":
			return "Excel";
		case "csv":
		case "tsv":
			return "Sheet";
		case "png":
		case "jpg":
		case "gif":
		case "svg":
		case "jpeg":
		case "heic":
		case "webp":
			return "Image";
		case "htm":
		case "html":
			return "Page";
		case "txt":
		case "rtf":
		case "md":
			return "Note";
		case "doc":
		case "docx":
		case "pages":
			return "Doc";
		case "":
			return "Folder";
		default:
			return text.ToUpperInvariant();
		}
	}

	public static ClickyPaperFileKind ForFileName(string name)
	{
		switch (Extension(name))
		{
		case "key":
		case "ppt":
		case "pptx":
			return ClickyPaperFileKind.Slides;
		case "pdf":
			return ClickyPaperFileKind.Pdf;
		case "xls":
		case "csv":
		case "tsv":
		case "xlsx":
		case "numbers":
			return ClickyPaperFileKind.Sheet;
		case "png":
		case "jpg":
		case "gif":
		case "svg":
		case "jpeg":
		case "heic":
		case "webp":
			return ClickyPaperFileKind.Image;
		case "htm":
		case "html":
			return ClickyPaperFileKind.Page;
		case "txt":
		case "doc":
		case "rtf":
		case "docx":
		case "md":
		case "pages":
			return ClickyPaperFileKind.Doc;
		default:
			return ClickyPaperFileKind.Other;
		}
	}
}
