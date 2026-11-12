using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickySVGPath
{
	private abstract record Token
	{
		public sealed record Command(char Letter) : Token();

		public sealed record Number(double Value) : Token();
	}

	public static PaperPath Path(string data)
	{
		PaperPath paperPath = new PaperPath();
		Queue<Token> tokens = new Queue<Token>(Tokenize(data));
		Point current = new Point(0f, 0f);
		Point point = new Point(0f, 0f);
		Point? point2 = null;
		Point? point3 = null;
		char c = 'M';
		while (tokens.Count > 0)
		{
			if (tokens.Peek() is Token.Command command)
			{
				c = command.Letter;
				tokens.Dequeue();
			}
			bool flag = char.IsLower(c);
			switch (char.ToUpperInvariant(c))
			{
			case 'M':
			{
				Point? point4 = Pt(flag);
				if (point4.HasValue)
				{
					Point valueOrDefault = point4.GetValueOrDefault();
					paperPath.MoveTo(valueOrDefault);
					current = valueOrDefault;
					point = valueOrDefault;
					c = (flag ? 'l' : 'L');
					point2 = null;
					point3 = null;
					break;
				}
				return paperPath;
			}
			case 'L':
			{
				Point? point4 = Pt(flag);
				if (point4.HasValue)
				{
					Point valueOrDefault17 = point4.GetValueOrDefault();
					paperPath.LineTo(valueOrDefault17);
					current = valueOrDefault17;
					point2 = null;
					point3 = null;
					break;
				}
				return paperPath;
			}
			case 'H':
			{
				double? num = Number();
				if (num.HasValue)
				{
					double valueOrDefault13 = num.GetValueOrDefault();
					current = new Point(flag ? (current.X + valueOrDefault13) : valueOrDefault13, current.Y);
					paperPath.LineTo(current);
					point2 = null;
					point3 = null;
					break;
				}
				return paperPath;
			}
			case 'V':
			{
				double? num = Number();
				if (num.HasValue)
				{
					double valueOrDefault20 = num.GetValueOrDefault();
					current = new Point(current.X, flag ? (current.Y + valueOrDefault20) : valueOrDefault20);
					paperPath.LineTo(current);
					point2 = null;
					point3 = null;
					break;
				}
				return paperPath;
			}
			case 'C':
			{
				Point? point4 = Pt(flag);
				if (point4.HasValue)
				{
					Point valueOrDefault10 = point4.GetValueOrDefault();
					point4 = Pt(flag);
					if (point4.HasValue)
					{
						Point valueOrDefault11 = point4.GetValueOrDefault();
						point4 = Pt(flag);
						if (point4.HasValue)
						{
							Point valueOrDefault12 = point4.GetValueOrDefault();
							paperPath.CubicTo(valueOrDefault12, valueOrDefault10, valueOrDefault11);
							point2 = valueOrDefault11;
							current = valueOrDefault12;
							point3 = null;
							break;
						}
					}
				}
				return paperPath;
			}
			case 'S':
			{
				Point? point4 = Pt(flag);
				if (point4.HasValue)
				{
					Point valueOrDefault14 = point4.GetValueOrDefault();
					point4 = Pt(flag);
					if (point4.HasValue)
					{
						Point valueOrDefault15 = point4.GetValueOrDefault();
						Point point7;
						if (point2.HasValue)
						{
							Point valueOrDefault16 = point2.GetValueOrDefault();
							point7 = new Point(2.0 * current.X - valueOrDefault16.X, 2.0 * current.Y - valueOrDefault16.Y);
						}
						else
						{
							point7 = current;
						}
						Point control = point7;
						paperPath.CubicTo(valueOrDefault15, control, valueOrDefault14);
						point2 = valueOrDefault14;
						current = valueOrDefault15;
						point3 = null;
						break;
					}
				}
				return paperPath;
			}
			case 'Q':
			{
				Point? point4 = Pt(flag);
				if (point4.HasValue)
				{
					Point valueOrDefault18 = point4.GetValueOrDefault();
					point4 = Pt(flag);
					if (point4.HasValue)
					{
						Point valueOrDefault19 = point4.GetValueOrDefault();
						paperPath.QuadTo(valueOrDefault19, valueOrDefault18);
						point3 = valueOrDefault18;
						current = valueOrDefault19;
						point2 = null;
						break;
					}
				}
				return paperPath;
			}
			case 'T':
			{
				Point? point4 = Pt(flag);
				if (point4.HasValue)
				{
					Point valueOrDefault2 = point4.GetValueOrDefault();
					Point point5;
					if (point3.HasValue)
					{
						Point valueOrDefault3 = point3.GetValueOrDefault();
						point5 = new Point(2.0 * current.X - valueOrDefault3.X, 2.0 * current.Y - valueOrDefault3.Y);
					}
					else
					{
						point5 = current;
					}
					Point point6 = point5;
					paperPath.QuadTo(valueOrDefault2, point6);
					point3 = point6;
					current = valueOrDefault2;
					point2 = null;
					break;
				}
				return paperPath;
			}
			case 'A':
			{
				double? num = Number();
				if (num.HasValue)
				{
					double valueOrDefault4 = num.GetValueOrDefault();
					num = Number();
					if (num.HasValue)
					{
						double valueOrDefault5 = num.GetValueOrDefault();
						num = Number();
						if (num.HasValue)
						{
							double valueOrDefault6 = num.GetValueOrDefault();
							num = Number();
							if (num.HasValue)
							{
								double valueOrDefault7 = num.GetValueOrDefault();
								num = Number();
								if (num.HasValue)
								{
									double valueOrDefault8 = num.GetValueOrDefault();
									Point? point4 = Pt(flag);
									if (point4.HasValue)
									{
										Point valueOrDefault9 = point4.GetValueOrDefault();
										AddArc(paperPath, current, valueOrDefault9, valueOrDefault4, valueOrDefault5, valueOrDefault6, valueOrDefault7 != 0.0, valueOrDefault8 != 0.0);
										current = valueOrDefault9;
										point2 = null;
										point3 = null;
										break;
									}
								}
							}
						}
					}
				}
				return paperPath;
			}
			case 'Z':
				paperPath.Close();
				current = point;
				point2 = null;
				point3 = null;
				if (tokens.Count > 0 && tokens.Peek() is Token.Number)
				{
					c = 'L';
				}
				break;
			default:
				tokens.Dequeue();
				break;
			}
		}
		return paperPath;
		double? Number()
		{
			if (tokens.Count == 0 || !(tokens.Peek() is Token.Number number))
			{
				return null;
			}
			tokens.Dequeue();
			return number.Value;
		}
		Point? Pt(bool relative)
		{
			double? num2 = Number();
			if (num2.HasValue)
			{
				double valueOrDefault21 = num2.GetValueOrDefault();
				num2 = Number();
				if (num2.HasValue)
				{
					double valueOrDefault22 = num2.GetValueOrDefault();
					return relative ? new Point(current.X + valueOrDefault21, current.Y + valueOrDefault22) : new Point(valueOrDefault21, valueOrDefault22);
				}
			}
			return null;
		}
	}

	private static List<Token> Tokenize(string data)
	{
		List<Token> tokens = new List<Token>();
		StringBuilder buffer = new StringBuilder();
		foreach (char c in data)
		{
			if (char.IsLetter(c) && c != 'e' && c != 'E')
			{
				Flush();
				tokens.Add(new Token.Command(c));
				continue;
			}
			switch (c)
			{
			case '-':
			{
				bool flag = buffer.Length > 0;
				if (flag)
				{
					StringBuilder stringBuilder = buffer;
					char c2 = stringBuilder[stringBuilder.Length - 1];
					bool flag2 = ((c2 == 'E' || c2 == 'e') ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					buffer.Append(c);
					break;
				}
				Flush();
				buffer.Append(c);
				break;
			}
			case '.':
			{
				string text = buffer.ToString();
				if (text.Contains('.') && !text.Contains('e'))
				{
					Flush();
				}
				buffer.Append(c);
				break;
			}
			default:
				if (char.IsDigit(c) || c == 'e' || c == 'E')
				{
					buffer.Append(c);
				}
				else
				{
					Flush();
				}
				break;
			}
		}
		Flush();
		return tokens;
		void Flush()
		{
			if (buffer.Length > 0 && double.TryParse(buffer.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				tokens.Add(new Token.Number(result));
			}
			buffer.Clear();
		}
	}

	private static void AddArc(PaperPath path, Point start, Point end, double rxIn, double ryIn, double rotationDegrees, bool largeArc, bool sweep)
	{
		if (start == end)
		{
			return;
		}
		double rx = Math.Abs(rxIn);
		double ry = Math.Abs(ryIn);
		if (rx <= 0.0 || ry <= 0.0)
		{
			path.LineTo(end);
			return;
		}
		double num = rotationDegrees * Math.PI / 180.0;
		double cosPhi = Math.Cos(num);
		double sinPhi = Math.Sin(num);
		double num2 = (start.X - end.X) / 2.0;
		double num3 = (start.Y - end.Y) / 2.0;
		double num4 = cosPhi * num2 + sinPhi * num3;
		double num5 = (0.0 - sinPhi) * num2 + cosPhi * num3;
		double num6 = num4 * num4 / (rx * rx) + num5 * num5 / (ry * ry);
		if (num6 > 1.0)
		{
			rx *= Math.Sqrt(num6);
			ry *= Math.Sqrt(num6);
		}
		double num7 = ((largeArc != sweep) ? 1 : (-1));
		double num8 = Math.Max(0.0, rx * rx * ry * ry - rx * rx * num5 * num5 - ry * ry * num4 * num4);
		double val = rx * rx * num5 * num5 + ry * ry * num4 * num4;
		double num9 = num7 * Math.Sqrt(num8 / Math.Max(val, 2.220446049250313E-16));
		double num10 = num9 * rx * num5 / ry;
		double num11 = (0.0 - num9) * ry * num4 / rx;
		double cx = cosPhi * num10 - sinPhi * num11 + (start.X + end.X) / 2.0;
		double cy = sinPhi * num10 + cosPhi * num11 + (start.Y + end.Y) / 2.0;
		double num12 = Angle(1.0, 0.0, (num4 - num10) / rx, (num5 - num11) / ry);
		double num13 = Angle((num4 - num10) / rx, (num5 - num11) / ry, (0.0 - num4 - num10) / rx, (0.0 - num5 - num11) / ry);
		if (!sweep && num13 > 0.0)
		{
			num13 -= Math.PI * 2.0;
		}
		else if (sweep && num13 < 0.0)
		{
			num13 += Math.PI * 2.0;
		}
		int num14 = Math.Max(1, (int)Math.Ceiling(Math.Abs(num13) / (Math.PI / 2.0)));
		double num15 = num13 / (double)num14;
		double num16 = 1.3333333333333333 * Math.Tan(num15 / 4.0);
		double num17 = num12;
		for (int i = 0; i < num14; i++)
		{
			double num18 = num17 + num15;
			Point point = On(num17);
			Point to = On(num18);
			Point point2 = D(num17);
			Point point3 = D(num18);
			path.CubicTo(to, new Point(point.X + num16 * point2.X, point.Y + num16 * point2.Y), new Point(to.X - num16 * point3.X, to.Y - num16 * point3.Y));
			num17 = num18;
		}
		static double Angle(double ux, double uy, double vx, double vy)
		{
			double num19 = ux * vx + uy * vy;
			double num20 = Math.Sqrt((ux * ux + uy * uy) * (vx * vx + vy * vy));
			double num21 = Math.Acos(Math.Max(-1.0, Math.Min(1.0, num19 / num20)));
			if (ux * vy - uy * vx < 0.0)
			{
				num21 = 0.0 - num21;
			}
			return num21;
		}
		Point D(double t)
		{
			return new Point((0.0 - rx) * Math.Sin(t) * cosPhi - ry * Math.Cos(t) * sinPhi, (0.0 - rx) * Math.Sin(t) * sinPhi + ry * Math.Cos(t) * cosPhi);
		}
		Point On(double t)
		{
			return new Point(cx + rx * Math.Cos(t) * cosPhi - ry * Math.Sin(t) * sinPhi, cy + rx * Math.Cos(t) * sinPhi + ry * Math.Sin(t) * cosPhi);
		}
	}
}
