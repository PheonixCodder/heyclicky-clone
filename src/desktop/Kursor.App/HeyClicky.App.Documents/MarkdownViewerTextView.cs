using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents.Markdown;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerTextView : Grid
{
	private enum BoxKind
	{
		Quote,
		Code,
		Rule
	}

	private sealed class Box
	{
		public BoxKind Kind;

		public double PadLeft;

		public double PadRight;

		public double PadTop;

		public double PadBottom;

		public double MarginTop;

		public double MarginBottom;

		public double RuleHeight = 1.0;

		public Para? First;

		public Para? Last;
	}

	private sealed class Para
	{
		public required Paragraph P;

		public required List<Box> Boxes;

		public double SpacingBefore;

		public double SpacingAfter;

		public double Indent;

		public double ExtraRight;

		public double LineHeight;
	}

	private sealed record CopySegment(TextElement Element, string Text);

	private static class QuotePlate
	{
		public const double CornerRadius = 8.0;

		public const double BarInset = 12.0;

		public const double BarWidth = 2.0;

		public static double TextLeadingInset => 24.0;
	}

	private sealed class Builder(MarkdownViewerTextView view, MarkdownViewerPreparedDocument document)
	{
		private double pendingSeparator;

		private static TextBlock? measuring;

		private double S(double size)
		{
			if (!(size <= 1.0))
			{
				return Math.Round(size * view.TextScale, MidpointRounding.ToEven);
			}
			return size;
		}

		private double F(double metric)
		{
			return metric * view.TextScale;
		}

		public void Render(IReadOnlyList<MarkdownViewerBlock> blocks)
		{
			for (int i = 0; i < blocks.Count; i++)
			{
				if (i > 0)
				{
					AppendBlockSeparator();
				}
				AppendBlock(blocks[i], new List<Box>());
			}
		}

		private void AppendBlockSeparator()
		{
			pendingSeparator = 1.0;
		}

		private unsafe void AppendBlock(MarkdownViewerBlock block, List<Box> enclosing)
		{
			if (!(block is MarkdownViewerBlock.Heading heading))
			{
				if (!(block is MarkdownViewerBlock.Paragraph paragraph))
				{
					if (!(block is MarkdownViewerBlock.CodeBlock codeBlock))
					{
						if (!(block is MarkdownViewerBlock.Blockquote blockquote))
						{
							if (!(block is MarkdownViewerBlock.List list))
							{
								if (!(block is MarkdownViewerBlock.Table table))
								{
									if (!(block is MarkdownViewerBlock.HorizontalRule))
									{
										if (block is MarkdownViewerBlock.Image image)
										{
											AppendImage(image.AlternateText, image.Source, enclosing);
										}
									}
									else
									{
										AppendHorizontalRule(enclosing);
									}
								}
								else
								{
									AppendTable(table.HeaderCells, table.Rows, enclosing);
								}
							}
							else
							{
								AppendList(list.Items, enclosing);
							}
							return;
						}
						bool flag = MarkdownViewerParser.IsCallout(blockquote.ChildBlocks);
						Box item = new Box
						{
							Kind = BoxKind.Quote,
							PadLeft = QuotePlate.TextLeadingInset,
							PadRight = (flag ? 12.0 : 42.0),
							PadTop = 10.0,
							PadBottom = 10.0,
							MarginTop = 4.0,
							MarginBottom = 4.0
						};
						view.boxes.Add(item);
						int count = view.segments.Count;
						int count2 = view.paras.Count;
						List<Box> enclosing2 = new List<Box>(enclosing) { item };
						for (int i = 0; i < blockquote.ChildBlocks.Count; i++)
						{
							if (i > 0)
							{
								AppendBlockSeparator();
							}
							AppendBlock(blockquote.ChildBlocks[i], enclosing2);
						}
						if (!flag && view.paras.Count > count2)
						{
							string text = string.Concat(from s in view.segments.Skip(count)
								select s.Text).Trim();
							Para para = view.paras[count2];
							if (text.Length > 0)
							{
								MarkdownViewerTextView markdownViewerTextView = view;
								List<Box> boxes = para.Boxes;
								markdownViewerTextView.AddCopyButton(boxes[boxes.Count - 1], text);
							}
						}
					}
					else
					{
						Box target = AppendCodeBlock(codeBlock.Code, enclosing);
						if (codeBlock.Code.Length > 0)
						{
							view.AddCopyButton(target, codeBlock.Code);
						}
					}
				}
				else
				{
					Para para2 = NewParagraph(enclosing, F(7.0), F(7.0), S(16.0), F(5.5));
					AppendInline(para2, paragraph.InlineMarkdown, S(16.0), MarkdownFontWeight.Regular, view.colors.BodyText);
				}
			}
			else
			{
				object obj = heading.Level switch
				{
					1 => (28.0, 18.0), 
					2 => (22.0, 24.0), 
					3 => (18.0, 18.0), 
					_ => (16.0, 14.0), 
				};
				double item2 = ((ValueTuple<double, double>*)(&obj))->Item1;
				double item3 = ((ValueTuple<double, double>*)(&obj))->Item2;
				Para para3 = NewParagraph(enclosing, F(item3), F(6.0), S(item2), 0.0);
				AppendInline(para3, heading.InlineMarkdown, S(item2), (heading.Level <= 2) ? MarkdownFontWeight.Bold : MarkdownFontWeight.Semibold, view.colors.HeadingText);
			}
		}

		private Para NewParagraph(List<Box> enclosing, double spacingBefore, double spacingAfter, double fontSize, double lineSpacing, bool fixedLineHeight = true, double lineHeightFactor = 1.193)
		{
			Paragraph paragraph = new Paragraph
			{
				FontSize = fontSize
			};
			Para para = new Para
			{
				P = paragraph,
				Boxes = new List<Box>(enclosing),
				SpacingBefore = spacingBefore + pendingSeparator,
				SpacingAfter = spacingAfter
			};
			if (view.paras.Count > 0)
			{
				List<CopySegment> segments = view.segments;
				List<Para> paras = view.paras;
				segments.Add(new CopySegment(paras[paras.Count - 1].P, (pendingSeparator > 0.0) ? "\n\n" : "\n"));
			}
			pendingSeparator = 0.0;
			if (fixedLineHeight)
			{
				para.LineHeight = Math.Round(fontSize * lineHeightFactor + lineSpacing, 2);
				paragraph.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
				paragraph.LineHeight = para.LineHeight;
			}
			foreach (Box item in enclosing)
			{
				Box current;
				Box box = (current = item);
				if (current.First == null)
				{
					current.First = para;
				}
				box.Last = para;
			}
			view.paras.Add(para);
			view.text.Blocks.Add(paragraph);
			return para;
		}

		private void AddRun(Para para, Run run, InlineCollection? target = null)
		{
			(target ?? para.P.Inlines).Add(run);
			view.segments.Add(new CopySegment(run, run.Text));
		}

		private void AppendInline(Para para, string inlineMarkdown, double fontSize, MarkdownFontWeight weight, Color color)
		{
			foreach (MarkdownInlineRun item in MarkdownInlineParser.Parse(MarkdownViewerParser.RewriteInlineImagesAsLinks(inlineMarkdown)))
			{
				Run run = new Run
				{
					Text = item.Text,
					FontSize = fontSize,
					Foreground = Ui.Brush(color),
					FontFamily = MarkdownViewerFonts.System(fontSize)
				};
				MarkdownFontWeight weight2 = weight;
				if (item.IsCode)
				{
					run.FontFamily = MarkdownViewerFonts.Mono;
					run.FontSize = fontSize - 2.0;
					if (item.IsStrong)
					{
						weight2 = MarkdownFontWeight.Semibold;
					}
					view.inlineCodeRuns.Add(run);
				}
				else
				{
					if (item.IsStrong)
					{
						weight2 = MarkdownFontWeight.Semibold;
					}
					if (item.IsEmphasized)
					{
						run.FontStyle = FontStyle.Italic;
					}
				}
				run.FontWeight = MarkdownViewerFonts.Weight(weight2);
				if (item.IsStrikethrough)
				{
					run.TextDecorations = TextDecorations.Strikethrough;
				}
				string link = item.Link;
				if (link != null)
				{
					run.Foreground = Ui.Brush(view.colors.LinkText);
					Hyperlink hyperlink = new Hyperlink
					{
						UnderlineStyle = UnderlineStyle.Single,
						Foreground = Ui.Brush(view.colors.LinkText)
					};
					Uri uri = ResolveLink(link);
					if ((object)uri != null)
					{
						bool flag;
						switch (uri.Scheme)
						{
						case "http":
						case "https":
						case "mailto":
							flag = true;
							break;
						default:
							flag = false;
							break;
						}
						if (flag)
						{
							hyperlink.NavigateUri = uri;
						}
						hyperlink.Click += delegate(Hyperlink s, HyperlinkClickEventArgs _)
						{
							MarkdownViewerLinks.Opened(s.XamlRoot, uri);
						};
					}
					para.P.Inlines.Add(hyperlink);
					AddRun(para, run, hyperlink.Inlines);
				}
				else
				{
					AddRun(para, run);
				}
			}
		}

		private Uri? ResolveLink(string link)
		{
			if (Uri.TryCreate(link, UriKind.Absolute, out Uri result) && (!result.IsFile || link.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
			{
				return result;
			}
			string baseDirectory = document.BaseDirectory;
			if (baseDirectory != null)
			{
				try
				{
					return new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, Uri.UnescapeDataString(link.Split('#')[0]))));
				}
				catch
				{
					return null;
				}
			}
			return null;
		}

		private Box AppendCodeBlock(string code, List<Box> enclosing)
		{
			Box box = new Box
			{
				Kind = BoxKind.Code,
				PadLeft = 16.0,
				PadRight = 16.0,
				PadTop = 12.0,
				PadBottom = 12.0
			};
			view.boxes.Add(box);
			List<Box> enclosing2 = new List<Box>(enclosing) { box };
			string[] array = code.Split('\n');
			double fontSize = S(13.0);
			for (int i = 0; i < array.Length; i++)
			{
				bool flag = i == 0;
				bool flag2 = i == array.Length - 1;
				Para para = NewParagraph(enclosing2, flag ? F(8.0) : 0.0, flag2 ? F(8.0) : 0.0, fontSize, F(3.5), fixedLineHeight: true, 1.19);
				if (flag)
				{
					para.ExtraRight = 30.0;
				}
				para.P.FontFamily = MarkdownViewerFonts.Mono;
				AddRun(para, new Run
				{
					Text = array[i],
					FontFamily = MarkdownViewerFonts.Mono,
					FontSize = fontSize,
					Foreground = Ui.Brush(view.colors.BodyText)
				});
			}
			return box;
		}

		private void AppendList(IReadOnlyList<MarkdownViewerListItem> items, List<Box> enclosing)
		{
			Color bodyText = view.colors.BodyText;
			double fontSize = S(16.0);
			for (int i = 0; i < items.Count; i++)
			{
				MarkdownViewerListItem markdownViewerListItem = items[i];
				double num = F(24.0);
				double num2 = F(markdownViewerListItem.IndentLevel * 22);
				double spacingAfter = ((i == items.Count - 1) ? F(12.0) : F(5.0));
				Para para = NewParagraph(enclosing, (i == 0) ? F(7.0) : 0.0, spacingAfter, fontSize, F(5.5));
				para.Indent = num2 + num;
				para.P.TextIndent = 0.0 - num;
				(string Text, double Size, Color Color, bool Mono) tuple = ListItemMarker(markdownViewerListItem);
				string item = tuple.Text;
				double item2 = tuple.Size;
				Color item3 = tuple.Color;
				bool item4 = tuple.Mono;
				Run run = new Run
				{
					Text = item,
					FontSize = S(item2),
					Foreground = Ui.Brush(item3),
					FontFamily = ((!markdownViewerListItem.CheckboxState.HasValue) ? MarkdownViewerFonts.System(S(item2)) : MarkdownViewerFonts.Symbol)
				};
				if (item4)
				{
					Typography.SetNumeralAlignment(run, FontNumeralAlignment.Tabular);
				}
				AddRun(para, run);
				double num3 = MeasureWidth(item, S(item2), item4, run.FontFamily);
				InlineUIContainer inlineUIContainer = new InlineUIContainer
				{
					Child = new Border
					{
						Width = Math.Max(F(4.0), num - num3),
						Height = 1.0
					}
				};
				para.P.Inlines.Add(inlineUIContainer);
				view.segments.Add(new CopySegment(inlineUIContainer, "\t"));
				AppendInline(para, markdownViewerListItem.InlineMarkdown, fontSize, MarkdownFontWeight.Regular, bodyText);
			}
		}

		private (string Text, double Size, Color Color, bool Mono) ListItemMarker(MarkdownViewerListItem item)
		{
			bool? checkboxState = item.CheckboxState;
			if (checkboxState.HasValue)
			{
				bool valueOrDefault = checkboxState == true;
				return (Text: valueOrDefault ? "☑" : "☐", Size: 14.0, Color: valueOrDefault ? view.colors.BodyText : view.colors.SecondaryText, Mono: false);
			}
			string ordinalLabel = item.OrdinalLabel;
			if (ordinalLabel != null)
			{
				return (Text: ordinalLabel, Size: 16.0, Color: view.colors.SecondaryText, Mono: true);
			}
			return (Text: (item.IndentLevel == 0) ? "•" : "◦", Size: 16.0, Color: view.colors.SecondaryText, Mono: false);
		}

		private static double MeasureWidth(string text, double fontSize, bool tabular, FontFamily family)
		{
			if ((object)measuring == null)
			{
				measuring = new TextBlock
				{
					IsTextScaleFactorEnabled = false
				};
			}
			measuring.Text = text;
			measuring.FontSize = fontSize;
			measuring.FontFamily = family;
			Typography.SetNumeralAlignment(measuring, tabular ? FontNumeralAlignment.Tabular : FontNumeralAlignment.Normal);
			measuring.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			return measuring.DesiredSize.Width;
		}

		private void AppendTable(IReadOnlyList<string> headerCells, IReadOnlyList<IReadOnlyList<string>> rows, List<Box> enclosing)
		{
			int num = Math.Max(headerCells.Count, (rows.Count != 0) ? rows.Max((IReadOnlyList<string> r) => r.Count) : 0);
			if (num == 0)
			{
				return;
			}
			List<string> list = headerCells.Concat(Enumerable.Repeat("", num - headerCells.Count)).ToList();
			string text = string.Join("\n", from row in new IReadOnlyList<string>[1] { list }.Concat<IReadOnlyList<string>>(rows)
				select string.Join("\t", row.Select((string cell) => MarkdownInlineParser.PlainText(cell))));
			MarkdownViewerTableContent markdownViewerTableContent = new MarkdownViewerTableContent(list, rows, text);
			Para para = NewParagraph(enclosing, F(8.0), F(8.0), S(13.5), 0.0, fixedLineHeight: false);
			MarkdownViewerTableView markdownViewerTableView = new MarkdownViewerTableView(markdownViewerTableContent, view.look);
			InlineUIContainer inlineUIContainer = new InlineUIContainer
			{
				Child = markdownViewerTableView
			};
			para.P.Inlines.Add(inlineUIContainer);
			view.segments.Add(new CopySegment(inlineUIContainer, text));
			view.tables.Add((para, markdownViewerTableView, markdownViewerTableContent, inlineUIContainer));
		}

		private void AppendHorizontalRule(List<Box> enclosing)
		{
			Box item = new Box
			{
				Kind = BoxKind.Rule,
				RuleHeight = 1.0
			};
			view.boxes.Add(item);
			Para para = NewParagraph(new List<Box>(enclosing) { item }, F(18.0), F(18.0), 1.0, 0.0);
			para.LineHeight = 1.0;
			para.P.LineHeight = 1.0;
			AddRun(para, new Run
			{
				Text = "\u00a0",
				FontSize = 1.0
			});
		}

		private void AppendImage(string alternateText, string source, List<Box> enclosing)
		{
			bool flag = MarkdownViewerPreparedDocument.IsRemote(source);
			document.Images.TryGetValue(source, out MarkdownViewerImage value);
			if ((object)value == null)
			{
				string text = ((!flag) ? ((alternateText.Length == 0) ? source : alternateText) : ((alternateText.Length == 0) ? "Loading image…" : alternateText));
				double num = S(12.5);
				Para para = NewParagraph(enclosing, F(10.0), F(10.0), num, 0.0);
				AddRun(para, new Run
				{
					Text = "\ud83d\uddbc " + text,
					FontSize = num,
					Foreground = Ui.Brush(view.colors.SecondaryText),
					FontFamily = MarkdownViewerFonts.System(num)
				});
				return;
			}
			double num2 = Math.Min(1.0, Math.Min(660.0 / Math.Max(value.Width, 1.0), 360.0 / Math.Max(value.Height, 1.0)));
			Size item = new Size(value.Width * num2, value.Height * num2);
			BitmapImage bitmapImage = new BitmapImage();
			string path = value.Path;
			if (path != null)
			{
				bitmapImage.UriSource = new Uri(path);
			}
			else
			{
				byte[] bytes = value.Bytes;
				if (bytes != null)
				{
					SetBytesAsync(bitmapImage, bytes);
				}
			}
			Image image = new Image
			{
				Source = bitmapImage,
				Stretch = Stretch.Fill,
				Width = item.Width,
				Height = item.Height
			};
			Para para2 = NewParagraph(enclosing, F(10.0), F(10.0), S(16.0), 0.0, fixedLineHeight: false);
			InlineUIContainer inlineUIContainer = new InlineUIContainer
			{
				Child = image
			};
			para2.P.Inlines.Add(inlineUIContainer);
			view.segments.Add(new CopySegment(inlineUIContainer, ""));
			view.images.Add((para2, image, item));
		}

		private static async Task SetBytesAsync(BitmapImage bitmap, byte[] bytes)
		{
			_ = 1;
			try
			{
				using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
				await stream.WriteAsync(bytes.AsBuffer());
				stream.Seek(0uL);
				await bitmap.SetSourceAsync(stream);
			}
			catch (Exception ex)
			{
				Log.Info("markdown", "image not shown: " + ex.Message);
			}
		}
	}

	private sealed class CopyBlockButton : Pressable
	{
		public const double Size = 22.0;

		private readonly TextBlock glyph;

		private readonly MessagesPalette look;

		private bool didCopy;

		private DispatcherTimer? reset;

		public CopyBlockButton(string copyText, MessagesPalette look)
		{
			CopyBlockButton copyBlockButton = this;
			this.look = look;
			base.Width = 22.0;
			base.Height = 22.0;
			base.CornerRadius = new CornerRadius(6.0);
			glyph = Icons.Glyph("\ue8c8", 11.0, look.InkAt(0.5));
			glyph.FontWeight = FontWeights.Medium;
			base.Children.Add(glyph);
			this.Tip("Copy");
			AutomationProperties.SetName(this, "Copy");
			base.HoverChanged += delegate
			{
				copyBlockButton.Refresh();
			};
			base.Click += delegate
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.SetText(copyText);
				Clipboard.SetContent(dataPackage);
				ClickyAnalytics.TrackProductEvent("home_space_conversation_action", null, null, new JsonObject((JsonNodeOptions?)null) { ["action"] = "copy_block" });
				copyBlockButton.didCopy = true;
				copyBlockButton.Refresh();
				copyBlockButton.reset?.Stop();
				copyBlockButton.reset = new DispatcherTimer
				{
					Interval = TimeSpan.FromSeconds(1.4)
				};
				copyBlockButton.reset.Tick += delegate
				{
					copyBlockButton.reset?.Stop();
					copyBlockButton.didCopy = false;
					copyBlockButton.Refresh();
				};
				copyBlockButton.reset.Start();
			};
		}

		private void Refresh()
		{
			glyph.Text = (didCopy ? "\ue73e" : "\ue8c8");
			glyph.Foreground = Ui.Brush(look.InkAt((!didCopy && !base.IsHovered) ? 0.5 : (look.IsDark ? 0.95 : 0.8)));
			base.Background = Ui.Brush(look.InkAt((!base.IsHovered) ? 0.0 : (look.IsDark ? 0.1 : 0.07)));
		}
	}

	private readonly MessagesPalette look = MessagesPalette.Current;

	private readonly MarkdownViewerPalette colors = MarkdownViewerPalette.Current;

	private readonly Canvas plateLayer = new Canvas
	{
		IsHitTestVisible = false
	};

	private readonly RichTextBlock text;

	private readonly Canvas overlay = new Canvas();

	private readonly List<Para> paras = new List<Para>();

	private readonly List<Box> boxes = new List<Box>();

	private readonly List<(Box Target, string Text, CopyBlockButton Button)> copyButtons = new List<(Box, string, CopyBlockButton)>();

	private readonly List<(Para Para, MarkdownViewerTableView View, MarkdownViewerTableContent Content, InlineUIContainer Container)> tables = new List<(Para, MarkdownViewerTableView, MarkdownViewerTableContent, InlineUIContainer)>();

	private readonly List<(Para Para, FrameworkElement View, Size Size)> images = new List<(Para, FrameworkElement, Size)>();

	private readonly List<CopySegment> segments = new List<CopySegment>();

	private Size lastLayoutSize;

	private double lastAvailableWidth = -1.0;

	private readonly List<Run> inlineCodeRuns = new List<Run>();

	public bool UsesPageLayout { get; }

	public double TextScale { get; }

	public Func<string?>? WholeDocumentText { get; set; }

	private double ColumnWidth => Math.Max(0.0, base.ActualWidth - text.Margin.Left - text.Margin.Right);

	public MarkdownViewerTextView(bool usesPageLayout, double textScale = 1.0)
	{
		UsesPageLayout = usesPageLayout;
		TextScale = textScale;
		text = new RichTextBlock
		{
			IsTextSelectionEnabled = true,
			IsTextScaleFactorEnabled = false,
			TextWrapping = TextWrapping.Wrap,
			OpticalMarginAlignment = OpticalMarginAlignment.None,
			Foreground = Ui.Brush(colors.BodyText),
			FontFamily = MarkdownViewerFonts.System(16.0),
			FontSize = 16.0,
			SelectionHighlightColor = Ui.Brush(DS.Rgb(0.25, 0.42, 0.7))
		};
		base.Children.Add(plateLayer);
		base.Children.Add(text);
		base.Children.Add(overlay);
		overlay.IsHitTestVisible = true;
		overlay.Background = null;
		if (usesPageLayout)
		{
			text.Margin = new Thickness(36.0, 52.0, 36.0, 52.0);
		}
		base.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			OnWidthChanged(e.NewSize.Width);
		};
		text.LayoutUpdated += delegate
		{
			LayoutChrome();
		};
		text.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnKeyDown), handledEventsToo: true);
		MenuFlyout menuFlyout = new MenuFlyout();
		MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
		{
			Text = "Copy"
		};
		menuFlyoutItem.Click += delegate
		{
			CopySelection();
		};
		MenuFlyoutItem menuFlyoutItem2 = new MenuFlyoutItem
		{
			Text = "Select All"
		};
		menuFlyoutItem2.Click += delegate
		{
			text.SelectAll();
		};
		menuFlyout.Items.Add(menuFlyoutItem);
		menuFlyout.Items.Add(menuFlyoutItem2);
		text.ContextFlyout = menuFlyout;
		text.SelectionFlyout = null;
	}

	public void Install(MarkdownViewerPreparedDocument document)
	{
		paras.Clear();
		boxes.Clear();
		segments.Clear();
		tables.Clear();
		images.Clear();
		foreach (var copyButton in copyButtons)
		{
			CopyBlockButton item = copyButton.Button;
			overlay.Children.Remove(item);
		}
		copyButtons.Clear();
		inlineCodeRuns.Clear();
		text.Blocks.Clear();
		text.TextHighlighters.Clear();
		lastLayoutSize = default(Size);
		lastAvailableWidth = -1.0;
		new Builder(this, document).Render(document.Blocks);
		if (!UsesPageLayout && paras.Count > 0)
		{
			List<Para> list = paras;
			list[list.Count - 1].SpacingAfter = 0.0;
		}
		ApplyParagraphMetrics();
		LayoutEmbeddedContent(force: true);
		InvalidateMeasure();
	}

	private void OnWidthChanged(double width)
	{
		if (UsesPageLayout)
		{
			double num = Math.Max(36.0, (width - 660.0) / 2.0);
			if (Math.Abs(text.Margin.Left - num) > 0.5)
			{
				text.Margin = new Thickness(num, 52.0, num, 52.0);
			}
		}
		LayoutEmbeddedContent(force: false);
	}

	private double AvailableWidth(Para para, double columnWidth)
	{
		return Math.Max(1.0, columnWidth - para.Boxes.Sum((Box b) => b.PadLeft + b.PadRight));
	}

	private void LayoutEmbeddedContent(bool force)
	{
		double columnWidth = ColumnWidth;
		if (columnWidth <= 0.0 || (!force && Math.Abs(columnWidth - lastAvailableWidth) < 0.5))
		{
			return;
		}
		lastAvailableWidth = columnWidth;
		foreach (var table in tables)
		{
			table.View.LayoutTable(AvailableWidth(table.Para, columnWidth));
		}
		foreach (var image in images)
		{
			double num = AvailableWidth(image.Para, columnWidth);
			Size item = image.Size;
			double num2 = Math.Min(1.0, num / Math.Max(1.0, item.Width));
			FrameworkElement item2 = image.View;
			item = image.Size;
			item2.Width = item.Width * num2;
			FrameworkElement item3 = image.View;
			item = image.Size;
			item3.Height = item.Height * num2;
		}
	}

	private void ApplyParagraphMetrics()
	{
		double num = 0.0;
		for (int i = 0; i < paras.Count; i++)
		{
			Para para = paras[i];
			double num2 = para.Indent;
			double num3 = para.ExtraRight;
			double num4 = para.SpacingBefore;
			double num5 = para.SpacingAfter;
			foreach (Box box in para.Boxes)
			{
				num2 += box.PadLeft;
				num3 += box.PadRight;
				if (box.First == para)
				{
					num4 += box.PadTop + box.MarginTop;
				}
				if (box.Last == para)
				{
					num5 += box.PadBottom + box.MarginBottom;
				}
			}
			bool flag = i == paras.Count - 1;
			para.P.Margin = new Thickness(num2, num + num4, num3, flag ? num5 : 0.0);
			num = num5;
		}
	}

	private static double OffsetAbove(Box target, Para para)
	{
		object obj;
		if (para.Boxes.Count <= 0)
		{
			obj = null;
		}
		else
		{
			List<Box> list = para.Boxes;
			obj = list[list.Count - 1];
		}
		Box box = (Box)obj;
		double num = 0.0;
		if (box == null || box.First != para)
		{
			num += para.SpacingBefore;
		}
		for (int num2 = para.Boxes.Count - 1; num2 >= 0; num2--)
		{
			Box box2 = para.Boxes[num2];
			if (box2.First == para)
			{
				num += box2.PadTop;
				if (box2 == target)
				{
					return num;
				}
				num += box2.MarginTop;
				if (box2 == box)
				{
					num += para.SpacingBefore;
				}
			}
		}
		return num;
	}

	private static double OffsetBelow(Box target, Para para)
	{
		object obj;
		if (para.Boxes.Count <= 0)
		{
			obj = null;
		}
		else
		{
			List<Box> list = para.Boxes;
			obj = list[list.Count - 1];
		}
		Box box = (Box)obj;
		double num = 0.0;
		if (box == null || box.Last != para)
		{
			num += para.SpacingAfter;
		}
		for (int num2 = para.Boxes.Count - 1; num2 >= 0; num2--)
		{
			Box box2 = para.Boxes[num2];
			if (box2.Last == para)
			{
				num += box2.PadBottom;
				if (box2 == target)
				{
					return num;
				}
				num += box2.MarginBottom;
				if (box2 == box)
				{
					num += para.SpacingAfter;
				}
			}
		}
		return num;
	}

	private void LayoutChrome()
	{
		if (paras.Count == 0)
		{
			return;
		}
		Size size = new Size(ColumnWidth, text.ActualHeight);
		if (size == lastLayoutSize || size.Width <= 0.0)
		{
			return;
		}
		lastLayoutSize = size;
		plateLayer.Children.Clear();
		double left = text.Margin.Left;
		double top = text.Margin.Top;
		double width = size.Width;
		Dictionary<Box, Rect> dictionary = new Dictionary<Box, Rect>();
		foreach (Box box in boxes)
		{
			if (box.First == null || box.Last == null)
			{
				continue;
			}
			Rect rect = LineRect(box.First, start: true);
			Rect rect2 = LineRect(box.Last, start: false);
			double num = rect.Top - OffsetAbove(box, box.First);
			double num2 = rect2.Bottom + OffsetBelow(box, box.Last);
			double num3 = 0.0;
			double num4 = width;
			foreach (Box box2 in box.First.Boxes)
			{
				if (box2 == box)
				{
					break;
				}
				num3 += box2.PadLeft;
				num4 -= box2.PadRight;
			}
			Rect rect4 = (dictionary[box] = new Rect(left + num3, top + num, Math.Max(0.0, num4 - num3), Math.Max(0.0, num2 - num)));
			DrawPlate(box, rect4);
		}
		DrawInlineCodeChips(left, top);
		foreach (var (key, _, element) in copyButtons)
		{
			if (dictionary.TryGetValue(key, out var value))
			{
				Canvas.SetLeft(element, Math.Min(value.Right, left + width) - 22.0 - 6.0);
				Canvas.SetTop(element, value.Top + 6.0);
			}
		}
	}

	private Rect LineRect(Para para, bool start)
	{
		try
		{
			Rect characterRect = (start ? para.P.ContentStart : para.P.ContentEnd).GetCharacterRect(start ? LogicalDirection.Forward : LogicalDirection.Backward);
			if (para.LineHeight > 0.0)
			{
				double y = (start ? characterRect.Top : (characterRect.Bottom - para.LineHeight));
				return new Rect(characterRect.X, y, characterRect.Width, para.LineHeight);
			}
			return characterRect;
		}
		catch
		{
			return default(Rect);
		}
	}

	private void DrawPlate(Box box, Rect rect)
	{
		switch (box.Kind)
		{
		case BoxKind.Quote:
		{
			Rectangle rectangle2 = new Rectangle
			{
				Width = Math.Max(0.0, rect.Width - 1.0),
				Height = Math.Max(0.0, rect.Height - 1.0),
				RadiusX = 8.0,
				RadiusY = 8.0,
				Fill = Ui.Brush(colors.QuotePlateFill),
				Stroke = Ui.Brush(colors.QuotePlateStroke),
				StrokeThickness = 1.0
			};
			Canvas.SetLeft(rectangle2, rect.X + 0.5);
			Canvas.SetTop(rectangle2, rect.Y + 0.5);
			plateLayer.Children.Add(rectangle2);
			double num = rect.Height - 2.0 * box.PadTop;
			if (num > 0.0)
			{
				Rectangle rectangle3 = new Rectangle
				{
					Width = 2.0,
					Height = num,
					RadiusX = 1.0,
					RadiusY = 1.0,
					Fill = Ui.Brush(colors.QuotePlateBar)
				};
				Canvas.SetLeft(rectangle3, rect.X + 12.0);
				Canvas.SetTop(rectangle3, rect.Y + box.PadTop);
				plateLayer.Children.Add(rectangle3);
			}
			break;
		}
		case BoxKind.Code:
		case BoxKind.Rule:
		{
			Rectangle rectangle = new Rectangle
			{
				Width = rect.Width,
				Height = ((box.Kind == BoxKind.Rule) ? box.RuleHeight : rect.Height),
				Fill = Ui.Brush((box.Kind == BoxKind.Code) ? colors.CodeBlockBackground : colors.Hairline)
			};
			Canvas.SetLeft(rectangle, rect.X);
			Canvas.SetTop(rectangle, rect.Y);
			plateLayer.Children.Add(rectangle);
			break;
		}
		}
	}

	private void DrawInlineCodeChips(double originX, double originY)
	{
		foreach (Run inlineCodeRun in inlineCodeRuns)
		{
			int num = inlineCodeRun.ContentEnd.Offset - inlineCodeRun.ContentStart.Offset;
			Rect? line = null;
			for (int i = 0; i < num; i++)
			{
				Rect characterRect;
				Rect characterRect2;
				try
				{
					characterRect = inlineCodeRun.ContentStart.GetPositionAtOffset(i, LogicalDirection.Forward).GetCharacterRect(LogicalDirection.Forward);
					characterRect2 = inlineCodeRun.ContentStart.GetPositionAtOffset(i + 1, LogicalDirection.Forward).GetCharacterRect(LogicalDirection.Backward);
				}
				catch
				{
					continue;
				}
				double num2 = ((Math.Abs(characterRect2.Top - characterRect.Top) < 0.5) ? characterRect2.X : (characterRect.X + characterRect.Width));
				Rect value = new Rect(characterRect.X, characterRect.Top, Math.Max(0.0, num2 - characterRect.X), characterRect.Height);
				if (line.HasValue)
				{
					Rect valueOrDefault = line.GetValueOrDefault();
					if (Math.Abs(valueOrDefault.Top - value.Top) < 0.5)
					{
						line = new Rect(valueOrDefault.X, valueOrDefault.Y, Math.Max(valueOrDefault.Width, value.X + value.Width - valueOrDefault.X), valueOrDefault.Height);
						continue;
					}
				}
				Flush();
				line = value;
			}
			Flush();
			void Flush()
			{
				if (line.HasValue)
				{
					Rect valueOrDefault2 = line.GetValueOrDefault();
					if (!(valueOrDefault2.Width <= 0.0))
					{
						Rectangle rectangle = new Rectangle
						{
							Width = valueOrDefault2.Width,
							Height = valueOrDefault2.Height,
							Fill = Ui.Brush(colors.InlineCodeBackground)
						};
						Canvas.SetLeft(rectangle, originX + valueOrDefault2.X);
						Canvas.SetTop(rectangle, originY + valueOrDefault2.Y);
						plateLayer.Children.Add(rectangle);
					}
				}
			}
		}
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) && e.Key == VirtualKey.C)
		{
			CopySelection();
			e.Handled = true;
		}
	}

	public void SelectAllText()
	{
		text.SelectAll();
	}

	public void CopySelection()
	{
		string value = ((!string.IsNullOrEmpty(text.SelectedText)) ? SelectedPlainText() : WholeDocumentText?.Invoke());
		if (!string.IsNullOrEmpty(value))
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(value);
			Clipboard.SetContent(dataPackage);
		}
	}

	public string SelectedPlainText()
	{
		int num = text.SelectionStart.Offset;
		int num2 = text.SelectionEnd.Offset;
		if (num2 < num)
		{
			int num3 = num2;
			num2 = num;
			num = num3;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (CopySegment segment in segments)
		{
			TextElement element = segment.Element;
			if (!(element is Run run))
			{
				if (!(element is Paragraph paragraph))
				{
					if ((object)element != null && element.ContentStart.Offset >= num && element.ContentEnd.Offset <= num2)
					{
						stringBuilder.Append(segment.Text);
					}
					continue;
				}
				int offset = paragraph.ContentEnd.Offset;
				if (offset >= num && offset < num2)
				{
					stringBuilder.Append(segment.Text);
				}
			}
			else
			{
				int offset2 = run.ContentStart.Offset;
				int offset3 = run.ContentEnd.Offset;
				int num4 = Math.Max(num, offset2);
				int num5 = Math.Min(num2, offset3);
				if (num5 > num4)
				{
					stringBuilder.Append(run.Text.AsSpan(num4 - offset2, Math.Min(num5 - num4, run.Text.Length - (num4 - offset2))));
				}
			}
		}
		return stringBuilder.ToString();
	}

	private void AddCopyButton(Box target, string copyText)
	{
		CopyBlockButton copyBlockButton = new CopyBlockButton(copyText, look);
		overlay.Children.Add(copyBlockButton);
		copyButtons.Add((target, copyText, copyBlockButton));
	}
}
