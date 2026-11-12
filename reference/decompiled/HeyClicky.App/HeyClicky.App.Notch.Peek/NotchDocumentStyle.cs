using System.IO;
using HeyClicky.App.Design;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public sealed record NotchDocumentStyle(string Glyph, Color Tint, string KindLabel)
{
	public static NotchDocumentStyle For(string path, bool isWebLink = false)
	{
		if (isWebLink)
		{
			return new NotchDocumentStyle("\ue71b", DS.Hex(3900150u), "Link");
		}
		string text = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
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
					goto IL_0223;
				case 'g':
					goto IL_0248;
				case 'b':
					goto IL_025d;
				case 's':
					goto IL_0282;
				case 'c':
					goto IL_0297;
				case 't':
					goto IL_02cc;
				case 'x':
					goto IL_0311;
				case 'd':
					goto IL_0336;
				case 'r':
					goto IL_035b;
				case 'o':
					goto IL_0370;
				case 'h':
					goto IL_0385;
				case 'k':
					goto IL_03aa;
				case 'm':
					goto IL_03bf;
				case 'w':
					goto IL_0414;
				case 'a':
					if (text == "aac")
					{
						goto IL_084d;
					}
					if (!(text == "app"))
					{
						goto end_IL_0047;
					}
					goto IL_08c5;
				case 'y':
					goto IL_044e;
				case 'e':
					if (!(text == "exe"))
					{
						goto end_IL_0047;
					}
					goto IL_08c5;
				case 'z':
					goto IL_0478;
				default:
					goto end_IL_0047;
					IL_08c5:
					return new NotchDocumentStyle("\ue737", DS.Hex(959977u), "App");
				}
				switch (text)
				{
				case "pdf":
					return new NotchDocumentStyle("\uea90", DS.Hex(15680580u), "PDF");
				case "png":
					break;
				case "ppt":
					goto IL_07fd;
				default:
					goto end_IL_0047;
				}
				goto IL_070d;
			case 4:
				switch (text[2])
				{
				case 'e':
					break;
				case 'f':
					goto IL_04a2;
				case 'i':
					goto IL_04b7;
				case 'b':
					goto IL_04cc;
				case 's':
					goto IL_04f1;
				case 'c':
					goto IL_0516;
				case 'm':
					goto IL_052b;
				case 't':
					goto IL_0560;
				case 'a':
					goto IL_0575;
				case 'o':
					goto IL_058a;
				case 'v':
					goto IL_059f;
				default:
					goto end_IL_0047;
				}
				if (!(text == "jpeg"))
				{
					break;
				}
				goto IL_070d;
			case 5:
			{
				char c = text[0];
				if (c != 'p')
				{
					if (c != 's' || !(text == "swift"))
					{
						break;
					}
					goto IL_089d;
				}
				if (!(text == "pages"))
				{
					break;
				}
				goto IL_075d;
			}
			case 2:
				switch (text[0])
				{
				case 'm':
					break;
				case 't':
					goto IL_0608;
				case 'j':
					goto IL_061d;
				case 'p':
					goto IL_0632;
				case 'r':
					goto IL_0647;
				case 'g':
					goto IL_066c;
				case 'k':
					goto IL_0691;
				case 's':
					goto IL_06a6;
				case '7':
					goto IL_06bb;
				default:
					goto end_IL_0047;
				}
				if (!(text == "md"))
				{
					break;
				}
				goto IL_0785;
			case 1:
			{
				char c = text[0];
				if (c != 'c' && c != 'h')
				{
					break;
				}
				goto IL_089d;
			}
			case 7:
				if (!(text == "numbers"))
				{
					break;
				}
				goto IL_0735;
			case 8:
				if (!(text == "markdown"))
				{
					break;
				}
				goto IL_0785;
			case 0:
				{
					return new NotchDocumentStyle("\ue8b7", DS.Hex(959977u), "Folder");
				}
				IL_0223:
				if (text == "jpg")
				{
					goto IL_070d;
				}
				if (!(text == "jsx"))
				{
					break;
				}
				goto IL_089d;
				IL_06bb:
				if (!(text == "7z"))
				{
					break;
				}
				goto IL_08ea;
				IL_06a6:
				if (!(text == "sh"))
				{
					break;
				}
				goto IL_089d;
				IL_0691:
				if (!(text == "kt"))
				{
					break;
				}
				goto IL_089d;
				IL_066c:
				if (text == "go")
				{
					goto IL_089d;
				}
				if (!(text == "gz"))
				{
					break;
				}
				goto IL_08ea;
				IL_02cc:
				switch (text)
				{
				case "tsv":
					break;
				case "txt":
					return new NotchDocumentStyle("\ue8a5", DS.Hex(6583435u), "Text");
				case "tsx":
					goto IL_089d;
				case "tar":
					goto IL_08ea;
				default:
					goto end_IL_0047;
				}
				goto IL_0735;
				IL_0647:
				if (!(text == "rb") && !(text == "rs"))
				{
					break;
				}
				goto IL_089d;
				IL_0297:
				switch (text)
				{
				case "csv":
					break;
				case "cpp":
				case "css":
					goto IL_089d;
				default:
					goto end_IL_0047;
				}
				goto IL_0735;
				IL_0632:
				if (!(text == "py"))
				{
					break;
				}
				goto IL_089d;
				IL_061d:
				if (!(text == "js"))
				{
					break;
				}
				goto IL_089d;
				IL_0608:
				if (!(text == "ts"))
				{
					break;
				}
				goto IL_089d;
				IL_0875:
				return new NotchDocumentStyle("\ue943", DS.Hex(15381256u), "Data");
				IL_0785:
				return new NotchDocumentStyle("\ue8e4", DS.Hex(6514417u), "Markdown");
				IL_0735:
				return new NotchDocumentStyle("\ue80a", DS.Hex(2278750u), "Spreadsheet");
				IL_025d:
				if (text == "bmp")
				{
					goto IL_070d;
				}
				if (!(text == "bz2"))
				{
					break;
				}
				goto IL_08ea;
				IL_0282:
				if (!(text == "svg"))
				{
					break;
				}
				goto IL_070d;
				IL_059f:
				if (!(text == "java"))
				{
					break;
				}
				goto IL_089d;
				IL_058a:
				if (!(text == "json"))
				{
					break;
				}
				goto IL_0875;
				IL_0575:
				if (!(text == "flac"))
				{
					break;
				}
				goto IL_084d;
				IL_0560:
				if (!(text == "pptx"))
				{
					break;
				}
				goto IL_07fd;
				IL_052b:
				switch (text)
				{
				case "html":
					break;
				case "yaml":
				case "toml":
					goto IL_0875;
				default:
					goto end_IL_0047;
				}
				goto IL_07d5;
				IL_0516:
				if (!(text == "docx"))
				{
					break;
				}
				goto IL_075d;
				IL_04f1:
				if (text == "xlsx")
				{
					goto IL_0735;
				}
				if (!(text == "scss"))
				{
					break;
				}
				goto IL_089d;
				IL_08ea:
				return new NotchDocumentStyle("\uf012", DS.Hex(7893356u), "Archive");
				IL_04cc:
				if (text == "webp")
				{
					goto IL_070d;
				}
				if (!(text == "webm"))
				{
					break;
				}
				goto IL_0825;
				IL_0248:
				if (!(text == "gif"))
				{
					break;
				}
				goto IL_070d;
				IL_04b7:
				if (!(text == "heic"))
				{
					break;
				}
				goto IL_070d;
				IL_04a2:
				if (!(text == "tiff"))
				{
					break;
				}
				goto IL_070d;
				IL_07fd:
				return new NotchDocumentStyle("\ue8fd", DS.Hex(16096779u), "Slides");
				IL_0478:
				if (!(text == "zip"))
				{
					break;
				}
				goto IL_08ea;
				IL_044e:
				if (!(text == "yml"))
				{
					break;
				}
				goto IL_0875;
				IL_0414:
				if (!(text == "wav"))
				{
					break;
				}
				goto IL_084d;
				IL_03bf:
				switch (text)
				{
				case "mp4":
				case "mov":
				case "m4v":
					break;
				case "mp3":
				case "m4a":
					goto IL_084d;
				default:
					goto end_IL_0047;
				}
				goto IL_0825;
				IL_084d:
				return new NotchDocumentStyle("\ue8d6", DS.Hex(1357990u), "Audio");
				IL_0825:
				return new NotchDocumentStyle("\ue8b2", DS.Hex(9133302u), "Video");
				IL_03aa:
				if (!(text == "key"))
				{
					break;
				}
				goto IL_07fd;
				IL_0385:
				if (text == "htm")
				{
					goto IL_07d5;
				}
				if (!(text == "hpp"))
				{
					break;
				}
				goto IL_089d;
				IL_070d:
				return new NotchDocumentStyle("\ue91b", DS.Hex(15485081u), "Image");
				IL_07d5:
				return new NotchDocumentStyle("\ue774", DS.Hex(16347926u), "Web page");
				IL_0370:
				if (!(text == "odt"))
				{
					break;
				}
				goto IL_075d;
				IL_035b:
				if (!(text == "rtf"))
				{
					break;
				}
				goto IL_075d;
				IL_0336:
				if (text == "doc")
				{
					goto IL_075d;
				}
				if (!(text == "dmg"))
				{
					break;
				}
				goto IL_08ea;
				IL_089d:
				return new NotchDocumentStyle("\ue943", DS.Hex(11032055u), "Code");
				IL_075d:
				return new NotchDocumentStyle("\ue8a5", DS.Hex(2450411u), "Document");
				IL_0311:
				if (text == "xls")
				{
					goto IL_0735;
				}
				if (!(text == "xml"))
				{
					break;
				}
				goto IL_0875;
				end_IL_0047:
				break;
			}
		}
		return new NotchDocumentStyle("\ue8a5", DS.Hex(9741240u), text.ToUpperInvariant());
	}
}
