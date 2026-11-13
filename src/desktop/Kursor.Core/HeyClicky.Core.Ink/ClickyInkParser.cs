using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace HeyClicky.Core.Ink;

public static class ClickyInkParser
{
	public sealed record DirectiveHeader(string Name, string Label, Dictionary<string, string> Attributes);

	public sealed record ListStart(ClickyInkDocument.Kind Kind, ClickyInkDocument.Item Item)
	{
		public string Text => Item.Text;
	}

	public static class Emphasis
	{
		public const char BoldOn = '\ue001';

		public const char BoldOff = '\ue002';

		public const char HighlightOn = '\ue003';

		public const char HighlightOff = '\ue004';

		public const char LinkOn = '\ue005';

		public const char LinkOff = '\ue006';

		public static readonly IReadOnlySet<char> All = new HashSet<char> { '\ue001', '\ue002', '\ue003', '\ue004', '\ue005', '\ue006' };

		public static bool IsMarker(char c)
		{
			if (c >= '\ue001')
			{
				return c <= '\ue006';
			}
			return false;
		}

		public static bool IsMarker(string character)
		{
			if (character.Length == 1)
			{
				return IsMarker(character[0]);
			}
			return false;
		}

		public static string Strip(string text)
		{
			bool flag = false;
			string text2 = text;
			for (int i = 0; i < text2.Length; i++)
			{
				if (IsMarker(text2[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return text;
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			text2 = text;
			foreach (char c in text2)
			{
				if (!IsMarker(c))
				{
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}
	}

	private sealed class Builder
	{
		public ClickyInkDocument Document = new ClickyInkDocument(null, new List<ClickyInkDocument.Page>());

		private readonly List<string> paragraph = new List<string>();

		private int tileStrips;

		public List<ClickyInkDocument.Block>? CurrentBlocks
		{
			get
			{
				if (Document.Pages.Count <= 0)
				{
					return null;
				}
				List<ClickyInkDocument.Page> pages = Document.Pages;
				return pages[pages.Count - 1].Blocks;
			}
		}

		public void StartPage(string name, bool isBoard)
		{
			if (Document.Pages.Count == 1)
			{
				List<ClickyInkDocument.Page> pages = Document.Pages;
				if (pages[pages.Count - 1].Blocks.Count == 0)
				{
					Document.Pages[0] = new ClickyInkDocument.Page(name, isBoard, new List<ClickyInkDocument.Block>());
					return;
				}
			}
			if (Document.Pages.Count < 10)
			{
				Document.Pages.Add(new ClickyInkDocument.Page((name.Length == 0) ? $"Page {Document.Pages.Count + 1}" : name, isBoard, new List<ClickyInkDocument.Block>()));
			}
		}

		public void Add(ClickyInkDocument.Block block)
		{
			if (Document.Pages.Count == 0)
			{
				Document.Pages.Add(new ClickyInkDocument.Page("In short", isBoard: false, new List<ClickyInkDocument.Block>()));
			}
			List<ClickyInkDocument.Page> pages = Document.Pages;
			if (pages[pages.Count - 1].Blocks.Count < 30)
			{
				List<ClickyInkDocument.Page> pages2 = Document.Pages;
				pages2[pages2.Count - 1].Blocks.Add(block);
			}
		}

		public void AppendParagraphLine(string line)
		{
			paragraph.Add(line);
		}

		public void EndParagraph()
		{
			if (paragraph.Count == 0)
			{
				return;
			}
			List<string> list = paragraph.ToList();
			paragraph.Clear();
			List<string> list2 = new List<string>();
			foreach (string item in list)
			{
				List<(string, string)> list3 = LinksOnly(item);
				if (list3 != null)
				{
					AddProse(list2);
					list2 = new List<string>();
					foreach (var item2 in list3)
					{
						Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Link, Clean(item2.Item1), new Dictionary<string, string> { ["url"] = item2.Item2 }));
					}
				}
				else
				{
					list2.Add(item);
				}
			}
			AddProse(list2);
		}

		private void AddProse(List<string> lines)
		{
			if (lines.Count != 0)
			{
				var (text, text2) = InlineLinks(string.Join((lines.Count > 1 && lines.Take(lines.Count - 1).All((string l) => l.CharacterCount() >= 64)) ? " " : "\n", lines));
				var (text3, dictionary) = TrailingAttributes(text);
				if (text2 != null && !dictionary.ContainsKey("url"))
				{
					dictionary["url"] = text2;
				}
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Paragraph, Clean(text3), dictionary));
			}
		}

		public void AddLeaf(DirectiveHeader directive)
		{
			ClickyInkDocument.Kind kind;
			switch (directive.Name)
			{
			case "answer":
				kind = ClickyInkDocument.Kind.Answer;
				break;
			case "figure":
			case "number":
			case "stat":
				kind = ClickyInkDocument.Kind.Figure;
				break;
			case "sticky":
			case "postit":
				kind = ClickyInkDocument.Kind.Sticky;
				break;
			case "ticket":
			case "flight":
			case "pass":
				kind = ClickyInkDocument.Kind.Ticket;
				break;
			case "photo":
			case "image":
			case "polaroid":
				kind = ClickyInkDocument.Kind.Photo;
				break;
			case "place":
			case "pin":
			case "map":
				kind = ClickyInkDocument.Kind.Place;
				break;
			case "clip":
			case "quote":
			case "clipping":
				kind = ClickyInkDocument.Kind.Clip;
				break;
			case "file":
			case "preview":
				kind = ClickyInkDocument.Kind.File;
				break;
			case "source":
			case "link":
				kind = ClickyInkDocument.Kind.Link;
				break;
			case "button":
			case "action":
			case "ask":
				kind = ClickyInkDocument.Kind.Ask;
				break;
			case "question":
			case "callout":
				kind = ClickyInkDocument.Kind.Question;
				break;
			case "margin":
			case "note":
			case "aside":
				kind = ClickyInkDocument.Kind.Note;
				break;
			case "metric":
			case "change":
			case "delta":
			case "kpi":
				kind = ClickyInkDocument.Kind.Figure;
				break;
			case "spark":
			case "trend":
			case "sparkline":
				kind = ClickyInkDocument.Kind.Spark;
				break;
			case "status":
			case "signal":
			case "health":
			case "light":
				kind = ClickyInkDocument.Kind.Status;
				break;
			case "goal":
			case "meter":
			case "progress":
			case "bar":
				kind = ClickyInkDocument.Kind.Progress;
				break;
			case "tally":
			case "count":
			case "counter":
				kind = ClickyInkDocument.Kind.Tally;
				break;
			case "habits":
			case "streak":
			case "habit":
				kind = ClickyInkDocument.Kind.Streak;
				break;
			case "until":
			case "deadline":
			case "due":
			case "countdown":
				kind = ClickyInkDocument.Kind.Countdown;
				break;
			case "stages":
			case "funnel":
			case "pipeline":
				kind = ClickyInkDocument.Kind.Pipeline;
				break;
			case "run":
			case "runline":
			case "edition":
				kind = ClickyInkDocument.Kind.Run;
				break;
			case "quiet":
			case "allquiet":
			case "nochange":
			case "nothing":
			case "unchanged":
				kind = ClickyInkDocument.Kind.Quiet;
				break;
			case "connect":
			case "integration":
			case "connectapp":
				kind = ClickyInkDocument.Kind.Connect;
				break;
			case "event":
			case "calendar":
			case "meeting":
			case "appointment":
				kind = ClickyInkDocument.Kind.Event;
				break;
			default:
				kind = ((directive.Name.Length == 0) ? ClickyInkDocument.Kind.Paragraph : ClickyInkDocument.Kind.Unknown);
				break;
			}
			ClickyInkDocument.Kind kind2 = kind;
			if (kind2 == ClickyInkDocument.Kind.Unknown)
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>(directive.Attributes);
				dictionary.TryAdd("was", directive.Name);
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Paragraph, directive.Label, dictionary));
			}
			else
			{
				Add(new ClickyInkDocument.Block(kind2, directive.Label, new Dictionary<string, string>(directive.Attributes)));
			}
		}

		public void AddContainer(DirectiveHeader directive, List<string> body)
		{
			List<string> list = (from l in body
				select l.TrimWs() into l
				where l.Length > 0
				select l).ToList();
			Dictionary<string, string> dictionary = new Dictionary<string, string>(directive.Attributes);
			string name = directive.Name;
			switch (name)
			{
			case "receipt":
			case "budget":
			case "bill":
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Receipt, directive.Label, dictionary, list.Take(40).Select(AmountLine).ToList()));
				return;
			case "agenda":
			case "schedule":
			case "day":
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Day, directive.Label, dictionary, list.Take(40).Select(TimedLine).ToList()));
				return;
			case "message":
			case "letter":
			case "draft":
			case "email":
				if (directive.Label.Length > 0)
				{
					dictionary.TryAdd("subject", directive.Label);
				}
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Letter, string.Join("\n", body).TrimWsNl(), dictionary));
				return;
			case "drawing":
			case "diagram":
			case "sketch":
			case "draw":
				dictionary["sketch"] = string.Join("\n", list);
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Drawing, directive.Label, dictionary));
				return;
			case "chart":
			case "graph":
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Chart, directive.Label, dictionary, list.Take(16).Select(AmountLine).ToList()));
				return;
			case "sticky":
			case "note":
				Add(new ClickyInkDocument.Block((directive.Name == "sticky") ? ClickyInkDocument.Kind.Sticky : ClickyInkDocument.Kind.Note, string.Join("\n", list), dictionary));
				return;
			case "callout":
			case "question":
				Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Question, string.Join(" ", from s in new string[1] { directive.Label }.Concat(list)
					where s.Length > 0
					select s), dictionary));
				return;
			}
			if (!ObjectHoldingContainers.Contains(name))
			{
				switch (name)
				{
				case "review":
				case "approve":
				case "confirm":
				case "proposed":
				case "approval":
				{
					IEnumerable<string> values = body.Select(delegate(string line)
					{
						string text12 = line.TrimWs();
						bool flag = line.HasPrefix("  ") || line.HasPrefix("\t");
						return (text12.Length == 0 || flag || (object)ListItemStart(text12) != null) ? line : ("- [ ] " + text12);
					});
					List<ClickyInkDocument.Item> items2 = Parse(string.Join("\n", values)).AllBlocks.Select<(int, ClickyInkDocument.Block), ClickyInkDocument.Block>(((int Page, ClickyInkDocument.Block Block) b) => b.Block).Where(delegate(ClickyInkDocument.Block b)
					{
						ClickyInkDocument.Kind kind = b.Kind;
						return (uint)(kind - 4) <= 2u;
					}).SelectMany((ClickyInkDocument.Block b) => b.Items)
						.Take(40)
						.Select(delegate(ClickyInkDocument.Item item)
						{
							bool? isChecked = item.IsChecked;
							bool valueOrDefault = isChecked == true;
							if (!isChecked.HasValue)
							{
								valueOrDefault = false;
								bool? isChecked2 = valueOrDefault;
								item.IsChecked = isChecked2;
							}
							return item;
						})
						.ToList();
					dictionary["review"] = directive.Label;
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Review, directive.Label, dictionary, items2));
					return;
				}
				case "inputs":
				case "fields":
				case "form":
				{
					List<ClickyInkDocument.Item> items4 = list.Take(40).Select(delegate(string line)
					{
						string text16 = ListItemStart(line)?.Text ?? line;
						int num8 = text16.IndexOf(':');
						if (num8 >= 0)
						{
							string text17 = text16.Substring(0, num8).TrimWs();
							string text18 = text16;
							int num9 = num8 + 1;
							string text19 = text18.Substring(num9, text18.Length - num9).TrimWs();
							return new ClickyInkDocument.Item(Clean(text17), (text19.Length == 0) ? null : text19, null, null, null, null, null);
						}
						return new ClickyInkDocument.Item(Clean(text16), null, null, null, null, null, null);
					}).ToList();
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Form, directive.Label, dictionary, items4));
					return;
				}
				case "rows":
				case "leads":
				case "items":
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Items, directive.Label, dictionary, ListItems(body)));
					return;
				case "tracker":
				case "todo":
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Todo, directive.Label, dictionary, ListItems(body, boxed: true)));
					return;
				case "quiz":
				case "cards":
				case "drill":
				case "flashcards":
				{
					List<ClickyInkDocument.Item> list4 = new List<ClickyInkDocument.Item>();
					foreach (string item6 in list)
					{
						string text7 = ListItemStart(item6)?.Text ?? item6;
						string s2 = text7.Lowered();
						if (s2.HasPrefix("a:") || s2.HasPrefix("answer:"))
						{
							if (list4.Count != 0)
							{
								string text8 = text7;
								int num4 = text7.IndexOf(':') + 1;
								string text9 = text8.Substring(num4, text8.Length - num4).TrimWs();
								ClickyInkDocument.Item item5 = list4[list4.Count - 1];
								string[] array2 = new string[2];
								array2[0] = list4[list4.Count - 1].Detail;
								array2[1] = Clean(text9);
								item5.Detail = string.Join(" ", array2.Where((string s) => s != null));
							}
						}
						else if (s2.HasPrefix("q:") || s2.HasPrefix("question:"))
						{
							string text8 = text7;
							int num4 = text7.IndexOf(':') + 1;
							string text10 = text8.Substring(num4, text8.Length - num4).TrimWs();
							list4.Add(new ClickyInkDocument.Item(Clean(text10), null, null, null, null, null, null));
						}
						else
						{
							int num5 = text7.Find(" | ");
							if (num5 >= 0)
							{
								string text11 = Clean(text7.Substring(0, num5));
								string text8 = text7;
								int num4 = num5 + 3;
								list4.Add(new ClickyInkDocument.Item(text11, Clean(text8.Substring(num4, text8.Length - num4)), null, null, null, null, null));
							}
							else
							{
								list4.Add(new ClickyInkDocument.Item(Clean(text7), null, null, null, null, null, null));
							}
						}
					}
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Quiz, directive.Label, dictionary, list4.Take(40).ToList()));
					return;
				}
				case "gallery":
				case "variants":
				{
					List<ClickyInkDocument.Item> items = list.Take(16).Select(delegate(string line)
					{
						string text = ListItemStart(line)?.Text ?? line;
						(string Words, Dictionary<string, string> Attributes) tuple = TrailingAttributes(text);
						string item2 = tuple.Words;
						Dictionary<string, string> item3 = tuple.Attributes;
						text = item2;
						string value;
						string value2;
						string text2 = (item3.TryGetValue("path", out value) ? value : (item3.TryGetValue("url", out value2) ? value2 : null));
						string text3 = text;
						string text5;
						if (text2 == null)
						{
							int num = -1;
							int num2 = 0;
							string[] array = new string[3] { " — ", " – ", " - " };
							foreach (string text4 in array)
							{
								int num3 = text.Find(text4);
								if (num3 >= 0)
								{
									num = num3;
									num2 = text4.Length;
									break;
								}
							}
							if (num >= 0)
							{
								text2 = text.Substring(0, num).TrimWs();
								text5 = text;
								int j = num + num2;
								text3 = text5.Substring(j, text5.Length - j).TrimWs();
							}
							else
							{
								text2 = text.TrimWs();
								text3 = "";
							}
						}
						ClickyInkDocument.Mark? mark = ClickyInkDocumentKinds.FirstMark(item3.Keys);
						string text6 = Clean(text3);
						text5 = text2;
						return new ClickyInkDocument.Item(text6, null, null, mark, null, text5, null);
					}).ToList();
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Gallery, directive.Label, dictionary, items));
					return;
				}
				case "filters":
				case "criteria":
				{
					List<ClickyInkDocument.Item> items3 = list.Take(16).Select(delegate(string line)
					{
						string text13 = ListItemStart(line)?.Text ?? line;
						int num6 = text13.IndexOf(':');
						if (num6 >= 0)
						{
							string text14 = Clean(text13.Substring(0, num6));
							string text15 = text13;
							int num7 = num6 + 1;
							return new ClickyInkDocument.Item(text14, text15.Substring(num7, text15.Length - num7).TrimWs(), null, null, null, null, null);
						}
						return new ClickyInkDocument.Item(Clean(text13), null, null, null, null, null, null);
					}).ToList();
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Criteria, directive.Label, dictionary, items3));
					return;
				}
				case "stages":
				case "funnel":
				case "pipeline":
					if (!dictionary.ContainsKey("steps"))
					{
						dictionary["steps"] = ClickyInkPipeline.Attribute((from s in list.Select(delegate(string line)
							{
								ClickyInkDocument.Item item4 = AmountLine(line);
								return (item4.Cells.Count <= 1) ? ClickyInkPipeline.StageOf(item4.Text) : new ClickyInkPipeline.Stage?(new ClickyInkPipeline.Stage(item4.Cells[0], item4.Cells[1]));
							})
							where s.HasValue
							select s.Value).ToList());
					}
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Pipeline, directive.Label, dictionary));
					return;
				case "spark":
				case "trend":
				case "sparkline":
				{
					List<ClickyInkDocument.Item> list2 = list.Select(AmountLine).ToList();
					if (!dictionary.ContainsKey("values"))
					{
						dictionary["values"] = string.Join(", ", list2.Select((ClickyInkDocument.Item i) => (i.Cells.Count <= 1) ? i.Text : i.Cells[1]));
						List<string> list3 = (from i in list2
							where i.Cells.Count > 1
							select i.Cells[0]).ToList();
						if (list3.Count == list2.Count && list3.Count > 0 && !dictionary.ContainsKey("labels"))
						{
							dictionary["labels"] = string.Join(", ", list3);
						}
					}
					Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Spark, directive.Label, dictionary));
					return;
				}
				}
				{
					foreach (ClickyInkDocument.Page page in Parse(string.Join("\n", body)).Pages)
					{
						foreach (ClickyInkDocument.Block block in page.Blocks)
						{
							Add(block);
						}
					}
					return;
				}
			}
			tileStrips++;
			foreach (ClickyInkDocument.Page page2 in Parse(string.Join("\n", body)).Pages)
			{
				foreach (ClickyInkDocument.Block block2 in page2.Blocks)
				{
					if (ClickyInkDashboardKinds.TileKinds.Contains(block2.Kind) || block2.Kind == ClickyInkDocument.Kind.Paragraph)
					{
						block2.Attributes["_tiles"] = tileStrips.ToString(CultureInfo.InvariantCulture);
					}
					Add(block2);
				}
			}
		}

		private static List<ClickyInkDocument.Item> ListItems(List<string> body, bool boxed = false)
		{
			IEnumerable<string> values = body.Select(delegate(string line)
			{
				string text = line.TrimWs();
				bool flag = line.HasPrefix("  ") || line.HasPrefix("\t");
				return (text.Length == 0 || flag || (object)ListItemStart(text) != null) ? line : ((boxed ? "- [ ] " : "- ") + text);
			});
			return Parse(string.Join("\n", values)).AllBlocks.Select<(int, ClickyInkDocument.Block), ClickyInkDocument.Block>(((int Page, ClickyInkDocument.Block Block) b) => b.Block).Where(delegate(ClickyInkDocument.Block b)
			{
				ClickyInkDocument.Kind kind = b.Kind;
				return (uint)(kind - 4) <= 2u;
			}).SelectMany((ClickyInkDocument.Block b) => b.Items)
				.Take(40)
				.Select(delegate(ClickyInkDocument.Item item)
				{
					if (boxed && !item.IsChecked.HasValue)
					{
						item.IsChecked = false;
					}
					return item;
				})
				.ToList();
		}

		public static ClickyInkDocument.Item AmountLine(string line)
		{
			string text = line;
			ListStart listStart = ListItemStart(line);
			if ((object)listStart != null)
			{
				text = listStart.Text;
			}
			(string Words, Dictionary<string, string> Attributes) tuple = TrailingAttributes(text);
			string item = tuple.Words;
			ClickyInkDocument.Mark? mark = ClickyInkDocumentKinds.FirstMark(tuple.Attributes.Keys);
			string[] array = new string[7] { " | ", ": ", " … ", "...", " — ", " - ", "\t" };
			Span<string> span;
			int num3;
			foreach (string text2 in array)
			{
				int num = item.FindLast(text2);
				if (num >= 0)
				{
					string text3 = item.Substring(0, num).TrimSet(" .…");
					string text4 = item;
					int num2 = num + text2.Length;
					string text5 = text4.Substring(num2, text4.Length - num2).TrimWs();
					if (text3.Length > 0 && text5.Length > 0)
					{
						string text6 = Clean(text3);
						num2 = 2;
						List<string> list = new List<string>(num2);
						CollectionsMarshal.SetCount(list, num2);
						span = CollectionsMarshal.AsSpan(list);
						num3 = 0;
						span[num3] = Clean(text3);
						num3++;
						span[num3] = text5;
						return new ClickyInkDocument.Item(text6, null, list, mark, null, null, null);
					}
				}
			}
			string text7 = Clean(item);
			int i = 1;
			List<string> list2 = new List<string>(i);
			CollectionsMarshal.SetCount(list2, i);
			span = CollectionsMarshal.AsSpan(list2);
			num3 = 0;
			span[num3] = Clean(item);
			return new ClickyInkDocument.Item(text7, null, list2, mark, null, null, null);
		}

		public static ClickyInkDocument.Item TimedLine(string line)
		{
			string text = line;
			ListStart listStart = ListItemStart(line);
			if ((object)listStart != null)
			{
				text = listStart.Text;
			}
			(string Words, Dictionary<string, string> Attributes) tuple = TrailingAttributes(text);
			string item = tuple.Words;
			ClickyInkDocument.Mark? mark = ClickyInkDocumentKinds.FirstMark(tuple.Attributes.Keys);
			Match match = TimedLineRegex().Match(item);
			if (match.Success)
			{
				string text2 = item;
				int num = match.Index + match.Length;
				string text3 = text2.Substring(num, text2.Length - num);
				string text4 = null;
				string text5 = text3;
				int num2 = text3.Find(" — ");
				if (num2 < 0)
				{
					num2 = text3.Find(" – ");
				}
				if (num2 >= 0)
				{
					text5 = text3.Substring(0, num2);
					text2 = text3;
					num = num2 + 3;
					text4 = text2.Substring(num, text2.Length - num);
				}
				string text6 = Clean(text5);
				string detail = ((text4 == null) ? null : Clean(text4));
				num = 1;
				List<string> list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<string> span = CollectionsMarshal.AsSpan(list);
				int index = 0;
				span[index] = match.Groups[1].Value.TrimWs();
				return new ClickyInkDocument.Item(text6, detail, list, mark, null, null, null);
			}
			return new ClickyInkDocument.Item(Clean(item), null, null, mark, null, null, null);
		}

		public ClickyInkDocument Finished()
		{
			return new ClickyInkDocument(Document.Title, Document.Pages.Where((ClickyInkDocument.Page p) => p.Blocks.Count > 0).ToList());
		}
	}

	public const int MaximumPages = 10;

	public const int MaximumBlocksPerPage = 30;

	public const int MaximumItems = 40;

	public static readonly IReadOnlySet<string> KnownObjects = new HashSet<string>
	{
		"answer", "figure", "sticky", "ticket", "receipt", "photo", "place", "day", "letter", "clip",
		"quote", "file", "link", "source", "ask", "question", "chart", "note", "board", "drawing",
		"sketch", "diagram", "spark", "status", "progress", "tally", "streak", "countdown", "pipeline", "run",
		"quiet", "tiles", "review", "form", "connect", "items", "quiz", "todo", "gallery", "event",
		"criteria"
	};

	public static readonly IReadOnlySet<string> ObjectHoldingContainers = new HashSet<string> { "tiles", "stats", "strip", "grid", "kpis", "metrics", "dashboard" };

	public static ClickyInkDocument Parse(string source, bool isPartial = false)
	{
		List<string> source2 = source.Replace("\r\n", "\n").Split('\n').ToList();
		source2 = source2.SelectMany(delegate(string line)
		{
			string text = line.TrimWs();
			if (!text.HasPrefix("::") || text.HasPrefix(":::") || !text.Contains("} ::", StringComparison.Ordinal))
			{
				return new string[1] { line };
			}
			string[] pieces = text.Components("} ::");
			return pieces.Select((string piece, int index) => ((index == 0) ? "" : "::") + piece + ((piece.HasSuffix("}") || index == pieces.Length - 1) ? "" : "}")).ToArray();
		}).ToList();
		if (isPartial && !source.HasSuffix("\n") && source2.Count > 0)
		{
			List<string> list = source2;
			string text2 = list[list.Count - 1];
			string text3 = text2.TrimWs();
			bool num = text3.HasPrefix("::") || text3.HasPrefix("|") || text3.HasPrefix("```") || text3.HasPrefix("#") || text3.Contains('{') || !text3.Contains(' ') || text3.HasPrefix("[");
			source2.RemoveAt(source2.Count - 1);
			int num2 = text2.LastIndexOf(' ');
			if (!num && num2 >= 0)
			{
				string text4 = text2.Substring(0, num2);
				if ((ListItemStart(text4.TrimWs())?.Text ?? text4).TrimWs().Length > 0)
				{
					source2.Add(text4);
				}
			}
		}
		Builder builder = new Builder();
		int i = 0;
		while (i < source2.Count)
		{
			string text5 = source2[i].TrimWs();
			i++;
			if (text5.Length == 0)
			{
				builder.EndParagraph();
				continue;
			}
			if (text5.HasPrefix("```"))
			{
				builder.EndParagraph();
				string text6 = text5;
				string text7 = text6.Substring(3, text6.Length - 3).TrimWs().Lowered();
				List<string> list2 = new List<string>();
				bool flag = false;
				while (i < source2.Count)
				{
					string text8 = source2[i];
					i++;
					if (text8.TrimWs().HasPrefix("```"))
					{
						flag = true;
						break;
					}
					list2.Add(text8);
				}
				if (!flag && isPartial)
				{
					break;
				}
				if ((text7 == "draw" || text7 == "sketch") ? true : false)
				{
					builder.Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Drawing, "", new Dictionary<string, string>
					{
						["lang"] = text7,
						["sketch"] = string.Join("\n", list2)
					}));
				}
				else
				{
					builder.Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Code, string.Join("\n", list2), (text7.Length == 0) ? new Dictionary<string, string>() : new Dictionary<string, string> { ["lang"] = text7 }));
				}
				continue;
			}
			if (text5.HasPrefix(":::"))
			{
				builder.EndParagraph();
				string text6 = text5;
				string text9 = text6.Substring(3, text6.Length - 3);
				if (text9.TrimWs().Length == 0)
				{
					continue;
				}
				DirectiveHeader directiveHeader = ParseDirectiveHeader(text9);
				bool flag2 = ObjectHoldingContainers.Contains(directiveHeader.Name);
				bool flag3 = isPartial;
				for (int j = i; j < source2.Count; j++)
				{
					string text10 = source2[j].TrimWs();
					if (text10 == ":::")
					{
						flag3 = true;
						break;
					}
					if (text10.HasPrefix("# ") || text10.HasPrefix("## ") || (text10.HasPrefix(":::") && text10.Length > 3))
					{
						flag3 = false;
						break;
					}
				}
				List<string> list3 = new List<string>();
				List<string> list4 = new List<string>();
				bool flag4 = false;
				while (i < source2.Count)
				{
					string text11 = source2[i];
					string text12 = text11.TrimWs();
					if (text12 == ":::")
					{
						i++;
						flag4 = true;
						break;
					}
					if (!flag2 && flag3 && text12.HasPrefix("::") && !text12.HasPrefix(":::"))
					{
						list4.Add(text12);
						i++;
						continue;
					}
					if (text12.HasPrefix("# ") || text12.HasPrefix("## ") || (text12.HasPrefix(":::") && text12.Length > 3) || (!flag2 && text12.HasPrefix("::") && !text12.HasPrefix(":::")))
					{
						flag4 = true;
						break;
					}
					list3.Add(text11);
					i++;
				}
				if (!flag4 && isPartial)
				{
					builder.AddContainer(directiveHeader, list3);
					foreach (string item5 in list4)
					{
						text6 = item5;
						builder.AddLeaf(ParseDirectiveHeader(text6.Substring(2, text6.Length - 2)));
					}
					break;
				}
				builder.AddContainer(directiveHeader, list3);
				foreach (string item6 in list4)
				{
					text6 = item6;
					builder.AddLeaf(ParseDirectiveHeader(text6.Substring(2, text6.Length - 2)));
				}
				continue;
			}
			if ((text5.HasPrefix("- ::") || text5.HasPrefix("* ::")) && !text5.HasPrefix("- :::"))
			{
				builder.EndParagraph();
				string text6 = text5;
				builder.AddLeaf(ParseDirectiveHeader(text6.Substring(4, text6.Length - 4)));
				continue;
			}
			if (text5.HasPrefix("::"))
			{
				builder.EndParagraph();
				string text6 = text5;
				builder.AddLeaf(ParseDirectiveHeader(text6.Substring(2, text6.Length - 2)));
				continue;
			}
			if (text5.HasPrefix("# "))
			{
				builder.EndParagraph();
				string text6 = text5;
				string text13 = Clean(text6.Substring(2, text6.Length - 2));
				if (builder.Document.Title == null)
				{
					builder.Document.Title = text13;
				}
				else
				{
					builder.Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Heading, text13));
				}
				continue;
			}
			if (text5.HasPrefix("## "))
			{
				builder.EndParagraph();
				string text6 = text5;
				var (text14, dictionary) = TrailingAttributes(text6.Substring(3, text6.Length - 3));
				builder.StartPage(Clean(text14), dictionary.ContainsKey("board") || (dictionary.TryGetValue("layout", out var value) && value == "board"));
				continue;
			}
			if (text5.HasPrefix("### ") || text5.HasPrefix("#### "))
			{
				builder.EndParagraph();
				builder.Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Heading, Clean(text5.TrimStart('#').TrimWs())));
				continue;
			}
			bool flag5;
			switch (text5)
			{
			case "---":
			case "***":
			case "___":
				flag5 = true;
				break;
			default:
				flag5 = false;
				break;
			}
			if (flag5)
			{
				builder.EndParagraph();
				builder.Add(new ClickyInkDocument.Block(ClickyInkDocument.Kind.Rule));
				continue;
			}
			if (text5.HasPrefix(">"))
			{
				builder.EndParagraph();
				List<string> list5 = new List<string>();
				string text6 = text5;
				list5.Add(text6.Substring(1, text6.Length - 1).TrimWs());
				List<string> list6 = list5;
				for (; i < source2.Count && source2[i].TrimWs().HasPrefix(">"); i++)
				{
					text6 = source2[i].TrimWs();
					list6.Add(text6.Substring(1, text6.Length - 1).TrimWs());
				}
				(string Words, Dictionary<string, string> Attributes) tuple2 = TrailingAttributes(string.Join(" ", list6));
				string item = tuple2.Words;
				Dictionary<string, string> item2 = tuple2.Attributes;
				bool flag6 = builder.Document.Pages.Count <= 1 && (builder.CurrentBlocks?.Count ?? 0) == 0;
				builder.Add(new ClickyInkDocument.Block((flag6 || item2.ContainsKey("big")) ? ClickyInkDocument.Kind.Answer : ClickyInkDocument.Kind.Quote, Clean(item), item2));
				continue;
			}
			if (text5.HasPrefix("|"))
			{
				builder.EndParagraph();
				List<string> list7 = new List<string> { text5 };
				for (; i < source2.Count && source2[i].TrimWs().HasPrefix("|"); i++)
				{
					list7.Add(source2[i].TrimWs());
				}
				builder.Add(ParseTable(list7));
				continue;
			}
			ListStart listStart = ListItemStart(text5);
			if ((object)listStart != null)
			{
				builder.EndParagraph();
				List<(ListStart, List<string>)> list8 = new List<(ListStart, List<string>)> { (listStart, new List<string>()) };
				while (i < source2.Count)
				{
					string s = source2[i];
					string text15 = s.TrimWs();
					if (text15.Length == 0)
					{
						if (i + 1 >= source2.Count)
						{
							break;
						}
						ListStart listStart2 = ListItemStart(source2[i + 1].TrimWs());
						if ((object)listStart2 == null || listStart2.Kind != listStart.Kind)
						{
							break;
						}
						i++;
					}
					else if ((s.HasPrefix("  ") || s.HasPrefix("\t")) && text15.Length > 0)
					{
						string item3 = ListItemStart(text15)?.Text ?? text15;
						list8[list8.Count - 1].Item2.Add(item3);
						i++;
					}
					else
					{
						ListStart listStart3 = ListItemStart(text15);
						if ((object)listStart3 == null || listStart3.Kind != listStart.Kind)
						{
							break;
						}
						list8.Add((listStart3, new List<string>()));
						i++;
					}
				}
				builder.Add(new ClickyInkDocument.Block(listStart.Kind, "", null, list8.Take(40).Select<(ListStart, List<string>), ClickyInkDocument.Item>(delegate((ListStart Start, List<string> Detail) entry)
				{
					ClickyInkDocument.Item item4 = entry.Start.Item;
					if (entry.Detail.Count > 0)
					{
						item4.Detail = string.Join(" ", entry.Detail.Select(Clean));
					}
					return item4;
				}).ToList()));
			}
			else
			{
				builder.AppendParagraphLine(text5);
			}
		}
		builder.EndParagraph();
		return builder.Finished();
	}

	public static DirectiveHeader ParseDirectiveHeader(string text)
	{
		string text2 = text.TrimWs();
		int num = 0;
		while (true)
		{
			bool flag = num < text2.Length;
			if (flag)
			{
				bool flag2 = char.IsLetter(text2, num);
				if (!flag2)
				{
					char c = text2[num];
					bool flag3 = ((c == '-' || c == '_') ? true : false);
					flag2 = flag3;
				}
				flag = flag2;
			}
			if (!flag)
			{
				break;
			}
			num += ((!char.IsSurrogatePair(text2, num)) ? 1 : 2);
		}
		string name = text2.Substring(0, num).Lowered();
		string text3 = text2;
		int num2 = num;
		text2 = text3.Substring(num2, text3.Length - num2);
		string text4 = "";
		if (text2.Length > 0 && text2[0] == '[')
		{
			int num3 = 0;
			int num4 = 0;
			for (int i = 0; i < text2.Length; i++)
			{
				if (text2[i] == '[')
				{
					num3++;
				}
				if (text2[i] == ']')
				{
					num3--;
					if (num3 == 0)
					{
						num4 = i;
						break;
					}
				}
			}
			if (num4 > 0)
			{
				text4 = text2.Substring(1, num4 - 1);
				text3 = text2;
				num2 = num4 + 1;
				text2 = text3.Substring(num2, text3.Length - num2);
			}
			else
			{
				text3 = text2;
				text4 = text3.Substring(1, text3.Length - 1);
				text2 = "";
			}
		}
		var (s, attributes) = TrailingAttributes(text2);
		if (text4.Length == 0)
		{
			text4 = s.TrimWs();
		}
		int num5 = text4.IndexOf('{');
		if (num5 >= 0)
		{
			text4 = text4.Substring(0, num5).TrimWs();
		}
		return new DirectiveHeader(name, Clean(text4), attributes);
	}

	public static (string Words, Dictionary<string, string> Attributes) TrailingAttributes(string text)
	{
		var (text2, dictionary) = TrailingAttributeGroup(text);
		while (dictionary.Count > 0 && text2.HasSuffix("}"))
		{
			var (text3, dictionary2) = TrailingAttributeGroup(text2);
			if (dictionary2.Count == 0)
			{
				break;
			}
			text2 = text3;
			foreach (var (key, value) in dictionary2)
			{
				dictionary.TryAdd(key, value);
			}
		}
		return (Words: text2, Attributes: dictionary);
	}

	private static (string, Dictionary<string, string>) TrailingAttributeGroup(string text)
	{
		int num = text.LastIndexOf('{');
		if (!text.HasSuffix("}") || num < 0)
		{
			return (text.TrimWs(), new Dictionary<string, string>());
		}
		int num2 = num + 1;
		string inside = text.Substring(num2, text.Length - 1 - num2);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		int position = 0;
		int end = inside.Length;
		while (position < end)
		{
			SkipSeparators();
			if (position >= end)
			{
				break;
			}
			int num3 = position;
			for (; position < end && "=: ,;".IndexOf(inside[position]) < 0; position++)
			{
			}
			string text2 = inside;
			num2 = num3;
			string text3 = text2.Substring(num2, position - num2).Lowered().TrimSet("#.");
			if (text3.Length == 0)
			{
				if (position < end)
				{
					position++;
				}
				continue;
			}
			bool flag = position < end;
			if (flag)
			{
				char c = inside[position];
				bool flag2 = ((c == ':' || c == '=') ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				position++;
				flag = position < end;
				if (flag)
				{
					char c = inside[position];
					bool flag2 = ((c == '"' || c == '\'' || c == '“') ? true : false);
					flag = flag2;
				}
				string value;
				if (flag)
				{
					char c2 = ((inside[position] == '“') ? '”' : inside[position]);
					position++;
					int num4 = position;
					for (; position < end && inside[position] != c2; position++)
					{
					}
					string text4 = inside;
					num2 = num4;
					value = text4.Substring(num2, position - num2);
					if (position < end)
					{
						position++;
					}
				}
				else
				{
					int num5 = position;
					for (; position < end; position++)
					{
						char c3 = inside[position];
						if ((c3 != ' ' && c3 != ',') || 1 == 0)
						{
							continue;
						}
						int num6 = position + 1;
						while (true)
						{
							flag = num6 < end;
							if (flag)
							{
								char c = inside[num6];
								bool flag2 = ((c == ' ' || c == ',') ? true : false);
								flag = flag2;
							}
							if (!flag)
							{
								break;
							}
							num6++;
						}
						bool flag3 = num6 >= end;
						int num7 = num6;
						while (true)
						{
							flag = num7 < end;
							if (flag)
							{
								bool flag2 = char.IsLetterOrDigit(inside[num7]) || char.IsNumber(inside[num7]);
								if (!flag2)
								{
									char c = inside[num7];
									bool flag4 = ((c == '-' || c == '_') ? true : false);
									flag2 = flag4;
								}
								flag = flag2;
							}
							if (!flag)
							{
								break;
							}
							num7++;
						}
						bool flag5 = num7 > num6 && char.IsLetter(inside[num6]);
						int i;
						for (i = num7; i < end && inside[i] == ' '; i++)
						{
						}
						flag = i < end;
						if (flag)
						{
							char c = inside[i];
							bool flag2 = ((c == ':' || c == '=') ? true : false);
							flag = flag2;
						}
						bool flag6 = flag;
						if (flag3 || (flag5 && flag6))
						{
							break;
						}
					}
					string text5 = inside;
					num2 = num5;
					value = text5.Substring(num2, position - num2).TrimWs();
				}
				dictionary[text3] = value;
			}
			else
			{
				dictionary[text3] = "";
			}
		}
		if (dictionary.Count == 0)
		{
			return (text.TrimWs(), new Dictionary<string, string>());
		}
		return (text.Substring(0, num).TrimWs(), dictionary);
		void SkipSeparators()
		{
			while (true)
			{
				bool flag7 = position < end;
				if (flag7)
				{
					char c4 = inside[position];
					bool flag8 = ((c4 == ' ' || c4 == ',' || c4 == ';') ? true : false);
					flag7 = flag8;
				}
				if (!flag7)
				{
					break;
				}
				position++;
			}
		}
	}

	public static ListStart? ListItemStart(string line)
	{
		ClickyInkDocument.Kind kind = ClickyInkDocument.Kind.List;
		int? number = null;
		string text2;
		if (line.HasPrefix("- ") || line.HasPrefix("* ") || line.HasPrefix("• ") || line.HasPrefix("+ "))
		{
			string text = line;
			text2 = text.Substring(2, text.Length - 2);
		}
		else
		{
			int num = -1;
			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];
				if ((c == ')' || c == '.') ? true : false)
				{
					num = i;
					break;
				}
			}
			if (num <= 0 || !line.Substring(0, num).All(char.IsNumber) || line.Substring(0, num).CharacterCount() > 3 || num + 1 >= line.Length || line[num + 1] != ' ')
			{
				return null;
			}
			long? num2 = InkText.ParseInt(line.Substring(0, num));
			int? obj;
			if (num2.HasValue)
			{
				long valueOrDefault = num2.GetValueOrDefault();
				obj = (int)valueOrDefault;
			}
			else
			{
				obj = null;
			}
			number = obj;
			string text = line;
			int num3 = num + 2;
			text2 = text.Substring(num3, text.Length - num3);
			kind = ClickyInkDocument.Kind.Numbered;
		}
		bool? isChecked = null;
		if (text2.HasPrefix("[ ] ") || text2.HasPrefix("[ ]"))
		{
			isChecked = false;
			string text = text2;
			text2 = text.Substring(3, text.Length - 3);
		}
		else if (text2.Lowered().HasPrefix("[x]"))
		{
			isChecked = true;
			string text = text2;
			text2 = text.Substring(3, text.Length - 3);
		}
		if (isChecked.HasValue)
		{
			kind = ClickyInkDocument.Kind.Checklist;
		}
		ClickyInkDocument.Mark? mark = null;
		int num4 = text2.IndexOf('}');
		if (text2.HasPrefix("{") && num4 >= 0)
		{
			ClickyInkDocument.Mark? mark2 = ClickyInkDocumentKinds.MarkFromWord(text2.Substring(1, num4 - 1).TrimWs());
			if (mark2.HasValue)
			{
				ClickyInkDocument.Mark valueOrDefault2 = mark2.GetValueOrDefault();
				mark = valueOrDefault2;
				string text = text2;
				int num3 = num4 + 1;
				text2 = text.Substring(num3, text.Length - num3);
			}
		}
		Dictionary<string, string> dictionary;
		string text3;
		(text3, dictionary) = TrailingAttributes(text2.TrimWs());
		if (mark.HasValue)
		{
			ClickyInkDocument.Mark valueOrDefault3 = mark.GetValueOrDefault();
			dictionary[valueOrDefault3.RawValue()] = "";
		}
		(string Text, ClickyInkDocument.Mark? Mark) tuple2 = ExtractingMarks(text3);
		string item = tuple2.Text;
		ClickyInkDocument.Mark? item2 = tuple2.Mark;
		text3 = item;
		if (item2.HasValue)
		{
			ClickyInkDocument.Mark valueOrDefault4 = item2.GetValueOrDefault();
			dictionary[valueOrDefault4.RawValue()] = (dictionary.TryGetValue(valueOrDefault4.RawValue(), out var value) ? value : "");
		}
		ClickyInkDocument.Mark? mark3 = ClickyInkDocumentKinds.FirstMark(dictionary.Keys);
		string text4 = null;
		int num5 = text3.IndexOf(']');
		if (text3.HasPrefix("[") && num5 >= 0 && num5 + 1 < text3.Length && text3[num5 + 1] == '(')
		{
			int num6 = text3.IndexOf(')', num5);
			if (num6 >= 0)
			{
				string text5 = text3;
				int num3 = num5 + 2;
				string text6 = text5.Substring(num3, num6 - num3);
				if (text6.HasPrefix("http"))
				{
					text4 = text6;
					string text7 = text3.Substring(1, num5 - 1);
					string text = text3;
					num3 = num6 + 1;
					text3 = "\ue005" + text7 + "\ue006" + text.Substring(num3, text.Length - num3);
				}
			}
		}
		if (text4 == null && dictionary.TryGetValue("url", out var value2) && value2.HasPrefix("http"))
		{
			text4 = value2;
		}
		if (text4 == null)
		{
			text4 = InlineLinks(text3).Url;
			if (text4 != null)
			{
				text3 = MarkingFirstLink(text3);
			}
		}
		return new ListStart(kind, new ClickyInkDocument.Item(Clean(text3), null, null, mark3, isChecked, text4, number));
	}

	public static ClickyInkDocument.Block ParseTable(List<string> rows)
	{
		List<string> list = rows.Where((string r) => !IsDivider(r)).ToList();
		if (list.Count == 0)
		{
			return new ClickyInkDocument.Block(ClickyInkDocument.Kind.Table);
		}
		string row2 = list[0];
		List<ClickyInkDocument.Item> items = list.Skip(1).Take(40).Select(delegate(string row)
		{
			List<string> list2 = Cells(row);
			ClickyInkDocument.Mark? mark = null;
			for (int i = 0; i < list2.Count; i++)
			{
				(string Words, Dictionary<string, string> Attributes) tuple = TrailingAttributes(list2[i]);
				string item = tuple.Words;
				ClickyInkDocument.Mark? mark2 = ClickyInkDocumentKinds.FirstMark(tuple.Attributes.Keys);
				if (mark2.HasValue)
				{
					ClickyInkDocument.Mark valueOrDefault = mark2.GetValueOrDefault();
					ClickyInkDocument.Mark valueOrDefault2 = mark.GetValueOrDefault();
					if (!mark.HasValue)
					{
						valueOrDefault2 = valueOrDefault;
						mark = valueOrDefault2;
					}
					list2[i] = item;
				}
				var (value, mark3) = ExtractingMarks(list2[i]);
				if (mark3.HasValue)
				{
					ClickyInkDocument.Mark valueOrDefault3 = mark3.GetValueOrDefault();
					ClickyInkDocument.Mark valueOrDefault2 = mark.GetValueOrDefault();
					if (!mark.HasValue)
					{
						valueOrDefault2 = valueOrDefault3;
						mark = valueOrDefault2;
					}
					list2[i] = value;
				}
			}
			if (!mark.HasValue && list2.Count > 0 && (list2[0].HasPrefix("**") || list2[0].HasPrefix("★")))
			{
				mark = ClickyInkDocument.Mark.Pick;
			}
			if (list2.Count > 0 && list2[0].HasPrefix("~~"))
			{
				mark = ClickyInkDocument.Mark.Gone;
			}
			List<string> list3 = list2.Select(Clean).ToList();
			return new ClickyInkDocument.Item(string.Join(" · ", list3), null, list3, mark, null, null, null);
		})
			.ToList();
		return new ClickyInkDocument.Block(ClickyInkDocument.Kind.Table, "", null, items, Cells(row2).Select(Clean).ToList());
		static List<string> Cells(string row)
		{
			string text2 = row.TrimWs();
			if (text2.HasPrefix("|"))
			{
				string text3 = text2;
				text2 = text3.Substring(1, text3.Length - 1);
			}
			if (text2.HasSuffix("|"))
			{
				string text3 = text2;
				text2 = text3.Substring(0, text3.Length - 1);
			}
			return (from c in text2.Split('|')
				select c.TrimWs()).ToList();
		}
		static bool IsDivider(string row)
		{
			string text = row.Replace("|", "").TrimWs();
			if (text.Length > 0)
			{
				return text.All((char c) => "-: ".Contains(c));
			}
			return false;
		}
	}

	// [GeneratedRegex("\\s*\\{\\s*(new|needs|done|pick|now|gone|out|ruled-out|ruledout|no|skip)\\s*\\}", RegexOptions.IgnoreCase)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex MarkRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__MarkRegex_38.Instance;
	}

	public static (string Text, ClickyInkDocument.Mark? Mark) ExtractingMarks(string text)
	{
		if (!text.Contains('{'))
		{
			return (Text: text, Mark: null);
		}
		Match match = MarkRegex().Match(text);
		if (!match.Success)
		{
			return (Text: text, Mark: null);
		}
		ClickyInkDocument.Mark? item = ClickyInkDocumentKinds.MarkFromWord(match.Groups[1].Value);
		return (Text: MarkRegex().Replace(text, "").Replace("  ", " ").TrimWs(), Mark: item);
	}

	public static string Clean(string text)
	{
		string text2 = InlineLinks(text).Text.TrimWs();
		text2 = EmphasisMarks(text2);
		string[] array = new string[6] { "**", "__", "~~", "★ ", "==", "`" };
		foreach (string oldValue in array)
		{
			text2 = text2.Replace(oldValue, "");
		}
		return text2;
	}

	// [GeneratedRegex("\\*\\*(.+?)\\*\\*")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex BoldRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__BoldRegex_39.Instance;
	}

	// [GeneratedRegex("==(.+?)==")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex HighlightRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__HighlightRegex_40.Instance;
	}

	public static string EmphasisMarks(string text)
	{
		if (!text.Contains("**", StringComparison.Ordinal) && !text.Contains("==", StringComparison.Ordinal))
		{
			return text;
		}
		string input = BoldRegex().Replace(text, $"{Emphasis.BoldOn}$1{Emphasis.BoldOff}");
		return HighlightRegex().Replace(input, $"{Emphasis.HighlightOn}$1{Emphasis.HighlightOff}");
	}

	// [GeneratedRegex("\\[([^\\]]+)\\]\\((https?://[^)\\s]+)\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex LinkRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__LinkRegex_41.Instance;
	}

	// [GeneratedRegex("(\\[[^\\]]+\\]\\(https?://[^)\\s]+\\))[ \\t]*(?=\\[[^\\]]+\\]\\(https?://)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex AdjacentLinksRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__AdjacentLinksRegex_42.Instance;
	}

	public static string MarkingFirstLink(string text)
	{
		Match match = LinkRegex().Match(text);
		if (!match.Success)
		{
			return text;
		}
		string text2 = text.Substring(0, match.Index);
		string obj = $"[{Emphasis.LinkOn}{match.Groups[1].Value}{Emphasis.LinkOff}]({match.Groups[2].Value})";
		int num = match.Index + match.Length;
		return text2 + obj + text.Substring(num, text.Length - num);
	}

	public static (string Text, string? Url) InlineLinks(string text)
	{
		if (!text.Contains("](", StringComparison.Ordinal))
		{
			return (Text: text, Url: null);
		}
		text = AdjacentLinksRegex().Replace(text, "$1 · ");
		Match match = LinkRegex().Match(text);
		string item = (match.Success ? match.Groups[2].Value : null);
		return (Text: LinkRegex().Replace(text, "$1"), Url: item);
	}

	public static List<(string Label, string Url)>? LinksOnly(string line)
	{
		string text = line.TrimWs();
		if (!text.HasPrefix("[") || !text.Contains("](", StringComparison.Ordinal))
		{
			return null;
		}
		MatchCollection matchCollection = LinkRegex().Matches(text);
		if (matchCollection.Count == 0)
		{
			return null;
		}
		if (!LinkRegex().Replace(text, "").All((char c) => " \t·•,;|-–—".Contains(c)))
		{
			return null;
		}
		return matchCollection.Select((Match m) => (m.Groups[1].Value, m.Groups[2].Value)).ToList();
	}

	public static ClickyInkDocument.Item AmountLine(string line)
	{
		return Builder.AmountLine(line);
	}

	public static ClickyInkDocument.Item TimedLine(string line)
	{
		return Builder.TimedLine(line);
	}

	// [GeneratedRegex("^((?:\\d{1,2}(?::\\d{2})?\\s*(?:am|pm|AM|PM)?)(?:\\s*[–-]\\s*\\d{1,2}(?::\\d{2})?\\s*(?:am|pm|AM|PM)?)?|(?:Morning|Afternoon|Evening|Night|Lunch|Dinner|Breakfast)(?=\\s*[:·—-]))\\s*[:·—-]?\\s+")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex TimedLineRegex()
	{
		return _003CRegexGenerator_g_003EF2A86C3D8F8653D6C99BE1B8441A5DADEC6C855822CB294D49492A00C67E55137__TimedLineRegex_43.Instance;
	}
}
