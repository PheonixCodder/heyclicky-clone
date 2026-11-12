using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public static class ClickyInkDashboardResolving
{
	public static ClickyInkLiveNote.Block DashboardResolved(this ClickyInkLiveNote.Block block, double clock)
	{
		ClickyInkLiveNote.Block block2 = block.Resolved(clock);
		ClickyInkDocument.Kind kind = block.Source.Kind;
		if (!ClickyInkDashboardKinds.DashboardKinds.Contains(kind) && kind != ClickyInkDocument.Kind.Figure && kind != ClickyInkDocument.Kind.Answer)
		{
			return block2;
		}
		int num = block.Stamps.FindLastIndex((ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> s) => s.At <= clock);
		if (num <= 0)
		{
			return block2;
		}
		ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> stamp = block.Stamps[num];
		ClickyInkLiveNote.Stamp<ClickyInkDocument.Block> stamp2 = block.Stamps[num - 1];
		ClickyInkDocument.Block block3 = block2.Source.Clone();
		bool flag = false;
		foreach (var (text3, text4) in stamp.Value.Attributes)
		{
			if (text3.HasPrefix("_"))
			{
				continue;
			}
			string text5 = stamp2.Value[text3];
			if (!(text5 == text4))
			{
				if (text5 != null)
				{
					block3.Attributes["_prev_" + text3] = text5;
				}
				flag = true;
			}
		}
		if (!flag)
		{
			return block2;
		}
		block3.Attributes["_changed_at"] = InkText.DoubleDescription(stamp.At);
		int? edit = stamp.Edit;
		if (edit.HasValue)
		{
			int valueOrDefault = edit.GetValueOrDefault();
			block3.Attributes["_changed_edit"] = valueOrDefault.ToString(CultureInfo.InvariantCulture);
		}
		int num2 = 1;
		List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> list = new List<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>>(num2);
		CollectionsMarshal.SetCount(list, num2);
		Span<ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>> span = CollectionsMarshal.AsSpan(list);
		int index = 0;
		span[index] = new ClickyInkLiveNote.Stamp<ClickyInkDocument.Block>(0.0, block3, null);
		block2.Stamps = list;
		return block2;
	}
}
