using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace HeyClicky.Drawing3D;

internal static class PathFlattener
{
	public static List<List<Vector2>> Outlines(string pathData, float unit)
	{
		List<string> tokens = Tokenize(pathData);
		List<List<Vector2>> outlines = new List<List<Vector2>>();
		List<Vector2> current = new List<Vector2>();
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = vector;
		char c = 'M';
		int index = 0;
		while (index < tokens.Count)
		{
			if (tokens[index].Length == 1 && char.IsLetter(tokens[index][0]))
			{
				c = tokens[index][0];
				index++;
			}
			bool flag = char.IsLower(c);
			Vector2 vector3 = (flag ? vector : Vector2.Zero);
			switch (char.ToUpperInvariant(c))
			{
			case 'M':
			{
				if (!Number(out var value13) || !Number(out var value14))
				{
					index = tokens.Count;
					break;
				}
				Flush();
				vector = vector3 + new Vector2(value13, value14);
				vector2 = vector;
				current.Add(World(vector));
				c = (flag ? 'l' : 'L');
				break;
			}
			case 'L':
			{
				if (!Number(out var value16) || !Number(out var value17))
				{
					index = tokens.Count;
					break;
				}
				vector = vector3 + new Vector2(value16, value17);
				current.Add(World(vector));
				break;
			}
			case 'H':
			{
				if (!Number(out var value15))
				{
					index = tokens.Count;
					break;
				}
				vector = new Vector2((flag ? vector.X : 0f) + value15, vector.Y);
				current.Add(World(vector));
				break;
			}
			case 'V':
			{
				if (!Number(out var value6))
				{
					index = tokens.Count;
					break;
				}
				vector = new Vector2(vector.X, (flag ? vector.Y : 0f) + value6);
				current.Add(World(vector));
				break;
			}
			case 'C':
			{
				if (!Number(out var value7) || !Number(out var value8) || !Number(out var value9) || !Number(out var value10) || !Number(out var value11) || !Number(out var value12))
				{
					index = tokens.Count;
					break;
				}
				Vector2 vector7 = vector;
				Vector2 vector8 = vector3 + new Vector2(value7, value8);
				Vector2 vector9 = vector3 + new Vector2(value9, value10);
				Vector2 vector10 = vector3 + new Vector2(value11, value12);
				for (int j = 1; j <= 12; j++)
				{
					float num3 = (float)j / 12f;
					float num4 = 1f - num3;
					current.Add(World(num4 * num4 * num4 * vector7 + 3f * num4 * num4 * num3 * vector8 + 3f * num4 * num3 * num3 * vector9 + num3 * num3 * num3 * vector10));
				}
				vector = vector10;
				break;
			}
			case 'Q':
			{
				if (!Number(out var value2) || !Number(out var value3) || !Number(out var value4) || !Number(out var value5))
				{
					index = tokens.Count;
					break;
				}
				Vector2 vector4 = vector;
				Vector2 vector5 = vector3 + new Vector2(value2, value3);
				Vector2 vector6 = vector3 + new Vector2(value4, value5);
				for (int i = 1; i <= 12; i++)
				{
					float num = (float)i / 12f;
					float num2 = 1f - num;
					current.Add(World(num2 * num2 * vector4 + 2f * num2 * num * vector5 + num * num * vector6));
				}
				vector = vector6;
				break;
			}
			case 'Z':
				vector = vector2;
				Flush();
				break;
			default:
				index++;
				break;
			}
		}
		Flush();
		return outlines;
		void Flush()
		{
			if (current.Count >= 3)
			{
				outlines.Add(current);
			}
			current = new List<Vector2>();
		}
		bool Number(out float value)
		{
			value = 0f;
			if (index >= tokens.Count || !float.TryParse(tokens[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				return false;
			}
			index++;
			return true;
		}
		Vector2 World(Vector2 p)
		{
			return new Vector2(p.X * unit, (0f - p.Y) * unit);
		}
	}

	private static List<string> Tokenize(string pathData)
	{
		List<string> tokens = new List<string>();
		StringBuilder number = new StringBuilder();
		char c = ' ';
		for (int i = 0; i < pathData.Length; i++)
		{
			char c2 = pathData[i];
			if (char.IsLetter(c2) && c2 != 'e' && c2 != 'E')
			{
				FlushNumber();
				tokens.Add(c2.ToString());
			}
			else if (c2 == '-' && c != 'e' && c != 'E')
			{
				FlushNumber();
				number.Append('-');
			}
			else if (c2 == '.' && number.ToString().Contains('.'))
			{
				FlushNumber();
				number.Append('.');
			}
			else if (char.IsDigit(c2) || c2 == '.' || c2 == 'e' || c2 == 'E')
			{
				number.Append(c2);
			}
			else
			{
				FlushNumber();
			}
			c = c2;
		}
		FlushNumber();
		return tokens;
		void FlushNumber()
		{
			if (number.Length > 0)
			{
				tokens.Add(number.ToString());
				number.Clear();
			}
		}
	}
}
