using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public sealed class ClickyNoteView : Canvas
{
	public static readonly Size SheetSize = new Size(240f, 176f);

	public static readonly Thickness ShadowInsets = new Thickness(6.0, 5.0, 6.0, 9.0);

	public static readonly double[] Rules = new double[4] { 52.0, 78.0, 104.0, 130.0 };

	private static readonly Color NoteInk = ClickyPaperStock.Cream.Ink();

	private readonly ClickyNoteFace face;

	private readonly Action<string> onAction;

	private readonly Canvas sheet = new Canvas
	{
		Width = SheetSize.Width,
		Height = SheetSize.Height
	};

	public static Size BoxSize => new Size(SheetSize.Width + ShadowInsets.Left + ShadowInsets.Right, SheetSize.Height + ShadowInsets.Top + ShadowInsets.Bottom);

	private double FooterWidth
	{
		get
		{
			if (face.Stamp.HasValue && !IsStampMakingRoomForActions)
			{
				return StampedFooterWidth;
			}
			return SheetSize.Width - 28.0;
		}
	}

	private double StampedFooterWidth
	{
		get
		{
			ClickyThingStatus? stamp = face.Stamp;
			if (stamp.HasValue)
			{
				ClickyThingStatus valueOrDefault = stamp.GetValueOrDefault();
				double width = ClickyPaperStamp.SizeOf(valueOrDefault).Width;
				return SheetSize.Width - 14.0 - 12.0 - width - 10.0;
			}
			return SheetSize.Width - 28.0;
		}
	}

	private bool IsStampMakingRoomForActions
	{
		get
		{
			if (face.Stamp.HasValue && face.Footer is ClickyNoteFace.NoteFooter.Actions actions)
			{
				ClickyNoteFace.NoteAction[] items = actions.Items;
				if (items != null && items.Length > 0)
				{
					PaperFont font = ClickyPaperFonts.Typed(12.0, FontWeights.SemiBold);
					return actions.Items.Sum((ClickyNoteFace.NoteAction a) => Math.Ceiling(ClickyHandLineBreaker.Width(a.Title, font)) + 24.0) + (double)((actions.Items.Length - 1) * 6) > StampedFooterWidth;
				}
			}
			return false;
		}
	}

	public ClickyNoteView(ClickyNoteFace face, Action<string>? onAction = null)
	{
		this.face = face;
		this.onAction = onAction ?? ((Action<string>)delegate
		{
		});
		base.Width = BoxSize.Width;
		base.Height = BoxSize.Height;
		Canvas.SetLeft(sheet, ShadowInsets.Left);
		Canvas.SetTop(sheet, ShadowInsets.Top);
		base.Children.Add(sheet);
		Paper();
		Content();
	}

	private void At(FrameworkElement e, double x, double y)
	{
		Canvas.SetLeft(e, x);
		Canvas.SetTop(e, y);
		sheet.Children.Add(e);
	}

	private void Paper()
	{
		ClickyNoteShape shape = new ClickyNoteShape(!face.IsIdea);
		PaperFill fill = (face.IsIdea ? PaperFill.Vertical((0.0, PaperColor.Hex("#FFFDF6")), (1.0, PaperColor.Hex("#FCF8EE"))) : PaperFill.Vertical((0.0, PaperColor.Hex("#FFFCF3")), (0.4, PaperColor.Hex("#FBF7EC")), (1.0, PaperColor.Hex("#F7F2E4"))));
		PaperSurface paperSurface = new PaperSurface(shape, fill, face.IsIdea ? 0.35 : 0.42)
		{
			Width = SheetSize.Width,
			Height = SheetSize.Height
		}.WithShadow(PaperColor.Hex("#0A101E").Opacity(face.IsIdea ? 0.12 : 0.16), 0.5, 0.0, 1.0).WithShadow(PaperColor.Hex("#0A1428").Opacity(face.IsIdea ? 0.12 : 0.15), 6.0, 0.0, 7.0);
		if (!face.IsIdea)
		{
			paperSurface.WithWash(PaperFill.Evenly(new Point(0.5, 0.0), new Point(0.5, 1.0), PaperColor.Hex("#78643C").Opacity(0.08), PaperColor.Clear), (Size s) => new Rect(0.0, 0.0, s.Width, 7.0), clipped: false);
		}
		At(paperSurface, 0.0, 0.0);
		double[] rules = Rules;
		foreach (double y in rules)
		{
			At(new Border
			{
				Width = SheetSize.Width,
				Height = 1.0,
				Background = Ui.Brush(ClickyPaperInk.NoteRule)
			}, 0.0, y);
		}
		if (face.IsIdea)
		{
			ClickyShapeView clickyShapeView = ClickyShapeView.StrokeBorder(ClickyShape.RoundedRectangle(3.0), new PaperStroke(PaperColor.Hex("#E0A030").Opacity(0.85), 1.6, PenLineCap.Flat, PenLineJoin.Miter, new double[2] { 5.0, 3.5 }));
			clickyShapeView.Width = SheetSize.Width - 8.0;
			clickyShapeView.Height = SheetSize.Height - 8.0;
			At(clickyShapeView, 4.0, 4.0);
		}
	}

	private void Content()
	{
		At(TitleRow(), 16.0, 15.0);
		string meta = face.Meta;
		if (meta != null)
		{
			PaperFont font = ClickyPaperFonts.Typed(10.5, FontWeights.Medium);
			double num = ClickyHandLineBreaker.Ascent(font);
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5.0,
				MaxWidth = SheetSize.Width - 32.0
			};
			ClickyPaperGlyph? metaLogo = face.MetaLogo;
			if (metaLogo.HasValue)
			{
				ClickyPaperGlyph valueOrDefault = metaLogo.GetValueOrDefault();
				stackPanel.Children.Add(new ClickyPaperGlyphView(valueOrDefault, PaperColor.Hex("#E5483B"), 11.0, 2.0)
				{
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0.0, num - 10.0, 0.0, 0.0)
				});
			}
			stackPanel.Children.Add(PaperText.Line(meta, font, face.MetaIsClicky ? ClickyPaperInk.ClickyBlue : NoteInk.Opacity(0.55)));
			sheet.Children.Add(new ClickyBaselinePlaced(stackPanel, 16.0, Rules[0] - 5.0, num));
		}
		ClickyNoteFace.NoteBody body = face.Body;
		if (!(body is ClickyNoteFace.NoteBody.Lines lines))
		{
			if (body is ClickyNoteFace.NoteBody.Chart chart)
			{
				ChartView(chart.Series, chart.ShowsTrend);
			}
		}
		else
		{
			LinesView(lines.Items);
		}
		FrameworkElement frameworkElement = FooterView();
		if ((object)frameworkElement != null)
		{
			Grid grid = new Grid
			{
				Width = FooterWidth,
				Height = 26.0
			};
			grid.Children.Add(frameworkElement);
			grid.Clip = new RectangleGeometry
			{
				Rect = new Rect(0.0, 0.0, FooterWidth, 26.0)
			};
			At(grid, 14.0, SheetSize.Height - 11.0 - 26.0);
		}
		ClickyThingStatus? stamp = face.Stamp;
		if (stamp.HasValue)
		{
			ClickyThingStatus valueOrDefault2 = stamp.GetValueOrDefault();
			if (!IsStampMakingRoomForActions)
			{
				ClickyPaperStamp clickyPaperStamp = new ClickyPaperStamp(valueOrDefault2);
				At(clickyPaperStamp, SheetSize.Width - 12.0 - clickyPaperStamp.StampSize.Width, SheetSize.Height - 13.0 - clickyPaperStamp.StampSize.Height);
			}
		}
	}

	public static List<(string Label, ClickyPaperFileKind Kind)> DistinctFileKinds(IEnumerable<string> names)
	{
		HashSet<string> hashSet = new HashSet<string>();
		List<(string, ClickyPaperFileKind)> list = new List<(string, ClickyPaperFileKind)>();
		foreach (string name in names)
		{
			string text = ClickyPaperFileKinds.ShortLabel(name);
			if (hashSet.Add(text))
			{
				list.Add((text, ClickyPaperFileKinds.ForFileName(name)));
			}
		}
		return list;
	}

	public static int FileChipsThatFit(IReadOnlyList<string> labels, double width)
	{
		PaperFont font = ClickyPaperFonts.Typed(11.0, FontWeights.SemiBold);
		double num = 0.0;
		int num2 = 0;
		foreach (var item3 in labels.Take(3).Select((string l, int i) => (l: l, i: i)))
		{
			string item = item3.l;
			int item2 = item3.i;
			double num3 = 32.0 + Math.Ceiling(ClickyHandLineBreaker.Width(item, font));
			double num4 = ((num2 > 0) ? 6 : 0);
			double num5 = ((item2 < labels.Count - 1) ? 30 : 0);
			if (num + num4 + num3 + num5 > width)
			{
				break;
			}
			num += num4 + num3;
			num2++;
		}
		return Math.Max(Math.Min(1, labels.Count), num2);
	}

	private FrameworkElement TitleRow()
	{
		StackPanel obj = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 7.0,
			Height = 21.0,
			MaxWidth = SheetSize.Width - 32.0,
			Children = { (UIElement)new ClickyPaperGlyphView(face.Glyph, NoteInk.Opacity(0.82))
			{
				VerticalAlignment = VerticalAlignment.Center
			} }
		};
		TextBlock textBlock = PaperText.Line(face.Title, ClickyPaperFonts.Hand(21.0), NoteInk);
		textBlock.Margin = new Thickness(0.0, 1.0, 0.0, 0.0);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.MaxWidth = SheetSize.Width - 32.0 - 24.0;
		obj.Children.Add(textBlock);
		return obj;
	}

	private void LinesView(ClickyNoteFace.NoteLine[] lines)
	{
		bool num = lines.Any((ClickyNoteFace.NoteLine l) => !(l.Mark is ClickyNoteFace.LineMark.None));
		double num2 = (num ? 18 : 19);
		double num3 = (num ? 36 : 16);
		PaperFont font = ClickyPaperFonts.Hand(num2);
		double num4 = ClickyHandLineBreaker.Ascent(font);
		for (int i = 0; i < Math.Min(3, lines.Length); i++)
		{
			ClickyNoteFace.NoteLine noteLine = lines[i];
			double num5 = Rules[i + 1];
			if (noteLine.IsCircled)
			{
				ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyHandRing(i + 3), new PaperStroke(ClickyPaperInk.DoneGreen, 1.8, PenLineCap.Round));
				clickyShapeView.Width = 100.0;
				clickyShapeView.Height = 28.0;
				At(clickyShapeView, num3 - 9.0, num5 - 24.0);
			}
			MarkView(noteLine.Mark, num5);
			(List<ClickyNoteFace.LineRun> Before, List<ClickyNoteFace.LineRun> After) tuple = CaretSplit(noteLine);
			List<ClickyNoteFace.LineRun> item = tuple.Before;
			List<ClickyNoteFace.LineRun> item2 = tuple.After;
			double num6 = num2 - 1.0;
			double num7 = (noteLine.HasCaret ? Math.Max(num4, num6 * 0.82) : num4);
			double maxWidth = SheetSize.Width - num3 - (double)((noteLine.Value == null) ? 14 : 60);
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				MaxWidth = maxWidth
			};
			TextBlock textBlock = LineText(item, noteLine, font);
			textBlock.Margin = new Thickness(0.0, num7 - num4, 0.0, 0.0);
			FrameworkElement frameworkElement = Ui.Shrink(textBlock, HorizontalAlignment.Left, 0.86);
			frameworkElement.MaxWidth = maxWidth;
			stackPanel.Children.Add(frameworkElement);
			if (noteLine.HasCaret)
			{
				stackPanel.Children.Add(new ClickyWritingCaret(num6)
				{
					Margin = new Thickness(2.0, num7 - num6 * 0.82, 2.0, 0.0),
					VerticalAlignment = VerticalAlignment.Top
				});
			}
			if (item2.Count > 0)
			{
				TextBlock textBlock2 = LineText(item2, noteLine, font);
				textBlock2.Margin = new Thickness(0.0, num7 - num4, 0.0, 0.0);
				stackPanel.Children.Add(textBlock2);
			}
			sheet.Children.Add(new ClickyBaselinePlaced(stackPanel, num3, num5 - 2.0, num7));
			string value = noteLine.Value;
			if (value != null)
			{
				PaperFont font2 = ClickyPaperFonts.Rounded(13.0, FontWeights.SemiBold);
				TextBlock textBlock3 = PaperText.Line(value, font2, NoteInk, TextAlignment.Right);
				Typography.SetNumeralAlignment(textBlock3, FontNumeralAlignment.Tabular);
				textBlock3.Width = SheetSize.Width - 18.0;
				sheet.Children.Add(new ClickyBaselinePlaced(textBlock3, 0.0, num5 - 3.0, ClickyHandLineBreaker.Ascent(font2)));
			}
		}
	}

	public static (List<ClickyNoteFace.LineRun> Before, List<ClickyNoteFace.LineRun> After) CaretSplit(ClickyNoteFace.NoteLine line)
	{
		ClickyNoteFace.LineRun[] runs = line.Runs;
		if (runs == null || runs.Length <= 0)
		{
			return (Before: new List<ClickyNoteFace.LineRun>(), After: new List<ClickyNoteFace.LineRun>());
		}
		int num = Array.FindLastIndex(runs, (ClickyNoteFace.LineRun r) => r.IsClicky);
		if (!line.HasCaret || num < 0)
		{
			return (Before: runs.ToList(), After: new List<ClickyNoteFace.LineRun>());
		}
		return (Before: runs[..(num + 1)].ToList(), After: runs[(num + 1)..].ToList());
	}

	private TextBlock LineText(List<ClickyNoteFace.LineRun> runs, ClickyNoteFace.NoteLine line, PaperFont font)
	{
		TextBlock textBlock = font.Apply(new TextBlock
		{
			TextWrapping = TextWrapping.NoWrap,
			TextTrimming = TextTrimming.CharacterEllipsis
		});
		if (runs.Count == 0)
		{
			textBlock.Text = line.Text;
			textBlock.Foreground = Ui.Brush(LineColor(line));
			return textBlock;
		}
		foreach (ClickyNoteFace.LineRun run in runs)
		{
			textBlock.Inlines.Add(new Run
			{
				Text = run.Text,
				Foreground = Ui.Brush(run.IsClicky ? ClickyPaperInk.ClickyBlue : NoteInk)
			});
		}
		return textBlock;
	}

	private static Color LineColor(ClickyNoteFace.NoteLine line)
	{
		ClickyNoteFace.LineMark mark = line.Mark;
		ClickyNoteFace.LineTone tone = line.Tone;
		if (!(mark is ClickyNoteFace.LineMark.Now) && tone != ClickyNoteFace.LineTone.Clicky)
		{
			if (mark is ClickyNoteFace.LineMark.Todo || tone == ClickyNoteFace.LineTone.Quiet)
			{
				return PaperColor.Hex("#A3A7B3");
			}
			return NoteInk;
		}
		return ClickyPaperInk.ClickyBlue;
	}

	private void MarkView(ClickyNoteFace.LineMark mark, double ruleY)
	{
		if (!(mark is ClickyNoteFace.LineMark.Done))
		{
			if (!(mark is ClickyNoteFace.LineMark.Now))
			{
				if (!(mark is ClickyNoteFace.LineMark.Todo))
				{
					if (mark is ClickyNoteFace.LineMark.Number number)
					{
						PaperFont font = ClickyPaperFonts.Hand(18.0);
						sheet.Children.Add(new ClickyBaselinePlaced(PaperText.Line($"{number.Value}", font, NoteInk), 16.0, ruleY - 2.0, ClickyHandLineBreaker.Ascent(font)));
					}
				}
				else
				{
					ClickyShapeView clickyShapeView = ClickyShapeView.Stroke(new ClickyWobblyRect((int)ruleY), PaperColor.Hex("#A3A7B3"), 1.6);
					double width = (clickyShapeView.Height = 12.0);
					clickyShapeView.Width = width;
					At(clickyShapeView, 18.0, ruleY - 15.0);
				}
			}
			else
			{
				sheet.Children.Add(new PaperDrawing().Stroke(new PaperPath().AddArc(new Point(23.5, ruleY - 8.5), 5.0, -Math.PI / 2.0, 3.490658503988659, clockwise: false), new PaperStroke(ClickyPaperInk.ClickyBlue, 2.0, PenLineCap.Round)));
			}
		}
		else
		{
			sheet.Children.Add(new PaperDrawing().Stroke(new PaperPath().MoveTo(18.0, ruleY - 8.0).LineTo(22.0, ruleY - 4.0).LineTo(29.0, ruleY - 13.0), new PaperStroke(ClickyPaperInk.DoneGreen, 2.0, PenLineCap.Round, PenLineJoin.Round)));
		}
	}

	private void ChartView(double[] series, bool showsTrend)
	{
		Rect frame = new Rect(16f, 52f, 208f, 80f);
		PaperDrawing paperDrawing = new PaperDrawing();
		paperDrawing.Stroke(new PaperPath().MoveTo(frame.X + 4.0, frame.Y + 6.0).LineTo(frame.X + 4.0, frame.Y + 70.0).LineTo(frame.Right - 4.0, frame.Y + 70.0), new PaperStroke(NoteInk.Opacity(0.35), 1.4, PenLineCap.Round));
		if (series.Length > 1)
		{
			double low = series.Min();
			double num = series.Max();
			double span = Math.Max(num - low, 0.0001);
			List<Point> list = series.Select((double value, int index) => new Point(frame.X + 10.0 + (double)index / (double)(series.Length - 1) * (frame.Width - 22.0), frame.Y + 62.0 - (value - low) / span * 46.0)).ToList();
			paperDrawing.Stroke(new PaperPath().AddLines(list), new PaperStroke(PaperColor.Hex("#2E9E62"), 2.2, PenLineCap.Round, PenLineJoin.Round));
			if (showsTrend)
			{
				paperDrawing.Stroke(new PaperPath().MoveTo(list[0].X, list[0].Y + 4.0).LineTo(list[list.Count - 1].X + 2.0, list[list.Count - 1].Y - 6.0), new PaperStroke(ClickyPaperInk.ClickyBlue, 2.0, PenLineCap.Round, PenLineJoin.Miter, new double[2] { 6.0, 5.0 }));
			}
		}
		sheet.Children.Add(paperDrawing);
	}

	private FrameworkElement? FooterView()
	{
		ClickyNoteFace.NoteFooter footer = face.Footer;
		if (!(footer is ClickyNoteFace.NoteFooter.Actions actions))
		{
			if (!(footer is ClickyNoteFace.NoteFooter.Files files))
			{
				if (!(footer is ClickyNoteFace.NoteFooter.Progress progress))
				{
					if (footer is ClickyNoteFace.NoteFooter.Text text)
					{
						TextBlock textBlock = PaperText.Line(text.Words, ClickyPaperFonts.Typed(10.5, FontWeights.Medium), NoteInk.Opacity(0.55));
						textBlock.VerticalAlignment = VerticalAlignment.Center;
						return new Grid
						{
							Height = 26.0,
							Children = { (UIElement)textBlock }
						};
					}
					return null;
				}
				StackPanel obj = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Spacing = 7.0,
					Height = 26.0,
					Children = { (UIElement)new ClickyPaperProgressBar(progress.Fraction, 74.0, 4.0, null, null)
					{
						VerticalAlignment = VerticalAlignment.Center
					} }
				};
				TextBlock textBlock2 = PaperText.Line($"{progress.Step} of {progress.Total}", ClickyPaperFonts.Typed(10.5, FontWeights.Medium), NoteInk.Opacity(0.6));
				Typography.SetNumeralAlignment(textBlock2, FontNumeralAlignment.Tabular);
				textBlock2.VerticalAlignment = VerticalAlignment.Center;
				obj.Children.Add(textBlock2);
				return obj;
			}
			List<(string, ClickyPaperFileKind)> list = DistinctFileKinds(files.Names);
			int num = FileChipsThatFit(list.Select<(string, ClickyPaperFileKind), string>(((string Label, ClickyPaperFileKind Kind) k) => k.Label).ToList(), FooterWidth);
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6.0,
				Height = 26.0
			};
			foreach (var item in list.Take(num))
			{
				stackPanel.Children.Add(new ClickyPaperFileChip(item.Item1, item.Item2)
				{
					VerticalAlignment = VerticalAlignment.Center
				});
			}
			if (list.Count > num)
			{
				TextBlock textBlock3 = PaperText.Line($"+{list.Count - num}", ClickyPaperFonts.Typed(10.5, FontWeights.SemiBold), NoteInk.Opacity(0.55));
				textBlock3.VerticalAlignment = VerticalAlignment.Center;
				stackPanel.Children.Add(textBlock3);
			}
			return stackPanel;
		}
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 6.0,
			Height = 26.0
		};
		ClickyNoteFace.NoteAction[] items = actions.Items;
		foreach (ClickyNoteFace.NoteAction action in items)
		{
			stackPanel2.Children.Add(new ClickyPaperGelButton(action.Title, delegate
			{
				onAction(action.Id);
			}, action.Gel, 26.0, 12.0));
		}
		return stackPanel2;
	}
}
