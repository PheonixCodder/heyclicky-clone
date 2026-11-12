using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkDashboardBoard
{
	public sealed record Section(string Id, ClickyInkLiveNote.Words Name, List<ClickyInkGroup> Groups)
	{
		public bool IsNamedNew => Name.Current.ToLowerInvariant().Contains("new", StringComparison.Ordinal);

		public int NewCount(double clock)
		{
			return Groups.SelectMany((ClickyInkGroup g) => g.Blocks).SelectMany((ClickyInkLiveNote.Block b) => b.Items).Count((ClickyInkLiveNote.Item i) => i.Source.Mark == ClickyInkDocument.Mark.New && !i.IsStruck(clock));
		}
	}

	public sealed record ChangeCounts
	{
		public int New { get; set; }

		public int Updated { get; set; }

		public int Gone { get; set; }

		public int Total => New + Updated + Gone;

		private static readonly HashSet<string> Bookkeeping = new HashSet<string> { "since", "asked", "_tiles", "new" };

		public ChangeCounts()
		{
		}

		public static ChangeCounts Of(ClickyInkLiveNote note, double clock)
		{
			int? num = note.LatestEdit(clock);
			if (num.HasValue)
			{
				int valueOrDefault = num.GetValueOrDefault();
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				foreach (ClickyInkLiveNote.Block item in note.Pages.SelectMany((ClickyInkLiveNote.Page p) => p.Blocks))
				{
					ClickyInkDocument.Kind kind = item.Source.Kind;
					if ((kind == ClickyInkDocument.Kind.Answer || (uint)(kind - 32) <= 1u) ? true : false)
					{
						continue;
					}
					if (item.BornInEdit == valueOrDefault)
					{
						if (clock >= item.LandsAt)
						{
							num2 += Math.Max(1, item.Items.Count);
						}
						continue;
					}
					if (item.GoneInEdit == valueOrDefault)
					{
						if (item.IsStruck(clock))
						{
							num4++;
						}
						continue;
					}
					bool flag = false;
					foreach (ClickyInkLiveNote.Item item2 in item.Items)
					{
						if (item2.Stamps.Count > 0 && item2.Stamps[0].Edit == valueOrDefault && clock >= item2.LandsAt)
						{
							num2++;
							flag = true;
						}
						else if (item2.GoneInEdit == valueOrDefault && item2.IsStruck(clock))
						{
							num4++;
							flag = true;
						}
						else
						{
							if (item2.Stamps.Count <= 1)
							{
								continue;
							}
							List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>> stamps = item2.Stamps;
							ClickyInkLiveNote.Stamp<ClickyInkDocument.Item> stamp = stamps[stamps.Count - 1];
							if (stamp.Edit == valueOrDefault && stamp.At <= clock)
							{
								List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>> stamps2 = item2.Stamps;
								if (FactsChanged(stamps2[stamps2.Count - 2].Value, stamp.Value))
								{
									num3++;
									flag = true;
								}
							}
						}
					}
					if (flag || item.Stamps.Count <= 1)
					{
						continue;
					}
					List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> stamps3 = item.Stamps;
					ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> stamp2 = stamps3[stamps3.Count - 1];
					if (stamp2.Edit == valueOrDefault && stamp2.At <= clock)
					{
						List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> stamps4 = item.Stamps;
						ClickyInkDocument.Block value = stamps4[stamps4.Count - 2].Value;
						if ((item.Source.Kind != ClickyInkDocument.Kind.Spark || !(ClickyInkSeries.Tokens(stamp2.Value["values"]).LastOrDefault() == ClickyInkSeries.Tokens(value["values"]).LastOrDefault())) && FactsChanged(value, stamp2.Value))
						{
							num3++;
						}
					}
				}
				return new ChangeCounts
				{
					New = num2,
					Updated = num3,
					Gone = num4
				};
			}
			return new ChangeCounts();
		}

		public static bool FactsChanged(ClickyInkDocument.Item before, ClickyInkDocument.Item after)
		{
			if (!(before.Text != after.Text) && before.Cells.SequenceEqual(after.Cells) && before.IsChecked == after.IsChecked)
			{
				return before.Detail != after.Detail;
			}
			return true;
		}

		public static bool FactsChanged(ClickyInkDocument.Block before, ClickyInkDocument.Block after)
		{
			if (!(before.Text != after.Text) && ClickyInkDocument.Block.AttributesEqual(Facts(before.Attributes), Facts(after.Attributes)))
			{
				return !before.Items.Select((ClickyInkDocument.Item i) => i.Text).SequenceEqual(after.Items.Select((ClickyInkDocument.Item i) => i.Text));
			}
			return true;
			static Dictionary<string, string> Facts(Dictionary<string, string> attributes)
			{
				return attributes.Where<KeyValuePair<string, string>>((KeyValuePair<string, string> p) => !Bookkeeping.Contains(p.Key)).ToDictionary((KeyValuePair<string, string> p) => p.Key, (KeyValuePair<string, string> p) => p.Value);
			}
		}

		[CompilerGenerated]
		private ChangeCounts(ChangeCounts original)
		{
			New = original.New;
			Updated = original.Updated;
			Gone = original.Gone;
		}
	}

	private static readonly HashSet<ClickyInkDocument.Kind> ImplicitStripKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Figure,
		ClickyInkDocument.Kind.Spark,
		ClickyInkDocument.Kind.Progress,
		ClickyInkDocument.Kind.Tally,
		ClickyInkDocument.Kind.Streak,
		ClickyInkDocument.Kind.Countdown
	};

	private static readonly HashSet<string> MovePageNames = new HashSet<string> { "your move", "needs you", "for you", "your call", "decide", "decisions", "to decide" };

	private static readonly HashSet<string> FirstPageNames = new HashSet<string> { "in short", "today", "now", "this run" };

	public ClickyInkLiveNote.Block? Run { get; set; }

	public ClickyInkGroup? Answer { get; set; }

	public ClickyInkGroup? Quiet { get; set; }

	public List<ClickyInkLiveNote.Block> Strip { get; set; } = new List<ClickyInkLiveNote.Block>();

	public Section? Move { get; set; }

	public List<ClickyInkLiveNote.Block> Sources { get; set; } = new List<ClickyInkLiveNote.Block>();

	public List<Section> Sections { get; set; } = new List<Section>();

	public ClickyInkStatusState Overall { get; set; }

	public ChangeCounts Changes { get; set; } = new ChangeCounts();

	public ClickyInkDashboardBoard(ClickyInkLiveNote note, double clock, string firstSectionName = "This run")
	{
		List<(ClickyInkLiveNote.Page, List<ClickyInkGroup>)> list = note.Pages.Select((ClickyInkLiveNote.Page page) => (Page: page, Groups: ClickyInkGroup.Groups(page, clock))).ToList();
		string text = list.SelectMany<(ClickyInkLiveNote.Page, List<ClickyInkGroup>), ClickyInkGroup>(((ClickyInkLiveNote.Page Page, List<ClickyInkGroup> Groups) p) => p.Groups).FirstOrDefault((ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.Tiles)?.Id;
		List<ClickyInkGroup> list2 = new List<ClickyInkGroup>();
		bool flag = false;
		bool flag2 = false;
		for (int j = 0; j < list.Count; j++)
		{
			(ClickyInkLiveNote.Page, List<ClickyInkGroup>) tuple = list[j];
			ClickyInkLiveNote.Page item = tuple.Item1;
			List<ClickyInkGroup> item2 = tuple.Item2;
			List<ClickyInkGroup> list3 = new List<ClickyInkGroup>();
			foreach (ClickyInkGroup item4 in item2)
			{
				ClickyInkLiveNote.Block block = item4.Blocks[0];
				ClickyInkDocument.Kind kind = item4.Kind;
				if (kind == ClickyInkDocument.Kind.Run && Run == null)
				{
					Run = block;
				}
				else if (kind == ClickyInkDocument.Kind.Answer && (object)Answer == null)
				{
					Answer = item4;
				}
				else if (kind == ClickyInkDocument.Kind.Quiet)
				{
					if ((object)Quiet == null && !block.IsStruck(clock))
					{
						Quiet = item4;
					}
				}
				else if (kind == ClickyInkDocument.Kind.Tiles && item4.Id == text)
				{
					Strip = item4.Blocks.ToList();
				}
				else if (text == null && j == 0 && ImplicitStripKinds.Contains(kind) && Strip.Count < 8)
				{
					Strip.AddRange(item4.Blocks.Take(8 - Strip.Count));
				}
				else if (((uint)(kind - 21) <= 1u || kind == ClickyInkDocument.Kind.Review) ? true : false)
				{
					list2.Add(item4);
					if (kind != ClickyInkDocument.Kind.Ask)
					{
						flag = true;
					}
				}
				else if (kind == ClickyInkDocument.Kind.Link && item4.IsMadeLink)
				{
					list2.Add(item4);
				}
				else if (kind == ClickyInkDocument.Kind.Link)
				{
					Sources.AddRange(item4.Blocks);
				}
				else
				{
					list3.Add(item4);
				}
				if (item4.Blocks.Any((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Status && ClickyInkStatus.State(b.Source["state"] ?? b.Source["status"]) == ClickyInkStatusState.Bad))
				{
					flag2 = true;
				}
				if (item4.Blocks.SelectMany((ClickyInkLiveNote.Block b) => b.Items).Any((ClickyInkLiveNote.Item i) => i.Source.Mark == ClickyInkDocument.Mark.Needs))
				{
					flag = true;
				}
			}
			string item3 = TrimPunctuation(ClickyInkParser.Emphasis.Strip(item.Name.Current).ToLowerInvariant());
			if (MovePageNames.Contains(item3))
			{
				list2.AddRange(list3);
			}
			else if (list3.Count > 0)
			{
				bool flag3 = j == 0 && FirstPageNames.Contains(item.Name.Current.ToLowerInvariant());
				Sections.Add(new Section(item.Id, flag3 ? ClickyInkLiveNote.Words.Plain(firstSectionName) : item.Name, list3));
			}
		}
		if (list2.Count > 0)
		{
			Move = new Section("move", ClickyInkLiveNote.Words.Plain("Your move"), list2);
		}
		Overall = (flag2 ? ClickyInkStatusState.Bad : (flag ? ClickyInkStatusState.Watch : ClickyInkStatusState.Ok));
		Changes = ChangeCounts.Of(note, clock);
	}

	public static ClickyInkDashboardBoard ForFrontPage(ClickyInkLiveNote note, double clock)
	{
		ClickyInkDashboardBoard clickyInkDashboardBoard = new ClickyInkDashboardBoard(note, clock);
		if (clickyInkDashboardBoard.Overall == ClickyInkStatusState.Ok)
		{
			Section? move = clickyInkDashboardBoard.Move;
			if ((object)move != null && move.Groups.Any((ClickyInkGroup g) => g.Kind != ClickyInkDocument.Kind.Ask && !g.IsMadeLink))
			{
				clickyInkDashboardBoard.Overall = ClickyInkStatusState.Watch;
			}
		}
		return clickyInkDashboardBoard;
	}

	private static string TrimPunctuation(string text)
	{
		return InkText.TrimWhere(text, char.IsPunctuation);
	}
}
