using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HeyClicky.Drawing3D;

internal static class EarClipping
{
	public static List<int> Triangulate(List<Vector2> polygon)
	{
		List<int> list = Enumerable.Range(0, polygon.Count).ToList();
		List<int> list2 = new List<int>();
		int num = 0;
		while (list.Count > 3 && num < polygon.Count * polygon.Count)
		{
			num++;
			bool flag = false;
			for (int i = 0; i < list.Count; i++)
			{
				int num2 = list[(i + list.Count - 1) % list.Count];
				int num3 = list[i];
				int num4 = list[(i + 1) % list.Count];
				Vector2 vector = polygon[num2];
				Vector2 vector2 = polygon[num3];
				Vector2 vector3 = polygon[num4];
				if (Cross(vector2 - vector, vector3 - vector2) <= 0f)
				{
					continue;
				}
				bool flag2 = false;
				foreach (int item in list)
				{
					if (item != num2 && item != num3 && item != num4 && Contains(polygon[item], vector, vector2, vector3))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					list2.AddRange(new _003C_003Ez__ReadOnlyArray<int>(new int[3] { num2, num3, num4 }));
					list.RemoveAt(i);
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				break;
			}
		}
		if (list.Count >= 3)
		{
			for (int j = 1; j < list.Count - 1; j++)
			{
				list2.AddRange(new _003C_003Ez__ReadOnlyArray<int>(new int[3]
				{
					list[0],
					list[j],
					list[j + 1]
				}));
			}
		}
		return list2;
		static bool Contains(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
		{
			if (Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f)
			{
				return Cross(a - c, p - c) >= 0f;
			}
			return false;
		}
		static float Cross(Vector2 a, Vector2 b)
		{
			return a.X * b.Y - a.Y * b.X;
		}
	}
}
