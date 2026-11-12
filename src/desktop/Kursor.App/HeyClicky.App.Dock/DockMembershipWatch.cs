using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.App.Dock;

public sealed class DockMembershipWatch
{
	private (bool ShowsDock, string Cards)? last;

	public bool Note(bool showsDock, IEnumerable<Guid>? liveSuggestionCardIds)
	{
		string item = ((liveSuggestionCardIds == null) ? "-" : string.Join(",", liveSuggestionCardIds.Order()));
		(bool, string) tuple = (showsDock, item);
		(bool, string)? tuple2 = last;
		(bool, string) tuple3 = tuple;
		bool hasValue = tuple2.HasValue;
		if (hasValue)
		{
			if (hasValue)
			{
				(bool, string) valueOrDefault = tuple2.GetValueOrDefault();
				if (valueOrDefault.Item1 != tuple3.Item1 || !(valueOrDefault.Item2 == tuple3.Item2))
				{
					goto IL_0075;
				}
			}
			return false;
		}
		goto IL_0075;
		IL_0075:
		last = tuple;
		return true;
	}
}
