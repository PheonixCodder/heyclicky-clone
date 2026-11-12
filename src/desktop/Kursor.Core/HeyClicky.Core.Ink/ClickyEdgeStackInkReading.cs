using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed class ClickyEdgeStackInkReading
{
	public enum State
	{
		Writing,
		NeedsYou,
		Ready,
		Done,
		Watching,
		Paused
	}

	public sealed record Changes
	{
		public int New { get; set; }

		public int Updated { get; set; }

		public int Gone { get; set; }

		public bool IsEmpty => New + Updated + Gone == 0;
	}

	private static readonly ClickyInkDocument.Kind[] WordsKinds = new ClickyInkDocument.Kind[6]
	{
		ClickyInkDocument.Kind.Paragraph,
		ClickyInkDocument.Kind.List,
		ClickyInkDocument.Kind.Checklist,
		ClickyInkDocument.Kind.Numbered,
		ClickyInkDocument.Kind.Quiet,
		ClickyInkDocument.Kind.Quote
	};

	public State CurrentState { get; }

	public ClickyInkLiveNote.Words? Title { get; }

	public ClickyInkLiveNote.Block? Lead { get; }

	public ClickyInkLiveNote.Block? Support { get; }

	public HashSet<string> ChangedItemIDs { get; } = new HashSet<string>();

	public string? WaitingBlockID { get; }

	public string? WaitingLine { get; }

	public string? PendingBlockID { get; }

	public string? PendingLine { get; }

	public Changes ChangeCounts { get; } = new Changes();

	public bool TitleChanged { get; }

	public bool LeadChanged { get; }

	public bool IsPenBusy { get; }

	public ClickyEdgeStackInkReading(ClickyInkLiveNote note, double clock, bool thingIsRunning, bool thingWantsTheUser, State restState, double? seenClock, ClickyInkActionFeedback feedback)
	{
		IsPenBusy = clock < note.FinishedClock;
		Title = note.Title;
		List<ClickyInkLiveNote.Block> list = ((note.Pages.Count > 0) ? note.Pages[0].Blocks.Where((ClickyInkLiveNote.Block b) => b.IsPresent(clock) && !b.IsStruck(clock)).ToList() : new List<ClickyInkLiveNote.Block>());
		ClickyInkLiveNote.Block answer = list.FirstOrDefault((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Answer);
		Lead = (answer ?? list.FirstOrDefault((ClickyInkLiveNote.Block b) => WordsKinds.Contains(b.Source.Kind)))?.Resolved(clock);
		if (answer != null)
		{
			int num = list.FindIndex((ClickyInkLiveNote.Block b) => b.Id == answer.Id);
			if (num >= 0)
			{
				Support = list.Skip(num + 1).FirstOrDefault((ClickyInkLiveNote.Block b) => WordsKinds.Contains(b.Source.Kind))?.Resolved(clock);
			}
		}
		(string, string)? tuple = null;
		foreach (ClickyInkLiveNote.Page page in note.Pages)
		{
			if (tuple.HasValue)
			{
				break;
			}
			foreach (ClickyInkLiveNote.Block block3 in page.Blocks)
			{
				if (block3.IsPresent(clock) && !block3.IsStruck(clock))
				{
					string text = WaitingLineFor(block3.SourceAt(clock), feedback);
					if (text != null)
					{
						tuple = (block3.Id, text);
						break;
					}
				}
			}
		}
		WaitingBlockID = tuple?.Item1;
		WaitingLine = tuple?.Item2;
		ClickyInkSentAction pending = feedback.Pending;
		if ((object)pending != null)
		{
			foreach (ClickyInkLiveNote.Page page2 in note.Pages)
			{
				if (PendingBlockID != null)
				{
					break;
				}
				ClickyInkLiveNote.Block block = page2.Blocks.FirstOrDefault((ClickyInkLiveNote.Block b) => b.IsPresent(clock) && ClickyInkObjectKey.Of(b.SourceAt(clock)) == pending.ObjectKey);
				if (block != null)
				{
					PendingBlockID = block.Id;
				}
			}
			PendingLine = pending.MarginWords;
		}
		if (thingIsRunning || IsPenBusy)
		{
			CurrentState = State.Writing;
		}
		else if (tuple.HasValue || thingWantsTheUser)
		{
			CurrentState = State.NeedsYou;
		}
		else
		{
			CurrentState = (((restState == State.Ready || (uint)(restState - 4) <= 1u) ? true : false) ? restState : State.Done);
		}
		if (!seenClock.HasValue)
		{
			return;
		}
		double seen = seenClock.GetValueOrDefault();
		if (seen <= 0.5 || seen >= clock)
		{
			return;
		}
		foreach (ClickyInkLiveNote.Page page3 in note.Pages)
		{
			foreach (ClickyInkLiveNote.Block block4 in page3.Blocks)
			{
				bool flag = block4.IsPreview;
				if (!flag)
				{
					ClickyInkDocument.Kind kind = block4.Source.Kind;
					bool flag2 = ((kind == ClickyInkDocument.Kind.Heading || kind == ClickyInkDocument.Kind.Rule) ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					continue;
				}
				double? goneAt = block4.GoneAt;
				int num2;
				if (goneAt.HasValue)
				{
					double valueOrDefault = goneAt.GetValueOrDefault();
					num2 = ((valueOrDefault <= clock) ? 1 : 0);
				}
				else
				{
					num2 = 0;
				}
				bool flag3 = (byte)num2 != 0;
				if (block4.LandsAt > seen)
				{
					if (block4.LandsAt <= clock && !flag3)
					{
						ChangeCounts.New++;
					}
					continue;
				}
				goneAt = block4.GoneAt;
				if (goneAt.HasValue)
				{
					double valueOrDefault2 = goneAt.GetValueOrDefault();
					if (valueOrDefault2 > seen && valueOrDefault2 <= clock)
					{
						ChangeCounts.Gone++;
						continue;
					}
				}
				if (!flag3 && block4.Stamps.Skip(1).Any<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>>((ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> s) => s.At > seen && s.At <= clock))
				{
					ChangeCounts.Updated++;
				}
			}
		}
		ClickyInkLiveNote.Words title = note.Title;
		TitleChanged = (object)title != null && Changed(title, seen, clock);
		ClickyInkLiveNote.Block lead = Lead;
		if (lead != null)
		{
			LeadChanged = lead.Items.Count == 0 && (lead.LandsAt > seen || lead.ShownSince() > seen || Changed(lead.Words, seen, clock));
		}
		ClickyInkLiveNote.Block[] array = new ClickyInkLiveNote.Block[2] { Lead, Support };
		foreach (ClickyInkLiveNote.Block block2 in array)
		{
			if (block2 == null)
			{
				continue;
			}
			foreach (ClickyInkLiveNote.Item item in block2.Items)
			{
				if (item.LandsAt > seen || item.Stamps.Skip(1).Any<ClickyInkLiveNote.Stamp<ClickyInkDocument.Item>>((ClickyInkLiveNote.Stamp<ClickyInkDocument.Item> s) => s.At > seen) || Changed(item.Words, seen, clock))
				{
					ChangedItemIDs.Add(item.Id);
				}
			}
		}
	}

	public static string? WaitingLineFor(ClickyInkDocument.Block source, ClickyInkActionFeedback feedback)
	{
		switch (source.Kind)
		{
		case ClickyInkDocument.Kind.Question:
		{
			if ((object)feedback.AnswerFor(source) != null)
			{
				return null;
			}
			string text3 = ClickyInkNoteActions.Plain(source.Text);
			if (text3.Length != 0)
			{
				return text3;
			}
			return null;
		}
		case ClickyInkDocument.Kind.Review:
		{
			ClickyInkSentAction clickyInkSentAction = feedback.AnswerFor(source);
			IReadOnlyList<string> readOnlyList = clickyInkSentAction?.ReviewedItems;
			if (readOnlyList != null && readOnlyList.Count == 0)
			{
				return null;
			}
			IReadOnlyList<string> alreadySent = clickyInkSentAction?.ReviewedItems ?? Array.Empty<string>();
			List<ClickyInkDocument.Item> list = source.Items.Where((ClickyInkDocument.Item i) => ClickyInkReviewRules.IsOpen(i) && !ClickyInkReviewRules.WasSent(i, alreadySent)).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			string text = ClickyInkReviewRules.Label(source);
			string text2 = ((list.Count == 1) ? "1 to decide" : $"{list.Count} to decide");
			if (text.Length != 0)
			{
				return text + " · " + text2;
			}
			return "Your OK · " + text2;
		}
		default:
			return null;
		}
	}

	private static bool Changed(ClickyInkLiveNote.Words words, double seenClock, double clock)
	{
		return words.Segments.Any(delegate(ClickyInkLiveNote.Segment segment)
		{
			if (segment.IsPreview)
			{
				return false;
			}
			InkRange? written = segment.Written;
			int num;
			if (written.HasValue)
			{
				InkRange valueOrDefault = written.GetValueOrDefault();
				if (valueOrDefault.LowerBound > seenClock)
				{
					num = ((valueOrDefault.LowerBound <= clock) ? 1 : 0);
					goto IL_0048;
				}
			}
			num = 0;
			goto IL_0048;
			IL_0048:
			written = segment.Struck;
			int num2;
			if (written.HasValue)
			{
				InkRange valueOrDefault2 = written.GetValueOrDefault();
				if (valueOrDefault2.LowerBound > seenClock)
				{
					num2 = ((valueOrDefault2.LowerBound <= clock) ? 1 : 0);
					goto IL_0085;
				}
			}
			num2 = 0;
			goto IL_0085;
			IL_0085:
			bool flag = (byte)num2 != 0;
			return (byte)((uint)num | (flag ? 1u : 0u)) != 0;
		});
	}
}
