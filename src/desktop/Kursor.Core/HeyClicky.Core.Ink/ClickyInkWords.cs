using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI;

namespace HeyClicky.Core.Ink;

public static class ClickyInkWords
{
	private sealed class Builder(ClickyInkStyle style)
	{
		private enum Target
		{
			Before,
			PenGlyph,
			After
		}

		private List<ClickyInkRun> before = new List<ClickyInkRun>();

		private List<ClickyInkRun> penGlyph = new List<ClickyInkRun>();

		private readonly List<ClickyInkRun> after = new List<ClickyInkRun>();

		private ClickyInkPenAttribute? pen;

		private bool isBold;

		private bool isHighlighted;

		private bool isLink;

		private Target target;

		private bool HasPen
		{
			get
			{
				ClickyInkPenAttribute? clickyInkPenAttribute = pen;
				return clickyInkPenAttribute.HasValue;
			}
		}

		public void Add(ClickyInkRun run)
		{
			string text = run.Text;
			bool flag = false;
			string text2 = text;
			for (int i = 0; i < text2.Length; i++)
			{
				if (ClickyInkParser.Emphasis.IsMarker(text2[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag && !isBold && !isHighlighted && !isLink)
			{
				Append(run);
				return;
			}
			StringBuilder piece = new StringBuilder();
			text2 = text;
			foreach (char c in text2)
			{
				switch (c)
				{
				case '\ue001':
					Flush();
					isBold = true;
					break;
				case '\ue002':
					Flush();
					isBold = false;
					break;
				case '\ue003':
					Flush();
					isHighlighted = true;
					break;
				case '\ue004':
					Flush();
					isHighlighted = false;
					break;
				case '\ue005':
					Flush();
					isLink = true;
					break;
				case '\ue006':
					Flush();
					isLink = false;
					break;
				default:
					piece.Append(c);
					break;
				}
			}
			Flush();
			void Flush()
			{
				if (piece.Length != 0)
				{
					ClickyInkRun clickyInkRun = run with
					{
						Text = piece.ToString()
					};
					if (isBold)
					{
						clickyInkRun = clickyInkRun with
						{
							IsBold = true
						};
					}
					if (isHighlighted && !clickyInkRun.IsClear)
					{
						clickyInkRun = clickyInkRun with
						{
							Background = style.Highlighter
						};
					}
					if (isLink)
					{
						clickyInkRun = StyleAsLink(clickyInkRun);
					}
					Append(clickyInkRun);
					piece.Clear();
				}
			}
		}

		private ClickyInkRun StyleAsLink(ClickyInkRun run)
		{
			Color? foreground = run.Foreground;
			if (foreground.HasValue)
			{
				Color valueOrDefault = foreground.GetValueOrDefault();
				if (!(valueOrDefault != style.Ink) || !(valueOrDefault != style.Fresh))
				{
					return run with
					{
						Foreground = style.Fresh,
						IsLink = true,
						LinkTint = style.Fresh
					};
				}
			}
			return run;
		}

		private void Append(ClickyInkRun run)
		{
			switch (target)
			{
			case Target.Before:
				before.Add(run);
				break;
			case Target.PenGlyph:
				penGlyph.Add(run);
				break;
			default:
				after.Add(run);
				break;
			}
		}

		public void AddBeingWritten(string text, double progress, Color color, bool pencil, Color pencilTint)
		{
			(int Characters, double Wet) tuple = ClickyInkPen.Revealed(text, progress);
			int item = tuple.Characters;
			double item2 = tuple.Wet;
			string[] array = text.Characters();
			string text2 = string.Concat(array.Take(item));
			int i;
			for (i = 0; i < text2.Length && text2[i] == ' '; i++)
			{
			}
			if (i > 0)
			{
				Add(new ClickyInkRun(text2.Substring(0, i), null, IsBold: false, null, null, IsLink: false, null));
			}
			string text3 = text2;
			int num = i;
			Add(new ClickyInkRun(text3.Substring(num, text3.Length - num), color, IsBold: false, null, null, IsLink: false, null));
			if (item < array.Length)
			{
				string text4 = array[item];
				Color color2 = ((isLink && (color == style.Ink || color == style.Fresh)) ? style.Fresh : color);
				ClickyInkRun run = new ClickyInkRun(text4, (item2 > 0.0) ? Multiply(color2, 0.2 + 0.8 * item2) : ClickyInkRun.Clear, IsBold: false, null, null, IsLink: false, null);
				ClickyInkRun run2 = new ClickyInkRun(string.Concat(array.Skip(item + 1)), ClickyInkRun.Clear, IsBold: false, null, null, IsLink: false, null);
				if (pencil && !HasPen)
				{
					target = Target.PenGlyph;
					Add(run);
					pen = new ClickyInkPenAttribute(item2, pencilTint);
					target = Target.After;
					Add(run2);
				}
				else
				{
					Add(run);
					Add(run2);
				}
			}
		}

		public void AddPoised(string text, Color pencilTint)
		{
			if (HasPen)
			{
				AddClear(text);
			}
			else
			{
				AddBeingWritten(text, 0.0, ClickyInkRun.Clear, pencil: true, pencilTint);
			}
		}

		public void AddClear(string text)
		{
			Add(new ClickyInkRun(text, ClickyInkRun.Clear, IsBold: false, null, null, IsLink: false, null));
		}

		public void AddStruck(string text, double progress, Color color)
		{
			int i;
			for (i = 0; i < text.Length && text[text.Length - 1 - i] == ' '; i++)
			{
			}
			string[] array = text.Substring(0, text.Length - i).Characters();
			int num = (int)Math.Ceiling((double)array.Length * ClickyInkClock.EaseInOut(progress));
			string text2 = string.Concat(array.Take(num));
			Color? foreground = Multiply(style.Quiet, 0.75);
			Color? strike = style.Fresh;
			Add(new ClickyInkRun(text2, foreground, IsBold: false, null, strike, IsLink: false, null));
			if (num < array.Length)
			{
				Add(new ClickyInkRun(string.Concat(array.Skip(num)), color, IsBold: false, null, null, IsLink: false, null));
			}
			if (i > 0)
			{
				Add(new ClickyInkRun(new string(' ', i), null, IsBold: false, null, null, IsLink: false, null));
			}
		}

		public void StrikeEverything(double progress)
		{
			int num = before.Sum((ClickyInkRun r) => r.Text.CharacterCount());
			int num2 = (int)Math.Ceiling((double)(num + penGlyph.Sum((ClickyInkRun r) => r.Text.CharacterCount())) * ClickyInkClock.EaseInOut(progress));
			before = Strike(before, Math.Min(num2, num));
			penGlyph = Strike(penGlyph, Math.Max(0, num2 - num));
		}

		private List<ClickyInkRun> Strike(List<ClickyInkRun> runs, int count)
		{
			if (count <= 0)
			{
				return runs;
			}
			List<ClickyInkRun> list = new List<ClickyInkRun>();
			int num = 0;
			foreach (ClickyInkRun run in runs)
			{
				string[] array = run.Text.Characters();
				int num2 = Math.Max(0, num);
				int num3 = Math.Min(count, num + array.Length);
				if (num2 < num3 && !run.IsClear)
				{
					int num4 = num2 - num;
					int num5 = num3 - num;
					if (num4 > 0)
					{
						list.Add(run with
						{
							Text = string.Concat(array.Take(num4))
						});
					}
					list.Add(run with
					{
						Text = string.Concat(array.Skip(num4).Take(num5 - num4)),
						Foreground = Multiply(style.Quiet, 0.7),
						Strike = style.Fresh,
						IsLink = false,
						LinkTint = null
					});
					if (num5 < array.Length)
					{
						list.Add(run with
						{
							Text = string.Concat(array.Skip(num5))
						});
					}
				}
				else
				{
					list.Add(run);
				}
				num += array.Length;
			}
			return list;
		}

		public ClickyInkTextContent Result()
		{
			ClickyInkTextContent clickyInkTextContent = new ClickyInkTextContent(before);
			ClickyInkPenAttribute? clickyInkPenAttribute = pen;
			if (clickyInkPenAttribute.HasValue)
			{
				ClickyInkPenAttribute valueOrDefault = clickyInkPenAttribute.GetValueOrDefault();
				if (penGlyph.Sum((ClickyInkRun r) => r.Text.Length) > 0)
				{
					clickyInkTextContent.PenRun = clickyInkTextContent.Runs.Count;
					clickyInkTextContent.Pen = valueOrDefault;
					clickyInkTextContent.Runs.Add(new ClickyInkRun(string.Concat(penGlyph.Select((ClickyInkRun r) => r.Text)), penGlyph[0].Foreground, penGlyph[0].IsBold, penGlyph[0].Background, penGlyph[0].Strike, IsLink: false, null));
					goto IL_0146;
				}
			}
			clickyInkTextContent.Runs.AddRange(penGlyph.Select((ClickyInkRun r) => r with
			{
				IsLink = false,
				LinkTint = null
			}));
			goto IL_0146;
			IL_0146:
			clickyInkTextContent.Runs.AddRange(after.Select((ClickyInkRun r) => r with
			{
				IsLink = false,
				LinkTint = null
			}));
			clickyInkTextContent.Runs.RemoveAll((ClickyInkRun r) => r.Text.Length == 0 && r.Symbol == null);
			if (clickyInkTextContent.PenRun >= 0)
			{
				int num = 0;
				int num2 = 0;
				foreach (ClickyInkRun item in before)
				{
					if (item.Text.Length > 0)
					{
						num2++;
					}
				}
				num = num2;
				clickyInkTextContent.PenRun = num;
			}
			return clickyInkTextContent;
		}
	}

	public static ClickyInkTextContent Text(ClickyInkLiveNote.Words words, double clock, ClickyInkStyle style, bool pencil = true, Color? color = null, InkRange? struckOver = null)
	{
		Color color2 = color ?? style.Ink;
		Builder builder = new Builder(style);
		IReadOnlyList<ClickyInkLiveNote.Segment> segments = words.Segments;
		int num = -1;
		for (int num2 = segments.Count - 1; num2 >= 0; num2--)
		{
			ClickyInkLiveNote.Segment segment = segments[num2];
			double? vanishesAt = segment.VanishesAt;
			if (vanishesAt.HasValue)
			{
				double valueOrDefault = vanishesAt.GetValueOrDefault();
				if (clock >= valueOrDefault)
				{
					continue;
				}
			}
			InkRange? written = segment.Written;
			if (!written.HasValue || clock >= written.GetValueOrDefault().LowerBound)
			{
				num = num2;
				break;
			}
		}
		int? num3 = null;
		if (pencil)
		{
			ClickyInkPenSchedule penSchedule = style.PenSchedule;
			if ((object)penSchedule != null && !penSchedule.IsWriting(clock))
			{
				for (int i = 0; i < segments.Count; i++)
				{
					ClickyInkLiveNote.Segment segment2 = segments[i];
					InkRange? written = segment2.Written;
					if (!written.HasValue)
					{
						continue;
					}
					InkRange valueOrDefault2 = written.GetValueOrDefault();
					double? vanishesAt = segment2.VanishesAt;
					if (vanishesAt.HasValue)
					{
						double valueOrDefault3 = vanishesAt.GetValueOrDefault();
						if (clock >= valueOrDefault3)
						{
							continue;
						}
					}
					if (valueOrDefault2.LowerBound > clock && valueOrDefault2.LowerBound <= clock + 0.35)
					{
						num3 = i;
						break;
					}
				}
			}
		}
		for (int j = 0; j < segments.Count; j++)
		{
			ClickyInkLiveNote.Segment segment3 = segments[j];
			double? vanishesAt = segment3.VanishesAt;
			if (vanishesAt.HasValue)
			{
				double valueOrDefault4 = vanishesAt.GetValueOrDefault();
				if (clock >= valueOrDefault4)
				{
					continue;
				}
			}
			bool flag = segment3.Edit.HasValue && segment3.Edit == style.LatestEdit;
			InkRange? written = segment3.Written;
			if (written.HasValue)
			{
				InkRange valueOrDefault5 = written.GetValueOrDefault();
				if (clock < valueOrDefault5.LowerBound)
				{
					if (j == num3)
					{
						builder.AddPoised(segment3.Text, color2);
						continue;
					}
					vanishesAt = words.ReservedUntil;
					if (vanishesAt.HasValue)
					{
						double valueOrDefault6 = vanishesAt.GetValueOrDefault();
						if (valueOrDefault5.LowerBound < valueOrDefault6 && j > num)
						{
							builder.AddClear(segment3.Text);
						}
					}
					continue;
				}
				double num4 = ClickyInkClock.Progress(clock, valueOrDefault5.LowerBound, valueOrDefault5.UpperBound);
				if (num4 < 1.0)
				{
					builder.AddBeingWritten(segment3.Text, num4, flag ? style.Fresh : color2, pencil, color2);
					continue;
				}
			}
			int k;
			for (k = 0; k < segment3.Text.Length && segment3.Text[k] == ' '; k++)
			{
			}
			string text = segment3.Text.Substring(0, k);
			if (text.Length > 0 && segment3.Text.Length > text.Length)
			{
				builder.Add(new ClickyInkRun(text, null, IsBold: false, null, null, IsLink: false, null));
			}
			string text3;
			if (text.Length != 0 && segment3.Text.Length != text.Length)
			{
				string text2 = segment3.Text;
				int length = text.Length;
				text3 = text2.Substring(length, text2.Length - length);
			}
			else
			{
				text3 = segment3.Text;
			}
			string text4 = text3;
			written = segment3.Struck;
			if (written.HasValue)
			{
				InkRange valueOrDefault7 = written.GetValueOrDefault();
				if (clock >= valueOrDefault7.LowerBound)
				{
					builder.AddStruck(text4, ClickyInkClock.Progress(clock, valueOrDefault7.LowerBound, valueOrDefault7.UpperBound), color2);
					continue;
				}
			}
			ClickyInkRun clickyInkRun = new ClickyInkRun(text4, flag ? style.Fresh : color2, IsBold: false, null, null, IsLink: false, null);
			if (flag)
			{
				double num5 = segment3.Written?.UpperBound ?? double.NegativeInfinity;
				double num6 = ClickyInkClock.EaseOut(ClickyInkClock.Progress(clock, num5 + 0.12, num5 + 0.12 + 0.45));
				if (num6 > 0.0)
				{
					clickyInkRun = clickyInkRun with
					{
						Background = Multiply(style.Highlighter, num6)
					};
				}
			}
			builder.Add(clickyInkRun);
		}
		if (struckOver.HasValue)
		{
			InkRange valueOrDefault8 = struckOver.GetValueOrDefault();
			if (clock >= valueOrDefault8.LowerBound)
			{
				builder.StrikeEverything(ClickyInkClock.Progress(clock, valueOrDefault8.LowerBound, valueOrDefault8.UpperBound));
			}
		}
		ClickyInkTextContent clickyInkTextContent = builder.Result();
		clickyInkTextContent.PenClock = clock;
		return clickyInkTextContent;
	}

	public static bool IsStarted(ClickyInkLiveNote.Words words, double clock)
	{
		return words.Segments.Any(delegate(ClickyInkLiveNote.Segment s)
		{
			InkRange? written = s.Written;
			if (written.HasValue)
			{
				InkRange valueOrDefault = written.GetValueOrDefault();
				if (!(clock >= valueOrDefault.LowerBound))
				{
					return false;
				}
			}
			double? vanishesAt = s.VanishesAt;
			if (vanishesAt.HasValue)
			{
				double valueOrDefault2 = vanishesAt.GetValueOrDefault();
				return !(clock >= valueOrDefault2);
			}
			return true;
		});
	}

	public static bool MayHavePencil(this ClickyInkLiveNote.Words words, double clock)
	{
		return words.Segments.Any(delegate(ClickyInkLiveNote.Segment s)
		{
			InkRange? written = s.Written;
			if (written.HasValue)
			{
				InkRange valueOrDefault = written.GetValueOrDefault();
				if (clock >= valueOrDefault.LowerBound - 0.35)
				{
					return clock < valueOrDefault.UpperBound;
				}
			}
			return false;
		});
	}

	public static Color Multiply(Color color, double opacity)
	{
		return Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0.0, 1.0) * (double)(int)color.A), color.R, color.G, color.B);
	}
}
