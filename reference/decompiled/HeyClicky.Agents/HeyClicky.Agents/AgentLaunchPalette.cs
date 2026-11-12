using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Agents;

public static class AgentLaunchPalette
{
	public static readonly IReadOnlyList<uint> SwatchRgb = new global::_003C_003Ez__ReadOnlyArray<uint>(new uint[4] { 3375359u, 16731212u, 13934615u, 2540161u });

	public static int SlotCount => SwatchRgb.Count;

	public static int NextIndex(IReadOnlyList<int> assignedMostRecentFirst, IReadOnlySet<int> activeSlots)
	{
		int slotCount = SlotCount;
		List<int> list = assignedMostRecentFirst.Where((int i) => i >= 0).ToList();
		int num = (((list.Count > 0) ? list[0] : (-1)) + 1) % slotCount;
		HashSet<int> hashSet = new HashSet<int>(activeSlots.Where((int i) => i >= 0));
		hashSet.UnionWith(list.Take(Math.Max(0, slotCount - 1)));
		for (int j = 0; j < slotCount; j++)
		{
			int num2 = (num + j) % slotCount;
			if (!hashSet.Contains(num2))
			{
				return num2;
			}
		}
		for (int k = 0; k < slotCount; k++)
		{
			int num3 = (num + k) % slotCount;
			if (!activeSlots.Contains(num3))
			{
				return num3;
			}
		}
		return num;
	}
}
