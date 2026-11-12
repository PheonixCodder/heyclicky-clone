using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Dock;

public static class ArcSymbols
{
	public static FrameworkElement Make(string name, double size, Color color, bool bold = true)
	{
		string text;
		switch (name)
		{
		case "checkmark":
			text = "\ue73e";
			break;
		case "checkmark.circle.fill":
			text = "\uec61";
			break;
		case "play.fill":
			text = "\uf5b0";
			break;
		case "keyboard":
			text = "\ue765";
			break;
		case "doc.on.doc":
			text = "\ue8c8";
			break;
		case "doc.text":
			text = "\ue8a5";
			break;
		case "arrow.clockwise":
			text = "\ue72c";
			break;
		case "stop.fill":
			text = "\ue71a";
			break;
		case "globe":
			text = "\ue774";
			break;
		case "link":
			text = "\ue71b";
			break;
		case "clock":
			text = "\ue823";
			break;
		case "chevron.left":
			text = "\ue76b";
			break;
		case "chevron.right":
			text = "\ue76c";
			break;
		case "pin.fill":
			text = "\ue842";
			break;
		case "bolt.fill":
			text = "\ue945";
			break;
		case "photo.fill":
			text = "\ueb9f";
			break;
		case "doc.fill":
		case "doc.text.fill":
		case "doc.richtext.fill":
		case "doc.plaintext.fill":
			text = "\ue8a5";
			break;
		case "tablecells.fill":
			text = "\ue80a";
			break;
		case "text.alignleft":
			text = "\ue8e4";
			break;
		case "rectangle.on.rectangle.fill":
			text = "\ue8a1";
			break;
		case "film.fill":
			text = "\ue8b2";
			break;
		case "waveform":
			text = "\ue8d6";
			break;
		case "curlybraces":
		case "chevron.left.forwardslash.chevron.right":
			text = "\ue943";
			break;
		case "doc.zipper":
			text = "\uf012";
			break;
		case "folder.fill":
			text = "\ue8b7";
			break;
		case "doc.on.doc.fill":
			text = "\ue8c8";
			break;
		case "arrow.up.right.square":
			text = "\ue8a7";
			break;
		default:
			text = null;
			break;
		}
		string text2 = text;
		if (text2 != null)
		{
			TextBlock textBlock = Icons.Glyph(text2, size, color);
			if (bold)
			{
				textBlock.FontWeight = FontWeights.Bold;
			}
			return textBlock;
		}
		if (name != null)
		{
			int length = name.Length;
			if (length <= 16)
			{
				if (length == 8)
				{
					char c = name[0];
					if (c != 'i')
					{
						if (c != 's' || !(name == "sparkles"))
						{
							goto IL_0674;
						}
						text = "M10,3 C10.6,7.6 12.4,9.4 17,10 C12.4,10.6 10.6,12.4 10,17 C9.4,12.4 7.6,10.6 3,10 C7.6,9.4 9.4,7.6 10,3 Z M18,13 C18.3,15.2 18.8,15.7 21,16 C18.8,16.3 18.3,16.8 18,19 C17.7,16.8 17.2,16.3 15,16 C17.2,15.7 17.7,15.2 18,13 Z M17,2 C17.2,3.4 17.6,3.8 19,4 C17.6,4.2 17.2,4.6 17,6 C16.8,4.6 16.4,4.2 15,4 C16.4,3.8 16.8,3.4 17,2 Z";
					}
					else
					{
						if (!(name == "infinity"))
						{
							goto IL_0674;
						}
						text = null;
					}
					goto IL_0676;
				}
				if (length == 16)
				{
					char c = name[0];
					if (c != 'h')
					{
						if (c == 's' && name == "stop.circle.fill")
						{
							text = "F0 M12 2a10 10 0 1 1 0 20a10 10 0 1 1 0-20Z M8.6 8.6h6.8v6.8H8.6Z";
							goto IL_0676;
						}
					}
					else if (name == "hand.raised.fill")
					{
						goto IL_0648;
					}
				}
			}
			else if (length != 19)
			{
				if (length != 23)
				{
					if (length == 29 && name == "exclamationmark.triangle.fill")
					{
						text = "F0 M12 2.6c.6 0 1.1.3 1.4.8l8.9 15.5c.6 1.1-.2 2.5-1.4 2.5H3.1c-1.2 0-2-1.4-1.4-2.5L10.6 3.4c.3-.5.8-.8 1.4-.8Z M10.9 8.3h2.2l-.3 6.4h-1.6Z M12 16.1a1.3 1.3 0 1 1 0 2.6a1.3 1.3 0 1 1 0-2.6Z";
						goto IL_0676;
					}
				}
				else if (name == "hand.point.up.left.fill")
				{
					goto IL_0648;
				}
			}
			else if (name == "cursorarrow.click.2")
			{
				text = "M8.2 6.4l10.1 6.1-4.4 1.1 2.6 5.2-2 1-2.6-5.2-3.5 3.1Z";
				goto IL_0676;
			}
		}
		goto IL_0674;
		IL_0648:
		text = "M8.4 4.2a1.45 1.45 0 0 1 2.9 0V10.4h.6V2.9a1.45 1.45 0 0 1 2.9 0V10.4h.6V4.4a1.45 1.45 0 0 1 2.9 0V14c0 4.3-2.9 7.6-7 7.6c-2.6 0-4.4-1.2-5.8-3.4L2.9 13.5a1.45 1.45 0 0 1 2.4-1.6L8.4 15Z";
		goto IL_0676;
		IL_0674:
		text = null;
		goto IL_0676;
		IL_0676:
		string text3 = text;
		if (text3 != null)
		{
			return Icons.Vector(size * 1.15, color, text3);
		}
		return name switch
		{
			"arrow.up.right" => Icons.StrokeIcon("M5,19 L19,5 M8,5 L19,5 L19,16", size, color, bold ? 1.9 : 1.5), 
			"infinity" => Ui.Text("∞", size * 1.25, FontWeights.Bold, color), 
			"hand.raised.fill" => Icons.Glyph("\ue768", size, color), 
			_ => Icons.Glyph("\uf4a5", size, color), 
		};
	}
}
