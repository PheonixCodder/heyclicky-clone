using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public static class ClickyInkItemsRules
{
	public static List<string> Buttons(ClickyInkDocument.Block block)
	{
		List<string> list = ClickyInkChoices.Of(block["actions"] ?? block["buttons"]);
		List<string> list2;
		if (list.Count != 0)
		{
			list2 = list;
		}
		else
		{
			int num = 2;
			list2 = new List<string>(num);
			CollectionsMarshal.SetCount(list2, num);
			Span<string> span = CollectionsMarshal.AsSpan(list2);
			int num2 = 0;
			span[num2] = "Go ahead";
			num2++;
			span[num2] = "Skip";
		}
		return list2.Take(3).ToList();
	}
}
