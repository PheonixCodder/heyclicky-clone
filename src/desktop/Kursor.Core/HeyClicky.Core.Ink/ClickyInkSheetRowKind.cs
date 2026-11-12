using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public abstract record ClickyInkSheetRowKind
{
	public sealed record Title(ClickyInkLiveNote.Words Words) : ClickyInkSheetRowKind();

	public sealed record PageName(int Index, string PageID, ClickyInkLiveNote.Words Name, double LandsAt, double? VanishesAt) : ClickyInkSheetRowKind();

	public sealed record Group(ClickyInkGroup Value, Resize? Resized) : ClickyInkSheetRowKind();

	public sealed record Resize(double At, ClickyInkGroup Before);

	public ClickyInkSheetRowKind Resolved(double asOf, double liveClock)
	{
		if (asOf == liveClock || !(this is Group group))
		{
			return this;
		}
		List<ClickyInkLiveNote.Block> blocks = group.Value.Blocks.Select(delegate(ClickyInkLiveNote.Block original)
		{
			ClickyInkLiveNote.Block block = original.Clone();
			block.Words = block.Words with
			{
				AsOf = asOf
			};
			block.Items = block.Items.Select(delegate(ClickyInkLiveNote.Item originalItem)
			{
				ClickyInkLiveNote.Item item = originalItem.Clone();
				item.Words = item.Words with
				{
					AsOf = asOf
				};
				ClickyInkLiveNote.Words detail = item.Detail;
				item.Detail = (((object)detail != null) ? detail with
				{
					AsOf = asOf
				} : null);
				item.Cells = item.Cells.Select((ClickyInkLiveNote.Words cell) => cell with
				{
					AsOf = asOf
				}).ToList();
				return item;
			}).ToList();
			return block;
		}).ToList();
		return new Group(group.Value with
		{
			Blocks = blocks
		}, group.Resized);
	}
}
