using System;
using System.Diagnostics;
using System.IO;
using HeyClicky.App.Design;
using HeyClicky.App.Portraits;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Home;

public static class ChatBlockViews
{
	public const double BubbleTailDrop = 7.0;

	public static bool IsImage(string p)
	{
		string text = System.IO.Path.GetExtension(p).ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = text[1];
					if (c != 'j')
					{
						if (c != 't')
						{
							if (c == 'w' && text == ".webp")
							{
								goto IL_00d1;
							}
						}
						else if (text == ".tiff")
						{
							goto IL_00d1;
						}
					}
					else if (text == ".jpeg")
					{
						goto IL_00d1;
					}
				}
			}
			else
			{
				char c = text[1];
				if ((uint)c <= 103u)
				{
					if (c != 'b')
					{
						if (c == 'g' && text == ".gif")
						{
							goto IL_00d1;
						}
					}
					else if (text == ".bmp")
					{
						goto IL_00d1;
					}
				}
				else if (c != 'j')
				{
					if (c == 'p' && text == ".png")
					{
						goto IL_00d1;
					}
				}
				else if (text == ".jpg")
				{
					goto IL_00d1;
				}
			}
		}
		return false;
		IL_00d1:
		return true;
	}

	public static FrameworkElement SentBubblePlate(bool tail)
	{
		return BubblePlate(SentFill(), SentStroke(), tail, mirrored: true);
	}

	public static FrameworkElement IncomingBubblePlate(bool tail)
	{
		return BubblePlate(Ui.Brush(HomeTheme.IncomingBubble), null, tail, mirrored: false);
	}

	private static Brush SentFill()
	{
		return Ui.Vertical((0.0, DS.Hex(15594751u)), (0.18, DS.Hex(14084095u)), (0.5, DS.Hex(10668031u)), (0.8, DS.Hex(10079224u)), (1.0, DS.Hex(10670839u)));
	}

	private static Brush SentStroke()
	{
		return Ui.Vertical((0.0, DS.Hex(2234520u)), (1.0, DS.Hex(6447714u)));
	}

	internal static FrameworkElement BubblePlate(Brush fill, Brush? stroke, bool tail, bool mirrored, double corner = 17.0, double tailDrop = 7.0)
	{
		if (!tail)
		{
			return new Border
			{
				CornerRadius = new CornerRadius(corner),
				Background = fill,
				IsHitTestVisible = false,
				BorderBrush = stroke,
				BorderThickness = new Thickness(((object)stroke == null) ? 0.0 : 1.3)
			};
		}
		Microsoft.UI.Xaml.Shapes.Path path = new Microsoft.UI.Xaml.Shapes.Path
		{
			Fill = fill,
			Stroke = stroke,
			StrokeThickness = (((object)stroke == null) ? 0.0 : 1.3),
			IsHitTestVisible = false,
			Stretch = Stretch.None
		};
		Grid grid = new Grid();
		grid.IsHitTestVisible = false;
		grid.Children.Add(path);
		grid.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			double width = e.NewSize.Width;
			double height = e.NewSize.Height;
			if (!(width <= 0.0) && !(height <= 0.0))
			{
				double num = (((object)stroke == null) ? 0.0 : 0.65);
				path.Data = BubblePath(width - 2.0 * num, height - 2.0 * num, corner, tailDrop).ToXaml();
				path.RenderTransform = (mirrored ? ((Transform)new CompositeTransform
				{
					ScaleX = -1.0,
					TranslateX = width - num,
					TranslateY = num
				}) : ((Transform)new TranslateTransform
				{
					X = num,
					Y = num
				}));
			}
		};
		return grid;
	}

	private static PPath BubblePath(double w, double h, double corner, double tailDrop)
	{
		double bodyH = h - tailDrop;
		double num = Math.Min(corner, bodyH / 2.0);
		double s = tailDrop / 9.0;
		PPath p = new PPath().M(num, 0.0).L(w - num, 0.0);
		p.C(w - num * 0.45, 0.0, w, num * 0.45, w, num).L(w, bodyH - num).C(w, bodyH - num * 0.45, w - num * 0.45, bodyH, w - num, bodyH);
		(double, double) tuple = C(32.13, 0.0);
		p.L(tuple.Item1, tuple.Item2);
		Cv(C(26.48, 0.0), C(21.08, 1.97), C(16.12, 4.69));
		Cv(C(12.04, 6.95), C(8.53, 8.49), C(7.66, 8.55));
		Cv(C(5.03, 8.76), C(4.69, 6.28), C(5.72, 4.94));
		Cv(C(6.74, 3.61), C(7.91, 1.88), C(8.36, 0.94));
		Cv(C(9.38, -1.19), C(8.18, -3.45), C(6.43, -5.03));
		Cv(C(2.48, -8.6), (0.0, bodyH - num * 0.86), (0.0, bodyH - num));
		p.L(0.0, num).C(0.0, num * 0.45, num * 0.45, 0.0, num, 0.0).Z();
		return p;
		(double, double) C(double dx, double dy)
		{
			return (dx * s, bodyH + dy * s);
		}
		void Cv((double, double) c1, (double, double) c2, (double, double) to)
		{
			p.C(c1.Item1, c1.Item2, c2.Item1, c2.Item2, to.Item1, to.Item2);
		}
	}

	public static void OpenPath(string path)
	{
		try
		{
			ShellOpen.Start(path);
		}
		catch (Exception ex)
		{
			Log.Warn("chat", "open " + path + ": " + ex.Message);
		}
	}

	public static void RevealPath(string path)
	{
		try
		{
			Process.Start("explorer.exe", "/select,\"" + path + "\"");
		}
		catch (Exception ex)
		{
			Log.Warn("chat", "reveal " + path + ": " + ex.Message);
		}
	}

	public static string Kind(string path)
	{
		string text = System.IO.Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
		if (text != null)
		{
			switch (text.Length)
			{
			case 3:
				switch (text[0])
				{
				case 'p':
					break;
				case 'j':
					goto IL_01fd;
				case 'g':
					goto IL_0222;
				case 'b':
					goto IL_0237;
				case 's':
					goto IL_025c;
				case 'c':
					goto IL_0271;
				case 't':
					goto IL_02a6;
				case 'x':
					goto IL_02eb;
				case 'd':
					goto IL_0310;
				case 'r':
					goto IL_0335;
				case 'o':
					goto IL_034a;
				case 'h':
					goto IL_035f;
				case 'k':
					goto IL_0384;
				case 'm':
					goto IL_0399;
				case 'w':
					goto IL_03ee;
				case 'a':
					if (text == "aac")
					{
						goto IL_06fb;
					}
					if (!(text == "app"))
					{
						goto end_IL_0021;
					}
					goto IL_0713;
				case 'y':
					goto IL_0428;
				case 'e':
					if (!(text == "exe"))
					{
						goto end_IL_0021;
					}
					goto IL_0713;
				case 'z':
					goto IL_0452;
				default:
					goto end_IL_0021;
					IL_0713:
					return "App";
				}
				switch (text)
				{
				case "pdf":
					return "PDF";
				case "png":
					break;
				case "ppt":
					goto IL_06eb;
				default:
					goto end_IL_0021;
				}
				goto IL_06bb;
			case 4:
				switch (text[2])
				{
				case 'e':
					break;
				case 'f':
					goto IL_047c;
				case 'i':
					goto IL_0491;
				case 'b':
					goto IL_04a6;
				case 's':
					goto IL_04cb;
				case 'c':
					goto IL_04f0;
				case 'm':
					goto IL_0505;
				case 't':
					goto IL_053a;
				case 'a':
					goto IL_054f;
				case 'o':
					goto IL_0564;
				case 'v':
					goto IL_0579;
				default:
					goto end_IL_0021;
				}
				if (!(text == "jpeg"))
				{
					break;
				}
				goto IL_06bb;
			case 5:
			{
				char c = text[0];
				if (c != 'p')
				{
					if (c != 's' || !(text == "swift"))
					{
						break;
					}
					goto IL_070b;
				}
				if (!(text == "pages"))
				{
					break;
				}
				goto IL_06cb;
			}
			case 2:
				switch (text[0])
				{
				case 'm':
					break;
				case 't':
					goto IL_05e2;
				case 'j':
					goto IL_05f7;
				case 'p':
					goto IL_060c;
				case 'r':
					goto IL_0621;
				case 'g':
					goto IL_0646;
				case 'k':
					goto IL_066b;
				case 's':
					goto IL_0680;
				case '7':
					goto IL_0692;
				default:
					goto end_IL_0021;
				}
				if (!(text == "md"))
				{
					break;
				}
				goto IL_06d3;
			case 1:
			{
				char c = text[0];
				if (c != 'c' && c != 'h')
				{
					break;
				}
				goto IL_070b;
			}
			case 7:
				if (!(text == "numbers"))
				{
					break;
				}
				goto IL_06c3;
			case 8:
				if (!(text == "markdown"))
				{
					break;
				}
				goto IL_06d3;
			case 0:
				{
					return "Folder";
				}
				IL_01fd:
				if (text == "jpg")
				{
					goto IL_06bb;
				}
				if (!(text == "jsx"))
				{
					break;
				}
				goto IL_070b;
				IL_0692:
				if (!(text == "7z"))
				{
					break;
				}
				goto IL_071b;
				IL_0680:
				if (!(text == "sh"))
				{
					break;
				}
				goto IL_070b;
				IL_066b:
				if (!(text == "kt"))
				{
					break;
				}
				goto IL_070b;
				IL_0646:
				if (text == "go")
				{
					goto IL_070b;
				}
				if (!(text == "gz"))
				{
					break;
				}
				goto IL_071b;
				IL_02a6:
				switch (text)
				{
				case "tsv":
					break;
				case "txt":
					return "Text";
				case "tsx":
					goto IL_070b;
				case "tar":
					goto IL_071b;
				default:
					goto end_IL_0021;
				}
				goto IL_06c3;
				IL_0621:
				if (!(text == "rb") && !(text == "rs"))
				{
					break;
				}
				goto IL_070b;
				IL_0271:
				switch (text)
				{
				case "csv":
					break;
				case "cpp":
				case "css":
					goto IL_070b;
				default:
					goto end_IL_0021;
				}
				goto IL_06c3;
				IL_060c:
				if (!(text == "py"))
				{
					break;
				}
				goto IL_070b;
				IL_05f7:
				if (!(text == "js"))
				{
					break;
				}
				goto IL_070b;
				IL_05e2:
				if (!(text == "ts"))
				{
					break;
				}
				goto IL_070b;
				IL_0703:
				return "Data";
				IL_06d3:
				return "Markdown";
				IL_06c3:
				return "Spreadsheet";
				IL_0237:
				if (text == "bmp")
				{
					goto IL_06bb;
				}
				if (!(text == "bz2"))
				{
					break;
				}
				goto IL_071b;
				IL_025c:
				if (!(text == "svg"))
				{
					break;
				}
				goto IL_06bb;
				IL_0579:
				if (!(text == "java"))
				{
					break;
				}
				goto IL_070b;
				IL_0564:
				if (!(text == "json"))
				{
					break;
				}
				goto IL_0703;
				IL_054f:
				if (!(text == "flac"))
				{
					break;
				}
				goto IL_06fb;
				IL_053a:
				if (!(text == "pptx"))
				{
					break;
				}
				goto IL_06eb;
				IL_0505:
				switch (text)
				{
				case "html":
					break;
				case "yaml":
				case "toml":
					goto IL_0703;
				default:
					goto end_IL_0021;
				}
				goto IL_06e3;
				IL_04f0:
				if (!(text == "docx"))
				{
					break;
				}
				goto IL_06cb;
				IL_04cb:
				if (text == "xlsx")
				{
					goto IL_06c3;
				}
				if (!(text == "scss"))
				{
					break;
				}
				goto IL_070b;
				IL_071b:
				return "Archive";
				IL_04a6:
				if (text == "webp")
				{
					goto IL_06bb;
				}
				if (!(text == "webm"))
				{
					break;
				}
				goto IL_06f3;
				IL_0222:
				if (!(text == "gif"))
				{
					break;
				}
				goto IL_06bb;
				IL_0491:
				if (!(text == "heic"))
				{
					break;
				}
				goto IL_06bb;
				IL_047c:
				if (!(text == "tiff"))
				{
					break;
				}
				goto IL_06bb;
				IL_06eb:
				return "Slides";
				IL_0452:
				if (!(text == "zip"))
				{
					break;
				}
				goto IL_071b;
				IL_0428:
				if (!(text == "yml"))
				{
					break;
				}
				goto IL_0703;
				IL_03ee:
				if (!(text == "wav"))
				{
					break;
				}
				goto IL_06fb;
				IL_0399:
				switch (text)
				{
				case "mp4":
				case "mov":
				case "m4v":
					break;
				case "mp3":
				case "m4a":
					goto IL_06fb;
				default:
					goto end_IL_0021;
				}
				goto IL_06f3;
				IL_06fb:
				return "Audio";
				IL_06f3:
				return "Video";
				IL_0384:
				if (!(text == "key"))
				{
					break;
				}
				goto IL_06eb;
				IL_035f:
				if (text == "htm")
				{
					goto IL_06e3;
				}
				if (!(text == "hpp"))
				{
					break;
				}
				goto IL_070b;
				IL_06bb:
				return "Image";
				IL_06e3:
				return "Web page";
				IL_034a:
				if (!(text == "odt"))
				{
					break;
				}
				goto IL_06cb;
				IL_0335:
				if (!(text == "rtf"))
				{
					break;
				}
				goto IL_06cb;
				IL_0310:
				if (text == "doc")
				{
					goto IL_06cb;
				}
				if (!(text == "dmg"))
				{
					break;
				}
				goto IL_071b;
				IL_070b:
				return "Code";
				IL_06cb:
				return "Document";
				IL_02eb:
				if (text == "xls")
				{
					goto IL_06c3;
				}
				if (!(text == "xml"))
				{
					break;
				}
				goto IL_0703;
				end_IL_0021:
				break;
			}
		}
		return text.ToUpperInvariant();
	}
}
