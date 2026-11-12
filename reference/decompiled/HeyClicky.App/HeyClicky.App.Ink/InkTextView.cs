using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Text;

namespace HeyClicky.App.Ink;

public sealed class InkTextView : Panel, IInkBaseline
{
	private readonly record struct GlyphBox(double Left, double Right, double Top, double Bottom, double Baseline);

	private readonly TextBlock block = new TextBlock
	{
		TextWrapping = TextWrapping.Wrap,
		IsTextScaleFactorEnabled = false,
		LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
		OpticalMarginAlignment = OpticalMarginAlignment.None,
		TextLineBounds = TextLineBounds.Full
	};

	private readonly Canvas behind = new Canvas
	{
		IsHitTestVisible = false
	};

	private readonly Canvas overlay = new Canvas
	{
		IsHitTestVisible = false
	};

	private readonly List<Run> runs = new List<Run>();

	private ClickyInkTextContent content;

	private PaperFont font;

	private bool dirty = true;

	private double appliedSize;

	private string overlayKey = "";

	public ClickyInkTextContent Content
	{
		get
		{
			return content;
		}
		set
		{
			content = value;
			dirty = true;
			InvalidateMeasure();
		}
	}

	public PaperFont Font
	{
		get
		{
			return font;
		}
		set
		{
			font = value;
			dirty = true;
			InvalidateMeasure();
		}
	}

	public Color? Foreground { get; set; }

	public int? LineLimit { get; set; }

	public double LineSpacing { get; set; }

	public Color? Strikethrough { get; set; }

	public double Tracking { get; set; }

	public bool Uppercase { get; set; }

	public bool Italic { get; set; }

	public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;

	public double MinimumScale { get; set; } = 1.0;

	public bool MonospacedDigit { get; set; }

	public bool FillsWidth { get; set; }

	public double? FirstBaseline
	{
		get
		{
			if (!(block.BaselineOffset > 0.0))
			{
				return null;
			}
			return block.BaselineOffset;
		}
	}

	public InkTextView(ClickyInkTextContent content, PaperFont font, Color? foreground = null)
	{
		this.content = content;
		this.font = font;
		Foreground = foreground;
		base.Children.Add(behind);
		base.Children.Add(block);
		base.Children.Add(overlay);
		AutomationProperties.SetName(this, content.PlainText);
	}

	public static InkTextView Plain(string text, PaperFont font, Color color)
	{
		return new InkTextView(ClickyInkTextContent.Plain(text, null), font, color);
	}

	public InkTextView With(Action<InkTextView> configure)
	{
		configure(this);
		dirty = true;
		return this;
	}

	private void Build(double size)
	{
		appliedSize = size;
		block.Inlines.Clear();
		runs.Clear();
		block.FontFamily = font.Family;
		block.FontWeight = font.Weight;
		block.FontStyle = (Italic ? FontStyle.Italic : FontStyle.Normal);
		block.FontSize = size;
		block.TextAlignment = TextAlignment;
		block.LineHeight = size * 1.193 + LineSpacing;
		block.CharacterSpacing = ((Tracking != 0.0) ? ((int)Math.Round(Tracking * 1000.0 / size)) : 0);
		Typography.SetNumeralAlignment(block, MonospacedDigit ? FontNumeralAlignment.Tabular : FontNumeralAlignment.Normal);
		block.Foreground = Ui.Brush(Foreground ?? PaperColor.Black);
		int? lineLimit = LineLimit;
		if (lineLimit.HasValue)
		{
			int valueOrDefault = lineLimit.GetValueOrDefault();
			block.MaxLines = valueOrDefault;
			block.TextTrimming = TextTrimming.CharacterEllipsis;
			if (valueOrDefault == 1)
			{
				block.TextWrapping = TextWrapping.NoWrap;
			}
		}
		else
		{
			block.MaxLines = 0;
			block.TextTrimming = TextTrimming.None;
			block.TextWrapping = TextWrapping.Wrap;
		}
		foreach (ClickyInkRun run2 in content.Runs)
		{
			Run run = new Run
			{
				Text = (Uppercase ? run2.Text.ToUpperInvariant() : run2.Text)
			};
			string symbol = run2.Symbol;
			if (symbol != null)
			{
				run.Text = ClickyInkIcon.Resolve(symbol)?.ToString() ?? "";
				run.FontFamily = DS.Fonts.Symbols;
			}
			Color? foreground = run2.Foreground;
			if (foreground.HasValue)
			{
				Color valueOrDefault2 = foreground.GetValueOrDefault();
				run.Foreground = Ui.Brush(valueOrDefault2);
			}
			if (run2.IsBold)
			{
				run.FontWeight = ((font.Weight.Weight >= FontWeights.Bold.Weight) ? FontWeights.Black : FontWeights.Bold);
			}
			runs.Add(run);
			block.Inlines.Add(run);
		}
		overlayKey = "";
		dirty = false;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		double size = font.Size;
		if (dirty || appliedSize <= 0.0)
		{
			Build(size);
		}
		behind.Measure(new Size(0f, 0f));
		overlay.Measure(new Size(0f, 0f));
		if (MinimumScale < 1.0 && LineLimit == 1 && !double.IsInfinity(availableSize.Width))
		{
			if (appliedSize != size)
			{
				Build(size);
			}
			block.Measure(new Size(double.PositiveInfinity, availableSize.Height));
			double width = block.DesiredSize.Width;
			if (width > availableSize.Width && width > 0.0)
			{
				double num = size * Math.Max(MinimumScale, availableSize.Width / width);
				Build(Math.Floor(num * 10.0) / 10.0);
			}
		}
		block.Measure(availableSize);
		Size desiredSize = block.DesiredSize;
		return new Size((FillsWidth && !double.IsInfinity(availableSize.Width)) ? availableSize.Width : desiredSize.Width, desiredSize.Height);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		behind.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
		block.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
		overlay.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
		DrawMarks();
		return finalSize;
	}

	private List<GlyphBox>? Glyphs(int index, double baselineInLine)
	{
		try
		{
			Run run = runs[index];
			int length = run.Text.Length;
			if (length == 0)
			{
				return new List<GlyphBox>();
			}
			List<GlyphBox> list = new List<GlyphBox>(length);
			for (int i = 0; i < length; i++)
			{
				TextPointer positionAtOffset = run.ContentStart.GetPositionAtOffset(i, LogicalDirection.Forward);
				if ((object)positionAtOffset == null)
				{
					return null;
				}
				Rect characterRect = positionAtOffset.GetCharacterRect(LogicalDirection.Forward);
				double right = ((characterRect.Width > 0.0) ? (characterRect.X + characterRect.Width) : characterRect.X);
				if (characterRect.Width <= 0.0 && i + 1 <= length)
				{
					Rect? rect = ((i + 1 < length) ? run.ContentStart.GetPositionAtOffset(i + 1, LogicalDirection.Forward) : run.ContentEnd)?.GetCharacterRect((i + 1 < length) ? LogicalDirection.Forward : LogicalDirection.Backward);
					if (rect.HasValue)
					{
						Rect valueOrDefault = rect.GetValueOrDefault();
						if (Math.Abs(valueOrDefault.Y - characterRect.Y) < 0.5)
						{
							right = valueOrDefault.X;
						}
					}
				}
				list.Add(new GlyphBox(characterRect.X, right, characterRect.Y, characterRect.Y + characterRect.Height, characterRect.Y + baselineInLine));
			}
			return list;
		}
		catch
		{
			return null;
		}
	}

	private static List<GlyphBox> Lines(List<GlyphBox> glyphs, Func<int, bool>? include = null)
	{
		List<GlyphBox> list = new List<GlyphBox>();
		for (int i = 0; i < glyphs.Count; i++)
		{
			if (include != null && !include(i))
			{
				continue;
			}
			GlyphBox item = glyphs[i];
			if (list.Count > 0)
			{
				if (Math.Abs(list[list.Count - 1].Baseline - item.Baseline) < 0.5)
				{
					if (item.Left >= list[list.Count - 1].Left - 0.5)
					{
						int index = list.Count - 1;
						GlyphBox value = list[list.Count - 1];
						value.Right = Math.Max(list[list.Count - 1].Right, item.Right);
						list[index] = value;
						continue;
					}
				}
			}
			list.Add(item);
		}
		return list;
	}

	private void DrawMarks()
	{
		if (!Strikethrough.HasValue && content.PenRun < 0 && !content.Runs.Any((ClickyInkRun r) => r.Strike.HasValue || r.IsLink || r.Background.HasValue))
		{
			if (overlayKey.Length > 0)
			{
				behind.Children.Clear();
				overlay.Children.Clear();
				overlayKey = "";
			}
			return;
		}
		double num = appliedSize;
		double num2 = 0.0;
		try
		{
			if (runs.Count > 0)
			{
				num2 = runs[0].ContentStart.GetCharacterRect(LogicalDirection.Forward).Y;
			}
		}
		catch
		{
		}
		double baselineInLine = block.BaselineOffset - num2;
		List<(PaperPath, Color)> list = new List<(PaperPath, Color)>();
		List<(PaperPath, PaperStroke)> list2 = new List<(PaperPath, PaperStroke)>();
		StringBuilder stringBuilder = new StringBuilder();
		(double, double, double)? tuple = null;
		for (int j = 0; j < runs.Count && j < content.Runs.Count; j++)
		{
			ClickyInkRun r2 = content.Runs[j];
			Color? color = r2.Strike ?? Strikethrough;
			bool flag = j == content.PenRun && content.Pen.HasValue;
			if (!color.HasValue && !r2.IsLink && !r2.Background.HasValue && !flag)
			{
				continue;
			}
			List<GlyphBox> list3 = Glyphs(j, baselineInLine);
			if (list3 == null || list3.Count <= 0)
			{
				continue;
			}
			foreach (GlyphBox item in list3)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
				handler.AppendFormatted(item.Left, "0.#");
				handler.AppendLiteral(",");
				handler.AppendFormatted(item.Baseline, "0.#");
				handler.AppendLiteral(";");
				stringBuilder2.Append(ref handler);
			}
			Color? background = r2.Background;
			if (background.HasValue)
			{
				Color valueOrDefault = background.GetValueOrDefault();
				foreach (GlyphBox item2 in Lines(list3))
				{
					list.Add((new PaperPath().AddRect(new Rect(item2.Left, item2.Top, Math.Max(0.0, item2.Right - item2.Left), item2.Bottom - item2.Top)), valueOrDefault));
				}
			}
			if (color.HasValue)
			{
				Color valueOrDefault2 = color.GetValueOrDefault();
				foreach (GlyphBox item3 in Lines(list3, (int i) => i < r2.Text.TrimEnd(' ').Length))
				{
					double y = item3.Baseline - num * 0.26;
					list2.Add((new PaperPath().MoveTo(item3.Left, y).LineTo(item3.Right, y), new PaperStroke(valueOrDefault2, Math.Max(1.0, num * 0.05))));
				}
			}
			if (r2.IsLink)
			{
				background = r2.LinkTint;
				if (background.HasValue)
				{
					Color valueOrDefault3 = background.GetValueOrDefault();
					foreach (GlyphBox item4 in Lines(list3))
					{
						PaperPath paperPath = LinkUnderline(item4, num);
						if (paperPath != null)
						{
							list2.Add((paperPath, new PaperStroke(valueOrDefault3.Opacity(0.55), 1.25, PenLineCap.Round, PenLineJoin.Round)));
						}
					}
				}
			}
			if (flag)
			{
				GlyphBox glyphBox = list3[0];
				double num3 = list3.Max((GlyphBox x) => x.Right);
				tuple = ((content.Pen.Value.Wet > 0.0) ? num3 : glyphBox.Left, glyphBox.Baseline, num * 0.9521484375);
			}
		}
		string text = stringBuilder.ToString() + "|" + content.Pen.ToString() + content.PenClock;
		if (text == overlayKey)
		{
			return;
		}
		overlayKey = text;
		behind.Children.Clear();
		overlay.Children.Clear();
		foreach (var (paperPath2, c) in list)
		{
			behind.Children.Add(new Path
			{
				Data = paperPath2.ToGeometry(),
				Fill = Ui.Brush(c)
			});
		}
		foreach (var (paperPath3, paperStroke) in list2)
		{
			overlay.Children.Add(new Path
			{
				Data = paperPath3.ToGeometry(),
				Stroke = Ui.Brush(paperStroke.Color),
				StrokeThickness = paperStroke.Width,
				StrokeStartLineCap = paperStroke.Cap,
				StrokeEndLineCap = paperStroke.Cap,
				StrokeLineJoin = paperStroke.Join
			});
		}
		if (tuple.HasValue)
		{
			(double, double, double) valueOrDefault4 = tuple.GetValueOrDefault();
			ClickyInkPenAttribute? pen = content.Pen;
			if (pen.HasValue)
			{
				ClickyInkPenAttribute valueOrDefault5 = pen.GetValueOrDefault();
				overlay.Children.Add(Pencil(valueOrDefault5, valueOrDefault4.Item1, valueOrDefault4.Item2, valueOrDefault4.Item3, content.PenClock));
			}
		}
	}

	private static PaperPath? LinkUnderline(GlyphBox line, double size)
	{
		double num = line.Right - line.Left;
		if (num <= 2.0)
		{
			return null;
		}
		double num2 = line.Baseline + Math.Max(1.6, size * 0.2412109375 * 0.42);
		double num3 = line.Baseline - size * 0.9521484375;
		double num4 = (int)(line.Left * 7.0 + num3 * 3.0) % 97;
		double num5 = line.Left + 0.5;
		double num6 = line.Right - 0.5;
		PaperPath paperPath = new PaperPath().MoveTo(num5, num2 + 0.3);
		int num7 = Math.Max(2, (int)(num / 18.0));
		for (int i = 1; i <= num7; i++)
		{
			double num8 = (double)i / (double)num7;
			double x = num5 + (num6 - num5) * num8;
			double num9 = Math.Sin(num4 + num8 * 7.3) * 0.55 + (num8 - 0.5) * 0.5;
			paperPath.LineTo(x, num2 + num9);
		}
		return paperPath;
	}

	private static FrameworkElement Pencil(ClickyInkPenAttribute pen, double edge, double baseline, double ascent, double clock)
	{
		double num = Math.Max(9.0, ascent * 0.85);
		double num2 = edge + 0.5;
		double num3 = Math.Sin(clock * 2.0 * Math.PI * 4.5) * 0.7;
		FrameworkElement frameworkElement = ClickyInkPencilGlyph.Make(num, pen.Tint);
		frameworkElement.Opacity = 0.62;
		Canvas.SetLeft(frameworkElement, num2 - num * 0.06);
		Canvas.SetTop(frameworkElement, baseline - ascent * 0.18 + num3 - num * 0.94);
		frameworkElement.RenderTransform = new RotateTransform
		{
			Angle = Math.Sin(clock * 2.0 * Math.PI * 2.3) * 2.5,
			CenterX = num * 0.06,
			CenterY = num * 0.94
		};
		return frameworkElement;
	}
}
