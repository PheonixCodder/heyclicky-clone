using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HeyClicky.App.Documents;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.Shell;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents.Spreadsheet;
using HeyClicky.Core.Ink;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace HeyClicky.App.Demo;

public static class DocumentGallery
{
	public sealed record Scene(string Name, double Width, double Height, Func<FrameworkElement> Build, Color? Fill = null, double Seconds = 1.6);

	[CompilerGenerated]
	private sealed class _003CDescendants_003Ed__35 : IEnumerable<DependencyObject>, IEnumerable, IEnumerator<DependencyObject>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private DependencyObject _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private DependencyObject root;

		public DependencyObject _003C_003E3__root;

		private Stack<DependencyObject> _003Cstack_003E5__2;

		private DependencyObject _003Cnode_003E5__3;

		DependencyObject IEnumerator<DependencyObject>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CDescendants_003Ed__35(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003Cstack_003E5__2 = null;
			_003Cnode_003E5__3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			IEnumerable<DependencyObject> enumerable;
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003Cstack_003E5__2 = new Stack<DependencyObject>();
				_003Cstack_003E5__2.Push(root);
				break;
			case 1:
				{
					_003C_003E1__state = -1;
					DependencyObject dependencyObject = _003Cnode_003E5__3;
					if (!(dependencyObject is Panel panel))
					{
						if (!(dependencyObject is Border border))
						{
							if (!(dependencyObject is ContentControl { Content: DependencyObject content }))
							{
								goto IL_00d2;
							}
							enumerable = new global::_003C_003Ez__ReadOnlySingleElementList<DependencyObject>(content);
						}
						else
						{
							if ((object)border.Child == null)
							{
								goto IL_00d2;
							}
							enumerable = new global::_003C_003Ez__ReadOnlySingleElementList<DependencyObject>(border.Child);
						}
					}
					else
					{
						enumerable = panel.Children.Cast<DependencyObject>();
					}
					goto IL_00d9;
				}
				IL_00d2:
				enumerable = Array.Empty<DependencyObject>();
				goto IL_00d9;
				IL_00d9:
				foreach (DependencyObject item in enumerable)
				{
					_003Cstack_003E5__2.Push(item);
				}
				_003Cnode_003E5__3 = null;
				break;
			}
			if (_003Cstack_003E5__2.Count > 0)
			{
				_003Cnode_003E5__3 = _003Cstack_003E5__2.Pop();
				_003C_003E2__current = _003Cnode_003E5__3;
				_003C_003E1__state = 1;
				return true;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<DependencyObject> IEnumerable<DependencyObject>.GetEnumerator()
		{
			_003CDescendants_003Ed__35 _003CDescendants_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CDescendants_003Ed__ = this;
			}
			else
			{
				_003CDescendants_003Ed__ = new _003CDescendants_003Ed__35(0);
			}
			_003CDescendants_003Ed__.root = _003C_003E3__root;
			return _003CDescendants_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<DependencyObject>)this).GetEnumerator();
		}
	}

	private static readonly DateTimeOffset DashboardNow = new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, 21)));

	private const string EdgeQuestionNote = "# Short clip editor\n## In short\n> Start with CapCut. {big=\"CapCut\"}\n- Auto captions speed up short edits.\n::question[What is the clip for?]{yes=\"Vertical social\" no=\"A polished Mac edit\" yes_label=\"Social\" no_label=\"Polished\"}";

	private const string EdgeReviewNote = "# Clean up Downloads\n## In short\n> Nine old installers take 3.1 GB. {big=\"3.1 GB\"}\n:::review[Move these to the Trash?]{go=\"Move these to the Trash\" skip=\"Keep them\"}\n- [x] Figma-124.dmg — an old installer\n- [ ] Zoom.pkg — installed in 2023\n- [x] Xcode_15.xip — replaced by Xcode 16\n:::\n## Kept\n- Your photo exports";

	private const double ClickyStickyScreenCorner = 10.0;

	private const string RemotePhoto = "https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/PNG_transparency_demonstration_1.png/330px-PNG_transparency_demonstration_1.png";

	private const string PickNote = "# Headphones\n\n## In short\n> Sony for flights; Bose if comfort comes first {big=\"$449.99\"}\n::figure[Battery]{value=\"30 h\" delta=\"+6 h\"}\n::figure[Noise cut]{value=\"-38 dB\" delta=\"down 2 dB\"}\n::figure[Weight]{value=\"250 g\" delta=\"lightest\"}\n| Headphones | Price | ANC battery | Best for |\n|---|---|---|---|\n| **Sony WH-1000XM6** {pick} | $449.99 | 30 h | Flights and the office, the best noise cancelling we tried |\n| Bose QuietComfort Ultra | $299.95 | 24 h | Comfort on long days |\n| Sennheiser Momentum 4 {out} | $379.95 | 60 h | Battery above everything |\n::sticky[Buy from the airline store: 10% off]{color=pink}\n::sticky[Return window is 30 days]\n::sticky[Case is sold apart]{color=blue}\n:::chart[Reviewer scores]{unit=/10}\nSony WH-1000XM6: 9.1 {pick}\nBose QuietComfort Ultra: 8.4\nSennheiser Momentum 4: 7.9\n:::";

	private static readonly Color GalleryDesk = PaperColor.Hex("#EDEBE6");

	private static readonly Color InkDesk = PaperColor.Hex("#E9E6DF");

	internal const string SampleReport = "# Q3 revenue report\nRevenue grew **12%** quarter over quarter, led by *annual* plans and a ~~flat~~ steady churn rate. See the\n[dashboard](https://example.com/dashboard) or run `npm run report` to rebuild it.\n\n## Highlights\n- Annual plans: **$412k** booked\n- Churn held at `2.1%`\n  - Enterprise churn: 0.4%\n  - Self-serve churn: 3.0%\n- [x] Pricing page shipped\n- [ ] Billing migration\n\n![Revenue by month](chart.png)\n\n### Next steps\n1. Review the cancellation requests with the team.\n2. Update the payment details for North Star Studio.\n10. Send the board memo.\n\n---\n\n> **Tip** · Ask Clicky to refresh this report every Monday.\n\n![A picture from the web](https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png)";

	private const string SampleBlocks = "# Heading one\n## Heading two\n### Heading three\n#### Heading four\n##### Heading five\n###### Heading six\n\nSetext heading\n==============\n\nA paragraph with **bold**, *italic*, ***both***, `inline code`, ~~struck~~ and a [link](https://heyclicky.com).\nLines of one paragraph join into one.\n\n> A quote to lift out: a draft reply.\n>\n> - with a list\n> - and a second item\n>\n> ```\n> let x = 1\n> ```\n\n> **Note** · A callout has no copy button.\n\n```swift\nfunc greet(name: String) -> String {\n    return \"Hello, \\(name)! This line is long enough that it has to wrap inside the code block's plate on a narrow page.\"\n}\n```\n\n1. First\n2. Second\n    - nested bullet\n        - deeper\n3. Third\n\n***\n\nDone.";

	private const string SampleTables = "# Tables\n\nBefore the table.\n\n| Customer | Plan | Revenue | Next step |\n| --- | --- | --- | --- |\n| North Star Studio | Professional annual | $12,450.00 | Contact the billing owner to update their payment details. |\n| Other studio | Enterprise | $145,000.00 | Review the cancellation request with the team. |\n\nAfter the table.\n\n| Option | Price | Verdict |\n|:--|--:|:-:|\n| ✓ Breville Bambino | $299 | **best** |\n| Gaggia Classic | $449 | `good` |\n\n| a | b | c | d | e | f |\n|---|---|---|---|---|---|\n| one | two | three | four | five | six |";

	public static string SampleDirectory
	{
		get
		{
			string text = Path.Combine(Path.GetTempPath(), "HeyClicky-document-gallery");
			Directory.CreateDirectory(text);
			return text;
		}
	}

	public static List<Scene> Scenes()
	{
		List<Scene> list = new List<Scene>();
		AddSpreadsheetScenes(list);
		AddMarkdownScenes(list);
		AddInkScenes(list);
		return list;
	}

	private static void AddSpreadsheetScenes(List<Scene> list)
	{
		Color sidebarFill = HomeTheme.SidebarFill;
		list.Add(new Scene("sheet-csv", 900.0, 560.0, delegate
		{
			SpreadsheetDocumentView obj = (SpreadsheetDocumentView)DocumentViewers.CreateViewer(SpreadsheetSampleFiles.WriteCsv(Sample("orders.csv")));
			obj.PreviewReady += delegate(SpreadsheetPreview preview)
			{
				preview.GridView.Select(new SpreadsheetCellRange(1, 1, 3, 3));
			};
			return obj;
		}, sidebarFill));
		list.Add(new Scene("sheet-semicolon", 700.0, 360.0, () => Preview(SpreadsheetSampleFiles.WriteSemicolonCsv(Sample("städte.csv")), 0, new SpreadsheetCellRange(1, 0, 1, 0)), sidebarFill));
		list.Add(new Scene("sheet-tsv", 700.0, 360.0, () => Preview(SpreadsheetSampleFiles.WriteTsv(Sample("scores.tsv")), 0, new SpreadsheetCellRange(1, 2, 3, 2)), sidebarFill));
		string xlsx = Sample("quarterly.xlsx");
		list.Add(new Scene("sheet-xlsx-revenue", 1000.0, 600.0, () => Preview(SpreadsheetSampleFiles.WriteWorkbook(xlsx), 0, new SpreadsheetCellRange(1, 1, 5, 2)), sidebarFill));
		list.Add(new Scene("sheet-xlsx-total", 1000.0, 600.0, () => Preview(SpreadsheetSampleFiles.WriteWorkbook(xlsx), 0, new SpreadsheetCellRange(5, 1, 5, 1)), sidebarFill));
		list.Add(new Scene("sheet-xlsx-summary", 800.0, 420.0, () => Preview(SpreadsheetSampleFiles.WriteWorkbook(xlsx), 1, new SpreadsheetCellRange(4, 0, 4, 0)), sidebarFill));
		list.Add(new Scene("sheet-xlsx-merged", 800.0, 420.0, () => Preview(SpreadsheetSampleFiles.WriteWorkbook(xlsx), 1, new SpreadsheetCellRange(0, 1, 0, 1)), sidebarFill));
		list.Add(new Scene("sheet-xlsx-wide", 1000.0, 520.0, () => Preview(SpreadsheetSampleFiles.WriteWorkbook(xlsx), 2, new SpreadsheetCellRange(2, 0, 8, 2)), sidebarFill));
		static string Sample(string name)
		{
			return Path.Combine(SampleDirectory, name);
		}
	}

	private static void AddMarkdownScenes(List<Scene> list)
	{
		string dir = Path.Combine(SampleDirectory, "markdown");
		Directory.CreateDirectory(dir);
		WriteSamplePicture(Path.Combine(dir, "chart.png"));
		string report = Path.Combine(dir, "report.md");
		File.WriteAllText(report, "# Q3 revenue report\nRevenue grew **12%** quarter over quarter, led by *annual* plans and a ~~flat~~ steady churn rate. See the\n[dashboard](https://example.com/dashboard) or run `npm run report` to rebuild it.\n\n## Highlights\n- Annual plans: **$412k** booked\n- Churn held at `2.1%`\n  - Enterprise churn: 0.4%\n  - Self-serve churn: 3.0%\n- [x] Pricing page shipped\n- [ ] Billing migration\n\n![Revenue by month](chart.png)\n\n### Next steps\n1. Review the cancellation requests with the team.\n2. Update the payment details for North Star Studio.\n10. Send the board memo.\n\n---\n\n> **Tip** · Ask Clicky to refresh this report every Monday.\n\n![A picture from the web](https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png)");
		string blocks = Path.Combine(dir, "blocks.md");
		File.WriteAllText(blocks, "# Heading one\n## Heading two\n### Heading three\n#### Heading four\n##### Heading five\n###### Heading six\n\nSetext heading\n==============\n\nA paragraph with **bold**, *italic*, ***both***, `inline code`, ~~struck~~ and a [link](https://heyclicky.com).\nLines of one paragraph join into one.\n\n> A quote to lift out: a draft reply.\n>\n> - with a list\n> - and a second item\n>\n> ```\n> let x = 1\n> ```\n\n> **Note** · A callout has no copy button.\n\n```swift\nfunc greet(name: String) -> String {\n    return \"Hello, \\(name)! This line is long enough that it has to wrap inside the code block's plate on a narrow page.\"\n}\n```\n\n1. First\n2. Second\n    - nested bullet\n        - deeper\n3. Third\n\n***\n\nDone.");
		string tables = Path.Combine(dir, "tables.md");
		File.WriteAllText(tables, "# Tables\n\nBefore the table.\n\n| Customer | Plan | Revenue | Next step |\n| --- | --- | --- | --- |\n| North Star Studio | Professional annual | $12,450.00 | Contact the billing owner to update their payment details. |\n| Other studio | Enterprise | $145,000.00 | Review the cancellation request with the team. |\n\nAfter the table.\n\n| Option | Price | Verdict |\n|:--|--:|:-:|\n| ✓ Breville Bambino | $299 | **best** |\n| Gaggia Classic | $449 | `good` |\n\n| a | b | c | d | e | f |\n|---|---|---|---|---|---|\n| one | two | three | four | five | six |");
		list.Add(new Scene("md-report", 520.0, 900.0, () => OnSidebar(new MarkdownViewerContent(report)), MarkdownViewerPalette.Dark.PageBackground, 3.5));
		list.Add(new Scene("md-blocks", 520.0, 1100.0, () => OnSidebar(new MarkdownViewerContent(blocks)), MarkdownViewerPalette.Dark.PageBackground, 3.5));
		list.Add(new Scene("md-tables", 520.0, 760.0, delegate
		{
			MarkdownViewerContent content = new MarkdownViewerContent(tables);
			Once.After(2.5, delegate
			{
				content.TextView.SelectAllText();
				string text = content.TextView.SelectedPlainText();
				Log.Info("markdown", $"gallery copy check: tables={text.Contains("$145,000.00") && text.Contains("Customer\tPlan")} before={text.Contains("Before the table.")} after={text.Contains("After the table.")} placeholder={text.Contains('￼')}");
			}, DispatcherQueue.GetForCurrentThread());
			return OnSidebar(content);
		}, MarkdownViewerPalette.Dark.PageBackground, 3.5));
		string remote = Path.Combine(dir, "remote.md");
		File.WriteAllText(remote, "# A picture from the web\n\n![](https://upload.wikimedia.org/wikipedia/commons/4/47/PNG_transparency_demonstration_1.png)\n\nAfter the picture.\n");
		list.Add(new Scene("md-remote", 520.0, 700.0, () => OnSidebar(new MarkdownViewerContent(remote)), MarkdownViewerPalette.Dark.PageBackground, 6.0));
		list.Add(new Scene("md-wide", 980.0, 900.0, () => OnSidebar(new MarkdownViewerContent(report)), MarkdownViewerPalette.Dark.PageBackground, 3.5));
		list.Add(new Scene("md-inline", 520.0, 1000.0, () => OnSidebar(new ScrollViewer
		{
			Content = new MarkdownViewerInlineDocument("# Heading one\n## Heading two\n### Heading three\n#### Heading four\n##### Heading five\n###### Heading six\n\nSetext heading\n==============\n\nA paragraph with **bold**, *italic*, ***both***, `inline code`, ~~struck~~ and a [link](https://heyclicky.com).\nLines of one paragraph join into one.\n\n> A quote to lift out: a draft reply.\n>\n> - with a list\n> - and a second item\n>\n> ```\n> let x = 1\n> ```\n\n> **Note** · A callout has no copy button.\n\n```swift\nfunc greet(name: String) -> String {\n    return \"Hello, \\(name)! This line is long enough that it has to wrap inside the code block's plate on a narrow page.\"\n}\n```\n\n1. First\n2. Second\n    - nested bullet\n        - deeper\n3. Third\n\n***\n\nDone.", dir)
			{
				Margin = new Thickness(16.0)
			},
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
		}), HomeTheme.SidebarFill, 3.5));
		list.Add(new Scene("md-loading-error", 520.0, 300.0, () => OnSidebar(new MarkdownViewerContent(Path.Combine(dir, "missing.md"))), HomeTheme.SidebarFill, 2.0));
	}

	private static void AddInkScenes(List<Scene> list)
	{
		AddInkPaperScenes(list);
		AddInkViewScenes(list);
	}

	internal static void AddSteps(AppHost host, Action<string, Action, double, Func<UIElement?>> add)
	{
		PanelWindow window = null;
		foreach (Scene item in Scenes())
		{
			Scene s = item;
			add("documents-" + s.Name, delegate
			{
				host.HideAuxWindows();
				window?.HideWindow();
				window = new PanelWindow("Documents " + s.Name, !host.Options.AllowCapture, showInTaskbar: true, 14.0, s.Fill);
				try
				{
					window.Panel.Children.Add(s.Build());
				}
				catch (Exception ex)
				{
					Log.Error("demo", "document scene " + s.Name + " failed", ex);
				}
				window.SetPanelSize(s.Width, s.Height);
				window.ShowCentered();
			}, s.Seconds, () => window?.Panel);
		}
		add("documents-end", delegate
		{
			window?.HideWindow();
		}, 0.3, () => (UIElement?)null);
	}

	private static void AddInkPaperScenes(List<Scene> list)
	{
		ClickyHomeRendering.IsOffscreen = true;
		Add("ink-paper-stocks", 1060.0, 520.0, PaperStocksScene, GalleryDesk);
		Add("ink-paper-pieces", 760.0, 420.0, PaperPiecesScene, GalleryDesk);
		Add("ink-paper-tags", 560.0, 120.0, PaperTagsScene, PaperColor.Black);
		Add("ink-paper-glyphs", 1000.0, 980.0, PaperGlyphsScene, GalleryDesk);
		Add("ink-paper-sketch", 880.0, 560.0, PaperSketchScene, PaperColor.Hex("#F6F8FC"));
		Add("ink-paper-hand", 980.0, 420.0, PaperHandScene, PaperColor.White);
		Add("ink-paper-cards", 1130.0, 300.0, PaperCardsScene, GalleryDesk);
		Add("ink-paper-notes", 820.0, 440.0, PaperNotesScene, GalleryDesk);
		Add("ink-paper-edge", 760.0, 360.0, PaperEdgeScene, GalleryDesk);
		void Add(string name, double width, double height, Func<FrameworkElement> build, Color desk)
		{
			list.Add(new Scene(name, width, height, () => new Grid
			{
				Background = Ui.Brush(desk),
				Children = { (UIElement)build() }
			}, desk));
		}
	}

	private static void AddInkViewScenes(List<Scene> list)
	{
		ClickyHomeRendering.IsOffscreen = true;
		AddInkPageScenes(list);
		AddInkObjectScenes(list);
		AddInkInteractiveScenes(list);
		AddInkDashboardScenes(list);
		AddInkFormScenes(list);
		AddInkFrontPageScenes(list);
		AddInkEdgeScenes(list);
	}

	private static FrameworkElement DashPinned(Func<FrameworkElement> build)
	{
		Func<DateTimeOffset> referenceNow = ClickyInkDates.ReferenceNow;
		ClickyInkDates.ReferenceNow = () => DashboardNow;
		try
		{
			return build();
		}
		finally
		{
			ClickyInkDates.ReferenceNow = referenceNow;
		}
	}

	private static double DashChangeClock(ClickyInkLiveNote note)
	{
		return ClickyInkChange.ChangedAt(note.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Spark && b.Source.Text == "Leads found").DashboardResolved(note.FinishedClock + 30.0).Source) ?? note.FinishedClock;
	}

	private static FrameworkElement DashColumns(ClickyInkLiveNote note, double clock, string caption)
	{
		List<ClickyInkGroup> list = ClickyInkGroup.Groups(note.Pages[0], clock);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 20.0
		};
		List<StackPanel> list2 = (from _ in Enumerable.Range(0, 3)
			select new StackPanel
			{
				Spacing = 10.0
			}).ToList();
		double[] array = new double[3];
		foreach (ClickyInkGroup item in list)
		{
			ClickyInkGroup g = item;
			int num = Array.IndexOf(array, array.Min());
			array[num] += ((g.Kind != ClickyInkDocument.Kind.Tiles) ? 1 : 3);
			list2[num].Children.Add(InkGroupScene(note, (ClickyInkGroup x) => x.Id == g.Id, clock, 0, null));
		}
		foreach (StackPanel item2 in list2)
		{
			stackPanel.Children.Add(item2);
		}
		return new StackPanel
		{
			Spacing = 10.0,
			Margin = new Thickness(24.0),
			Children = 
			{
				(UIElement)DashCaption(caption),
				(UIElement)stackPanel
			}
		};
	}

	private static FrameworkElement DashPreviewGroup(ClickyInkLiveNote note, Func<ClickyInkGroup, bool> pick, double clock, ClickyInkActions actions)
	{
		ClickyHomeSpreadPaper note2 = ClickyHomeSpreadPaper.Note;
		ClickyInkStyle style = new ClickyInkStyle(note2.Ink, note2.Quiet, note2.Deep, note.LatestEdit(clock)).Resting(note, null);
		ClickyInkGroup group = ClickyInkGroup.Groups(note.Pages[0], clock).First(pick);
		FrameworkElement view;
		using (ClickyInkEnvironment.Use(ClickyInkEnvironment.Default with
		{
			IsWindowPreview = true
		}))
		{
			view = ClickyInkGroupView.Make(group, clock, style, note2, actions);
		}
		return DashOnPaper(view, 378.0);
	}

	private static FrameworkElement DashOnPaper(FrameworkElement view, double width)
	{
		return new Grid
		{
			Width = width + 72.0,
			Padding = new Thickness(36.0, 20.0, 36.0, 20.0),
			HorizontalAlignment = HorizontalAlignment.Left,
			Children = 
			{
				(UIElement)new PaperSurface(ClickyShape.Rectangle, ClickyHomeSpreadPaper.Note.Fill, 0.45)
				{
					Margin = new Thickness(-36.0, -20.0, -36.0, -20.0)
				},
				(UIElement)view
			}
		};
	}

	private static TextBlock DashCaption(string text)
	{
		return Ui.Text(text, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A"));
	}

	private static void AddInkEdgeScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-edge-rest", 1500.0, 330.0, delegate
		{
			StackPanel obj = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 24.0,
				Margin = new Thickness(24.0)
			};
			ClickyInkLiveNote clickyInkLiveNote = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			ClickyInkLiveNote clickyInkLiveNote2 = ClickyInkSamples.LiveNote("# Short clip editor\n## In short\n> Start with CapCut. {big=\"CapCut\"}\n- Auto captions speed up short edits.\n::question[What is the clip for?]{yes=\"Vertical social\" no=\"A polished Mac edit\" yes_label=\"Social\" no_label=\"Polished\"}");
			double clock = clickyInkLiveNote.FinishedClock * 0.45;
			obj.Children.Add(EdgeLabelled("writing (job running)", new ClickyEdgeStackInkCard(EdgeThing("job-a", ClickyInkCardSubject.SubjectKind.Job, ClickyInkCardSubject.SubjectPhase.Working), ClickyInkSamples.HomeInkNote(clickyInkLiveNote, clock, null), ClickyEdgeStackInkPresentation.AtRest, "reading rtings.com", null)));
			obj.Children.Add(EdgeLabelled("needs you (a question)", new ClickyEdgeStackInkCard(EdgeThing("job-b", ClickyInkCardSubject.SubjectKind.Job, ClickyInkCardSubject.SubjectPhase.Done), ClickyInkSamples.HomeInkNote(clickyInkLiveNote2, clickyInkLiveNote2.FinishedClock + 20.0, null), ClickyEdgeStackInkPresentation.AtRest, null, null)));
			ClickyInkLiveNote clickyInkLiveNote3 = ClickyInkSamples.LiveNote(ClickyInkSamples.DeskPickVersions);
			obj.Children.Add(EdgeLabelled("ready (kept, sky)", new ClickyEdgeStackInkCard(EdgeThing("kept-c", ClickyInkCardSubject.SubjectKind.Kept, ClickyInkCardSubject.SubjectPhase.Done, ClickyThingStatus.Ready, ClickyPaperStock.Sky), ClickyInkSamples.HomeInkNote(clickyInkLiveNote3, clickyInkLiveNote3.FinishedClock + 20.0, null), ClickyEdgeStackInkPresentation.AtRest, null, null)));
			obj.Children.Add(EdgeLabelled("done (kept, mint): no tape", new ClickyEdgeStackInkCard(EdgeThing("kept-d", ClickyInkCardSubject.SubjectKind.Kept, ClickyInkCardSubject.SubjectPhase.Done, ClickyThingStatus.Done, ClickyPaperStock.Mint), ClickyInkSamples.HomeInkNote(clickyInkLiveNote3, clickyInkLiveNote3.FinishedClock + 20.0, null), ClickyEdgeStackInkPresentation.AtRest, null, null)));
			return obj;
		});
		AddInkScene(list, "ink-edge-fanned", 1150.0, 330.0, delegate
		{
			StackPanel obj2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 24.0,
				Margin = new Thickness(24.0)
			};
			ClickyInkLiveNote clickyInkLiveNote4 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			ClickyInkLiveNote clickyInkLiveNote5 = ClickyInkSamples.LiveNote(ClickyInkSamples.DeskPickVersions);
			ClickyHomeInkNote clickyHomeInkNote = ClickyInkSamples.HomeInkNote(clickyInkLiveNote5, clickyInkLiveNote5.FinishedClock + 5.0, null);
			obj2.Children.Add(EdgeLabelled("front, writing", new ClickyEdgeStackInkCard(EdgeThing("job-a", ClickyInkCardSubject.SubjectKind.Job, ClickyInkCardSubject.SubjectPhase.Working), ClickyInkSamples.HomeInkNote(clickyInkLiveNote4, clickyInkLiveNote4.FinishedClock * 0.3, null), ClickyEdgeStackInkPresentation.InFan(isFront: true), "comparing milk frothers", null)));
			obj2.Children.Add(EdgeLabelled("behind: changed since you looked (looked at clock 20)", new ClickyEdgeStackInkCard(EdgeThing("job-e", ClickyInkCardSubject.SubjectKind.Job, ClickyInkCardSubject.SubjectPhase.Done, ClickyThingStatus.Ready), clickyHomeInkNote, ClickyEdgeStackInkPresentation.InFan(isFront: false), null, clickyHomeInkNote.StartedAt.AddSeconds(20.0))));
			obj2.Children.Add(EdgeLabelled("behind: kept, periwinkle", new ClickyEdgeStackInkCard(EdgeThing("kept-f", ClickyInkCardSubject.SubjectKind.Kept, ClickyInkCardSubject.SubjectPhase.Done, ClickyThingStatus.Done, ClickyPaperStock.Periwinkle), ClickyInkSamples.HomeInkNote(clickyInkLiveNote5, clickyInkLiveNote5.FinishedClock + 60.0, null), ClickyEdgeStackInkPresentation.InFan(isFront: false), null, null)));
			return obj2;
		});
		AddInkScene(list, "ink-edge-pulled", 1060.0, 800.0, delegate
		{
			StackPanel obj3 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 60.0,
				Margin = new Thickness(60.0)
			};
			ClickyInkLiveNote clickyInkLiveNote6 = ClickyInkSamples.LiveNote("# Short clip editor\n## In short\n> Start with CapCut. {big=\"CapCut\"}\n- Auto captions speed up short edits.\n::question[What is the clip for?]{yes=\"Vertical social\" no=\"A polished Mac edit\" yes_label=\"Social\" no_label=\"Polished\"}");
			ClickyInkLiveNote clickyInkLiveNote7 = ClickyInkSamples.LiveNote("# Clean up Downloads\n## In short\n> Nine old installers take 3.1 GB. {big=\"3.1 GB\"}\n:::review[Move these to the Trash?]{go=\"Move these to the Trash\" skip=\"Keep them\"}\n- [x] Figma-124.dmg — an old installer\n- [ ] Zoom.pkg — installed in 2023\n- [x] Xcode_15.xip — replaced by Xcode 16\n:::\n## Kept\n- Your photo exports");
			obj3.Children.Add(EdgeLabelled("pulled: a question", new ClickyEdgeStackInkCard(EdgeThing("job-b", ClickyInkCardSubject.SubjectKind.Job, ClickyInkCardSubject.SubjectPhase.Done), ClickyInkSamples.HomeInkNote(clickyInkLiveNote6, clickyInkLiveNote6.FinishedClock + 20.0, null), ClickyEdgeStackInkPresentation.PulledOut, null, null)));
			obj3.Children.Add(EdgeLabelled("pulled: a review (kept, butter)", new ClickyEdgeStackInkCard(EdgeThing("kept-g", ClickyInkCardSubject.SubjectKind.Kept, ClickyInkCardSubject.SubjectPhase.Done, ClickyThingStatus.Done, ClickyPaperStock.Butter), ClickyInkSamples.HomeInkNote(clickyInkLiveNote7, clickyInkLiveNote7.FinishedClock + 20.0, null), ClickyEdgeStackInkPresentation.PulledOut, null, null)));
			return obj3;
		});
		AddInkScene(list, "ink-edge-screen", 560.0, 380.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote8 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			return new Grid
			{
				Width = 480.0,
				Height = 300.0,
				Margin = new Thickness(40.0),
				Children = 
				{
					(UIElement)new ClickyEdgeStackInkScreenPaper(ClickyHomeSpreadPaper.Of(ClickyPaperStock.Sky)),
					(UIElement)new ClickyEdgeStackInkCard(EdgeThing("kept-c", ClickyInkCardSubject.SubjectKind.Kept, ClickyInkCardSubject.SubjectPhase.Done, ClickyThingStatus.Ready, ClickyPaperStock.Sky), ClickyInkSamples.HomeInkNote(clickyInkLiveNote8, clickyInkLiveNote8.FinishedClock + 20.0, null), ClickyEdgeStackInkPresentation.InFan(isFront: true), null, null, null, null, null, null, 640.0, fillsScreen: true)
				}
			};
		});
		AddInkScene(list, "ink-edge-face", 860.0, 320.0, delegate
		{
			StackPanel obj4 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 40.0,
				Margin = new Thickness(30.0)
			};
			ClickyInkLiveNote clickyInkLiveNote9 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			ClickyInkLiveNote clickyInkLiveNote10 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Checking which machine under $500 makes the best milk\n- [x] Find the machines people recommend\n- [ ] Compare prices and milk {now}\n- [ ] Pick one and the catch\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| Bambino Plus | $399 | auto |");
			obj4.Children.Add(EdgeLabelled("face: finished", new ClickyInkFaceView(clickyInkLiveNote9, clickyInkLiveNote9.FinishedClock + 5.0, null, isWorking: false, null)));
			obj4.Children.Add(EdgeLabelled("face: working, sky", new ClickyInkFaceView(clickyInkLiveNote10, clickyInkLiveNote10.FinishedClock + 5.0, new global::_003C_003Ez__ReadOnlySingleElementList<ClickyInkActivity>(new ClickyInkActivity(0.0, "search", "searching “best espresso under $500”")), isWorking: true, ClickyHomeSpreadPaper.Of(ClickyPaperStock.Sky))));
			return obj4;
		});
		AddInkScene(list, "ink-edge-edition", 960.0, 1100.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote11 = ClickyInkSamples.LiveNote("# Pocketlog daily\n\n## Today\n::run{n=12 at=\"Sat Oct 10 · 7:00\" next=\"Sun 7:00\" note=\"App Store, Play Console, Stripe\"}\n> Crashes jumped on Android 2.14; signups held. {big=\"98.1%\" note=\"crash-free, down\"}\n:::tiles\n::spark[Signups]{values=\"118, 121, 133, 129, 142, 138\"}\n::figure[Revenue]{value=\"$1,319\" delta=\"+3%\"}\n::status[Crash-free]{state=bad note=\"98.1%: 2.14 crashes at login\"}\n::countdown[2.15 review]{to=\"2026-10-12 09:00\"}\n:::\n::sticky[Pause the 2.14 rollout: 212 crashes, all at login]{color=pink}\n\n## Your move\n::question[Pause the 2.14 rollout?]{yes=\"Pause the Android 2.14 rollout in the Play Console\" no=\"Keep it going and watch it\" yes_label=\"Pause it\" no_label=\"Keep going\" since=\"Oct 10\"}\n\n## Worth a look\n- Android 2.14 crashes at login on Pixel 8 {new}\n  212 crashes, 97% on 2.14\n- Onboarding step 3 drop-off is down to 38%\n\n## Funnel\n:::pipeline[Yesterday]\nVisited: 4,050\nSigned up: 138\nStarted trial: 64\nPaid: 11\n:::");
			return new Grid
			{
				Margin = new Thickness(30.0),
				Children = { (UIElement)new ClickyInkEditionView(clickyInkLiveNote11, clickyInkLiveNote11.FinishedClock + 5.0, 12, "Tuesday, October 21", null) }
			};
		});
		AddInkScene(list, "ink-edge-desk", 1440.0, 1100.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote12 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			return new ClickyInkDeskView(clickyInkLiveNote12, clickyInkLiveNote12.FinishedClock + 5.0, 1440.0, null);
		});
	}

	private static ClickyInkCardSubject EdgeThing(string id, ClickyInkCardSubject.SubjectKind kind, ClickyInkCardSubject.SubjectPhase phase, ClickyThingStatus status = ClickyThingStatus.Done, ClickyPaperStock stock = ClickyPaperStock.Cream)
	{
		return new ClickyInkCardSubject(id, kind, phase == ClickyInkCardSubject.SubjectPhase.Working, phase, status, stock);
	}

	private static FrameworkElement EdgeLabelled(string label, FrameworkElement content)
	{
		return new StackPanel
		{
			Spacing = 6.0,
			Children = 
			{
				(UIElement)content,
				(UIElement)Caption(label)
			}
		};
	}

	private static FrameworkElement Captioned(FrameworkElement view, string caption)
	{
		return new StackPanel
		{
			Spacing = 6.0,
			VerticalAlignment = VerticalAlignment.Top,
			Children = 
			{
				(UIElement)view,
				(UIElement)Ui.Text(caption, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A"))
			}
		};
	}

	private static FrameworkElement Sheet(ClickyInkLiveNote note, double clock, string caption, ClickyHomeSpreadPaper? paper = null, bool? isWorking = null, double? followsPenInHeight = null)
	{
		return Captioned(new ClickyInkSheetView(note, clock, paper, null, isEmbedded: false, followsPenInHeight, null, isWorking, null)
		{
			VerticalAlignment = VerticalAlignment.Top
		}, caption);
	}

	private static FrameworkElement TopOfNote(ClickyInkLiveNote note, double clock, bool isWorking, bool animates, bool glance, double width, double height, string caption, ClickyPaperStock stock = ClickyPaperStock.Sky)
	{
		ClickyHomeSpreadPaper paper = ClickyHomeSpreadPaper.Of(stock);
		ClickyHomeInkNote ink = ClickyInkSamples.HomeInkNote(note, clock, null);
		return Captioned(new Grid
		{
			Width = width,
			Height = height,
			CornerRadius = new CornerRadius(10.0),
			Children = 
			{
				(UIElement)new ClickyEdgeStackInkScreenPaper(paper),
				(UIElement)new ClickyInkTopOfNote(ink, paper, new ClickyInkActions(), isWorking, animates, glance)
			}
		}, caption);
	}

	private static void AddInkFrontPageScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-dashform-board", 960.0, 640.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote = Runs();
			FrameworkElement view2 = ClickyInkDashboardView.Make(clickyInkLiveNote, clickyInkLiveNote.FinishedClock + 30.0, null, null, null, null);
			return new StackPanel
			{
				Margin = new Thickness(40.0, 30.0, 24.0, 24.0),
				Children = { (UIElement)Captioned(view2, "dashboard, run 13 at rest (two columns, 864 wide)") }
			};
		});
		AddInkScene(list, "ink-dashform-run", 960.0, 600.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote2 = ClickyInkSamples.LiveNote("# Pocketlog daily\n\n## Today\n::run{n=12 at=\"Sat Oct 10 · 7:00\" next=\"Sun 7:00\" note=\"App Store, Play Console, Stripe\"}\n> Crashes jumped on Android 2.14; signups held. {big=\"98.1%\" note=\"crash-free, down\"}\n:::tiles\n::spark[Signups]{values=\"118, 121, 133, 129, 142, 138\"}\n::figure[Revenue]{value=\"$1,319\" delta=\"+3%\"}\n::status[Crash-free]{state=bad note=\"98.1%: 2.14 crashes at login\"}\n::countdown[2.15 review]{to=\"2026-10-12 09:00\"}\n:::\n::sticky[Pause the 2.14 rollout: 212 crashes, all at login]{color=pink}\n\n## Your move\n::question[Pause the 2.14 rollout?]{yes=\"Pause the Android 2.14 rollout in the Play Console\" no=\"Keep it going and watch it\" yes_label=\"Pause it\" no_label=\"Keep going\" since=\"Oct 10\"}\n\n## Worth a look\n- Android 2.14 crashes at login on Pixel 8 {new}\n  212 crashes, 97% on 2.14\n- Onboarding step 3 drop-off is down to 38%\n\n## Funnel\n:::pipeline[Yesterday]\nVisited: 4,050\nSigned up: 138\nStarted trial: 64\nPaid: 11\n:::");
			double clock = clickyInkLiveNote2.FinishedClock + 30.0;
			ClickyHomeSpreadPaper? paper = ClickyHomeSpreadPaper.Of(ClickyPaperStock.Sky);
			int? runNumber = 12;
			FrameworkElement view3 = ClickyInkDashboardView.Make(clickyInkLiveNote2, clock, paper, null, null, runNumber);
			return new StackPanel
			{
				Margin = new Thickness(40.0, 30.0, 24.0, 24.0),
				Children = { (UIElement)Captioned(view3, "dashboard, run 12 on sky paper (your move on the right)") }
			};
		});
		AddInkScene(list, "ink-dashform-narrow", 1160.0, 960.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote3 = Runs();
			StackPanel obj = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 40.0,
				Margin = new Thickness(40.0, 30.0, 24.0, 24.0)
			};
			UIElementCollection children = obj.Children;
			double clock2 = clickyInkLiveNote3.FinishedClock + 30.0;
			double? width = 520.0;
			children.Add(Captioned(ClickyInkDashboardView.Make(clickyInkLiveNote3, clock2, null, null, width, null), "one column (520 wide)"));
			ClickyInkLiveNote clickyInkLiveNote4 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			UIElementCollection children2 = obj.Children;
			double clock3 = clickyInkLiveNote4.FinishedClock + 30.0;
			width = 520.0;
			ClickyInkDashboardView.Job job = new ClickyInkDashboardView.Job("Made by Clicky · espresso", ClickyThingStatus.Ready);
			children2.Add(Captioned(ClickyInkDashboardView.Make(clickyInkLiveNote4, clock3, null, null, width, null, null, job), "a job's board"));
			return obj;
		});
		AddInkScene(list, "ink-dashform-face", 1180.0, 300.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote5 = Runs();
			StackPanel obj2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 34.0,
				Margin = new Thickness(36.0, 40.0, 24.0, 24.0)
			};
			ClickyInkLiveNote clickyInkLiveNote6 = ClickyInkSamples.LiveNote("# Pocketlog daily\n\n## Today\n::run{n=12 at=\"Sat Oct 10 · 7:00\" next=\"Sun 7:00\" note=\"App Store, Play Console, Stripe\"}\n> Crashes jumped on Android 2.14; signups held. {big=\"98.1%\" note=\"crash-free, down\"}\n:::tiles\n::spark[Signups]{values=\"118, 121, 133, 129, 142, 138\"}\n::figure[Revenue]{value=\"$1,319\" delta=\"+3%\"}\n::status[Crash-free]{state=bad note=\"98.1%: 2.14 crashes at login\"}\n::countdown[2.15 review]{to=\"2026-10-12 09:00\"}\n:::\n::sticky[Pause the 2.14 rollout: 212 crashes, all at login]{color=pink}\n\n## Your move\n::question[Pause the 2.14 rollout?]{yes=\"Pause the Android 2.14 rollout in the Play Console\" no=\"Keep it going and watch it\" yes_label=\"Pause it\" no_label=\"Keep going\" since=\"Oct 10\"}\n\n## Worth a look\n- Android 2.14 crashes at login on Pixel 8 {new}\n  212 crashes, 97% on 2.14\n- Onboarding step 3 drop-off is down to 38%\n\n## Funnel\n:::pipeline[Yesterday]\nVisited: 4,050\nSigned up: 138\nStarted trial: 64\nPaid: 11\n:::");
			obj2.Children.Add(Captioned(ClickyInkDashboardFaceView.Make(clickyInkLiveNote6, clickyInkLiveNote6.FinishedClock + 30.0, null, null), "face, run 12"));
			obj2.Children.Add(Captioned(ClickyInkDashboardFaceView.Make(clickyInkLiveNote5, 41.2, null, null), "face, run 13 landing (clock 41.2)"));
			obj2.Children.Add(Captioned(ClickyInkDashboardFaceView.Make(clickyInkLiveNote5, clickyInkLiveNote5.FinishedClock + 30.0, ClickyHomeSpreadPaper.Of(ClickyPaperStock.Mint), null), "face, run 13 at rest, mint"));
			return obj2;
		});
		(ClickyInkFrontPageVariant, string)[] array = new(ClickyInkFrontPageVariant, string)[2]
		{
			(ClickyInkFrontPageVariant.Broadsheet, "broadsheet"),
			(ClickyInkFrontPageVariant.MissionControl, "mission")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(ClickyInkFrontPageVariant, string) tuple = array[i];
			ClickyInkFrontPageVariant item = tuple.Item1;
			string name = tuple.Item2;
			ClickyInkFrontPageVariant v = item;
			AddInkScene(list, "ink-front-" + name, 1000.0, 1240.0, delegate
			{
				ClickyInkLiveNote clickyInkLiveNote7 = Runs();
				ClickyInkLiveNote clickyInkLiveNote8 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
				return new StackPanel
				{
					Spacing = 24.0,
					Margin = new Thickness(40.0, 30.0, 24.0, 24.0),
					Children = 
					{
						(UIElement)Captioned(ClickyInkFrontPageView.Make(clickyInkLiveNote7, clickyInkLiveNote7.FinishedClock + 30.0, v, null, null, null, null), name + ": a kept routine's run 13"),
						(UIElement)Captioned(ClickyInkFrontPageView.Make(clickyInkLiveNote8, clickyInkLiveNote8.FinishedClock + 30.0, v, null, null, null, null), name + ": a one-off job")
					}
				};
			});
		}
		AddInkScene(list, "ink-front-writing", 1000.0, 640.0, delegate
		{
			ClickyInkLiveNote note = Runs();
			return new StackPanel
			{
				Spacing = 24.0,
				Margin = new Thickness(40.0, 30.0, 24.0, 24.0),
				Children = { (UIElement)Captioned(ClickyInkFrontPageView.Make(note, 41.5, ClickyInkFrontPageVariant.MissionControl, null, null, null, null), "mission control while run 13 is written (clock 41.5): LIVE") }
			};
		});
		static FrameworkElement Captioned(FrameworkElement view, string caption)
		{
			return new StackPanel
			{
				Spacing = 6.0,
				Children = 
				{
					(UIElement)view,
					(UIElement)Ui.Text(caption, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A"))
				}
			};
		}
		static ClickyInkLiveNote Runs()
		{
			return ClickyInkSamples.LiveNote(("# Pocketlog daily\n\n## Today\n::run{n=12 at=\"Sat Oct 10 · 7:00\" next=\"Sun 7:00\" note=\"App Store, Play Console, Stripe\"}\n> Crashes jumped on Android 2.14; signups held. {big=\"98.1%\" note=\"crash-free, down\"}\n:::tiles\n::spark[Signups]{values=\"118, 121, 133, 129, 142, 138\"}\n::figure[Revenue]{value=\"$1,319\" delta=\"+3%\"}\n::status[Crash-free]{state=bad note=\"98.1%: 2.14 crashes at login\"}\n::countdown[2.15 review]{to=\"2026-10-12 09:00\"}\n:::\n::sticky[Pause the 2.14 rollout: 212 crashes, all at login]{color=pink}\n\n## Your move\n::question[Pause the 2.14 rollout?]{yes=\"Pause the Android 2.14 rollout in the Play Console\" no=\"Keep it going and watch it\" yes_label=\"Pause it\" no_label=\"Keep going\" since=\"Oct 10\"}\n\n## Worth a look\n- Android 2.14 crashes at login on Pixel 8 {new}\n  212 crashes, 97% on 2.14\n- Onboarding step 3 drop-off is down to 38%\n\n## Funnel\n:::pipeline[Yesterday]\nVisited: 4,050\nSigned up: 138\nStarted trial: 64\nPaid: 11\n:::", 0.0, false), ("# Pocketlog daily\n\n## Today\n::run{n=13 at=\"Sun Oct 11 · 7:00\" next=\"Mon 7:00\" note=\"App Store, Play Console, Stripe\"}\n> 2.14 paused; crash-free back to 99.6%. {big=\"99.6%\" note=\"crash-free, up\"}\n:::tiles\n::spark[Signups]{values=\"118, 121, 133, 129, 142, 138, 151\"}\n::figure[Revenue]{value=\"$1,402\" delta=\"+6%\"}\n::status[Crash-free]{state=ok note=\"99.6% since the pause\"}\n::countdown[2.15 review]{to=\"2026-10-12 09:00\"}\n:::\n\n## Worth a look\n- Android 2.14 rollout paused at 20% {new}\n- Onboarding step 3 drop-off is down to 38%\n\n## Funnel\n:::pipeline[Yesterday]\nVisited: 4,210\nSigned up: 151\nStarted trial: 70\nPaid: 13\n:::", 40.0, true));
		}
	}

	private static StackPanel InteractiveRow(params FrameworkElement[] children)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 22.0,
			Margin = new Thickness(24.0)
		};
		foreach (FrameworkElement frameworkElement in children)
		{
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			stackPanel.Children.Add(frameworkElement);
		}
		return stackPanel;
	}

	private static FrameworkElement InteractiveCaptioned(FrameworkElement view, string caption)
	{
		return new StackPanel
		{
			Spacing = 6.0,
			Children = 
			{
				(UIElement)view,
				(UIElement)Ui.Text(caption, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A"))
			}
		};
	}

	private static FrameworkElement InkObjectScene(Func<FrameworkElement> make, string caption)
	{
		InkObjectState.ResetAll();
		ClickyHomeSpreadPaper note = ClickyHomeSpreadPaper.Note;
		return InteractiveCaptioned(new Grid
		{
			Width = 450.0,
			Padding = new Thickness(36.0, 24.0, 36.0, 24.0),
			Children = 
			{
				(UIElement)new PaperSurface(ClickyShape.Rectangle, note.Fill, 0.45)
				{
					Margin = new Thickness(-36.0, -24.0, -36.0, -24.0)
				},
				(UIElement)make()
			}
		}, caption);
	}

	private static ClickyInkLiveNote.Block Block(string text, ClickyInkDocument.Kind kind)
	{
		return FindBlock(ClickyInkSamples.LiveNote(text), kind);
	}

	private static ClickyInkLiveNote.Block FindBlock(ClickyInkLiveNote note, ClickyInkDocument.Kind kind)
	{
		return note.Pages.SelectMany((ClickyInkLiveNote.Page p) => p.Blocks).First((ClickyInkLiveNote.Block b) => b.Source.Kind == kind);
	}

	private static ClickyInkActions WithFeedback(ClickyInkNoteAction? pending = null, ClickyInkNoteAction? answered = null)
	{
		DateTimeOffset now = DateTimeOffset.Now;
		List<ClickyInkSentAction> list = new List<ClickyInkSentAction>();
		if ((object)answered != null)
		{
			list.Add(new ClickyInkSentAction(answered, now.AddMinutes(-3.0), wasSentWhileRunning: false));
		}
		if ((object)pending != null)
		{
			list.Add(new ClickyInkSentAction(pending, now.AddSeconds(-2.0), wasSentWhileRunning: false));
		}
		DateTimeOffset value = (((object)pending == null) ? now.AddMinutes(-1.0) : now.AddMinutes(-2.0));
		return new ClickyInkActions
		{
			Feedback = ClickyInkActionFeedback.Make(list, value, isRunning: true, null, null, now)
		};
	}

	private static FrameworkElement FillForm(ClickyInkLiveNote note, ClickyInkLiveNote.Block form)
	{
		ClickyInkForm.SetForGallery(form, new Dictionary<string, string>
		{
			["Your studio name"] = "Bright Pixel Studio",
			["Tone"] = "Friendly",
			["Portfolio"] = "C:\\Users\\maya\\Documents\\Studio portfolio 2026"
		});
		return InkGroupScene(note, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Form, null, 0, null);
	}

	private static ClickyInkInlineAnswerField? FindField(FrameworkElement root)
	{
		return Descendants(root).OfType<ClickyInkInlineAnswerField>().FirstOrDefault();
	}

	[IteratorStateMachine(typeof(_003CDescendants_003Ed__35))]
	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CDescendants_003Ed__35(-2)
		{
			_003C_003E3__root = root
		};
	}

	private static void WriteSamplePng(string path, (int R, int G, int B) a, (int R, int G, int B) b, int size = 240)
	{
		if (File.Exists(path))
		{
			return;
		}
		byte[] array = new byte[size * (size * 3 + 1)];
		for (int i = 0; i < size; i++)
		{
			int num = i * (size * 3 + 1);
			array[num] = 0;
			for (int j = 0; j < size; j++)
			{
				double num2 = (double)(j + i) / (2.0 * (double)size);
				double num3 = (double)j - (double)size / 2.0;
				double num4 = (double)i - (double)size / 2.0;
				int num8;
				int num9;
				int num10;
				if (!(num3 * num3 + num4 * num4 < (double)(size * size) / 9.0))
				{
					int num5 = (int)((double)a.R + (double)(b.R - a.R) * num2 * 0.5);
					int num6 = (int)((double)a.G + (double)(b.G - a.G) * num2 * 0.5);
					int num7 = (int)((double)a.B + (double)(b.B - a.B) * num2 * 0.5);
					num8 = num7;
					num9 = num6;
					num10 = num5;
				}
				else
				{
					(num10, num9, num8) = b;
				}
				array[num + 1 + j * 3] = (byte)num10;
				array[num + 2 + j * 3] = (byte)num9;
				array[num + 3 + j * 3] = (byte)num8;
			}
		}
		FileStream file = File.Create(path);
		try
		{
			file.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
			byte[] array2 = new byte[13];
			byte[] bytes = BitConverter.GetBytes(size);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes);
			}
			bytes.CopyTo(array2, 0);
			bytes.CopyTo(array2, 4);
			array2[8] = 8;
			array2[9] = 2;
			Chunk("IHDR", array2);
			using MemoryStream memoryStream = new MemoryStream();
			using (ZLibStream zLibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
			{
				zLibStream.Write(array);
			}
			Chunk("IDAT", memoryStream.ToArray());
			Chunk("IEND", Array.Empty<byte>());
		}
		finally
		{
			if (file != null)
			{
				((IDisposable)file).Dispose();
			}
		}
		void Chunk(string type, byte[] data)
		{
			byte[] bytes2 = Encoding.ASCII.GetBytes(type);
			byte[] bytes3 = BitConverter.GetBytes(data.Length);
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes3);
			}
			file.Write(bytes3);
			file.Write(bytes2);
			file.Write(data);
			byte[] array3 = bytes2;
			int num11 = 0;
			byte[] array4 = new byte[array3.Length + data.Length];
			ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(array3);
			readOnlySpan.CopyTo(new Span<byte>(array4).Slice(num11, readOnlySpan.Length));
			num11 += readOnlySpan.Length;
			ReadOnlySpan<byte> readOnlySpan2 = new ReadOnlySpan<byte>(data);
			readOnlySpan2.CopyTo(new Span<byte>(array4).Slice(num11, readOnlySpan2.Length));
			num11 += readOnlySpan2.Length;
			byte[] bytes4 = BitConverter.GetBytes(Crc32(array4));
			if (BitConverter.IsLittleEndian)
			{
				Array.Reverse(bytes4);
			}
			file.Write(bytes4);
		}
	}

	private static uint Crc32(byte[] data)
	{
		uint num = uint.MaxValue;
		foreach (byte b in data)
		{
			num ^= b;
			for (int j = 0; j < 8; j++)
			{
				num = (((num & 1) != 0) ? (0xEDB88320u ^ (num >> 1)) : (num >> 1));
			}
		}
		return ~num;
	}

	private static string TravelNote()
	{
		string value = SamplePicture("belem.png", (R: 240, G: 170, B: 120), (R: 90, G: 120, B: 170));
		string value2 = SamplePicture("market.png", (R: 200, G: 90, B: 60), (R: 250, G: 220, B: 150));
		return $"# Lisbon in May\n\n## In short\n> Fly ANA on the 12th; the whole trip comes to $3,380 {{big=\"$3,380\" note=\"under budget by $420\"}}\n::ticket[ANA NH7, nonstop]{{from=\"Boston (BOS)\" to=\"Lisbon (LIS)\" when=\"May 12, 9:40 pm\" price=\"$842\" seat=\"32K\" kind=flight}}\n:::receipt[Trip budget]\nFlights: $1,684\nHotel, 5 nights: $1,120\nFood and trams … $576\n:::\n::place[Time Out Market]{{note=\"go hungry, 8 stalls\" rating=4.6 hours=\"10am–midnight\"}}\n:::day[Saturday May 13 · Belém · 9 km]\n9:30 Pastéis de Belém — before the line\n11:00 Jerónimos Monastery\n1:00 pm Lunch at the market — Time Out, stall 12\n:::\n:::day[Proposal timeline]\n- Draft to Dana\n- Review with legal — Thursday\n:::\n::photo[Belém at sunset]{{path=\"{value}\"}}\n::photo[The market hall]{{path=\"{value2}\"}}\n::photo[A web picture]{{url=\"{"https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/PNG_transparency_demonstration_1.png/330px-PNG_transparency_demonstration_1.png"}\"}}";
	}

	private static string LinksNote()
	{
		string value = SamplePicture("site-shot.png", (R: 245, G: 245, B: 240), (R: 60, G: 110, B: 220));
		string sampleDirectory = SampleDirectory;
		File.WriteAllText(Path.Combine(sampleDirectory, "index.html"), "<html><body>Site</body></html>");
		string[] array = new string[8] { "hero.jpg", "styles.css", "app.js", "data.json", "logo.svg", "about.html", "contact.html", "favicon.ico" };
		foreach (string text in array)
		{
			File.WriteAllText(Path.Combine(sampleDirectory, text), text);
		}
		return $"# Launch reply\n\n## In short\n> The draft is ready in your Gmail {{big=\"1\"}}\n::link[Open the Gmail draft]{{url=\"https://mail.google.com/mail/u/0/#drafts/abc\"}}\n::file[The site, ready to open]{{path=\"{Path.Combine(sampleDirectory, "index.html")}\" thumb=\"{value}\"}}\n:::letter{{to=\"Dana Reyes\" subject=\"Re: launch date\" send=\"Send this reply to Dana from my Gmail\"}}\nHi Dana,\n\nThursday works. I moved the review to 3 pm so legal can join, and the deck is in the shared folder.\n\nTalk soon,\nMaya\n:::\n::clip[The steadiest desk we tested at standing height.]{{by=Wirecutter source=https://www.nytimes.com/wirecutter/reviews/best-standing-desk/}}\n::question[Mostly milk drinks?]{{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}}\n::question[Which city first?]{{options=\"Lisbon | Porto | Madeira\" other=\"Somewhere else…\" since=\"Oct 9\"}}\n::ask[Find a grinder to match]{{say=\"Find the best grinder under $150\"}}\n::ask[Book the hotel]{{say=\"Book the hotel in Belém\"}}\n::file[Hero photo]{{path=\"{Path.Combine(sampleDirectory, "hero.jpg")}\"}}\n::file[Styles]{{path=\"{Path.Combine(sampleDirectory, "styles.css")}\"}}\n::file[Script]{{path=\"{Path.Combine(sampleDirectory, "app.js")}\"}}\n::file[Data]{{path=\"{Path.Combine(sampleDirectory, "data.json")}\"}}\n::file[Logo]{{path=\"{Path.Combine(sampleDirectory, "logo.svg")}\"}}\n::file[About]{{path=\"{Path.Combine(sampleDirectory, "about.html")}\"}}\n::file[Contact]{{path=\"{Path.Combine(sampleDirectory, "contact.html")}\"}}\n::file[Icon]{{path=\"{Path.Combine(sampleDirectory, "favicon.ico")}\"}}\n::link[Wirecutter: espresso machines]{{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}}\n::link[Reddit thread]{{url=https://www.reddit.com/r/espresso/}}";
	}

	private static void AddInkScene(List<Scene> list, string name, double width, double height, Func<FrameworkElement> build, double seconds)
	{
		list.Add(new Scene(name, width, height, () => new Grid
		{
			Background = Ui.Brush(InkDesk),
			Children = { (UIElement)build() }
		}, InkDesk, seconds));
	}

	private static FrameworkElement ObjectsRow(params FrameworkElement[] columns)
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 22.0,
			Margin = new Thickness(24.0)
		};
		foreach (FrameworkElement frameworkElement in columns)
		{
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			stackPanel.Children.Add(frameworkElement);
		}
		return stackPanel;
	}

	private static FrameworkElement ObjectsCaptioned(FrameworkElement view, string caption)
	{
		return new StackPanel
		{
			Spacing = 6.0,
			Children = 
			{
				(UIElement)view,
				(UIElement)Ui.Text(caption, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A"))
			}
		};
	}

	private static string SamplePicture(string name, (int R, int G, int B) sky, (int R, int G, int B) land)
	{
		string text = Path.Combine(SampleDirectory, name);
		if (File.Exists(text))
		{
			return text;
		}
		byte[] array = new byte[307440];
		for (int i = 0; i < 240; i++)
		{
			int num = i * 1281;
			array[num] = 0;
			for (int j = 0; j < 320; j++)
			{
				double num2 = 148.8 + Math.Sin((double)j / 37.0) * 18.0 + Math.Sin((double)j / 11.0) * 5.0;
				bool flag = Math.Pow(j - 230, 2.0) + Math.Pow(i - 70, 2.0) < 676.0;
				double num3 = (double)i / 240.0;
				double num7;
				double num8;
				double num9;
				if (!((double)i > num2))
				{
					if (!flag)
					{
						double num4 = (double)sky.R + (double)(255 - sky.R) * num3 * 0.6;
						double num5 = (double)sky.G + (double)(255 - sky.G) * num3 * 0.6;
						double num6 = (double)sky.B + (double)(255 - sky.B) * num3 * 0.6;
						num7 = num6;
						num8 = num5;
						num9 = num4;
					}
					else
					{
						double num6 = 236.0;
						double num5 = 170.0;
						num7 = num5;
						num8 = num6;
						num9 = 255.0;
					}
				}
				else
				{
					double num10 = (double)land.R * (1.0 - num3 * 0.3);
					double num5 = (double)land.G * (1.0 - num3 * 0.3);
					double num6 = (double)land.B * (1.0 - num3 * 0.3);
					num7 = num6;
					num8 = num5;
					num9 = num10;
				}
				int num11 = num + 1 + j * 4;
				array[num11] = (byte)num9;
				array[num11 + 1] = (byte)num8;
				array[num11 + 2] = (byte)num7;
				array[num11 + 3] = byte.MaxValue;
			}
		}
		FileStream file = File.Create(text);
		try
		{
			file.Write(new byte[8] { 137, 80, 78, 71, 13, 10, 26, 10 });
			byte[] array2 = new byte[13];
			BigEndian(320u).CopyTo(array2, 0);
			BigEndian(240u).CopyTo(array2, 4);
			array2[8] = 8;
			array2[9] = 6;
			Chunk("IHDR", array2);
			using MemoryStream memoryStream = new MemoryStream();
			using (ZLibStream zLibStream = new ZLibStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
			{
				zLibStream.Write(array);
			}
			Chunk("IDAT", memoryStream.ToArray());
			Chunk("IEND", Array.Empty<byte>());
			return text;
		}
		finally
		{
			if (file != null)
			{
				((IDisposable)file).Dispose();
			}
		}
		void Chunk(string type, byte[] data)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(type);
			file.Write(BigEndian((uint)data.Length));
			file.Write(bytes);
			file.Write(data);
			file.Write(BigEndian(Crc(bytes.Concat(data).ToArray())));
		}
	}

	private static byte[] BigEndian(uint value)
	{
		return new byte[4]
		{
			(byte)(value >> 24),
			(byte)(value >> 16),
			(byte)(value >> 8),
			(byte)value
		};
	}

	private static uint Crc(byte[] data)
	{
		uint num = uint.MaxValue;
		foreach (byte b in data)
		{
			num ^= b;
			for (int j = 0; j < 8; j++)
			{
				num = (((num & 1) != 0) ? (0xEDB88320u ^ (num >> 1)) : (num >> 1));
			}
		}
		return num ^ 0xFFFFFFFFu;
	}

	private static TextBlock Caption(string text)
	{
		return Ui.Text(text, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A"));
	}

	private static FrameworkElement Labelled(string label, FrameworkElement content)
	{
		return new StackPanel
		{
			Spacing = 6.0,
			Children = 
			{
				(UIElement)content,
				(UIElement)Caption(label)
			}
		};
	}

	private static FrameworkElement PaperStocksScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 22.0,
			Margin = new Thickness(24.0)
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 18.0
		};
		ClickyPaperStock[] allCases = ClickyPaperStockStyle.AllCases;
		foreach (ClickyPaperStock stock in allCases)
		{
			ClickyEdgeStackInkPaper item = new ClickyEdgeStackInkPaper(ClickyHomeSpreadPaper.Of(stock), isJob: false)
			{
				Width = 100.0,
				Height = 130.0
			};
			Grid grid = new Grid
			{
				Width = 100.0,
				Height = 130.0,
				Children = { (UIElement)item }
			};
			grid.Children.Add(new StackPanel
			{
				Margin = new Thickness(12.0),
				Spacing = 4.0,
				Children = 
				{
					(UIElement)PaperText.Line(stock.RawValue(), ClickyPaperFonts.Hand(21.0), stock.Ink()),
					(UIElement)PaperText.Line("quiet ink", ClickyPaperFonts.Hand(16.0), stock.QuietInk()),
					(UIElement)PaperText.Line("deep", ClickyPaperFonts.Hand(16.0), stock.Deep()),
					(UIElement)new Border
					{
						Height = 14.0,
						Width = 60.0,
						Background = Ui.Brush(stock.Wash()),
						HorizontalAlignment = HorizontalAlignment.Left
					}
				}
			});
			stackPanel2.Children.Add(Labelled(stock.RawValue(), grid));
		}
		stackPanel.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 24.0
		};
		stackPanel3.Children.Add(Labelled("job note (perforated)", new ClickyEdgeStackInkPaper(ClickyHomeSpreadPaper.Note, isJob: true)
		{
			Width = 200.0,
			Height = 220.0
		}));
		stackPanel3.Children.Add(Labelled("kept, lifted", new ClickyEdgeStackInkPaper(ClickyHomeSpreadPaper.Of(ClickyPaperStock.Mint), isJob: false, isLifted: true)
		{
			Width = 200.0,
			Height = 220.0
		}));
		stackPanel3.Children.Add(Labelled("screen paper + board dots", new ClickyEdgeStackInkScreenPaper(ClickyHomeSpreadPaper.Of(ClickyPaperStock.Blush))
		{
			Width = 260.0,
			Height = 220.0
		}));
		stackPanel3.Children.Add(Labelled("cream note fill + grain", new PaperSurface(ClickyShape.Rectangle, ClickyHomeSpreadPaper.Note.Fill, 0.45)
		{
			Width = 160.0,
			Height = 220.0
		}));
		stackPanel.Children.Add(stackPanel3);
		return stackPanel;
	}

	private static FrameworkElement PaperPiecesScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 20.0,
			Margin = new Thickness(24.0)
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 18.0
		};
		ClickyThingStatus[] values = Enum.GetValues<ClickyThingStatus>();
		foreach (ClickyThingStatus status in values)
		{
			stackPanel2.Children.Add(new ClickyPaperStamp(status));
		}
		stackPanel2.Children.Add(new ClickyPaperStamp(ClickyThingStatus.NeedsYou, null, 14.0, -5.0));
		stackPanel2.Children.Add(new ClickyPaperStamp(ClickyThingStatus.Ready, "sent", 12.0));
		stackPanel.Children.Add(Labelled("stamps (every status at 9.5, NEEDS YOU at 14, \"sent\" at 12)", stackPanel2));
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 24.0,
			Margin = new Thickness(6.0, 6.0, 0.0, 0.0)
		};
		ClickyPaperStickerKind[] values2 = Enum.GetValues<ClickyPaperStickerKind>();
		foreach (ClickyPaperStickerKind kind in values2)
		{
			stackPanel3.Children.Add(new ClickyPaperSticker(kind));
		}
		stackPanel.Children.Add(Labelled("stickers", stackPanel3));
		StackPanel stackPanel4 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0
		};
		string[] array = new string[7] { "Deck.key", "Quote.pdf", "Budget.xlsx", "Hero.png", "Brief.md", "index.html", "data.json" };
		foreach (string text in array)
		{
			stackPanel4.Children.Add(new ClickyPaperFileChip(ClickyPaperFileKinds.ShortLabel(text), ClickyPaperFileKinds.ForFileName(text)));
		}
		stackPanel.Children.Add(Labelled("file chips", stackPanel4));
		StackPanel stackPanel5 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0
		};
		ClickyPaperGel[] values3 = Enum.GetValues<ClickyPaperGel>();
		for (int i = 0; i < values3.Length; i++)
		{
			ClickyPaperGel gel = values3[i];
			stackPanel5.Children.Add(new ClickyPaperGelButton(gel.ToString(), null, gel));
		}
		stackPanel5.Children.Add(new ClickyPaperGelButton("Copied", null, ClickyPaperGel.Blue, 26.0, 13.0, 12.0, "checkmark"));
		stackPanel5.Children.Add(new ClickyPaperGelButton("Allow once", null, ClickyPaperGel.White, 26.0, 12.0));
		stackPanel.Children.Add(Labelled("gel buttons", stackPanel5));
		StackPanel stackPanel6 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 18.0
		};
		stackPanel6.Children.Add(new ClickyPaperProgressBar(0.0, 74.0, 4.0, null, null));
		stackPanel6.Children.Add(new ClickyPaperProgressBar(0.4, 74.0, 4.0, null, null));
		stackPanel6.Children.Add(new ClickyPaperProgressBar(1.0, 74.0, 4.0, null, null));
		stackPanel6.Children.Add(new ClickyPaperProgressBar(0.66, 140.0, 6.0, null, null));
		stackPanel.Children.Add(Labelled("progress bars", stackPanel6));
		return stackPanel;
	}

	private static FrameworkElement PaperTagsScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 10.0,
			Margin = new Thickness(20.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		(ClickyThingStatus, string)[] array = new(ClickyThingStatus, string)[6]
		{
			(ClickyThingStatus.NeedsYou, "Needs you"),
			(ClickyThingStatus.Ready, "Ready"),
			(ClickyThingStatus.Working, "Writing"),
			(ClickyThingStatus.Watching, "Watching"),
			(ClickyThingStatus.Done, "Done"),
			(ClickyThingStatus.Paused, "Paused")
		};
		for (int i = 0; i < array.Length; i++)
		{
			var (status, text) = array[i];
			stackPanel.Children.Add(new ClickyPaperStatusTag(status, text));
		}
		return stackPanel;
	}

	private static FrameworkElement PaperGlyphsScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 16.0,
			Margin = new Thickness(24.0)
		};
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 14.0
		};
		ClickyPaperGlyph[] allCases = ClickyPaperGlyphs.AllCases;
		foreach (ClickyPaperGlyph glyph in allCases)
		{
			stackPanel2.Children.Add(new StackPanel
			{
				Spacing = 6.0,
				Children = 
				{
					(UIElement)new ClickyPaperGlyphView(glyph, ClickyPaperStock.Cream.Ink(), 32.0),
					(UIElement)new ClickyPaperGlyphView(glyph, ClickyPaperInk.ClickyBlue),
					(UIElement)Caption(glyph.RawValue())
				}
			});
		}
		stackPanel.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 20.0
		};
		Grid grid = new Grid
		{
			Width = 120.0,
			Height = 70.0,
			Background = Ui.Brush(PaperColor.Hex("#1F2025"))
		};
		grid.Children.Add(new ClickyBrandMark(64.0, null)
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});
		stackPanel3.Children.Add(Labelled("brand mark, gradient", grid));
		stackPanel3.Children.Add(Labelled("ink, 21 @ 2.4", new ClickyBrandMark(21.0, ClickyPaperStock.Sky.Ink(), 2.4)));
		stackPanel3.Children.Add(Labelled("blue, 48", new ClickyBrandMark(48.0, ClickyPaperInk.ClickyBlue)));
		stackPanel.Children.Add(stackPanel3);
		stackPanel.Children.Add(Caption("SF Symbols the Ink notes use, as Segoe Fluent Icons"));
		ClickyNotebookFlow clickyNotebookFlow = new ClickyNotebookFlow
		{
			Spacing = 6.0,
			LineSpacing = 6.0,
			Width = 950.0
		};
		foreach (string item in ClickyInkIcon.KnownNames.OrderBy((string n) => n))
		{
			StackPanel stackPanel4 = new StackPanel
			{
				Width = 86.0,
				Spacing = 2.0
			};
			stackPanel4.Children.Add(ClickyInkIcon.Glyph(item, 18.0, ClickyPaperStock.Cream.Ink()));
			TextBlock textBlock = Ui.Text(item, 8.0, FontWeights.Normal, PaperColor.Hex("#6B6F7A"), TextAlignment.Center);
			textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			textBlock.MaxWidth = 86.0;
			stackPanel4.Children.Add(textBlock);
			clickyNotebookFlow.Children.Add(stackPanel4);
		}
		stackPanel.Children.Add(clickyNotebookFlow);
		return stackPanel;
	}

	private static FrameworkElement PaperSketchScene()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(24.0),
			ColumnSpacing = 24.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(380.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		ClickySketchCard clickySketchCard = new ClickySketchCard(ClickySketchTone.Blue1, "Voice", 3L, isFocus: true)
		{
			Height = 230.0,
			VerticalAlignment = VerticalAlignment.Top
		};
		clickySketchCard.With(new ClickyHandText("Cedar", 30.0, 1.1, null).WithForeground(ClickySketchInk.BigWords), new ClickySketchTip("say something, the bars move"), new ClickySketchMeter(new global::_003C_003Ez__ReadOnlyArray<double>(new double[10] { 0.1, 0.3, 0.6, 0.9, 0.4, 0.2, 0.05, 0.7, 0.5, 0.1 }), 15L)
		{
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		}, new ClickySketchBar(0.62, 10.0, null, null, 8L)
		{
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		});
		grid.Children.Add(clickySketchCard);
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 14.0
		};
		stackPanel.Children.Add(Ui.H(10.0, new ClickySketchButton("Upgrade", delegate
		{
		}, ClickySketchButtonKind.Teal, 34.0, 16.0, 18.0, 4L), new ClickySketchButton("Change", delegate
		{
		}, ClickySketchButtonKind.Plain, 34.0, 16.0, 18.0, 4L), new ClickySketchButton("Not now", delegate
		{
		}, ClickySketchButtonKind.Quiet, 34.0, 16.0, 18.0, 4L), new ClickySketchButton("Refresh", delegate
		{
		}, ClickySketchButtonKind.Plain, 34.0, 16.0, 18.0, 4L, "arrow.clockwise")));
		stackPanel.Children.Add(Ui.H(18.0, new ClickySketchLink("delete account", delegate
		{
		}, null), new ClickySketchCheck(null), new ClickySketchCheck(null, 1.5)));
		stackPanel.Children.Add(Ui.H(12.0, new ClickySketchToggle(isOn: true, 6L), new ClickySketchToggle(isOn: false, 7L)));
		stackPanel.Children.Add(new ClickySketchToggleRow("Speak finished work out loud, even when typed", isOn: true)
		{
			Width = 300.0,
			HorizontalAlignment = HorizontalAlignment.Left
		});
		stackPanel.Children.Add(new ClickySketchSegmented<string>(new global::_003C_003Ez__ReadOnlyArray<string>(new string[3] { "Calm", "Bright", "Bold" }), "Bright", (string s) => s, delegate
		{
		}, 17.0, 9L));
		stackPanel.Children.Add(new ClickySketchSlider(new global::_003C_003Ez__ReadOnlyArray<string>(new string[4] { "0.75×", "1×", "1.25×", "1.5×" }), 1, delegate
		{
		}, 262.0, 11L));
		StackPanel stackPanel2 = Ui.H(6.0);
		foreach (var (hex, num) in new string[5] { "#3380FF", "#FF4C4C", "#D4A017", "#26C281", "#9B59F5" }.Select((string h, int i) => (h: h, i: i)))
		{
			stackPanel2.Children.Add(new ClickySketchSwatch(PaperColor.Hex(hex), num == 0, delegate
			{
			}, 24.0, 13 + num));
		}
		stackPanel.Children.Add(stackPanel2);
		stackPanel.Children.Add(Ui.H(8.0, new ClickySketchIconTile((Color c) => ClickyInkIcon.Glyph("gearshape", 13.0, c), ClickySketchTone.BluePale, 20L), new ClickySketchIconTile((Color c) => ClickyInkIcon.Glyph("person", 13.0, c), ClickySketchTone.Blue2, 21L), new ClickySketchIconTile((Color c) => ClickyInkIcon.Glyph("mic", 13.0, c), ClickySketchTone.Blue3, 22L)));
		Grid.SetColumn(stackPanel, 1);
		grid.Children.Add(stackPanel);
		return grid;
	}

	private static FrameworkElement PaperHandScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 26.0,
			Margin = new Thickness(24.0)
		};
		(double, double)[] array = new(double, double)[3]
		{
			(16.0, 1.25),
			(19.0, 1.2),
			(24.0, 1.1)
		};
		for (int i = 0; i < array.Length; i++)
		{
			(double, double) tuple = array[i];
			double item = tuple.Item1;
			double item2 = tuple.Item2;
			StackPanel stackPanel2 = new StackPanel
			{
				Width = 200.0,
				Spacing = 10.0
			};
			stackPanel2.Children.Add(Caption($"hand {item} × {item2}"));
			ClickyHandText child = new ClickyHandText("Clicky writes the way the boards set it: every line box is size × line-height tall, with the glyphs centred in it.", item, item2, null).WithForeground(ClickyPaperStock.Cream.Ink());
			stackPanel2.Children.Add(new Border
			{
				BorderBrush = Ui.Brush(PaperColor.Hex("#FFB0B0")),
				BorderThickness = new Thickness(0.5),
				Child = child,
				HorizontalAlignment = HorizontalAlignment.Left
			});
			stackPanel2.Children.Add(new ClickyHandText("Clicky writes the way the boards set it: every line box is size × line-height tall, with the glyphs centred in it.", item, item2, 2).WithForeground(ClickyPaperInk.ClickyBlue));
			stackPanel2.Children.Add(new ClickyHandText("centred " + item, item, item2, null, 1.0, TextAlignment.Center)
			{
				Width = 200.0
			}.WithForeground(ClickyPaperInk.Pencil));
			stackPanel2.Children.Add(new ClickyHandText("one line that is long enough to shrink a little", item, item2, 1, 0.7)
			{
				Width = 200.0
			}.WithForeground(ClickyPaperStock.Cream.Ink()));
			stackPanel.Children.Add(stackPanel2);
		}
		StackPanel stackPanel3 = new StackPanel
		{
			Spacing = 12.0
		};
		stackPanel3.Children.Add(Caption("CSS text (system 13 @ 18)"));
		stackPanel3.Children.Add(ClickyHandText.CssText("Exact facts are typed in the system font and wrap greedily.", ClickyPaperFonts.Typed(13.0, null), 18.0, TextAlignment.Left, null).WithForeground(PaperColor.Black));
		stackPanel3.Children.Add(Caption("writing pencil (still offscreen)"));
		stackPanel3.Children.Add(new ClickyNotebookWritingPencil(ClickyHomeSpreadPaper.Note));
		stackPanel3.Children.Add(new ClickyNotebookWritingPencil(ClickyHomeSpreadPaper.Of(ClickyPaperStock.Periwinkle)));
		stackPanel3.Width = 220.0;
		stackPanel.Children.Add(stackPanel3);
		return stackPanel;
	}

	private static FrameworkElement PaperCardsScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 8.0,
			Margin = new Thickness(16.0)
		};
		stackPanel.Children.Add(new ClickyCardView(new ClickyCardFace("Tokyo flights", ClickyPaperGlyph.Plane, ClickyPaperStock.Sky)
		{
			Caps = "SFO → NRT · Oct 14",
			Headline = new ClickyCardFace.CardHeadline.Big("$612"),
			Detail = new ClickyCardFace.CardDetail.Sparkline(new double[8] { 680.0, 664.0, 671.0, 652.0, 640.0, 655.0, 630.0, 612.0 }, CircledEnd: true),
			Foot = "down $48 since Monday",
			Sticker = ClickyPaperStickerKind.Live
		}));
		stackPanel.Children.Add(new ClickyCardView(new ClickyCardFace("Inbox", ClickyPaperGlyph.Tray, ClickyPaperStock.Mint)
		{
			Caps = "this morning",
			Headline = new ClickyCardFace.CardHeadline.Count("3", "need you"),
			Detail = new ClickyCardFace.CardDetail.Waiting(new string[3] { "Omar Ruiz", "Design review", "Invoice #2231" }),
			Foot = "all answered by noon",
			FootTone = ClickyCardFace.CardFootTone.Good,
			Sticker = ClickyPaperStickerKind.NeedsYou
		}));
		stackPanel.Children.Add(new ClickyCardView(new ClickyCardFace("Talk prep", ClickyPaperGlyph.Bubble, ClickyPaperStock.Peach)
		{
			Caps = "Thursday · 2 pm",
			Headline = new ClickyCardFace.CardHeadline.Sentence("Three talking points, drafted"),
			Detail = new ClickyCardFace.CardDetail.Bullets(new string[3] { "open with the demo", "pricing last", "leave 10 min" }),
			Foot = "edited 5 min ago",
			FootTone = ClickyCardFace.CardFootTone.Quiet,
			Sticker = ClickyPaperStickerKind.New
		}));
		stackPanel.Children.Add(new ClickyCardView(new ClickyCardFace("Espresso picks", ClickyPaperGlyph.Cart, ClickyPaperStock.Kraft)
		{
			Caps = "under $500",
			Detail = new ClickyCardFace.CardDetail.Rows(new ClickyCardFace.CardRow[4]
			{
				new ClickyCardFace.CardRow("Breville Bambino", "$299"),
				new ClickyCardFace.CardRow("Gaggia Classic", "$449"),
				new ClickyCardFace.CardRow("Rancilio Silvia", "$495"),
				new ClickyCardFace.CardRow("Lelit Anna", "$480")
			}),
			Foot = "best: Gaggia"
		}));
		stackPanel.Children.Add(new ClickyCardView(new ClickyCardFace("Reply to Omar", ClickyPaperGlyph.Mail, ClickyPaperStock.Butter)
		{
			Caps = "from Omar Ruiz",
			Headline = new ClickyCardFace.CardHeadline.Message("Omar", "Can we move Thursday to Friday? Something came up.", "Friday works, same time?")
		}));
		return stackPanel;
	}

	private static FrameworkElement PaperNotesScene()
	{
		Grid grid = new Grid
		{
			Margin = new Thickness(16.0),
			ColumnSpacing = 12.0,
			RowSpacing = 12.0
		};
		for (int i = 0; i < 3; i++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
		}
		for (int j = 0; j < 2; j++)
		{
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
		}
		Add(new ClickyNoteView(new ClickyNoteFace("Book the dentist", ClickyPaperGlyph.Calendar, new ClickyNoteFace.NoteBody.Lines(new ClickyNoteFace.NoteLine[3]
		{
			new ClickyNoteFace.NoteLine("found 3 openings")
			{
				Mark = new ClickyNoteFace.LineMark.Done()
			},
			new ClickyNoteFace.NoteLine("asking which works")
			{
				Mark = new ClickyNoteFace.LineMark.Now()
			},
			new ClickyNoteFace.NoteLine("book it")
			{
				Mark = new ClickyNoteFace.LineMark.Todo()
			}
		}))
		{
			Meta = "Dr. Patel · Mission St",
			Footer = new ClickyNoteFace.NoteFooter.Progress(2, 3, 0.66),
			Stamp = ClickyThingStatus.Working
		}), 0, 0);
		ClickyNoteFace clickyNoteFace = new ClickyNoteFace("Q3 revenue", ClickyPaperGlyph.Chart, new ClickyNoteFace.NoteBody.Chart(new double[7] { 12.0, 14.0, 13.0, 17.0, 19.0, 18.0, 22.0 }, ShowsTrend: true));
		clickyNoteFace.Footer = new ClickyNoteFace.NoteFooter.Files(new string[5] { "Q3 deck.key", "numbers.xlsx", "summary.pdf", "chart.png", "notes.md" });
		clickyNoteFace.Stamp = ClickyThingStatus.Ready;
		Add(new ClickyNoteView(clickyNoteFace), 0, 1);
		clickyNoteFace = new ClickyNoteFace("Weekend in Big Sur", ClickyPaperGlyph.Pin, new ClickyNoteFace.NoteBody.Lines(new ClickyNoteFace.NoteLine[3]
		{
			new ClickyNoteFace.NoteLine("")
			{
				Runs = new ClickyNoteFace.LineRun[3]
				{
					new ClickyNoteFace.LineRun("Leave ", IsClicky: false),
					new ClickyNoteFace.LineRun("Saturday 8 am", IsClicky: true),
					new ClickyNoteFace.LineRun(" from home", IsClicky: false)
				},
				HasCaret = true
			},
			new ClickyNoteFace.NoteLine("Ventana Inn")
			{
				Value = "$495",
				IsCircled = true
			},
			new ClickyNoteFace.NoteLine("Post Ranch")
			{
				Value = "$1,250"
			}
		}));
		clickyNoteFace.Meta = "from your calendar";
		clickyNoteFace.MetaLogo = ClickyPaperGlyph.Calendar;
		clickyNoteFace.MetaIsClicky = true;
		clickyNoteFace.IsIdea = true;
		clickyNoteFace.Footer = new ClickyNoteFace.NoteFooter.Actions(new ClickyNoteFace.NoteAction[2]
		{
			new ClickyNoteFace.NoteAction("yes", "Yes, plan it"),
			new ClickyNoteFace.NoteAction("no", "Not now", ClickyPaperGel.White)
		});
		Add(new ClickyNoteView(clickyNoteFace), 0, 2);
		clickyNoteFace = new ClickyNoteFace("Reply to Omar", ClickyPaperGlyph.Bubble, new ClickyNoteFace.NoteBody.Lines(new ClickyNoteFace.NoteLine[2]
		{
			new ClickyNoteFace.NoteLine("Friday works, same time?")
			{
				Tone = ClickyNoteFace.LineTone.Clicky
			},
			new ClickyNoteFace.NoteLine("waiting for your OK")
			{
				Tone = ClickyNoteFace.LineTone.Quiet
			}
		}));
		clickyNoteFace.Meta = "to Omar Ruiz · about Thursday";
		clickyNoteFace.Footer = new ClickyNoteFace.NoteFooter.Actions(new ClickyNoteFace.NoteAction[2]
		{
			new ClickyNoteFace.NoteAction("send", "Allow once"),
			new ClickyNoteFace.NoteAction("not", "Not now", ClickyPaperGel.White)
		});
		clickyNoteFace.Stamp = ClickyThingStatus.NeedsYou;
		Add(new ClickyNoteView(clickyNoteFace), 1, 0);
		Add(new ClickyNoteView(new ClickyNoteFace("Groceries", ClickyPaperGlyph.Cart, new ClickyNoteFace.NoteBody.Lines(new ClickyNoteFace.NoteLine[3]
		{
			new ClickyNoteFace.NoteLine("oat milk")
			{
				Mark = new ClickyNoteFace.LineMark.Number(1),
				Value = "$4.99"
			},
			new ClickyNoteFace.NoteLine("sourdough")
			{
				Mark = new ClickyNoteFace.LineMark.Number(2),
				Value = "$7.50"
			},
			new ClickyNoteFace.NoteLine("coffee beans")
			{
				Mark = new ClickyNoteFace.LineMark.Number(3),
				Value = "$16"
			}
		}))
		{
			Footer = new ClickyNoteFace.NoteFooter.Text("delivery by 6 pm"),
			Stamp = ClickyThingStatus.Done
		}), 1, 1);
		return grid;
		void Add(FrameworkElement e, int row, int column)
		{
			Grid.SetRow(e, row);
			Grid.SetColumn(e, column);
			grid.Children.Add(e);
		}
	}

	private static FrameworkElement PaperEdgeScene()
	{
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 30.0,
			Margin = new Thickness(30.0)
		};
		ClickyEdgeStackInkReading.State[] values = Enum.GetValues<ClickyEdgeStackInkReading.State>();
		for (int i = 0; i < values.Length; i++)
		{
			ClickyEdgeStackInkReading.State state = values[i];
			Grid grid = new Grid
			{
				Width = 90.0,
				Height = 260.0
			};
			grid.Children.Add(new ClickyEdgeStackInkPaper(ClickyHomeSpreadPaper.Of(ClickyPaperStock.Sky), isJob: false));
			grid.Children.Add(new ClickyEdgeStackInkTape(state));
			stackPanel.Children.Add(Labelled(state.ToString() + (state.WantsAttention() ? " · attention" : ""), grid));
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 14.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel2.Children.Add(ClickyEdgeStackHighlight.Wrap(PaperText.Line("what changed", ClickyPaperFonts.Hand(21.0), ClickyPaperStock.Cream.Ink()), isOn: true, 2));
		stackPanel2.Children.Add(ClickyEdgeStackHighlight.Wrap(PaperText.Line("unchanged", ClickyPaperFonts.Hand(21.0), ClickyPaperStock.Cream.Ink()), isOn: false, 3));
		stackPanel.Children.Add(Labelled("highlighter", stackPanel2));
		return stackPanel;
	}

	private static void AddInkObjectScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-obj-pick", 1060.0, 900.0, delegate
		{
			ClickyInkLiveNote note2 = ClickyInkSamples.LiveNote("# Headphones\n\n## In short\n> Sony for flights; Bose if comfort comes first {big=\"$449.99\"}\n::figure[Battery]{value=\"30 h\" delta=\"+6 h\"}\n::figure[Noise cut]{value=\"-38 dB\" delta=\"down 2 dB\"}\n::figure[Weight]{value=\"250 g\" delta=\"lightest\"}\n| Headphones | Price | ANC battery | Best for |\n|---|---|---|---|\n| **Sony WH-1000XM6** {pick} | $449.99 | 30 h | Flights and the office, the best noise cancelling we tried |\n| Bose QuietComfort Ultra | $299.95 | 24 h | Comfort on long days |\n| Sennheiser Momentum 4 {out} | $379.95 | 60 h | Battery above everything |\n::sticky[Buy from the airline store: 10% off]{color=pink}\n::sticky[Return window is 30 days]\n::sticky[Case is sold apart]{color=blue}\n:::chart[Reviewer scores]{unit=/10}\nSony WH-1000XM6: 9.1 {pick}\nBose QuietComfort Ultra: 8.4\nSennheiser Momentum 4: 7.9\n:::");
			ClickyInkImages.LoadAsync("https://upload.wikimedia.org/wikipedia/commons/thumb/4/47/PNG_transparency_demonstration_1.png/330px-PNG_transparency_demonstration_1.png");
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel.Children.Add(InkGroupScene(note2, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Figure), null, 0, null));
			stackPanel.Children.Add(InkGroupScene(note2, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Table), null, 0, null));
			stackPanel.Children.Add(InkGroupScene(note2, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Chart), null, 0, null));
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel2.Children.Add(InkGroupScene(note2, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Sticky), null, 0, null));
			stackPanel2.Children.Add(InkGroupScene(note2, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Table), null, 0, null, 300.0));
			return ObjectsRow(stackPanel, stackPanel2);
		});
		AddInkScene(list, "ink-obj-trip", 1060.0, 980.0, delegate
		{
			ClickyInkLiveNote note3 = ClickyInkSamples.LiveNote(TravelNote());
			StackPanel stackPanel3 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel3.Children.Add(InkGroupScene(note3, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Ticket), null, 0, null));
			stackPanel3.Children.Add(InkGroupScene(note3, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Receipt), null, 0, null));
			stackPanel3.Children.Add(InkGroupScene(note3, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Place), null, 0, null));
			StackPanel stackPanel4 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel4.Children.Add(InkGroupScene(note3, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Day), null, 0, null));
			stackPanel4.Children.Add(InkGroupScene(note3, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Day) && g.Blocks[0].Words.Current.Contains("timeline"), null, 0, null));
			stackPanel4.Children.Add(InkGroupScene(note3, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Photo), null, 0, null));
			return ObjectsRow(stackPanel3, stackPanel4);
		}, 4.0);
		AddInkScene(list, "ink-obj-links", 1060.0, 1080.0, delegate
		{
			ClickyInkLiveNote note4 = ClickyInkSamples.LiveNote(LinksNote());
			StackPanel stackPanel5 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel5.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => g.IsMadeLink, null, 0, null));
			stackPanel5.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.File) && !g.IsSupportingFiles, null, 0, null));
			stackPanel5.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Letter), null, 0, null));
			stackPanel5.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Clip), null, 0, null));
			StackPanel stackPanel6 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel6.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Question), null, 0, null));
			stackPanel6.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Question) && g.Blocks[0].Source["options"] != null, null, 0, null));
			stackPanel6.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Ask), null, 0, null));
			stackPanel6.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => g.IsSupportingFiles, null, 0, null));
			stackPanel6.Children.Add(InkGroupScene(note4, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Link) && !g.IsMadeLink, null, 0, null));
			return ObjectsRow(stackPanel5, stackPanel6);
		}, 3.0);
		AddInkScene(list, "ink-obj-landing", 1060.0, 900.0, delegate
		{
			ClickyInkLiveNote note5 = ClickyInkSamples.LiveNote("# Headphones\n\n## In short\n> Sony for flights; Bose if comfort comes first {big=\"$449.99\"}\n::figure[Battery]{value=\"30 h\" delta=\"+6 h\"}\n::figure[Noise cut]{value=\"-38 dB\" delta=\"down 2 dB\"}\n::figure[Weight]{value=\"250 g\" delta=\"lightest\"}\n| Headphones | Price | ANC battery | Best for |\n|---|---|---|---|\n| **Sony WH-1000XM6** {pick} | $449.99 | 30 h | Flights and the office, the best noise cancelling we tried |\n| Bose QuietComfort Ultra | $299.95 | 24 h | Comfort on long days |\n| Sennheiser Momentum 4 {out} | $379.95 | 60 h | Battery above everything |\n::sticky[Buy from the airline store: 10% off]{color=pink}\n::sticky[Return window is 30 days]\n::sticky[Case is sold apart]{color=blue}\n:::chart[Reviewer scores]{unit=/10}\nSony WH-1000XM6: 9.1 {pick}\nBose QuietComfort Ultra: 8.4\nSennheiser Momentum 4: 7.9\n:::");
			ClickyInkLiveNote note6 = ClickyInkSamples.LiveNote(TravelNote());
			StackPanel stackPanel7 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel7.Children.Add(ObjectsCaptioned(InkGroupScene(note5, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Sticky), At(note5, ClickyInkDocument.Kind.Sticky, 0.3), 0, null), "stickies landing (0.3 s)"));
			stackPanel7.Children.Add(ObjectsCaptioned(InkGroupScene(note5, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Table), At(note5, ClickyInkDocument.Kind.Table, 1.2), 0, null), "table rows growing in, the pick's highlighter sweeping"));
			stackPanel7.Children.Add(ObjectsCaptioned(InkGroupScene(note5, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Chart), At(note5, ClickyInkDocument.Kind.Chart, 1.4), 0, null), "chart bars growing"));
			StackPanel stackPanel8 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel8.Children.Add(ObjectsCaptioned(InkGroupScene(note6, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Ticket), At(note6, ClickyInkDocument.Kind.Ticket, 0.5), 0, null), "ticket landing, the route being drawn"));
			stackPanel8.Children.Add(ObjectsCaptioned(InkGroupScene(note6, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Place), At(note6, ClickyInkDocument.Kind.Place, 0.5), 0, null), "the pin dropping"));
			stackPanel8.Children.Add(ObjectsCaptioned(InkGroupScene(note6, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Photo), At(note6, ClickyInkDocument.Kind.Photo, 0.8), 0, null), "polaroids developing"));
			return ObjectsRow(stackPanel7, stackPanel8);
		}, 4.0);
		AddInkScene(list, "ink-obj-struck", 1060.0, 760.0, delegate
		{
			string item = "# Headphones\n\n## In short\n> Sony for flights; Bose if comfort comes first {big=\"$449.99\"}\n::figure[Battery]{value=\"30 h\" delta=\"+6 h\"}\n::figure[Noise cut]{value=\"-38 dB\" delta=\"down 2 dB\"}\n::figure[Weight]{value=\"250 g\" delta=\"lightest\"}\n| Headphones | Price | ANC battery | Best for |\n|---|---|---|---|\n| **Sony WH-1000XM6** {pick} | $449.99 | 30 h | Flights and the office, the best noise cancelling we tried |\n| Bose QuietComfort Ultra | $299.95 | 24 h | Comfort on long days |\n| Sennheiser Momentum 4 {out} | $379.95 | 60 h | Battery above everything |\n::sticky[Buy from the airline store: 10% off]{color=pink}\n::sticky[Return window is 30 days]\n::sticky[Case is sold apart]{color=blue}\n:::chart[Reviewer scores]{unit=/10}\nSony WH-1000XM6: 9.1 {pick}\nBose QuietComfort Ultra: 8.4\nSennheiser Momentum 4: 7.9\n:::";
			string item2 = "# Headphones\n\n## In short\n> Sony for flights; Bose if comfort comes first {big=\"$449.99\"}\n::figure[Battery]{value=\"30 h\" delta=\"+6 h\"}\n::figure[Noise cut]{value=\"-38 dB\" delta=\"down 2 dB\"}\n::figure[Weight]{value=\"250 g\" delta=\"lightest\"}\n| Headphones | Price | ANC battery | Best for |\n|---|---|---|---|\n| **Sony WH-1000XM6** {pick} | $449.99 | 30 h | Flights and the office, the best noise cancelling we tried |\n| Bose QuietComfort Ultra | $299.95 | 24 h | Comfort on long days |\n| Sennheiser Momentum 4 {out} | $379.95 | 60 h | Battery above everything |\n::sticky[Buy from the airline store: 10% off]{color=pink}\n::sticky[Return window is 30 days]\n::sticky[Case is sold apart]{color=blue}\n:::chart[Reviewer scores]{unit=/10}\nSony WH-1000XM6: 9.1 {pick}\nBose QuietComfort Ultra: 8.4\nSennheiser Momentum 4: 7.9\n:::".Replace("::sticky[Case is sold apart]{color=blue}\n", "").Replace("| Sennheiser Momentum 4 {out} | $379.95 | 60 h | Battery above everything |\n", "").Replace(":::chart[Reviewer scores]{unit=/10}\nSony WH-1000XM6: 9.1 {pick}\nBose QuietComfort Ultra: 8.4\nSennheiser Momentum 4: 7.9\n:::", "");
			ClickyInkLiveNote clickyInkLiveNote = ClickyInkSamples.LiveNote((item, 0.3, false), (item2, 40.0, true));
			double value = (clickyInkLiveNote.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Chart).GoneAt ?? 41.0) + 1.2;
			StackPanel stackPanel9 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel9.Children.Add(ObjectsCaptioned(InkGroupScene(clickyInkLiveNote, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Chart), value, 0, null), "the chart struck by the edit"));
			stackPanel9.Children.Add(ObjectsCaptioned(InkGroupScene(clickyInkLiveNote, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Table), value, 0, null), "a row struck"));
			StackPanel stackPanel10 = new StackPanel
			{
				Spacing = 16.0
			};
			stackPanel10.Children.Add(ObjectsCaptioned(InkGroupScene(clickyInkLiveNote, (ClickyInkGroup g) => Kind(g, ClickyInkDocument.Kind.Sticky), value, 0, null), "stickies after the edit"));
			return ObjectsRow(stackPanel9, stackPanel10);
		}, 2.4);
		static double At(ClickyInkLiveNote note, ClickyInkDocument.Kind kind, double after)
		{
			return note.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == kind).LandsAt + after;
		}
		static bool Kind(ClickyInkGroup g, ClickyInkDocument.Kind kind)
		{
			return g.Kind == kind;
		}
	}

	private static void AddInkInteractiveScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-int-question", 1000.0, 330.0, () => InteractiveRow(InkObjectScene(() => ClickyInkQuestionAnswers.Make(Block("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::", ClickyInkDocument.Kind.Question), new ClickyInkActions()), "a question's extra answers"), InkObjectScene(delegate
		{
			FrameworkElement frameworkElement = ClickyInkQuestionAnswers.Make(Block("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::", ClickyInkDocument.Kind.Question), new ClickyInkActions());
			ClickyInkInlineAnswerField? clickyInkInlineAnswerField = FindField(frameworkElement);
			if ((object)clickyInkInlineAnswerField != null)
			{
				clickyInkInlineAnswerField.SetDraft("A bundle: audit then site");
				return frameworkElement;
			}
			return frameworkElement;
		}, "typing an answer")));
		AddInkScene(list, "ink-int-form", 1480.0, 600.0, delegate
		{
			InkObjectState.ResetAll();
			ClickyInkLiveNote note = Note();
			ClickyInkLiveNote note2 = Note();
			ClickyInkLiveNote note3 = Note();
			ClickyInkNoteAction answered = ClickyInkNoteActions.Form(FindBlock(note3, ClickyInkDocument.Kind.Form).Source, new global::_003C_003Ez__ReadOnlySingleElementList<(string, string)>(("Your studio name", "Bright Pixel")), usedDefaults: false);
			FrameworkElement[] obj = new FrameworkElement[3]
			{
				InteractiveCaptioned(InkGroupScene(note, IsForm, null, 0, null), "empty: Send rests until something's filled in"),
				InteractiveCaptioned(FillForm(note2, FindBlock(note2, ClickyInkDocument.Kind.Form)), "filled in, Friendly circled"),
				null
			};
			Func<ClickyInkGroup, bool> pick = IsForm;
			ClickyInkActions actions = WithFeedback(null, answered);
			obj[2] = InteractiveCaptioned(InkGroupScene(note3, pick, null, 0, null, 378.0, actions), "sent: done, with what was said");
			return InteractiveRow(obj);
		});
		AddInkScene(list, "ink-int-connect", 1400.0, 900.0, delegate
		{
			InkObjectState.ResetAll();
			ClickyInkConnections shared = ClickyInkConnections.Shared;
			shared.SetApps(new global::_003C_003Ez__ReadOnlyArray<ClickyInkConnections.App>(new ClickyInkConnections.App[7]
			{
				new ClickyInkConnections.App("gmail", "Gmail", null, IsConnected: false, NeedsReconnect: false),
				new ClickyInkConnections.App("slack", "Slack", null, IsConnected: false, NeedsReconnect: false),
				new ClickyInkConnections.App("notion", "Notion", null, IsConnected: false, NeedsReconnect: false),
				new ClickyInkConnections.App("github", "GitHub", null, IsConnected: false, NeedsReconnect: false),
				new ClickyInkConnections.App("googlecalendar", "Google Calendar", null, IsConnected: false, NeedsReconnect: true),
				new ClickyInkConnections.App("linear", "Linear", null, IsConnected: true, NeedsReconnect: false),
				new ClickyInkConnections.App("figma", "Figma", null, IsConnected: false, NeedsReconnect: false)
			}));
			shared.SetPhase(new ClickyInkConnections.Phase.Opening(), "slack");
			shared.SetPhase(new ClickyInkConnections.Phase.WaitingForSignIn(), "notion");
			shared.SetPhase(new ClickyInkConnections.Phase.Checking(), "github");
			shared.SetPhase(new ClickyInkConnections.Phase.Connected(), "figma");
			shared.SetPhase(new ClickyInkConnections.Phase.Failed("Couldn't open the sign-in page. Try again."), "dropbox");
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 16.0,
				Children = 
				{
					(UIElement)Card("Gmail", "not connected"),
					(UIElement)Card("Slack", "opening the sign-in page"),
					(UIElement)Card("Notion", "waiting for the sign-in"),
					(UIElement)Card("GitHub", "checking the connection")
				}
			};
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 16.0,
				Children = 
				{
					(UIElement)Card("Figma", "connected: the stamp, the job told"),
					(UIElement)Card("Linear", "connected elsewhere: Carry on"),
					(UIElement)Card("Google Calendar", "its sign-in expired: Reconnect"),
					(UIElement)Card("Dropbox", "failed (not in the catalog)", " app=dropbox")
				}
			};
			return InteractiveRow(stackPanel, stackPanel2);
		});
		AddInkScene(list, "ink-int-items", 1480.0, 470.0, delegate
		{
			ClickyInkLiveNote note4 = ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::");
			ClickyInkLiveNote.Block block = FindBlock(note4, ClickyInkDocument.Kind.Items);
			ClickyInkNoteAction answered2 = ClickyInkNoteActions.ItemAction(block.Source, block.Items[0].Source.Text, "Draft pitch");
			ClickyInkNoteAction pending = ClickyInkNoteActions.ItemAction(block.Source, block.Items[2].Source.Text, "Skip");
			ClickyInkLiveNote.Words words = block.Items[2].Words;
			double value = words.StartsAt.GetValueOrDefault() + (words.FinishedAt - words.StartsAt.GetValueOrDefault()) * 0.5;
			FrameworkElement[] obj2 = new FrameworkElement[3]
			{
				InteractiveCaptioned(InkGroupScene(note4, IsItems, null, 0, null), "every row with its buttons"),
				null,
				null
			};
			Func<ClickyInkGroup, bool> pick2 = IsItems;
			ClickyInkActions actions2 = WithFeedback(pending, answered2);
			obj2[1] = InteractiveCaptioned(InkGroupScene(note4, pick2, null, 0, null, 378.0, actions2), "one asked, one on its way");
			obj2[2] = InteractiveCaptioned(InkGroupScene(note4, IsItems, value, 0, null), $"being written (clock {value:0.00})");
			return InteractiveRow(obj2);
		});
		AddInkScene(list, "ink-int-quiz", 1480.0, 520.0, delegate
		{
			ClickyInkLiveNote note5 = ClickyInkSamples.LiveNote("# Dentist outreach\n## In short\n> 6 dentists without a website {big=\"6\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal\nPortfolio: file\n:::\n::connect[Gmail]{why=\"to save the drafts\"}\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Old Town Dentistry {out}\n  closed for renovation\n:::\n:::quiz[Drill]\nQ: They have a website guy.\nA: Offer a free audit.\nWhich call first? | The warmest lead\n:::\n:::todo[Plan]\n- [x] Day 1: send 5 pitches\nDay 2: follow up\n:::\n:::gallery[Logos]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png\n:::\n::event[Call]{when=\"2026-10-20 15:00\" end=\"15:30\"}\n:::criteria[What I look for]\nArea: Lisbon, Porto\n:::");
			ClickyInkLiveNote.Block quiz = FindBlock(note5, ClickyInkDocument.Kind.Quiz);
			return InteractiveRow(InteractiveCaptioned(Quiz(delegate
			{
			}), "fresh: tap to turn it over"), InteractiveCaptioned(Quiz(delegate
			{
				ClickyInkQuiz.SetForGallery(quiz, new global::_003C_003Ez__ReadOnlySingleElementList<string>(quiz.Items[0].Id), new Dictionary<string, bool>());
			}), "the first turned over: Got it / Missed it"), InteractiveCaptioned(Quiz(delegate
			{
				ClickyInkQuiz.SetForGallery(quiz, new global::_003C_003Ez__ReadOnlyArray<string>(new string[2]
				{
					quiz.Items[0].Id,
					quiz.Items[1].Id
				}), new Dictionary<string, bool>
				{
					[quiz.Items[0].Id] = true,
					[quiz.Items[1].Id] = false
				});
			}), "graded: the score and Send my score"));
			FrameworkElement Quiz(Action setUp)
			{
				InkObjectState.ResetAll();
				setUp();
				return InkGroupScene(note5, IsQuiz, null, 0, null);
			}
		});
		AddInkScene(list, "ink-int-todo", 960.0, 420.0, delegate
		{
			ClickyInkLiveNote note6 = ClickyInkSamples.LiveNote("# Guitar\n## Plan\n:::todo[14-day guitar plan]\n- [x] Day 1: tune and learn E minor\n- [x] Day 2: G and C\n- [ ] Day 3: switch G → C in time\n  slow, 60 bpm\n- [ ] Day 4: first song\n:::\n");
			ClickyInkLiveNote note7 = ClickyInkSamples.LiveNote("# Dentist outreach\n## In short\n> 6 dentists without a website {big=\"6\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal\nPortfolio: file\n:::\n::connect[Gmail]{why=\"to save the drafts\"}\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Old Town Dentistry {out}\n  closed for renovation\n:::\n:::quiz[Drill]\nQ: They have a website guy.\nA: Offer a free audit.\nWhich call first? | The warmest lead\n:::\n:::todo[Plan]\n- [x] Day 1: send 5 pitches\nDay 2: follow up\n:::\n:::gallery[Logos]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png\n:::\n::event[Call]{when=\"2026-10-20 15:00\" end=\"15:30\"}\n:::criteria[What I look for]\nArea: Lisbon, Porto\n:::");
			return InteractiveRow(InteractiveCaptioned(InkGroupScene(note7, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Todo, null, 0, null), "the Mac tests' plan"), InteractiveCaptioned(InkGroupScene(note6, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Todo, null, 0, null), "two of four done, a detail line"));
		});
		AddInkScene(list, "ink-int-gallery", 960.0, 560.0, delegate
		{
			InkObjectState.ResetAll();
			ClickyInkLiveNote note8 = ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::");
			string folder = Path.Combine(SampleDirectory, "ink");
			Directory.CreateDirectory(Path.Combine(folder, "output"));
			WriteSamplePng(Path.Combine(folder, "output", "logo-a.png"), (R: 45, G: 91, B: 227), (R: 255, G: 199, B: 225));
			WriteSamplePng(Path.Combine(folder, "output", "logo-b.png"), (R: 31, G: 138, B: 76), (R: 254, G: 238, B: 199));
			WriteSamplePng(Path.Combine(folder, "output", "logo-c.png"), (R: 194, G: 65, B: 12), (R: 215, G: 229, B: 255));
			ClickyInkNoteAction answered3 = ClickyInkNoteActions.Pick(FindBlock(note8, ClickyInkDocument.Kind.Gallery).Source, "thin serif");
			ClickyInkLiveNote note9 = ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::");
			FrameworkElement[] array = new FrameworkElement[2];
			Func<ClickyInkGroup, bool> pick3 = IsGallery;
			ClickyInkActions actions3 = Actions();
			array[0] = InteractiveCaptioned(InkGroupScene(note8, pick3, null, 0, null, 378.0, actions3), "the agent's pick ringed; a sound, a page, one ruled out");
			Func<ClickyInkGroup, bool> pick4 = IsGallery;
			actions3 = Actions(WithFeedback(null, answered3).Feedback);
			array[1] = InteractiveCaptioned(InkGroupScene(note9, pick4, null, 0, null, 378.0, actions3), "after Pick this: done, what was said");
			return InteractiveRow(array);
			ClickyInkActions Actions(ClickyInkActionFeedback? feedback = null)
			{
				return new ClickyInkActions
				{
					ResolvePath = (string raw) => (!Path.IsPathRooted(raw)) ? Path.Combine(folder, raw.Replace('/', '\\')) : raw,
					Feedback = (feedback ?? new ClickyInkActionFeedback())
				};
			}
		});
		AddInkScene(list, "ink-int-event", 960.0, 300.0, () => InteractiveRow(InteractiveCaptioned(InkGroupScene(ClickyInkSamples.LiveNote("# Dentist outreach\n## In short\n> 6 dentists without a website {big=\"6\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal\nPortfolio: file\n:::\n::connect[Gmail]{why=\"to save the drafts\"}\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Old Town Dentistry {out}\n  closed for renovation\n:::\n:::quiz[Drill]\nQ: They have a website guy.\nA: Offer a free audit.\nWhich call first? | The warmest lead\n:::\n:::todo[Plan]\n- [x] Day 1: send 5 pitches\nDay 2: follow up\n:::\n:::gallery[Logos]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png\n:::\n::event[Call]{when=\"2026-10-20 15:00\" end=\"15:30\"}\n:::criteria[What I look for]\nArea: Lisbon, Porto\n:::"), (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Event, null, 0, null), "a call, its times"), InteractiveCaptioned(InkGroupScene(ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::"), (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Event, null, 0, null), "all day, two days, a place")));
		AddInkScene(list, "ink-int-criteria", 960.0, 340.0, delegate
		{
			InkObjectState.ResetAll();
			FrameworkElement view = InkGroupScene(ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::"), IsCriteria, null, 0, null);
			ClickyInkLiveNote note10 = ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::".Replace("What I look for", "What I look for "));
			ClickyInkCriteria.SetEditingForGallery(FindBlock(note10, ClickyInkDocument.Kind.Criteria));
			return InteractiveRow(InteractiveCaptioned(view, "read: values stamped"), InteractiveCaptioned(InkGroupScene(note10, IsCriteria, null, 0, null), "Edit: a blank for each"));
		});
		AddInkScene(list, "ink-int-review", 1400.0, 1500.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote = ClickyInkSamples.LiveNote("# Cleanup\n## Your move\n:::review[Delete these 8 old installers?]{go=\"Delete the ticked installers\" go_label=\"Delete\" skip=\"Keep them all\" skip_label=\"Keep all\"}\n- [x] /tmp/clicky-review-test/Zoom-6.2.4.dmg — 212 MB, from 2023\n- [x] ~/Downloads/old installers/Figma-116.5.dmg — 180 MB\n- [ ] ~/Downloads/IMG_2041 (1).HEIC — a duplicate photo\n- [x] Slack-4.33.dmg — superseded\n- [ ] Notion-2.1.exe — 90 MB {new}\n- [x] Docker.dmg — 640 MB {done}\n- [ ] Spotify.dmg {out}\n  in use: it couldn't be moved\n- [ ] Arc-1.2.dmg — 210 MB\n:::\n:::review[Unfollow these?]{go=\"Unfollow these on Instagram\" go_label=\"Unfollow\" skip=\"Keep them all for now\"}\n- [ ] @old_brand_account — hasn't posted since 2023\n- [x] @spam_giveaways — a giveaway bot\n- [x] @deals_daily — 40 posts a day\n:::");
			List<ClickyInkLiveNote.Block> reviews = clickyInkLiveNote.Pages[0].Blocks.Where((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Review).ToList();
			ClickyInkDocument.Block source = reviews[1].Source;
			ClickyInkNoteAction pending2 = ClickyInkNoteActions.Review(source, ClickyInkReviewRules.ItemsToSend(source, Array.Empty<string>()), source.Items.Count(ClickyInkReviewRules.IsOpen));
			ClickyInkNoteAction answered4 = ClickyInkNoteActions.SkipReview(source);
			ClickyInkLiveNote note11 = ClickyInkSamples.LiveNote("# Cleanup\n## Your move\n:::review[Unfollow these?]{go=\"Unfollow\"}\n- [x] @spam_giveaways {done}\n- [x] @deals_daily {done}\n:::\n");
			StackPanel obj3 = new StackPanel
			{
				Spacing = 16.0,
				Children = { (UIElement)InteractiveCaptioned(InkGroupScene(clickyInkLiveNote, IsShort, null, 0, null), "waiting on you: the go-ahead and Not now") }
			};
			UIElementCollection children = obj3.Children;
			Func<ClickyInkGroup, bool> pick5 = IsShort;
			ClickyInkActions actions4 = WithFeedback(pending2);
			children.Add(InteractiveCaptioned(InkGroupScene(clickyInkLiveNote, pick5, null, 0, null, 378.0, actions4), "on its way: boxes rest, the slip in the margin"));
			UIElementCollection children2 = obj3.Children;
			Func<ClickyInkGroup, bool> pick6 = IsShort;
			actions4 = WithFeedback(null, answered4);
			children2.Add(InteractiveCaptioned(InkGroupScene(clickyInkLiveNote, pick6, null, 0, null, 378.0, actions4), "Keep them all: skipped"));
			obj3.Children.Add(InteractiveCaptioned(InkGroupScene(note11, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Review, null, 0, null), "all done"));
			StackPanel stackPanel3 = obj3;
			return InteractiveRow(InteractiveCaptioned(InkGroupScene(clickyInkLiveNote, IsLong, null, 0, null), "a long review: one ruled line each, paths cut in the middle"), stackPanel3);
			bool IsLong(ClickyInkGroup g)
			{
				if (g.Kind == ClickyInkDocument.Kind.Review)
				{
					return g.Blocks[0].Id == reviews[0].Id;
				}
				return false;
			}
			bool IsShort(ClickyInkGroup g)
			{
				if (g.Kind == ClickyInkDocument.Kind.Review)
				{
					return g.Blocks[0].Id == reviews[1].Id;
				}
				return false;
			}
		});
		static FrameworkElement Card(string app, string caption, string extra = "")
		{
			return InteractiveCaptioned(InkGroupScene(ClickyInkSamples.LiveNote($"# Connect\n## In short\n::connect[{app}]{{why=\"to save the drafts\"{extra}}}\n"), (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Connect, null, 0, null), caption);
		}
		static bool IsCriteria(ClickyInkGroup g)
		{
			return g.Kind == ClickyInkDocument.Kind.Criteria;
		}
		static bool IsForm(ClickyInkGroup g)
		{
			return g.Kind == ClickyInkDocument.Kind.Form;
		}
		static bool IsGallery(ClickyInkGroup g)
		{
			return g.Kind == ClickyInkDocument.Kind.Gallery;
		}
		static bool IsItems(ClickyInkGroup g)
		{
			return g.Kind == ClickyInkDocument.Kind.Items;
		}
		static bool IsQuiz(ClickyInkGroup g)
		{
			return g.Kind == ClickyInkDocument.Kind.Quiz;
		}
		static ClickyInkLiveNote Note()
		{
			return ClickyInkSamples.LiveNote("# Studio launch\n## In short\n> Three things need you before the pitches go out {big=\"3\"}\n::question[Which offer should I lead with?]{options=\"Free audit | $299 site | Monthly SEO\" other=\"Something else…\" file=\"Attach a brief…\"}\n:::form[Before I write the pitches]{submit=\"Here are my details\"}\nYour studio name\nPrice: e.g. $299\nTone: Friendly | Formal | Playful\nPortfolio: folder\n:::\n:::items[Dentists to pitch]{actions=\"Draft pitch | Skip\"}\n- [Smile Studio](https://example.com) — 4.9★, no website\n- Bright Dental — 4.7★, site from 2014 {pick}\n- Harbor Family Dentistry — 4.8★, slow site\n- Old Town Dentistry {out}\n  closed for renovation\n- Riverside Smiles {done}\n:::\n:::gallery[Three logo directions]{pick=\"Use this logo\"}\noutput/logo-a.png — bold and round\noutput/logo-b.png — thin serif {pick}\noutput/jingle.mp3 — the jingle\nhttps://example.com/brand — the brand page\noutput/logo-c.png — retro {out}\n:::\n::event[Studio open day]{when=\"2026-11-14\" end=\"2026-11-15\" where=\"12 Main St, Lisbon\"}\n:::criteria[What I look for]\nRoles: designer, editor, illustrator\nArea: Lisbon | Porto\nPay: from €40/hour\n:::");
		}
	}

	private static void AddInkDashboardScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-dash-rest", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote clickyInkLiveNote = ClickyInkSamples.DashboardRuns();
			return DashColumns(clickyInkLiveNote, clickyInkLiveNote.FinishedClock + 30.0, "after run 6, at rest");
		}));
		AddInkScene(list, "ink-dash-before", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote note = ClickyInkSamples.DashboardRuns();
			return DashColumns(note, DashChangeClock(note) - 2.0, "run 5, before run 6 lands");
		}));
		AddInkScene(list, "ink-dash-change", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote note2 = ClickyInkSamples.DashboardRuns();
			double num = DashChangeClock(note2) + 0.55;
			return DashColumns(note2, num, $"run 6 landing (clock {num:0.00}): numbers ticking, new point drawing in, light recolouring");
		}));
		AddInkScene(list, "ink-dash-change-late", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote note3 = ClickyInkSamples.DashboardRuns();
			double num2 = DashChangeClock(note3) + 1.25;
			return DashColumns(note3, num2, $"run 6 just landed (clock {num2:0.00}): highlighter under the new numbers, the old ones struck");
		}));
		AddInkScene(list, "ink-dash-missing", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote clickyInkLiveNote2 = ClickyInkSamples.LiveNote("# Broken board\n## Today\n::spark[Unreadable series]{values=\"a, b, c\"}\n::spark[No series]\n::status[Mystery light]{state=purple}\n::progress[Unknown progress]{value=soon}\n::tally[Nothing counted]\n::streak[No days]\n::countdown[Someday]{to=\"whenever\"}\n::countdown[Agent's own words]{left=\"3 days\"}\n:::pipeline[Empty pipeline]\n:::\n::run\n:::tiles\n::figure[Blank]{value=}\n::spark[Empty]{values=\"\"}\n::progress[Odd]{value=\"?\"}\n:::");
			return DashColumns(clickyInkLiveNote2, clickyInkLiveNote2.FinishedClock + 30.0, "values that can't be read: labels and empty lines, never markup");
		}));
		AddInkScene(list, "ink-dash-quiet-run", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote clickyInkLiveNote3 = ClickyInkSamples.LiveNote("# Competitor watch\n## Today\n::run[Morning scan]{n=8 at=\"Tue Oct 13, 7:02\"}\n::quiet[Nothing new since yesterday]{checked=14 at=\"7:02\" runs=3}\n::delta[MRR]{value=\"$18.4k\" was=\"$17.9k\"}\n::spark[Price of Competitor C]{values=\"49, 49, 52, 55\" unit=$ delta=\"+3\" good=down}\n::figure[Load time]{value=\"1.8s\" delta=\"-0.3s\" good=down}\n::figure[Visa]{value=\"$6\" delta=\"Indian passport\"}");
			double num3 = clickyInkLiveNote3.FinishedClock + 30.0;
			List<ClickyInkRoutineControl> routines = new List<ClickyInkRoutineControl>
			{
				new ClickyInkRoutineControl("r1", "Morning scan", 1440, IsPaused: false, IsPausedAfterFailures: false, IsRunning: false, DashboardNow.AddHours(22.0), 7),
				new ClickyInkRoutineControl("r2", "Price check", 240, IsPaused: true, IsPausedAfterFailures: true, IsRunning: false, DashboardNow.AddHours(3.0), 1)
			};
			ClickyInkActions clickyInkActions = new ClickyInkActions
			{
				Routines = routines,
				RunRoutineNow = (string _) => true
			};
			ClickyInkRunStamp.SetEditing(clickyInkLiveNote3.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Run).Id, isEditing: true);
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 14.0
			};
			stackPanel.Children.Add(DashCaption("the run line with routines, Edit open"));
			UIElementCollection children = stackPanel.Children;
			Func<ClickyInkGroup, bool> pick = (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Run;
			double? clock = num3;
			ClickyInkActions actions = clickyInkActions;
			children.Add(InkGroupScene(clickyInkLiveNote3, pick, clock, 0, null, 378.0, actions));
			stackPanel.Children.Add(DashCaption("the run line on a dock window's preview (no Edit)"));
			stackPanel.Children.Add(DashPreviewGroup(clickyInkLiveNote3, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Run, num3, clickyInkActions));
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 14.0
			};
			stackPanel2.Children.Add(DashCaption("a quiet run; changes with good=down; words in a delta"));
			ClickyInkDocument.Kind[] array = new ClickyInkDocument.Kind[2]
			{
				ClickyInkDocument.Kind.Quiet,
				ClickyInkDocument.Kind.Spark
			};
			foreach (ClickyInkDocument.Kind kind in array)
			{
				stackPanel2.Children.Add(InkGroupScene(clickyInkLiveNote3, (ClickyInkGroup g) => g.Kind == kind, num3, 0, null));
			}
			stackPanel2.Children.Add(InkGroupScene(clickyInkLiveNote3, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Figure, num3, 0, null));
			stackPanel2.Children.Add(DashCaption("the quiet stamp landing (clock + 0.2 s)"));
			ClickyInkLiveNote.Block block = clickyInkLiveNote3.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Quiet);
			stackPanel2.Children.Add(InkGroupScene(clickyInkLiveNote3, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Quiet, block.LandsAt + 0.2, 0, null));
			return new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 24.0,
				Margin = new Thickness(24.0),
				Children = 
				{
					(UIElement)stackPanel,
					(UIElement)stackPanel2
				}
			};
		}));
		AddInkScene(list, "ink-dash-tiles", 1500.0, 900.0, () => DashPinned(delegate
		{
			ClickyInkLiveNote clickyInkLiveNote4 = ClickyInkSamples.DashboardRuns();
			double at = clickyInkLiveNote4.FinishedClock + 30.0;
			ClickyInkLiveNote.Page page = clickyInkLiveNote4.Pages[0];
			ClickyInkGroup clickyInkGroup = ClickyInkGroup.Groups(page, at).First((ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Tiles);
			ClickyInkStyle style = new ClickyInkStyle(ClickyHomeSpreadPaper.Note.Ink, ClickyHomeSpreadPaper.Note.Quiet, ClickyHomeSpreadPaper.Note.Deep, clickyInkLiveNote4.LatestEdit(at));
			List<ClickyInkLiveNote.Block> source = clickyInkGroup.Blocks.Select((ClickyInkLiveNote.Block b) => b.DashboardResolved(at)).ToList();
			StackPanel stackPanel3 = new StackPanel
			{
				Spacing = 14.0
			};
			ClickyInkTiles.Look[] values = Enum.GetValues<ClickyInkTiles.Look>();
			foreach (ClickyInkTiles.Look look in values)
			{
				stackPanel3.Children.Add(DashCaption($"look: {look}"));
				UIElementCollection children2 = stackPanel3.Children;
				List<ClickyInkLiveNote.Block> blocks = source.Take(4).ToList();
				double clock2 = at;
				ClickyInkTiles.Look look2 = look;
				children2.Add(DashOnPaper(ClickyInkTiles.Create(blocks, clock2, style, 600.0, null, look2), 600.0));
			}
			StackPanel stackPanel4 = new StackPanel
			{
				Spacing = 14.0
			};
			stackPanel4.Children.Add(DashCaption("a dock window's preview: one row, tighter"));
			using (ClickyInkEnvironment.Use(ClickyInkEnvironment.Default with
			{
				IsWindowPreview = true,
				ContentWidth = 560.0
			}))
			{
				stackPanel4.Children.Add(DashOnPaper(ClickyInkTiles.Create(source.Take(3).ToList(), at, style, 560.0, null), 560.0));
			}
			ClickyInkLiveNote clickyInkLiveNote5 = ClickyInkSamples.LiveNote("# Competitor watch\n## Today\n::run[Morning scan]{n=8 at=\"Tue Oct 13, 7:02\"}\n::quiet[Nothing new since yesterday]{checked=14 at=\"7:02\" runs=3}\n::delta[MRR]{value=\"$18.4k\" was=\"$17.9k\"}\n::spark[Price of Competitor C]{values=\"49, 49, 52, 55\" unit=$ delta=\"+3\" good=down}\n::figure[Load time]{value=\"1.8s\" delta=\"-0.3s\" good=down}\n::figure[Visa]{value=\"$6\" delta=\"Indian passport\"}");
			double quietAt = clickyInkLiveNote5.FinishedClock + 30.0;
			List<ClickyInkLiveNote.Block> source2 = (from b in clickyInkLiveNote5.Pages[0].Blocks.Where(delegate(ClickyInkLiveNote.Block b)
				{
					ClickyInkDocument.Kind kind2 = b.Source.Kind;
					return (kind2 == ClickyInkDocument.Kind.Figure || kind2 == ClickyInkDocument.Kind.Spark) ? true : false;
				})
				select b.DashboardResolved(quietAt)).ToList();
			ClickyInkStyle style2 = new ClickyInkStyle(ClickyHomeSpreadPaper.Note.Ink, ClickyHomeSpreadPaper.Note.Quiet, ClickyHomeSpreadPaper.Note.Deep, clickyInkLiveNote5.LatestEdit(quietAt));
			stackPanel4.Children.Add(DashCaption("narrow tiles on a preview: a change too wide goes under its number"));
			using (ClickyInkEnvironment.Use(ClickyInkEnvironment.Default with
			{
				IsWindowPreview = true,
				ContentWidth = 330.0
			}))
			{
				stackPanel4.Children.Add(DashOnPaper(ClickyInkTiles.Create(source2.Skip(1).ToList(), quietAt, style2, 330.0, null), 330.0));
			}
			stackPanel4.Children.Add(DashCaption("a whole strip being ruled (first tile landing)"));
			double clock3 = clickyInkGroup.LandsAt + 0.3;
			ClickyInkGroup clickyInkGroup2 = ClickyInkGroup.Groups(page, clock3).First((ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Tiles);
			ClickyInkStyle style3 = new ClickyInkStyle(ClickyHomeSpreadPaper.Note.Ink, ClickyHomeSpreadPaper.Note.Quiet, ClickyHomeSpreadPaper.Note.Deep, clickyInkLiveNote4.LatestEdit(clock3));
			stackPanel4.Children.Add(DashOnPaper(ClickyInkTiles.Create(clickyInkGroup2.Blocks, clock3, style3, 560.0, null), 560.0));
			return new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 24.0,
				Margin = new Thickness(24.0),
				Children = 
				{
					(UIElement)stackPanel3,
					(UIElement)stackPanel4
				}
			};
		}));
	}

	private static void AddInkFormScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-sheet-espresso", 1460.0, 900.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			ClickyInkLiveNote.Words words = clickyInkLiveNote.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.List).Items[0].Words;
			double num = words.StartsAt.GetValueOrDefault() + (words.FinishedAt - words.StartsAt.GetValueOrDefault()) * 0.55;
			return new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 30.0,
				Margin = new Thickness(30.0),
				Children = 
				{
					(UIElement)Sheet(clickyInkLiveNote, num, $"writing the list (clock {num:0.00})", null, null, null),
					(UIElement)Sheet(clickyInkLiveNote, clickyInkLiveNote.FinishedClock + 30.0, "finished", null, null, null),
					(UIElement)Sheet(clickyInkLiveNote, clickyInkLiveNote.FinishedClock + 30.0, "finished, sky", ClickyHomeSpreadPaper.Of(ClickyPaperStock.Sky), null, null)
				}
			};
		});
		AddInkScene(list, "ink-sheet-edit", 1460.0, 760.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote2 = ClickyInkSamples.LiveNote(ClickyInkSamples.DeskPickVersions);
			double editStart = ((clickyInkLiveNote2.EditStarts.Count > 0) ? clickyInkLiveNote2.EditStarts[0] : 30.0);
			ClickyInkLiveNote.Block answer = clickyInkLiveNote2.Pages[0].Blocks[0];
			double num2 = MidWriting(clickyInkLiveNote2, (ClickyInkLiveNote.PenFocus f) => f.PlaceID == answer.Id && f.At >= editStart, 0.6);
			return new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 30.0,
				Margin = new Thickness(30.0),
				Children = 
				{
					(UIElement)Sheet(clickyInkLiveNote2, editStart - 1.0, "before the edit", null, null, null),
					(UIElement)Sheet(clickyInkLiveNote2, num2, $"the edit being written (clock {num2:0.00})", null, null, null),
					(UIElement)Sheet(clickyInkLiveNote2, clickyInkLiveNote2.FinishedClock + 0.4, "just written: highlighter sweeping", null, null, null)
				}
			};
		});
		AddInkScene(list, "ink-sheet-working", 1000.0, 900.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote3 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Checking which machine under $500 makes the best milk\n- [x] Find the machines people recommend\n- [ ] Compare prices and milk {now}\n- [ ] Pick one and the catch\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| Bambino Plus | $399 | auto |");
			ClickyInkLiveNote clickyInkLiveNote4 = ClickyInkSamples.LiveNote(ClickyInkSamples.DeskPickVersions);
			StackPanel obj = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 30.0,
				Margin = new Thickness(30.0)
			};
			UIElementCollection children = obj.Children;
			double clock = clickyInkLiveNote3.FinishedClock + 3.2;
			bool? isWorking = true;
			children.Add(Sheet(clickyInkLiveNote3, clock, "working: resting on {now}", null, isWorking, null));
			obj.Children.Add(Sheet(clickyInkLiveNote4, clickyInkLiveNote4.FinishedClock + 3.2, "working, no {now}: the pencil at the end", ClickyHomeSpreadPaper.Of(ClickyPaperStock.Mint), true, null));
			return obj;
		});
		AddInkScene(list, "ink-sheet-window", 1460.0, 520.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote5 = ClickyInkSamples.LiveNote("# Lisbon in May\n\n## In short\n> Fly ANA on the 12th, stay in Alfama. {big=\"$842\" note=\"nonstop, cheapest\"}\n::ticket[ANA NH7, nonstop]{from=\"Boston (BOS)\" to=\"Lisbon (LIS)\" when=\"May 12, 9:40 pm\" price=\"$842\" seat=\"32K\" kind=flight}\n:::receipt[Trip budget]{total=\"$3,380\"}\nFlights: $1,684\nHotel: $900\nFood: $520\nTrams and trains: $276\n:::\n::sticky[Book before Friday: the fare goes up $120]{color=yellow}\n\n## Saturday\n:::day[Saturday May 13]\n9:30 Pastéis de Belém — before the line\nLunch: Time Out Market\n18:00 Miradouro da Graça — sunset\n:::\n::place[Time Out Market]{note=\"go hungry, 8 stalls\" rating=4.6 hours=\"10am–midnight\"}\n::clip[The best custard tart in the city, still warm.]{by=Eater source=https://www.eater.com/lisbon}\n\n## Your move\n:::letter{to=\"Dana Reyes\" subject=\"Re: Lisbon dates\" send=\"Send this reply to Dana from my Gmail\"}\nHi Dana,\n\nThe 12th to the 19th works. I'll book the ANA flight tonight.\n\nBest,\nSam\n:::\n:::review[Cancel these holds?]{go=\"Cancel the ticked hotel holds\" go_label=\"Cancel\"}\n- [x] Hotel Avenida hold — the Alfama one is cheaper\n- [ ] TAP flight hold — keep as a backup\n:::\n:::sketch[How the trams run]{h=120}\nbox 10 40 100 40 \"Baixa\" blue fill\narrow 110 60 170 60 \"28\" curve\nbox 170 40 100 40 \"Alfama\" green\ncircle 330 60 26 \"Graça\" orange\n:::");
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 30.0,
				Margin = new Thickness(30.0)
			};
			double[] array = new double[3] { 0.25, 0.6, 1.0 };
			foreach (double num3 in array)
			{
				double num4 = ((num3 >= 1.0) ? (clickyInkLiveNote5.FinishedClock + 30.0) : (clickyInkLiveNote5.FinishedClock * num3));
				UIElementCollection children2 = stackPanel.Children;
				string caption = $"window 360, clock {num4:0.0}";
				ClickyHomeSpreadPaper? paper = ClickyHomeSpreadPaper.Of(ClickyPaperStock.Peach);
				double? followsPenInHeight = 360.0;
				children2.Add(Sheet(clickyInkLiveNote5, num4, caption, paper, null, followsPenInHeight));
			}
			return stackPanel;
		});
		AddInkScene(list, "ink-top-windows", 1620.0, 640.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote6 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			ClickyInkLiveNote clickyInkLiveNote7 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Checking which machine under $500 makes the best milk\n- [x] Find the machines people recommend\n- [ ] Compare prices and milk {now}\n- [ ] Pick one and the catch\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| Bambino Plus | $399 | auto |");
			ClickyInkLiveNote clickyInkLiveNote8 = ClickyInkSamples.LiveNote("# Pocketlog daily\n\n## Today\n::run{n=12 at=\"Sat Oct 10 · 7:00\" next=\"Sun 7:00\" note=\"App Store, Play Console, Stripe\"}\n> Crashes jumped on Android 2.14; signups held. {big=\"98.1%\" note=\"crash-free, down\"}\n:::tiles\n::spark[Signups]{values=\"118, 121, 133, 129, 142, 138\"}\n::figure[Revenue]{value=\"$1,319\" delta=\"+3%\"}\n::status[Crash-free]{state=bad note=\"98.1%: 2.14 crashes at login\"}\n::countdown[2.15 review]{to=\"2026-10-12 09:00\"}\n:::\n::sticky[Pause the 2.14 rollout: 212 crashes, all at login]{color=pink}\n\n## Your move\n::question[Pause the 2.14 rollout?]{yes=\"Pause the Android 2.14 rollout in the Play Console\" no=\"Keep it going and watch it\" yes_label=\"Pause it\" no_label=\"Keep going\" since=\"Oct 10\"}\n\n## Worth a look\n- Android 2.14 crashes at login on Pixel 8 {new}\n  212 crashes, 97% on 2.14\n- Onboarding step 3 drop-off is down to 38%\n\n## Funnel\n:::pipeline[Yesterday]\nVisited: 4,050\nSigned up: 138\nStarted trial: 64\nPaid: 11\n:::");
			Grid grid = new Grid
			{
				Margin = new Thickness(30.0),
				ColumnSpacing = 30.0,
				RowSpacing = 30.0
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			grid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			Put(TopOfNote(clickyInkLiveNote6, clickyInkLiveNote6.FinishedClock + 30.0, isWorking: false, animates: false, glance: false, 480.0, 220.0, "done, the window to itself"), 0, 0);
			Put(TopOfNote(clickyInkLiveNote6, clickyInkLiveNote6.FinishedClock + 30.0, isWorking: false, animates: false, glance: false, 300.0, 220.0, "done, beside its files", ClickyPaperStock.Butter), 0, 1);
			Put(TopOfNote(clickyInkLiveNote7, clickyInkLiveNote7.FinishedClock + 2.0, isWorking: true, animates: false, glance: false, 480.0, 220.0, "working (still: not the pointer's window)", ClickyPaperStock.Mint), 0, 2);
			Put(TopOfNote(clickyInkLiveNote6, clickyInkLiveNote6.FinishedClock + 30.0, isWorking: false, animates: false, glance: true, 480.0, 220.0, "glance (the deck's window)", ClickyPaperStock.Blush), 1, 0);
			Put(TopOfNote(clickyInkLiveNote8, clickyInkLiveNote8.FinishedClock + 30.0, isWorking: false, animates: false, glance: true, 480.0, 220.0, "glance, a dashboard", ClickyPaperStock.Periwinkle), 1, 1);
			Put(TopOfNote(clickyInkLiveNote6, clickyInkLiveNote6.FinishedClock * 0.45, isWorking: true, animates: true, glance: false, 480.0, 220.0, "writing on the pointer's window (animates)", ClickyPaperStock.Peach), 1, 2);
			return grid;
			void Put(FrameworkElement e, int row, int column)
			{
				Grid.SetRow(e, row);
				Grid.SetColumn(e, column);
				grid.Children.Add(e);
			}
		});
	}

	private static void AddInkScene(List<Scene> list, string name, double width, double height, Func<FrameworkElement> build)
	{
		list.Add(new Scene(name, width, height, () => new Grid
		{
			Background = Ui.Brush(InkDesk),
			Children = { (UIElement)build() }
		}, InkDesk, 2.2));
	}

	public static FrameworkElement InkPageScene(ClickyInkLiveNote note, int pageIndex, double clock, ClickyHomeSpreadPaper? paper = null, string? caption = null, ClickyInkActions? actions = null, int part = 0)
	{
		ClickyHomeSpreadPaper value = paper ?? ClickyHomeSpreadPaper.Note;
		Grid grid = new Grid
		{
			Width = ClickyInkLayout.PageSize.Width,
			Height = ClickyInkLayout.PageSize.Height
		};
		grid.Children.Add(new PaperSurface(ClickyShape.Rectangle, value.Fill, 0.45).WithShadow(PaperColor.Black.Opacity(0.12), 6.0, 0.0, 2.0));
		grid.Children.Add(new ClickyInkPageView(note, pageIndex, clock, value, actions, part));
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 6.0,
			Children = { (UIElement)grid }
		};
		if (caption != null)
		{
			stackPanel.Children.Add(Ui.Text(caption, 11.0, FontWeights.SemiBold, PaperColor.Hex("#6B6F7A")));
		}
		return stackPanel;
	}

	public static FrameworkElement InkGroupScene(ClickyInkLiveNote note, Func<ClickyInkGroup, bool> pick, double? clock = null, int pageIndex = 0, ClickyHomeSpreadPaper? paper = null, double width = 378.0, ClickyInkActions? actions = null)
	{
		ClickyHomeSpreadPaper paper2 = paper ?? ClickyHomeSpreadPaper.Note;
		double clock2 = clock ?? (note.FinishedClock + 30.0);
		ClickyInkStyle style = new ClickyInkStyle(paper2.Ink, paper2.Quiet, paper2.Deep, note.LatestEdit(clock2)).Resting(note, null);
		ClickyInkGroup clickyInkGroup = ClickyInkGroup.Groups(note.Pages[pageIndex], clock2).FirstOrDefault(pick);
		FrameworkElement item;
		using (ClickyInkEnvironment.Use(ClickyInkEnvironment.Default with
		{
			ContentWidth = width
		}))
		{
			item = (((object)clickyInkGroup == null) ? Ui.Text("no such group", 12.0, FontWeights.SemiBold, PaperColor.Black) : ClickyInkGroupView.Make(clickyInkGroup, clock2, style, paper2, actions ?? new ClickyInkActions()));
		}
		return new Grid
		{
			Width = width + 72.0,
			Padding = new Thickness(36.0, 24.0, 36.0, 24.0),
			Children = 
			{
				(UIElement)new PaperSurface(ClickyShape.Rectangle, paper2.Fill, 0.45)
				{
					Margin = new Thickness(-36.0, -24.0, -36.0, -24.0)
				},
				(UIElement)item
			}
		};
	}

	private static double MidWriting(ClickyInkLiveNote note, Func<ClickyInkLiveNote.PenFocus, bool> placeMatches, double fraction = 0.5)
	{
		foreach (ClickyInkLiveNote.PenFocus item in from f in note.PenFocusList()
			orderby f.At
			select f)
		{
			if (placeMatches(item) && !item.IsLanding && item.ItemID != null && item.Until > item.At + 0.3)
			{
				return item.At + (item.Until - item.At) * fraction;
			}
		}
		return note.FinishedClock * fraction;
	}

	private static void AddInkPageScenes(List<Scene> list)
	{
		AddInkScene(list, "ink-page-words", 1460.0, 640.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote = ClickyInkSamples.LiveNote("# Standing desk notes\n\n## What I read\n### The short version\nThree desks held steady at standing height; one wobbled.\n::note[The wobble only shows above 115 cm]\n> Reviewers keep coming back to the motor noise and the crossbar, not the price.\nA long paragraph is typed rather than handwritten so the page stays legible: the Uplift V2 and the Flexispot E7 Pro both passed the wobble test at full height, the Fully Jarvis wobbled a little, and the IKEA Trotten never got close. Noise ranged from forty to fifty decibels.\n- Flexispot E7 Pro is the quietest {new}\n- Uplift V2 has the steadiest frame {pick}\n- Check the return window {needs}\n- Order the cable tray {done}\n- IKEA Trotten {out}\n  a crank, not a motor\n- [The Wirecutter test](https://www.nytimes.com/wirecutter/reviews/best-standing-desk/) — the longest one\n1. Measure the room\n2. Pick the top size\n3. Order\n```\nheight_cm = 72 + 0.4 * (user_cm - 170)\n```\n---\n:::sketch[Where the desk goes]{h=110}\nbox 20 20 140 70 \"Desk\" blue fill\narrow 170 55 250 55 \"window\" curve\ncircle 300 55 30 \"Lamp\" orange\ndot 80 100 \"outlet\" small\n:::");
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 22.0,
				Margin = new Thickness(24.0)
			};
			List<(int, int)> source = ClickyInkPagination.Sheets(clickyInkLiveNote, null);
			foreach (var item2 in source.Take(3))
			{
				int page = item2.Item1;
				int item = item2.Item2;
				UIElementCollection children = stackPanel.Children;
				int pageIndex = page;
				double clock = clickyInkLiveNote.FinishedClock + 30.0;
				string caption = $"note page {page + 1}, sheet {item + 1} of {source.Count<(int, int)>(((int Page, int Part) s) => s.Page == page)}";
				int part = item;
				children.Add(InkPageScene(clickyInkLiveNote, pageIndex, clock, null, caption, null, part));
			}
			return stackPanel;
		});
		AddInkScene(list, "ink-page-espresso", 1460.0, 640.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote2 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Bambino Plus; budget $120 more for a grinder {big=\"$399\" note=\"best milk under $500\"}\n- Auto milk frothing that actually works\n- Heats up in 3 seconds\n::sticky[It needs a grinder: no beans pre-ground]{color=pink}\n::photo[Breville Bambino Plus]{query=\"Breville Bambino Plus espresso machine\"}\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| **Bambino Plus** | $399 | auto |\n| Gaggia Classic Pro | $449 | manual |\n- De'Longhi Dedica {out}\n  steam wand too weak in 3 reviews\n:::chart[Reviewer scores]{unit=/10}\nBambino Plus: 9.1 {pick}\nGaggia Classic Pro: 8.4\n:::\n\n## Next\n::ask[Find a grinder to match]{say=\"Find the best grinder under $150 to pair with the Bambino Plus\"}\n::question[Mostly milk drinks?]{yes=\"Mostly lattes, keep the Bambino\" no=\"Mostly espresso, reconsider the Gaggia\"}\n::link[Wirecutter: espresso machines]{url=https://www.nytimes.com/wirecutter/reviews/best-espresso-machine/}");
			ClickyInkLiveNote.Words words = clickyInkLiveNote2.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.List).Items[0].Words;
			double num = words.StartsAt.GetValueOrDefault() + (words.FinishedAt - words.StartsAt.GetValueOrDefault()) * 0.55;
			StackPanel obj = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 22.0,
				Margin = new Thickness(24.0),
				Children = 
				{
					(UIElement)InkPageScene(clickyInkLiveNote2, 0, clickyInkLiveNote2.FinishedClock + 30.0, null, "finished, cream note"),
					(UIElement)InkPageScene(clickyInkLiveNote2, 0, clickyInkLiveNote2.FinishedClock + 30.0, ClickyHomeSpreadPaper.Of(ClickyPaperStock.Sky), "finished, sky")
				}
			};
			UIElementCollection children2 = obj.Children;
			string caption2 = $"writing the list (clock {num:0.00})";
			children2.Add(InkPageScene(clickyInkLiveNote2, 0, num, null, caption2));
			return obj;
		});
		AddInkScene(list, "ink-page-edit", 1460.0, 640.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote3 = ClickyInkSamples.LiveNote(ClickyInkSamples.DeskPickVersions);
			double editStart = ((clickyInkLiveNote3.EditStarts.Count > 0) ? clickyInkLiveNote3.EditStarts[0] : 30.0);
			ClickyInkLiveNote.Block answer = clickyInkLiveNote3.Pages[0].Blocks[0];
			double num2 = MidWriting(clickyInkLiveNote3, (ClickyInkLiveNote.PenFocus f) => f.PlaceID == answer.Id && f.At >= editStart, 0.6);
			StackPanel obj2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 22.0,
				Margin = new Thickness(24.0),
				Children = { (UIElement)InkPageScene(clickyInkLiveNote3, 0, editStart - 1.0, null, "before the edit") }
			};
			UIElementCollection children3 = obj2.Children;
			string caption3 = $"the edit being written (clock {num2:0.00})";
			children3.Add(InkPageScene(clickyInkLiveNote3, 0, num2, null, caption3));
			obj2.Children.Add(InkPageScene(clickyInkLiveNote3, 0, clickyInkLiveNote3.FinishedClock + 0.4, null, "just written: highlighter sweeping"));
			return obj2;
		});
		AddInkScene(list, "ink-page-working", 1000.0, 640.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote4 = ClickyInkSamples.LiveNote("# Espresso pick\n\n## In short\n> Checking which machine under $500 makes the best milk\n- [x] Find the machines people recommend\n- [ ] Compare prices and milk {now}\n- [ ] Pick one and the catch\n\n## Compare\n| Machine | Price | Milk |\n|---|---|---|\n| Bambino Plus | $399 | auto |");
			return new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 22.0,
				Margin = new Thickness(24.0),
				Children = 
				{
					(UIElement)InkPageScene(clickyInkLiveNote4, 0, clickyInkLiveNote4.FinishedClock + 3.2, null, "working: the pencil rests on {now}"),
					(UIElement)InkPageScene(clickyInkLiveNote4, 1, clickyInkLiveNote4.FinishedClock + 30.0, null, "its second page")
				}
			};
		});
		AddInkScene(list, "ink-page-groups", 900.0, 560.0, delegate
		{
			ClickyInkLiveNote clickyInkLiveNote5 = ClickyInkSamples.LiveNote("# Standing desk notes\n\n## What I read\n### The short version\nThree desks held steady at standing height; one wobbled.\n::note[The wobble only shows above 115 cm]\n> Reviewers keep coming back to the motor noise and the crossbar, not the price.\nA long paragraph is typed rather than handwritten so the page stays legible: the Uplift V2 and the Flexispot E7 Pro both passed the wobble test at full height, the Fully Jarvis wobbled a little, and the IKEA Trotten never got close. Noise ranged from forty to fifty decibels.\n- Flexispot E7 Pro is the quietest {new}\n- Uplift V2 has the steadiest frame {pick}\n- Check the return window {needs}\n- Order the cable tray {done}\n- IKEA Trotten {out}\n  a crank, not a motor\n- [The Wirecutter test](https://www.nytimes.com/wirecutter/reviews/best-standing-desk/) — the longest one\n1. Measure the room\n2. Pick the top size\n3. Order\n```\nheight_cm = 72 + 0.4 * (user_cm - 170)\n```\n---\n:::sketch[Where the desk goes]{h=110}\nbox 20 20 140 70 \"Desk\" blue fill\narrow 170 55 250 55 \"window\" curve\ncircle 300 55 30 \"Lamp\" orange\ndot 80 100 \"outlet\" small\n:::");
			return new StackPanel
			{
				Spacing = 14.0,
				Margin = new Thickness(24.0),
				Children = 
				{
					(UIElement)InkGroupScene(clickyInkLiveNote5, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.List, null, 0, null),
					(UIElement)InkGroupScene(clickyInkLiveNote5, (ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Drawing, clickyInkLiveNote5.Pages[0].Blocks.First((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Drawing).LandsAt + 1.2, 0, null)
				}
			};
		});
	}

	private static Grid OnSidebar(FrameworkElement content)
	{
		return new Grid
		{
			Background = Ui.Brush(HomeTheme.SidebarFill),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Children = { (UIElement)content }
		};
	}

	private static void WriteSamplePicture(string path)
	{
		if (File.Exists(path))
		{
			return;
		}
		byte[] pixels = new byte[422400];
		int[] array = new int[6] { 60, 90, 120, 150, 140, 185 };
		for (int i = 0; i < 220; i++)
		{
			for (int j = 0; j < 480; j++)
			{
				int num = (i * 480 + j) * 4;
				byte b = 34;
				byte b2 = 36;
				byte b3 = 43;
				int num2 = (j - 30) / 72;
				int num3 = (j - 30) % 72;
				if (j >= 30 && num2 < array.Length && num3 < 48 && 200 - i < array[num2] && i < 200)
				{
					b = 91;
					b2 = 156;
					b3 = 248;
				}
				if (i == 200)
				{
					b = (b2 = (b3 = 96));
				}
				pixels[num] = b3;
				pixels[num + 1] = b2;
				pixels[num + 2] = b;
				pixels[num + 3] = byte.MaxValue;
			}
		}
		using FileStream fileStream = File.Create(path);
		Task<byte[]> task = Task.Run(async delegate
		{
			using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
			BitmapEncoder obj = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
			obj.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, 480u, 220u, 72.0, 72.0, pixels);
			await obj.FlushAsync();
			stream.Seek(0uL);
			byte[] bytes = new byte[stream.Size];
			await stream.AsStreamForRead().ReadExactlyAsync(bytes);
			return bytes;
		});
		fileStream.Write(task.GetAwaiter().GetResult());
	}

	private static FrameworkElement Preview(string path, int sheetIndex, SpreadsheetCellRange selection)
	{
		SpreadsheetPreview spreadsheetPreview = new SpreadsheetPreview(SpreadsheetWorkbook.Read(path) ?? throw new InvalidOperationException("couldn't read " + path));
		spreadsheetPreview.SelectSheet(sheetIndex);
		spreadsheetPreview.GridView.Select(selection);
		return spreadsheetPreview;
	}
}
