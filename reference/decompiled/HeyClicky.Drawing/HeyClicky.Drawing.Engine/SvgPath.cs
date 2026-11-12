using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;

namespace HeyClicky.Drawing.Engine;

internal sealed class SvgPath
{
	public enum Kind : byte
	{
		Move,
		Line,
		Quad,
		Cubic,
		Close
	}

	public readonly struct Segment
	{
		public readonly Kind Kind;

		public readonly Vector2 A;

		public readonly Vector2 B;

		public readonly Vector2 C;

		public Segment(Kind kind, Vector2 a = default(Vector2), Vector2 b = default(Vector2), Vector2 c = default(Vector2))
		{
			Kind = kind;
			A = a;
			B = b;
			C = c;
		}
	}

	public sealed class Measure
	{
		private readonly Vector2[] starts;

		private readonly Vector2[] ends;

		private readonly double[] startLengths;

		private readonly double[] lengths;

		private readonly Vector2? firstPoint;

		public double TotalLength { get; }

		public Measure(SvgPath path)
		{
			List<Vector2> list = new List<Vector2>();
			List<Vector2> list2 = new List<Vector2>();
			List<double> list3 = new List<double>();
			List<double> list4 = new List<double>();
			double num = 0.0;
			foreach (List<Vector2> item3 in path.FlattenedSubpaths())
			{
				Vector2 valueOrDefault = firstPoint.GetValueOrDefault();
				if (!firstPoint.HasValue)
				{
					valueOrDefault = item3[0];
					firstPoint = valueOrDefault;
				}
				for (int i = 1; i < item3.Count; i++)
				{
					Vector2 item = item3[i - 1];
					Vector2 item2 = item3[i];
					double num2 = Math.Sqrt((double)(item2.X - item.X) * (double)(item2.X - item.X) + (double)(item2.Y - item.Y) * (double)(item2.Y - item.Y));
					if (!(num2 <= 0.0))
					{
						list.Add(item);
						list2.Add(item2);
						list3.Add(num);
						list4.Add(num2);
						num += num2;
					}
				}
			}
			starts = list.ToArray();
			ends = list2.ToArray();
			startLengths = list3.ToArray();
			lengths = list4.ToArray();
			TotalLength = num;
		}

		public Vector2 PointAt(double distance)
		{
			if (starts.Length == 0)
			{
				return firstPoint ?? Vector2.Zero;
			}
			if (distance <= 0.0)
			{
				return starts[0];
			}
			if (distance >= TotalLength)
			{
				return ends[^1];
			}
			int num = 0;
			int num2 = starts.Length - 1;
			while (num < num2)
			{
				int num3 = (num + num2 + 1) / 2;
				if (startLengths[num3] <= distance)
				{
					num = num3;
				}
				else
				{
					num2 = num3 - 1;
				}
			}
			float num4 = (float)((distance - startLengths[num]) / lengths[num]);
			return starts[num] + (ends[num] - starts[num]) * num4;
		}
	}

	private ref struct NumberScanner
	{
		private readonly ReadOnlySpan<char> text;

		private int index;

		public NumberScanner(ReadOnlySpan<char> text)
		{
			this.text = text;
			index = 0;
		}

		public bool Peek(out char c)
		{
			if (index < text.Length)
			{
				c = text[index];
				return true;
			}
			c = '\0';
			return false;
		}

		public void Advance()
		{
			index++;
		}

		public void SkipSeparators()
		{
			while (true)
			{
				bool flag = index < text.Length;
				if (flag)
				{
					bool flag2;
					switch (text[index])
					{
					case '\t':
					case '\n':
					case '\f':
					case '\r':
					case ' ':
					case ',':
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					flag = flag2;
				}
				if (flag)
				{
					index++;
					continue;
				}
				break;
			}
		}

		public bool Flag(out bool value)
		{
			SkipSeparators();
			value = false;
			bool flag = index >= text.Length;
			if (!flag)
			{
				char c = text[index];
				bool flag2 = ((c == '0' || c == '1') ? true : false);
				flag = !flag2;
			}
			if (flag)
			{
				return false;
			}
			value = text[index] == '1';
			index++;
			return true;
		}

		public bool Number(out float value)
		{
			SkipSeparators();
			value = 0f;
			int num = index;
			bool flag = index < text.Length;
			if (flag)
			{
				char c = text[index];
				bool flag2 = ((c == '+' || c == '-') ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				index++;
			}
			bool flag3 = false;
			while (index < text.Length && char.IsAsciiDigit(text[index]))
			{
				index++;
				flag3 = true;
			}
			if (index < text.Length && text[index] == '.')
			{
				index++;
				while (index < text.Length && char.IsAsciiDigit(text[index]))
				{
					index++;
					flag3 = true;
				}
			}
			if (!flag3)
			{
				index = num;
				return false;
			}
			flag = index < text.Length;
			if (flag)
			{
				char c = text[index];
				bool flag2 = ((c == 'E' || c == 'e') ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				int num2 = index + 1;
				flag = num2 < text.Length;
				if (flag)
				{
					char c = text[num2];
					bool flag2 = ((c == '+' || c == '-') ? true : false);
					flag = flag2;
				}
				if (flag)
				{
					num2++;
				}
				bool flag4 = false;
				while (num2 < text.Length && char.IsAsciiDigit(text[num2]))
				{
					num2++;
					flag4 = true;
				}
				if (flag4)
				{
					index = num2;
				}
			}
			ReadOnlySpan<char> readOnlySpan = text;
			int num3 = num;
			if (!double.TryParse(readOnlySpan.Slice(num3, index - num3), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				index = num;
				return false;
			}
			value = (float)result;
			return true;
		}
	}

	public readonly List<Segment> Segments = new List<Segment>();

	public bool IsEmpty => Segments.Count == 0;

	public SvgPath(ReadOnlySpan<char> data)
	{
		Parse(data);
	}

	public SvgPath(string data)
		: this(data.AsSpan())
	{
	}

	public CanvasGeometry ToGeometry(ICanvasResourceCreator device, Matrix3x2? transform = null)
	{
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(device);
		canvasPathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
		bool flag = false;
		Vector2 startPoint = Vector2.Zero;
		foreach (Segment segment in Segments)
		{
			switch (segment.Kind)
			{
			case Kind.Move:
				if (flag)
				{
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
				canvasPathBuilder.BeginFigure(segment.A);
				flag = true;
				startPoint = segment.A;
				break;
			case Kind.Line:
				if (!flag)
				{
					canvasPathBuilder.BeginFigure(startPoint);
					flag = true;
				}
				canvasPathBuilder.AddLine(segment.A);
				startPoint = segment.A;
				break;
			case Kind.Quad:
				if (!flag)
				{
					canvasPathBuilder.BeginFigure(startPoint);
					flag = true;
				}
				canvasPathBuilder.AddQuadraticBezier(segment.A, segment.B);
				startPoint = segment.B;
				break;
			case Kind.Cubic:
				if (!flag)
				{
					canvasPathBuilder.BeginFigure(startPoint);
					flag = true;
				}
				canvasPathBuilder.AddCubicBezier(segment.A, segment.B, segment.C);
				startPoint = segment.C;
				break;
			case Kind.Close:
				if (flag)
				{
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
				}
				flag = false;
				break;
			}
		}
		if (flag)
		{
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		}
		CanvasGeometry canvasGeometry = CanvasGeometry.CreatePath(canvasPathBuilder);
		CanvasGeometry result;
		if (transform.HasValue)
		{
			Matrix3x2 valueOrDefault = transform.GetValueOrDefault();
			if (!valueOrDefault.IsIdentity)
			{
				result = canvasGeometry.Transform(valueOrDefault);
				goto IL_015d;
			}
		}
		result = canvasGeometry;
		goto IL_015d;
		IL_015d:
		return result;
	}

	public List<List<Vector2>> FlattenedSubpaths()
	{
		List<List<Vector2>> subpaths = new List<List<Vector2>>();
		List<Vector2> current = null;
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = Vector2.Zero;
		foreach (Segment segment in Segments)
		{
			switch (segment.Kind)
			{
			case Kind.Move:
				Flush();
				current = new List<Vector2> { segment.A };
				vector = (vector2 = segment.A);
				break;
			case Kind.Line:
				if (current == null)
				{
					current = new List<Vector2> { vector2 };
				}
				current.Add(segment.A);
				vector2 = segment.A;
				break;
			case Kind.Quad:
			{
				if (current == null)
				{
					current = new List<Vector2> { vector2 };
				}
				int num4 = SubdivisionCount(vector2, segment.A, segment.B);
				for (int j = 1; j <= num4; j++)
				{
					float num5 = (float)j / (float)num4;
					float num6 = 1f - num5;
					current.Add(num6 * num6 * vector2 + 2f * num6 * num5 * segment.A + num5 * num5 * segment.B);
				}
				vector2 = segment.B;
				break;
			}
			case Kind.Cubic:
			{
				if (current == null)
				{
					current = new List<Vector2> { vector2 };
				}
				int num = SubdivisionCount(vector2, segment.A, segment.B, segment.C);
				for (int i = 1; i <= num; i++)
				{
					float num2 = (float)i / (float)num;
					float num3 = 1f - num2;
					current.Add(num3 * num3 * num3 * vector2 + 3f * num3 * num3 * num2 * segment.A + 3f * num3 * num2 * num2 * segment.B + num2 * num2 * num2 * segment.C);
				}
				vector2 = segment.C;
				break;
			}
			case Kind.Close:
				if (current == null)
				{
					current = new List<Vector2> { vector2 };
				}
				current.Add(vector);
				vector2 = vector;
				Flush();
				break;
			}
		}
		Flush();
		return subpaths;
		void Flush()
		{
			if (current != null && current.Count > 0)
			{
				subpaths.Add(current);
			}
			current = null;
		}
	}

	private static int SubdivisionCount(Vector2 a, Vector2 b, Vector2 c)
	{
		return Math.Clamp((int)Math.Ceiling((Vector2.Distance(a, b) + Vector2.Distance(b, c)) / 0.5f), 16, 1024);
	}

	private static int SubdivisionCount(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		return Math.Clamp((int)Math.Ceiling((Vector2.Distance(a, b) + Vector2.Distance(b, c) + Vector2.Distance(c, d)) / 0.5f), 16, 1024);
	}

	public (double X, double Y, double Width, double Height) BoundingBox()
	{
		if (Segments.Count == 0)
		{
			return (X: 0.0, Y: 0.0, Width: 0.0, Height: 0.0);
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		bool flag = false;
		foreach (List<Vector2> item in FlattenedSubpaths())
		{
			foreach (Vector2 item2 in item)
			{
				flag = true;
				if (item2.X < num)
				{
					num = item2.X;
				}
				if (item2.X > num3)
				{
					num3 = item2.X;
				}
				if (item2.Y < num2)
				{
					num2 = item2.Y;
				}
				if (item2.Y > num4)
				{
					num4 = item2.Y;
				}
			}
		}
		(float, float, float, float) tuple = (flag ? (num, num2, num3 - num, num4 - num2) : (0f, 0f, 0f, 0f));
		return (X: tuple.Item1, Y: tuple.Item2, Width: tuple.Item3, Height: tuple.Item4);
	}

	private void Parse(ReadOnlySpan<char> data)
	{
		NumberScanner numberScanner = new NumberScanner(data);
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = Vector2.Zero;
		Vector2? vector3 = null;
		Vector2? vector4 = null;
		char c = '\0';
		while (true)
		{
			numberScanner.SkipSeparators();
			if (!numberScanner.Peek(out var c2))
			{
				break;
			}
			if (IsCommandLetter(c2))
			{
				c = c2;
				numberScanner.Advance();
			}
			else
			{
				if (c == '\0')
				{
					break;
				}
				if (c == 'M')
				{
					c = 'L';
				}
				else if (c == 'm')
				{
					c = 'l';
				}
				else if ((c == 'Z' || c == 'z') ? true : false)
				{
					break;
				}
			}
			bool flag = c >= 'a';
			Vector2 vector5 = (flag ? vector : Vector2.Zero);
			switch (char.ToLowerInvariant(c))
			{
			default:
				return;
			case 'm':
			{
				if (!numberScanner.Number(out var value16) || !numberScanner.Number(out var value17))
				{
					return;
				}
				vector = vector5 + new Vector2(value16, value17);
				vector2 = vector;
				Segments.Add(new Segment(Kind.Move, vector));
				vector4 = null;
				vector3 = vector4;
				break;
			}
			case 'l':
			{
				if (!numberScanner.Number(out var value28) || !numberScanner.Number(out var value29))
				{
					return;
				}
				vector = vector5 + new Vector2(value28, value29);
				Segments.Add(new Segment(Kind.Line, vector));
				vector4 = null;
				vector3 = vector4;
				break;
			}
			case 'h':
			{
				if (!numberScanner.Number(out var value22))
				{
					return;
				}
				vector = new Vector2(flag ? (vector.X + value22) : value22, vector.Y);
				Segments.Add(new Segment(Kind.Line, vector));
				vector4 = null;
				vector3 = vector4;
				break;
			}
			case 'v':
			{
				if (!numberScanner.Number(out var value23))
				{
					return;
				}
				vector = new Vector2(vector.X, flag ? (vector.Y + value23) : value23);
				Segments.Add(new Segment(Kind.Line, vector));
				vector4 = null;
				vector3 = vector4;
				break;
			}
			case 'c':
			{
				if (!numberScanner.Number(out var value10) || !numberScanner.Number(out var value11) || !numberScanner.Number(out var value12) || !numberScanner.Number(out var value13) || !numberScanner.Number(out var value14) || !numberScanner.Number(out var value15))
				{
					return;
				}
				Vector2 vector10 = vector5 + new Vector2(value12, value13);
				Vector2 vector11 = vector5 + new Vector2(value14, value15);
				Segments.Add(new Segment(Kind.Cubic, vector5 + new Vector2(value10, value11), vector10, vector11));
				vector3 = vector10;
				vector4 = null;
				vector = vector11;
				break;
			}
			case 's':
			{
				if (!numberScanner.Number(out var value18) || !numberScanner.Number(out var value19) || !numberScanner.Number(out var value20) || !numberScanner.Number(out var value21))
				{
					return;
				}
				Vector2 vector12;
				if (vector3.HasValue)
				{
					Vector2 valueOrDefault2 = vector3.GetValueOrDefault();
					vector12 = 2f * vector - valueOrDefault2;
				}
				else
				{
					vector12 = vector;
				}
				Vector2 a = vector12;
				Vector2 vector13 = vector5 + new Vector2(value18, value19);
				Vector2 vector14 = vector5 + new Vector2(value20, value21);
				Segments.Add(new Segment(Kind.Cubic, a, vector13, vector14));
				vector3 = vector13;
				vector4 = null;
				vector = vector14;
				break;
			}
			case 'q':
			{
				if (!numberScanner.Number(out var value24) || !numberScanner.Number(out var value25) || !numberScanner.Number(out var value26) || !numberScanner.Number(out var value27))
				{
					return;
				}
				Vector2 vector15 = vector5 + new Vector2(value24, value25);
				Vector2 vector16 = vector5 + new Vector2(value26, value27);
				Segments.Add(new Segment(Kind.Quad, vector15, vector16));
				vector4 = vector15;
				vector3 = null;
				vector = vector16;
				break;
			}
			case 't':
			{
				if (!numberScanner.Number(out var value) || !numberScanner.Number(out var value2))
				{
					return;
				}
				Vector2 vector6;
				if (vector4.HasValue)
				{
					Vector2 valueOrDefault = vector4.GetValueOrDefault();
					vector6 = 2f * vector - valueOrDefault;
				}
				else
				{
					vector6 = vector;
				}
				Vector2 vector7 = vector6;
				Vector2 vector8 = vector5 + new Vector2(value, value2);
				Segments.Add(new Segment(Kind.Quad, vector7, vector8));
				vector4 = vector7;
				vector3 = null;
				vector = vector8;
				break;
			}
			case 'a':
			{
				if (!numberScanner.Number(out var value3) || !numberScanner.Number(out var value4) || !numberScanner.Number(out var value5) || !numberScanner.Flag(out var value6) || !numberScanner.Flag(out var value7) || !numberScanner.Number(out var value8) || !numberScanner.Number(out var value9))
				{
					return;
				}
				Vector2 vector9 = vector5 + new Vector2(value8, value9);
				AddArc(vector, vector9, value3, value4, value5, value6, value7);
				vector4 = null;
				vector3 = vector4;
				vector = vector9;
				break;
			}
			case 'z':
			{
				Segments.Add(new Segment(Kind.Close));
				vector = vector2;
				vector4 = null;
				vector3 = vector4;
				numberScanner.SkipSeparators();
				if (numberScanner.Peek(out var c3) && !IsCommandLetter(c3))
				{
					return;
				}
				break;
			}
			}
		}
	}

	private static bool IsCommandLetter(char c)
	{
		switch (c)
		{
		case 'A':
		case 'C':
		case 'H':
		case 'L':
		case 'M':
		case 'Q':
		case 'S':
		case 'T':
		case 'V':
		case 'Z':
		case 'a':
		case 'c':
		case 'h':
		case 'l':
		case 'm':
		case 'q':
		case 's':
		case 't':
		case 'v':
		case 'z':
			return true;
		default:
			return false;
		}
	}

	private void AddArc(Vector2 start, Vector2 end, double radiusX, double radiusY, double rotationDegrees, bool largeArc, bool sweep)
	{
		if (start == end)
		{
			return;
		}
		double rx = Math.Abs(radiusX);
		double ry = Math.Abs(radiusY);
		if (rx == 0.0 || ry == 0.0)
		{
			Segments.Add(new Segment(Kind.Line, end));
			return;
		}
		double num = rotationDegrees * Math.PI / 180.0;
		double cosPhi = Math.Cos(num);
		double sinPhi = Math.Sin(num);
		double num2 = (double)(start.X - end.X) / 2.0;
		double num3 = (double)(start.Y - end.Y) / 2.0;
		double num4 = cosPhi * num2 + sinPhi * num3;
		double num5 = (0.0 - sinPhi) * num2 + cosPhi * num3;
		double num6 = num4 * num4 / (rx * rx) + num5 * num5 / (ry * ry);
		if (num6 > 1.0)
		{
			double num7 = Math.Sqrt(num6);
			rx *= num7;
			ry *= num7;
		}
		double num8 = rx * rx * ry * ry - rx * rx * num5 * num5 - ry * ry * num4 * num4;
		double num9 = rx * rx * num5 * num5 + ry * ry * num4 * num4;
		double num10 = ((num9 == 0.0) ? 0.0 : Math.Sqrt(Math.Max(0.0, num8 / num9)));
		if (largeArc == sweep)
		{
			num10 = 0.0 - num10;
		}
		double num11 = num10 * rx * num5 / ry;
		double num12 = (0.0 - num10) * ry * num4 / rx;
		double centerX = cosPhi * num11 - sinPhi * num12 + (double)(start.X + end.X) / 2.0;
		double centerY = sinPhi * num11 + cosPhi * num12 + (double)(start.Y + end.Y) / 2.0;
		double num13 = Angle(1.0, 0.0, (num4 - num11) / rx, (num5 - num12) / ry);
		double num14 = Angle((num4 - num11) / rx, (num5 - num12) / ry, (0.0 - num4 - num11) / rx, (0.0 - num5 - num12) / ry);
		if (!sweep && num14 > 0.0)
		{
			num14 -= Math.PI * 2.0;
		}
		if (sweep && num14 < 0.0)
		{
			num14 += Math.PI * 2.0;
		}
		int num15 = Math.Max(1, (int)Math.Ceiling(Math.Abs(num14) / (Math.PI / 2.0)));
		double num16 = num14 / (double)num15;
		double num17 = 1.3333333333333333 * Math.Tan(num16 / 4.0);
		double num18 = num13;
		for (int i = 0; i < num15; i++)
		{
			double num19 = num18 + num16;
			Vector2 vector = Point(num18);
			Vector2 vector2 = ((i == num15 - 1) ? end : Point(num19));
			Vector2 vector3 = Derivative(num18);
			Vector2 vector4 = Derivative(num19);
			Segments.Add(new Segment(Kind.Cubic, vector + (float)num17 * vector3, vector2 - (float)num17 * vector4, vector2));
			num18 = num19;
		}
		static double Angle(double ux, double uy, double vx, double vy)
		{
			double num20 = ((!(ux * vy - uy * vx < 0.0)) ? 1 : (-1));
			double num21 = ux * vx + uy * vy;
			double num22 = Math.Sqrt(ux * ux + uy * uy) * Math.Sqrt(vx * vx + vy * vy);
			return num20 * Math.Acos(Math.Clamp((num22 == 0.0) ? 1.0 : (num21 / num22), -1.0, 1.0));
		}
		Vector2 Derivative(double theta)
		{
			return new Vector2((float)((0.0 - rx) * Math.Sin(theta) * cosPhi - ry * Math.Cos(theta) * sinPhi), (float)((0.0 - rx) * Math.Sin(theta) * sinPhi + ry * Math.Cos(theta) * cosPhi));
		}
		Vector2 Point(double theta)
		{
			return new Vector2((float)(centerX + rx * Math.Cos(theta) * cosPhi - ry * Math.Sin(theta) * sinPhi), (float)(centerY + rx * Math.Cos(theta) * sinPhi + ry * Math.Sin(theta) * cosPhi));
		}
	}
}
