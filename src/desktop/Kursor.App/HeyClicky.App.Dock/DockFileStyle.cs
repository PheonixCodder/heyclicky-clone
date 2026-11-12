using System.IO;
using HeyClicky.App.Design;
using Windows.UI;

namespace HeyClicky.App.Dock;

internal readonly record struct DockFileStyle(string Glyph, Color Tint, string Kind)
{
	public static DockFileStyle For(string entry)
	{
		if (DockFiles.IsWebLink(entry))
		{
			return new DockFileStyle("\ue71b", DS.Hex(3900150u), "Link");
		}
		string text = Path.GetExtension(entry).TrimStart('.').ToLowerInvariant();
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
					goto IL_0240;
				case 'g':
					goto IL_0265;
				case 'b':
					goto IL_027a;
				case 's':
					goto IL_029f;
				case 'c':
					goto IL_02b4;
				case 't':
					goto IL_02e9;
				case 'x':
					goto IL_032e;
				case 'd':
					goto IL_0353;
				case 'r':
					goto IL_0378;
				case 'o':
					goto IL_038d;
				case 'h':
					goto IL_03a2;
				case 'k':
					goto IL_03c7;
				case 'm':
					goto IL_03dc;
				case 'w':
					goto IL_0431;
				case 'a':
					goto IL_0446;
				case 'y':
					goto IL_045b;
				case 'z':
					goto IL_0470;
				default:
					goto end_IL_004c;
				}
				switch (text)
				{
				case "pdf":
					return new DockFileStyle("\ue8a5", DS.Hex(15680580u), "PDF");
				case "png":
					break;
				case "ppt":
					goto IL_080a;
				case "ps1":
					goto IL_08aa;
				default:
					goto end_IL_004c;
				}
				goto IL_071a;
			case 4:
				switch (text[2])
				{
				case 'e':
					break;
				case 'f':
					goto IL_049a;
				case 'i':
					goto IL_04af;
				case 'b':
					goto IL_04c4;
				case 's':
					goto IL_04e9;
				case 'c':
					goto IL_050e;
				case 'm':
					goto IL_0523;
				case 't':
					goto IL_0558;
				case 'a':
					goto IL_056d;
				case 'o':
					goto IL_0582;
				case 'v':
					goto IL_0597;
				default:
					goto end_IL_004c;
				}
				if (!(text == "jpeg"))
				{
					break;
				}
				goto IL_071a;
			case 5:
			{
				char c = text[0];
				if (c != 'p')
				{
					if (c != 's' || !(text == "swift"))
					{
						break;
					}
					goto IL_08aa;
				}
				if (!(text == "pages"))
				{
					break;
				}
				goto IL_076a;
			}
			case 2:
				switch (text[0])
				{
				case 'm':
					break;
				case 't':
					goto IL_0600;
				case 'j':
					goto IL_0615;
				case 'p':
					goto IL_062a;
				case 'r':
					goto IL_063f;
				case 'g':
					goto IL_0664;
				case 'k':
					goto IL_0689;
				case 's':
					goto IL_069e;
				case 'c':
					goto IL_06b3;
				case '7':
					goto IL_06c8;
				default:
					goto end_IL_004c;
				}
				if (!(text == "md"))
				{
					break;
				}
				goto IL_0792;
			case 1:
			{
				char c = text[0];
				if (c != 'c' && c != 'h')
				{
					break;
				}
				goto IL_08aa;
			}
			case 7:
				if (!(text == "numbers"))
				{
					break;
				}
				goto IL_0742;
			case 8:
				if (!(text == "markdown"))
				{
					break;
				}
				goto IL_0792;
			case 0:
				{
					return new DockFileStyle("\ue8b7", DS.Hex(959977u), "Folder");
				}
				IL_08aa:
				return new DockFileStyle("\ue943", DS.Hex(11032055u), "Code");
				IL_06c8:
				if (!(text == "7z"))
				{
					break;
				}
				goto IL_08cf;
				IL_06b3:
				if (!(text == "cs"))
				{
					break;
				}
				goto IL_08aa;
				IL_069e:
				if (!(text == "sh"))
				{
					break;
				}
				goto IL_08aa;
				IL_0689:
				if (!(text == "kt"))
				{
					break;
				}
				goto IL_08aa;
				IL_0664:
				if (text == "go")
				{
					goto IL_08aa;
				}
				if (!(text == "gz"))
				{
					break;
				}
				goto IL_08cf;
				IL_02e9:
				switch (text)
				{
				case "tsv":
					break;
				case "txt":
					return new DockFileStyle("\ue8a5", DS.Hex(6583435u), "Text");
				case "tsx":
					goto IL_08aa;
				case "tar":
					goto IL_08cf;
				default:
					goto end_IL_004c;
				}
				goto IL_0742;
				IL_063f:
				if (!(text == "rb") && !(text == "rs"))
				{
					break;
				}
				goto IL_08aa;
				IL_02b4:
				switch (text)
				{
				case "csv":
					break;
				case "cpp":
				case "css":
					goto IL_08aa;
				default:
					goto end_IL_004c;
				}
				goto IL_0742;
				IL_062a:
				if (!(text == "py"))
				{
					break;
				}
				goto IL_08aa;
				IL_0615:
				if (!(text == "js"))
				{
					break;
				}
				goto IL_08aa;
				IL_0600:
				if (!(text == "ts"))
				{
					break;
				}
				goto IL_08aa;
				IL_0882:
				return new DockFileStyle("\ue943", DS.Hex(15381256u), "Data");
				IL_0792:
				return new DockFileStyle("\ue8e4", DS.Hex(6514417u), "Markdown");
				IL_0742:
				return new DockFileStyle("\ue80a", DS.Hex(2278750u), "Spreadsheet");
				IL_027a:
				if (text == "bmp")
				{
					goto IL_071a;
				}
				if (!(text == "bz2"))
				{
					break;
				}
				goto IL_08cf;
				IL_029f:
				if (!(text == "svg"))
				{
					break;
				}
				goto IL_071a;
				IL_0597:
				if (!(text == "java"))
				{
					break;
				}
				goto IL_08aa;
				IL_0582:
				if (!(text == "json"))
				{
					break;
				}
				goto IL_0882;
				IL_056d:
				if (!(text == "flac"))
				{
					break;
				}
				goto IL_085a;
				IL_0558:
				if (!(text == "pptx"))
				{
					break;
				}
				goto IL_080a;
				IL_0523:
				switch (text)
				{
				case "html":
					break;
				case "yaml":
				case "toml":
					goto IL_0882;
				default:
					goto end_IL_004c;
				}
				goto IL_07e2;
				IL_050e:
				if (!(text == "docx"))
				{
					break;
				}
				goto IL_076a;
				IL_04e9:
				if (text == "xlsx")
				{
					goto IL_0742;
				}
				if (!(text == "scss"))
				{
					break;
				}
				goto IL_08aa;
				IL_08cf:
				return new DockFileStyle("\uf012", DS.Hex(7893356u), "Archive");
				IL_04c4:
				if (text == "webp")
				{
					goto IL_071a;
				}
				if (!(text == "webm"))
				{
					break;
				}
				goto IL_0832;
				IL_0265:
				if (!(text == "gif"))
				{
					break;
				}
				goto IL_071a;
				IL_04af:
				if (!(text == "heic"))
				{
					break;
				}
				goto IL_071a;
				IL_049a:
				if (!(text == "tiff"))
				{
					break;
				}
				goto IL_071a;
				IL_080a:
				return new DockFileStyle("\ue8a1", DS.Hex(16096779u), "Slides");
				IL_0470:
				if (!(text == "zip"))
				{
					break;
				}
				goto IL_08cf;
				IL_045b:
				if (!(text == "yml"))
				{
					break;
				}
				goto IL_0882;
				IL_0446:
				if (!(text == "aac"))
				{
					break;
				}
				goto IL_085a;
				IL_0431:
				if (!(text == "wav"))
				{
					break;
				}
				goto IL_085a;
				IL_03dc:
				switch (text)
				{
				case "mp4":
				case "mov":
				case "m4v":
					break;
				case "mp3":
				case "m4a":
					goto IL_085a;
				default:
					goto end_IL_004c;
				}
				goto IL_0832;
				IL_085a:
				return new DockFileStyle("\ue8d6", DS.Hex(1357990u), "Audio");
				IL_0832:
				return new DockFileStyle("\ue8b2", DS.Hex(9133302u), "Video");
				IL_03c7:
				if (!(text == "key"))
				{
					break;
				}
				goto IL_080a;
				IL_03a2:
				if (text == "htm")
				{
					goto IL_07e2;
				}
				if (!(text == "hpp"))
				{
					break;
				}
				goto IL_08aa;
				IL_071a:
				return new DockFileStyle("\ueb9f", DS.Hex(15485081u), "Image");
				IL_07e2:
				return new DockFileStyle("\ue774", DS.Hex(16347926u), "Web page");
				IL_038d:
				if (!(text == "odt"))
				{
					break;
				}
				goto IL_076a;
				IL_0378:
				if (!(text == "rtf"))
				{
					break;
				}
				goto IL_076a;
				IL_0353:
				if (text == "doc")
				{
					goto IL_076a;
				}
				if (!(text == "dmg"))
				{
					break;
				}
				goto IL_08cf;
				IL_0240:
				if (text == "jpg")
				{
					goto IL_071a;
				}
				if (!(text == "jsx"))
				{
					break;
				}
				goto IL_08aa;
				IL_076a:
				return new DockFileStyle("\ue8a5", DS.Hex(2450411u), "Document");
				IL_032e:
				if (text == "xls")
				{
					goto IL_0742;
				}
				if (!(text == "xml"))
				{
					break;
				}
				goto IL_0882;
				end_IL_004c:
				break;
			}
		}
		return new DockFileStyle("\ue8a5", DS.Hex(9741240u), text.ToUpperInvariant());
	}
}
