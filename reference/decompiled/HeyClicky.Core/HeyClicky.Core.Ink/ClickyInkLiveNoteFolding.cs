using System;
using System.Linq;

namespace HeyClicky.Core.Ink;

public static class ClickyInkLiveNoteFolding
{
	public static ClickyInkLiveNote FoldingStruckWords(this ClickyInkLiveNote note, double delay)
	{
		ClickyInkLiveNote clickyInkLiveNote = note.Clone();
		ClickyInkLiveNote.Words title = note.Title;
		clickyInkLiveNote.Title = (((object)title != null) ? Fold(title) : null);
		clickyInkLiveNote.Pages = note.Pages.Select(delegate(ClickyInkLiveNote.Page page)
		{
			ClickyInkLiveNote.Page page2 = page.Clone();
			page2.Name = Fold(page.Name);
			page2.Blocks = page.Blocks.Select(delegate(ClickyInkLiveNote.Block block)
			{
				if (block.Source.Kind != ClickyInkDocument.Kind.Answer)
				{
					ClickyInkLiveNote.Block block2 = block.Clone();
					block2.Words = Fold(block.Words);
					block2.Items = block.Items.Select(delegate(ClickyInkLiveNote.Item item)
					{
						ClickyInkLiveNote.Item item2 = item.Clone();
						item2.Words = Fold(item.Words);
						ClickyInkLiveNote.Words detail = item.Detail;
						item2.Detail = (((object)detail != null) ? Fold(detail) : null);
						return item2;
					}).ToList();
					return block2;
				}
				return block;
			}).ToList();
			return page2;
		}).ToList();
		return clickyInkLiveNote;
		ClickyInkLiveNote.Words Fold(ClickyInkLiveNote.Words words)
		{
			if (words.Current.CharacterCount() <= 40 || !words.Segments.Any((ClickyInkLiveNote.Segment s) => s.Struck.HasValue))
			{
				return words;
			}
			return words with
			{
				Segments = words.Segments.Select((ClickyInkLiveNote.Segment segment) => FoldSegment(segment, delay)).ToList()
			};
		}
	}

	public static ClickyInkLiveNote FoldingStruckHeadlineWords(this ClickyInkLiveNote note, double delay)
	{
		ClickyInkLiveNote clickyInkLiveNote = note.Clone();
		clickyInkLiveNote.Pages = note.Pages.Select(delegate(ClickyInkLiveNote.Page page)
		{
			ClickyInkLiveNote.Page page2 = page.Clone();
			page2.Blocks = page.Blocks.Select(delegate(ClickyInkLiveNote.Block block)
			{
				if (block.Source.Kind == ClickyInkDocument.Kind.Answer && block.Words.Segments.Any((ClickyInkLiveNote.Segment s) => s.Struck.HasValue))
				{
					ClickyInkLiveNote.Block block2 = block.Clone();
					block2.Words = block.Words with
					{
						Segments = block.Words.Segments.Select((ClickyInkLiveNote.Segment s) => FoldSegment(s, delay)).ToList()
					};
					return block2;
				}
				return block;
			}).ToList();
			return page2;
		}).ToList();
		return clickyInkLiveNote;
	}

	private static ClickyInkLiveNote.Segment FoldSegment(ClickyInkLiveNote.Segment segment, double delay)
	{
		InkRange? struck = segment.Struck;
		if (struck.HasValue)
		{
			InkRange valueOrDefault = struck.GetValueOrDefault();
			return segment with
			{
				VanishesAt = Math.Min(segment.VanishesAt ?? double.PositiveInfinity, valueOrDefault.UpperBound + delay)
			};
		}
		return segment;
	}
}
