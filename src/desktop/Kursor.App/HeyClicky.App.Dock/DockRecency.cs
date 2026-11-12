using System;

namespace HeyClicky.App.Dock;

public readonly record struct DockRecency(bool IsActive, DateTimeOffset Date) : IComparable<DockRecency>
{
	public int CompareTo(DockRecency other)
	{
		if (IsActive == other.IsActive)
		{
			return Date.CompareTo(other.Date);
		}
		if (!IsActive)
		{
			return -1;
		}
		return 1;
	}
}
