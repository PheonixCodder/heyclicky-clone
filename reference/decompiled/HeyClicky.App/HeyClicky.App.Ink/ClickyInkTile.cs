using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkTile
{
	private static readonly Color[] TapeColors = new Color[3]
	{
		PaperColor.Hex("#F6C6CF"),
		PaperColor.Hex("#FFE07D"),
		PaperColor.Hex("#C3D0FF")
	};

	private static readonly double[] TapeTilt = new double[3] { -1.6, 1.1, -0.8 };

	public static string StateWord(string word)
	{
		return ClickyInkDashboardViewMath.StateWord(word);
	}

	public static bool StacksChange(ClickyInkDocument.Block block, double? width)
	{
		if (width.HasValue)
		{
			double valueOrDefault = width.GetValueOrDefault();
			(string, double, string)? tuple = ClickyInkDashboardViewMath.TileChangeMeasure(block);
			if (tuple.HasValue)
			{
				(string, double, string) valueOrDefault2 = tuple.GetValueOrDefault();
				return ClickyInkDashboardViewMath.TileStacksChange(MeasuredWidth(valueOrDefault2.Item1, ClickyPaperFonts.Rounded(valueOrDefault2.Item2, FontWeights.Bold)), changeWidth: MeasuredWidth(valueOrDefault2.Item3, ClickyPaperFonts.Rounded(11.5, FontWeights.SemiBold)), numberCharacters: valueOrDefault2.Item1.CharacterCount(), width: valueOrDefault);
			}
		}
		return false;
	}

	private static double MeasuredWidth(string text, PaperFont font)
	{
		TextBlock textBlock = font.Apply(new TextBlock
		{
			Text = text,
			TextWrapping = TextWrapping.NoWrap
		});
		textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return Math.Ceiling(textBlock.DesiredSize.Width);
	}

	public static double Height(ClickyInkDocument.Block block, double? width = null, bool compact = false)
	{
		return ClickyInkDashboardViewMath.TileHeight(block, StacksChange(block, width), compact);
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, int index = 0, ClickyInkTiles.Look look = ClickyInkTiles.Look.Ruled, double? width = null)
	{
		bool isWindowPreview = ClickyInkEnvironment.Current.IsWindowPreview;
		ClickyInkDocument.Block source = block.Source;
		double num = ClickyInkClock.Progress(clock, block.LandsAt, block.LandsAt + 0.55);
		double? goneAt = block.GoneAt;
		double num2;
		if (goneAt.HasValue)
		{
			double valueOrDefault = goneAt.GetValueOrDefault();
			num2 = ClickyInkClock.Progress(clock, valueOrDefault, valueOrDefault + 0.48);
		}
		else
		{
			num2 = 0.0;
		}
		double num3 = num2;
		double delay = 0.25 + (double)index * 0.22;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0
		};
		if (source.Kind != ClickyInkDocument.Kind.Paragraph)
		{
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(6.0, InkVerticalAlignment.Center);
			string text = ClickyInkIcon.SymbolName(source["icon"], source.Text);
			if (text != null)
			{
				TextBlock textBlock = ClickyInkIcon.Glyph(text, 10.0, style.Quiet, bold: true);
				if ((object)textBlock != null)
				{
					clickyInkHStack.Children.Add(textBlock);
				}
			}
			clickyInkHStack.Children.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Typed(9.5, FontWeights.Bold), pencil: false, style.Quiet, null).With(delegate(InkTextView t)
			{
				t.Tracking = 0.7;
				t.Uppercase = true;
				t.LineLimit = 1;
				t.MinimumScale = 0.75;
			}));
			clickyInkHStack.Children.Add(new ClickyInkSpacer());
			if (source.Kind == ClickyInkDocument.Kind.Status)
			{
				clickyInkHStack.Children.Add(Lamp(block, clock, delay, 16.0));
			}
			stackPanel.Children.Add(clickyInkHStack);
		}
		foreach (UIElement item in Content(block, clock, style, index, look, width, delay))
		{
			stackPanel.Children.Add(item);
		}
		Grid grid = new Grid
		{
			Padding = new Thickness(12.0, isWindowPreview ? 5 : 10, 12.0, isWindowPreview ? 2 : 9)
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid.Children.Add(stackPanel);
		string text2 = source["note"];
		if (text2 != null && text2.Length > 0 && source.Kind != ClickyInkDocument.Kind.Paragraph)
		{
			int? num4 = ClickyInkChange.ChangedEdit(source);
			bool flag = num4.HasValue && num4.GetValueOrDefault() == style.LatestEdit;
			InkTextView inkTextView = InkTextView.Plain(text2, ClickyPaperFonts.Hand(14.0), flag ? style.Fresh : style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
				t.MinimumScale = 0.8;
			});
			inkTextView.Opacity = ClickyInkClock.Progress(clock, block.Words.FinishedAt + 0.1, block.Words.FinishedAt + 0.4);
			inkTextView.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
			Grid.SetRow(inkTextView, 2);
			grid.Children.Add(inkTextView);
		}
		grid.Opacity = ((clock >= block.LandsAt) ? (Math.Min(1.0, num * 2.0) * (1.0 - 0.45 * num3)) : 0.0);
		grid.Transformed(0.0, -5.0 * (1.0 - num), 0.0, 1.0, null);
		if (num3 <= 0.0)
		{
			return grid;
		}
		ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyInkCrossOut(block.Id.CharacterCount()).Trim(0.0, num3), new PaperStroke(style.Fresh.Opacity(0.85), 2.0, PenLineCap.Round));
		clickyShapeView.Margin = new Thickness(14.0);
		return new ClickyInkOverlay(grid, clickyShapeView);
	}

	private static FrameworkElement Lamp(ClickyInkLiveNote.Block block, double clock, double delay, double diameter)
	{
		ClickyInkDocument.Block source = block.Source;
		ClickyInkStatusState state = ClickyInkStatus.State(source["state"] ?? source["status"] ?? source["value"]);
		string text = ClickyInkChange.Previous(source, "state");
		ClickyInkStatusState? previous = ((text != null) ? new ClickyInkStatusState?(ClickyInkStatus.State(text)) : ((ClickyInkStatusState?)null));
		return ClickyInkLamp.Create(state, previous, ClickyInkChange.Progress(source, clock, delay, 0.8), diameter, block.Id.CharacterCount(), ClickyInkClock.Progress(clock, block.LandsAt + 0.1, block.LandsAt + 0.6));
	}

	private static FrameworkElement NumberAndChange(bool stacksChange, double spacing, UIElement?[] number, UIElement? change)
	{
		if (stacksChange)
		{
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 1.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Children = { (UIElement)InkDashboardLayout.HStack(spacing, InkVerticalAlignment.FirstBaseline, number) }
			};
			if ((object)change != null)
			{
				stackPanel.Children.Add(change);
			}
			return stackPanel;
		}
		int num = 0;
		UIElement[] array = new UIElement[1 + number.Length];
		ReadOnlySpan<UIElement> readOnlySpan = new ReadOnlySpan<UIElement>(number);
		readOnlySpan.CopyTo(new Span<UIElement>(array).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		array[num] = change;
		ClickyInkHStack clickyInkHStack = InkDashboardLayout.HStack(spacing, InkVerticalAlignment.FirstBaseline, array);
		clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
		return clickyInkHStack;
	}

	private static List<UIElement> Content(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, int index, ClickyInkTiles.Look look, double? width, double delay)
	{
		ClickyInkDocument.Block source = block.Source;
		bool flag = ClickyInkChange.IsLatest(source, style.LatestEdit);
		bool flag2 = ClickyInkChange.Animates(source, style.LatestEdit);
		bool flag3 = StacksChange(source, width);
		double num = ClickyInkClock.Progress(clock, block.LandsAt + 0.15, block.LandsAt + 0.9);
		int num2 = block.Id.CharacterCount();
		List<UIElement> list = new List<UIElement>();
		ClickyInkStatusState clickyInkStatusState;
		string text;
		ClickyInkStatusState? clickyInkStatusState2;
		int num3;
		List<ClickyInkStreakDay> days;
		int? obj;
		int? previousCount2;
		FrameworkElement frameworkElement3;
		bool flag4;
		InkTextView inkTextView;
		FrameworkElement frameworkElement4;
		ClickyInkHStack clickyInkHStack4;
		switch (source.Kind)
		{
		case ClickyInkDocument.Kind.Figure:
		{
			string key = ((source["value"] != null) ? "value" : "big");
			(string, double)? computed = ((string, double)?)(flag ? ((ValueType)ClickyInkNumber.Difference(ClickyInkChange.Previous(source, key), source[key])) : ((ValueType)((string, double)?)null));
			FrameworkElement frameworkElement = NumberAndChange(flag3, 6.0, new UIElement[1] { ClickyInkTickingNumber.Create(source, key, clock, style, 28.0, delay) }, ClickyInkDeltaTag.Create(source, clock, source["delta"], computed, 11.5, "", flag3));
			if (look == ClickyInkTiles.Look.Tape)
			{
				frameworkElement.Margin = new Thickness(12.0, 3.0, 12.0, 3.0);
				ClickyShapeView clickyShapeView = ClickyShapeView.Fill(new ClickyInkTornTape(), TapeColors[index % TapeColors.Length].Opacity(0.82));
				ClickyShapeView clickyShapeView2 = ClickyShapeView.Fill(new ClickyInkTornTape(), PaperColor.Black.Opacity(0.08));
				clickyShapeView2.Transformed(0.0, 1.0, 0.0, 1.0, null);
				ClickyInkOverlay clickyInkOverlay = new ClickyInkOverlay(frameworkElement).Behind(clickyShapeView).Behind(clickyShapeView2);
				clickyShapeView.Opacity = num;
				clickyShapeView2.Opacity = num;
				clickyInkOverlay.HorizontalAlignment = HorizontalAlignment.Left;
				clickyInkOverlay.Transformed(0.0, 0.0, TapeTilt[index % TapeTilt.Length], 1.0, null);
				frameworkElement = clickyInkOverlay;
			}
			list.Add(frameworkElement);
			if (source["values"] != null)
			{
				ClickyInkSparkReading clickyInkSparkReading = new ClickyInkSparkReading(source);
				List<double> values = clickyInkSparkReading.Values;
				List<double>? previous = (flag2 ? clickyInkSparkReading.PreviousValues : null);
				double progress = ClickyInkChange.Progress(source, clock, delay);
				ClickyInkStyle style2 = style;
				int seed = num2;
				bool marksNew = flag;
				double? height = 26.0;
				list.Add(ClickyInkSparkLine.Create(values, previous, progress, num, null, style2, null, seed, marksNew, null, height));
			}
			break;
		}
		case ClickyInkDocument.Kind.Spark:
		{
			ClickyInkSparkReading clickyInkSparkReading2 = new ClickyInkSparkReading(source);
			UIElement[] obj2 = new UIElement[2]
			{
				ClickyInkTickingNumber.Create(source, "values", clock, style, 26.0, delay, clickyInkSparkReading2.Last, clickyInkSparkReading2.PreviousLast),
				null
			};
			string text2 = source["unit"];
			obj2[1] = ((text2 != null && text2.Length > 0) ? InkDashboardLayout.Fixed(InkTextView.Plain(text2, ClickyPaperFonts.Hand(14.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			})) : null);
			list.Add(NumberAndChange(flag3, 5.0, obj2, ClickyInkDeltaTag.Create(source, clock, source["delta"], clickyInkSparkReading2.Step, 11.5, "", flag3)));
			List<double> values2 = clickyInkSparkReading2.Values;
			List<double>? previous3 = (flag2 ? clickyInkSparkReading2.PreviousValues : null);
			double progress2 = ClickyInkChange.Progress(source, clock, delay);
			string text3 = source["goal"];
			double? goal = ((text3 == null) ? ((double?)null) : ClickyInkNumber.Parse(text3)?.Value);
			ClickyInkStyle style3 = style;
			int seed = num2;
			bool marksNew = flag;
			double? height = 30.0;
			list.Add(ClickyInkSparkLine.Create(values2, previous3, progress2, num, goal, style3, null, seed, marksNew, null, height));
			break;
		}
		case ClickyInkDocument.Kind.Status:
			clickyInkStatusState = ClickyInkStatus.State(source["state"] ?? source["status"] ?? source["value"]);
			text = ClickyInkChange.Previous(source, "state");
			clickyInkStatusState2 = ((text != null) ? new ClickyInkStatusState?(ClickyInkStatus.State(text)) : ((ClickyInkStatusState?)null));
			if (clickyInkStatusState2.HasValue)
			{
				ClickyInkStatusState valueOrDefault2 = clickyInkStatusState2.GetValueOrDefault();
				if (valueOrDefault2 != clickyInkStatusState && ClickyInkChange.ChangedEdit(source) == style.LatestEdit)
				{
					num3 = (style.LatestEdit.HasValue ? 1 : 0);
					goto IL_058e;
				}
			}
			num3 = 0;
			goto IL_058e;
		case ClickyInkDocument.Kind.Progress:
		{
			ClickyInkProgress clickyInkProgress = new ClickyInkProgress(source.Attributes);
			double? previous2 = (flag ? ClickyInkProgressView.PreviousFraction(source) : ((double?)null));
			list.Add(ClickyInkTickingNumber.Create(source, ClickyInkProgressView.ValueKey(source), clock, style, 24.0, delay, clickyInkProgress.Shown, (!previous2.HasValue) ? null : ClickyInkProgressView.PreviousShown(source), showsWas: false));
			FrameworkElement frameworkElement2 = ClickyInkProgressBar.Create(clickyInkProgress.Fill, previous2, ClickyInkChange.Progress(source, clock, delay), num, 11.0, style, num2, null);
			frameworkElement2.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
			list.Add(frameworkElement2);
			break;
		}
		case ClickyInkDocument.Kind.Tally:
		{
			ClickyInkTallyReading clickyInkTallyReading = new ClickyInkTallyReading(source, style.LatestEdit);
			UIElement[] obj3 = new UIElement[2]
			{
				ClickyInkTickingNumber.Create(source, clickyInkTallyReading.Key, clock, style, 26.0, delay),
				null
			};
			string text4 = source["goal"] ?? source["of"];
			obj3[1] = ((text4 != null) ? InkDashboardLayout.Fixed(InkTextView.Plain("of " + text4, ClickyPaperFonts.Hand(14.0), style.Quiet)) : null);
			ClickyInkHStack clickyInkHStack3 = InkDashboardLayout.HStack(4.0, InkVerticalAlignment.FirstBaseline, obj3);
			clickyInkHStack3.HorizontalAlignment = HorizontalAlignment.Left;
			list.Add(clickyInkHStack3);
			if (clickyInkTallyReading.Count >= 5)
			{
				int num4 = (int)Math.Min(2147483647L, clickyInkTallyReading.Fresh);
				int count = (int)Math.Min(2147483647L, clickyInkTallyReading.Count);
				double progress3 = ClickyInkChange.Progress(source, clock, delay, Math.Max(0.6, (double)num4 * 0.15));
				ClickyInkStyle style4 = style;
				double? height = 38.0;
				list.Add(ClickyInkTallyMarks.Create(count, num4, progress3, num, 15.0, style4, num2, null, height));
			}
			break;
		}
		case ClickyInkDocument.Kind.Streak:
		{
			ClickyInkStreakReading clickyInkStreakReading = new ClickyInkStreakReading(source);
			ClickyInkHStack clickyInkHStack = InkDashboardLayout.HStack(5.0, InkVerticalAlignment.FirstBaseline, ClickyInkTickingNumber.Create(source, "_streak", clock, style, 26.0, delay, clickyInkStreakReading.Shown, clickyInkStreakReading.PreviousShown, showsWas: false), InkTextView.Plain(clickyInkStreakReading.Unit, ClickyPaperFonts.Hand(14.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}));
			clickyInkHStack.HorizontalAlignment = HorizontalAlignment.Left;
			list.Add(clickyInkHStack);
			days = clickyInkStreakReading.Days.Skip(Math.Max(0, clickyInkStreakReading.Days.Count - 14)).ToList();
			if (flag)
			{
				int? previousCount = clickyInkStreakReading.PreviousCount;
				if (previousCount.HasValue)
				{
					int valueOrDefault = previousCount.GetValueOrDefault();
					obj = Math.Max(0, valueOrDefault - Math.Max(0, clickyInkStreakReading.Days.Count - 14));
					goto IL_0a49;
				}
			}
			obj = null;
			goto IL_0a49;
		}
		case ClickyInkDocument.Kind.Countdown:
		{
			ClickyInkCountdownReading clickyInkCountdownReading = new ClickyInkCountdownReading(source);
			ClickyInkHStack clickyInkHStack2 = InkDashboardLayout.HStack(5.0, InkVerticalAlignment.FirstBaseline, InkDashboardLayout.Fixed(InkTextView.Plain((clickyInkCountdownReading.Big.Length == 0) ? "–" : clickyInkCountdownReading.Big, ClickyPaperFonts.BigNumber(28.0), ClickyInkCountdown.Color(clickyInkCountdownReading, style)).With(delegate(InkTextView t)
			{
				t.MonospacedDigit = true;
			})), InkTextView.Plain(clickyInkCountdownReading.Unit, ClickyPaperFonts.Hand(15.0), style.Quiet).With(delegate(InkTextView t)
			{
				t.LineLimit = 1;
			}), clickyInkCountdownReading.IsPast ? InkDashboardLayout.Fixed(ClickyInkTag.Make("past", ClickyInkDashboardPalette.Word(ClickyInkStatusState.Bad))) : null);
			clickyInkHStack2.HorizontalAlignment = HorizontalAlignment.Left;
			list.Add(clickyInkHStack2);
			string dateLine = clickyInkCountdownReading.DateLine;
			if (dateLine != null)
			{
				list.Add(InkTextView.Plain(dateLine, ClickyPaperFonts.Typed(10.5, FontWeights.Medium), style.Quiet).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
					t.MinimumScale = 0.8;
				}));
			}
			break;
		}
		default:
			{
				list.Add(ClickyInkWordsView.Make(block.Words, clock, style, ClickyPaperFonts.Hand(16.0), pencil: true, null, null).With(delegate(InkTextView t)
				{
					t.LineLimit = 4;
				}));
				break;
			}
			IL_0a49:
			previousCount2 = obj;
			frameworkElement3 = ClickyInkStreakBoxes.Create(days, previousCount2, ClickyInkChange.Progress(source, clock, delay, 0.6), num, 10.0, 14, style, num2);
			frameworkElement3.Margin = new Thickness(0.0, 3.0, 0.0, 0.0);
			list.Add(frameworkElement3);
			break;
			IL_058e:
			flag4 = (byte)num3 != 0;
			inkTextView = InkTextView.Plain(StateWord(ClickyInkStatus.Word(source["state"] ?? source["status"], clickyInkStatusState)), ClickyPaperFonts.BigNumber(24.0), ClickyInkDashboardPalette.Word(clickyInkStatusState)).With(delegate(InkTextView t)
			{
				t.Tracking = -0.5;
				t.LineLimit = 1;
				t.MinimumScale = 0.6;
			});
			frameworkElement4 = inkTextView;
			if (flag4)
			{
				ClickyShapeView clickyShapeView3 = ClickyShapeView.Fill(ClickyShape.RoundedRectangle(3.0), style.Highlighter);
				clickyShapeView3.Margin = new Thickness(-3.0, 2.0, -3.0, 2.0);
				frameworkElement4 = new ClickyInkOverlay(inkTextView).Behind(clickyShapeView3);
			}
			clickyInkHStack4 = InkDashboardLayout.HStack(6.0, InkVerticalAlignment.FirstBaseline, frameworkElement4);
			if (flag4 && clickyInkStatusState2.HasValue)
			{
				ClickyInkStatusState valueOrDefault3 = clickyInkStatusState2.GetValueOrDefault();
				clickyInkHStack4.Children.Add(InkDashboardLayout.Fixed(InkTextView.Plain(StateWord(ClickyInkStatus.Word(text, valueOrDefault3)), ClickyPaperFonts.Rounded(12.0, FontWeights.SemiBold), style.Quiet).With(delegate(InkTextView t)
				{
					t.Strikethrough = style.Fresh;
				})));
			}
			clickyInkHStack4.HorizontalAlignment = HorizontalAlignment.Left;
			list.Add(clickyInkHStack4);
			break;
		}
		return list;
	}
}
