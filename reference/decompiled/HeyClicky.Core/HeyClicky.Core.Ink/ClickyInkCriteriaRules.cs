using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public static class ClickyInkCriteriaRules
{
	public static List<string> Values(string? text)
	{
		string text2 = ClickyInkNoteActions.Plain(text ?? "");
		List<string> list = (from v in text2.Contains('|') ? text2.Split('|') : text2.Split(',')
			select v.TrimWs() into v
			where v.Length > 0
			select v).ToList();
		if (list.Count != 0)
		{
			return list;
		}
		if (text2.Length != 0)
		{
			int num = 1;
			List<string> list2 = new List<string>(num);
			CollectionsMarshal.SetCount(list2, num);
			Span<string> span = CollectionsMarshal.AsSpan(list2);
			int index = 0;
			span[index] = text2;
			return list2;
		}
		return new List<string>();
	}
}
