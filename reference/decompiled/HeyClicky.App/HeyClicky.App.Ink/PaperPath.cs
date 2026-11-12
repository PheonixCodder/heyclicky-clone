using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class PaperPath
{
	private enum Kind
	{
		Move,
		Line,
		Quad,
		Cubic,
		Close
	}

	private readonly record struct Command(Kind Kind, Point A, Point B, Point C);

	private readonly List<Command> commands = new List<Command>();

	private Point current;

	private Point subpathStart;

	private bool hasCurrent;

	public bool IsEmpty => commands.Count == 0;

	public Point CurrentPoint => current;

	public double Length => Flattened().Sum<(List<Point>, bool)>(((List<Point> Points, bool Closed) s) => PolylineLength(s.Points));

	public PaperPath MoveTo(double x, double y)
	{
		return MoveTo(new Point(x, y));
	}

	public PaperPath MoveTo(Point p)
	{
		commands.Add(new Command(Kind.Move, p, default(Point), default(Point)));
		current = (subpathStart = p);
		hasCurrent = true;
		return this;
	}

	public PaperPath LineTo(double x, double y)
	{
		return LineTo(new Point(x, y));
	}

	public PaperPath LineTo(Point p)
	{
		if (!hasCurrent)
		{
			return MoveTo(p);
		}
		commands.Add(new Command(Kind.Line, p, default(Point), default(Point)));
		current = p;
		return this;
	}

	public PaperPath QuadTo(Point to, Point control)
	{
		if (!hasCurrent)
		{
			MoveTo(control);
		}
		commands.Add(new Command(Kind.Quad, control, to, default(Point)));
		current = to;
		return this;
	}

	public PaperPath CubicTo(Point to, Point control1, Point control2)
	{
		if (!hasCurrent)
		{
			MoveTo(control1);
		}
		commands.Add(new Command(Kind.Cubic, control1, control2, to));
		current = to;
		return this;
	}

	public PaperPath Close()
	{
		if (!hasCurrent)
		{
			return this;
		}
		commands.Add(new Command(Kind.Close, default(Point), default(Point), default(Point)));
		current = subpathStart;
		return this;
	}

	public PaperPath AddLines(IReadOnlyList<Point> points)
	{
		if (points.Count == 0)
		{
			return this;
		}
		MoveTo(points[0]);
		for (int i = 1; i < points.Count; i++)
		{
			LineTo(points[i]);
		}
		return this;
	}

	public PaperPath AddArc(Point center, double radius, double startAngle, double endAngle, bool clockwise)
	{
		double num = endAngle - startAngle;
		if (clockwise)
		{
			while (num > 0.0)
			{
				num -= Math.PI * 2.0;
			}
			for (; num <= Math.PI * -2.0; num += Math.PI * 2.0)
			{
			}
		}
		else
		{
			for (; num < 0.0; num += Math.PI * 2.0)
			{
			}
			while (num >= Math.PI * 2.0)
			{
				num -= Math.PI * 2.0;
			}
		}
		Point p = new Point(center.X + radius * Math.Cos(startAngle), center.Y + radius * Math.Sin(startAngle));
		if (hasCurrent)
		{
			LineTo(p);
		}
		else
		{
			MoveTo(p);
		}
		AppendArcBeziers(center, radius, radius, 0.0, startAngle, num);
		return this;
	}

	public PaperPath AddEllipse(Rect rect)
	{
		Point center = new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0);
		double num = rect.Width / 2.0;
		double ry = rect.Height / 2.0;
		MoveTo(new Point(center.X + num, center.Y));
		AppendArcBeziers(center, num, ry, 0.0, 0.0, Math.PI * 2.0);
		return Close();
	}

	public PaperPath AddRoundedRect(Rect rect, double radius)
	{
		double num = Math.Max(0.0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2.0));
		if (num <= 0.0)
		{
			return AddRect(rect);
		}
		double x = rect.X;
		double y = rect.Y;
		double width = rect.Width;
		double height = rect.Height;
		MoveTo(new Point(x + num, y));
		LineTo(new Point(x + width - num, y));
		AppendArcBeziers(new Point(x + width - num, y + num), num, num, 0.0, -Math.PI / 2.0, Math.PI / 2.0);
		LineTo(new Point(x + width, y + height - num));
		AppendArcBeziers(new Point(x + width - num, y + height - num), num, num, 0.0, 0.0, Math.PI / 2.0);
		LineTo(new Point(x + num, y + height));
		AppendArcBeziers(new Point(x + num, y + height - num), num, num, 0.0, Math.PI / 2.0, Math.PI / 2.0);
		LineTo(new Point(x, y + num));
		AppendArcBeziers(new Point(x + num, y + num), num, num, 0.0, Math.PI, Math.PI / 2.0);
		return Close();
	}

	public PaperPath AddUnevenRoundedRect(Rect rect, double topLeading, double bottomLeading, double bottomTrailing, double topTrailing)
	{
		double x = rect.X;
		double y = rect.Y;
		double width = rect.Width;
		double height = rect.Height;
		double val = Math.Min(width, height) / 2.0;
		double num = Math.Min(topLeading, val);
		double num2 = Math.Min(bottomLeading, val);
		double num3 = Math.Min(bottomTrailing, val);
		double num4 = Math.Min(topTrailing, val);
		MoveTo(new Point(x + num, y));
		LineTo(new Point(x + width - num4, y));
		if (num4 > 0.0)
		{
			AppendArcBeziers(new Point(x + width - num4, y + num4), num4, num4, 0.0, -Math.PI / 2.0, Math.PI / 2.0);
		}
		LineTo(new Point(x + width, y + height - num3));
		if (num3 > 0.0)
		{
			AppendArcBeziers(new Point(x + width - num3, y + height - num3), num3, num3, 0.0, 0.0, Math.PI / 2.0);
		}
		LineTo(new Point(x + num2, y + height));
		if (num2 > 0.0)
		{
			AppendArcBeziers(new Point(x + num2, y + height - num2), num2, num2, 0.0, Math.PI / 2.0, Math.PI / 2.0);
		}
		LineTo(new Point(x, y + num));
		if (num > 0.0)
		{
			AppendArcBeziers(new Point(x + num, y + num), num, num, 0.0, Math.PI, Math.PI / 2.0);
		}
		return Close();
	}

	public PaperPath AddRect(Rect rect)
	{
		MoveTo(new Point(rect.X, rect.Y));
		LineTo(new Point(rect.X + rect.Width, rect.Y));
		LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height));
		LineTo(new Point(rect.X, rect.Y + rect.Height));
		return Close();
	}

	public PaperPath AddCapsule(Rect rect)
	{
		return AddRoundedRect(rect, Math.Min(rect.Width, rect.Height) / 2.0);
	}

	public PaperPath Transformed(double scaleX, double scaleY, double dx = 0.0, double dy = 0.0)
	{
		PaperPath paperPath = new PaperPath();
		foreach (Command command in commands)
		{
			switch (command.Kind)
			{
			case Kind.Move:
				paperPath.MoveTo(T(command.A));
				break;
			case Kind.Line:
				paperPath.LineTo(T(command.A));
				break;
			case Kind.Quad:
				paperPath.QuadTo(T(command.B), T(command.A));
				break;
			case Kind.Cubic:
				paperPath.CubicTo(T(command.C), T(command.A), T(command.B));
				break;
			case Kind.Close:
				paperPath.Close();
				break;
			}
		}
		return paperPath;
		Point T(Point p)
		{
			return new Point(p.X * scaleX + dx, p.Y * scaleY + dy);
		}
	}

	public PaperPath Append(PaperPath other)
	{
		foreach (Command command in other.commands)
		{
			switch (command.Kind)
			{
			case Kind.Move:
				MoveTo(command.A);
				break;
			case Kind.Line:
				LineTo(command.A);
				break;
			case Kind.Quad:
				QuadTo(command.B, command.A);
				break;
			case Kind.Cubic:
				CubicTo(command.C, command.A, command.B);
				break;
			case Kind.Close:
				Close();
				break;
			}
		}
		return this;
	}

	private void AppendArcBeziers(Point center, double rx, double ry, double rotation, double startAngle, double sweep)
	{
		double cosPhi;
		double sinPhi;
		if (!(Math.Abs(sweep) < 1E-12))
		{
			int num = Math.Max(1, (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 2.0) - 1E-09));
			double num2 = sweep / (double)num;
			double num3 = 1.3333333333333333 * Math.Tan(num2 / 4.0);
			cosPhi = Math.Cos(rotation);
			sinPhi = Math.Sin(rotation);
			double num4 = startAngle;
			for (int i = 0; i < num; i++)
			{
				double num5 = num4 + num2;
				Point point = On(num4);
				Point to = On(num5);
				Point point2 = D(num4);
				Point point3 = D(num5);
				CubicTo(to, new Point(point.X + num3 * point2.X, point.Y + num3 * point2.Y), new Point(to.X - num3 * point3.X, to.Y - num3 * point3.Y));
				num4 = num5;
			}
		}
		Point D(double t)
		{
			return new Point((0.0 - rx) * Math.Sin(t) * cosPhi - ry * Math.Cos(t) * sinPhi, (0.0 - rx) * Math.Sin(t) * sinPhi + ry * Math.Cos(t) * cosPhi);
		}
		Point On(double t)
		{
			return new Point(center.X + rx * Math.Cos(t) * cosPhi - ry * Math.Sin(t) * sinPhi, center.Y + rx * Math.Cos(t) * sinPhi + ry * Math.Sin(t) * cosPhi);
		}
	}

	public List<(List<Point> Points, bool Closed)> Flattened()
	{
		List<(List<Point>, bool)> result = new List<(List<Point>, bool)>();
		List<Point> pts = null;
		Point last = default(Point);
		Point start = default(Point);
		foreach (Command command in commands)
		{
			switch (command.Kind)
			{
			case Kind.Move:
				Begin(command.A);
				break;
			case Kind.Line:
				if (pts == null)
				{
					Begin(last);
				}
				pts.Add(command.A);
				last = command.A;
				break;
			case Kind.Quad:
			{
				if (pts == null)
				{
					Begin(last);
				}
				Point a2 = last;
				int num = Steps(Distance(a2, command.A) + Distance(command.A, command.B));
				for (int i = 1; i <= num; i++)
				{
					double num2 = (double)i / (double)num;
					double num3 = 1.0 - num2;
					pts.Add(new Point(num3 * num3 * a2.X + 2.0 * num3 * num2 * command.A.X + num2 * num2 * command.B.X, num3 * num3 * a2.Y + 2.0 * num3 * num2 * command.A.Y + num2 * num2 * command.B.Y));
				}
				last = command.B;
				break;
			}
			case Kind.Cubic:
			{
				if (pts == null)
				{
					Begin(last);
				}
				Point a3 = last;
				int num4 = Steps(Distance(a3, command.A) + Distance(command.A, command.B) + Distance(command.B, command.C));
				for (int j = 1; j <= num4; j++)
				{
					double num5 = (double)j / (double)num4;
					double num6 = 1.0 - num5;
					double num7 = num6 * num6 * num6;
					double num8 = 3.0 * num6 * num6 * num5;
					double num9 = 3.0 * num6 * num5 * num5;
					double num10 = num5 * num5 * num5;
					pts.Add(new Point(num7 * a3.X + num8 * command.A.X + num9 * command.B.X + num10 * command.C.X, num7 * a3.Y + num8 * command.A.Y + num9 * command.B.Y + num10 * command.C.Y));
				}
				last = command.C;
				break;
			}
			case Kind.Close:
				if (pts != null && pts.Count > 0)
				{
					pts.Add(start);
					result.Add((pts, true));
				}
				pts = null;
				last = start;
				break;
			}
		}
		if (pts != null && pts.Count > 0)
		{
			result.Add((pts, false));
		}
		return result;
		void Begin(Point p)
		{
			if (pts != null && pts.Count > 0)
			{
				result.Add((pts, false));
			}
			int num11 = 1;
			List<Point> list = new List<Point>(num11);
			CollectionsMarshal.SetCount(list, num11);
			Span<Point> span = CollectionsMarshal.AsSpan(list);
			int index = 0;
			span[index] = p;
			pts = list;
			last = (start = p);
		}
		static double Distance(Point a, Point b)
		{
			return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
		}
		static int Steps(double hull)
		{
			return Math.Clamp((int)Math.Ceiling(hull / 2.0), 4, 64);
		}
	}

	private static double PolylineLength(List<Point> points)
	{
		double num = 0.0;
		for (int i = 1; i < points.Count; i++)
		{
			num += Math.Sqrt(Math.Pow(points[i].X - points[i - 1].X, 2.0) + Math.Pow(points[i].Y - points[i - 1].Y, 2.0));
		}
		return num;
	}

	public PaperPath Trimmed(double from, double to)
	{
		from = Math.Clamp(from, 0.0, 1.0);
		to = Math.Clamp(to, 0.0, 1.0);
		PaperPath paperPath = new PaperPath();
		if (to <= from)
		{
			return paperPath;
		}
		List<(List<Point>, bool)> list = Flattened();
		double num = list.Sum<(List<Point>, bool)>(((List<Point> Points, bool Closed) s) => PolylineLength(s.Points));
		if (num <= 0.0)
		{
			if (!(from <= 0.0) || !(to >= 1.0))
			{
				return paperPath;
			}
			return this;
		}
		double num2 = from * num;
		double num3 = to * num;
		double num4 = 0.0;
		foreach (var item2 in list)
		{
			List<Point> item = item2.Item1;
			bool flag = false;
			for (int i = 1; i < item.Count; i++)
			{
				Point point = item[i - 1];
				Point point2 = item[i];
				double num5 = Math.Sqrt((point2.X - point.X) * (point2.X - point.X) + (point2.Y - point.Y) * (point2.Y - point.Y));
				double num6 = num4;
				if ((num4 += num5) < num2 || num6 > num3 || num5 <= 0.0)
				{
					if (num6 > num3)
					{
						return paperPath;
					}
					continue;
				}
				double num7 = Math.Max(0.0, (num2 - num6) / num5);
				double num8 = Math.Min(1.0, (num3 - num6) / num5);
				Point p = new Point(point.X + (point2.X - point.X) * num7, point.Y + (point2.Y - point.Y) * num7);
				Point p2 = new Point(point.X + (point2.X - point.X) * num8, point.Y + (point2.Y - point.Y) * num8);
				if (!flag)
				{
					paperPath.MoveTo(p);
					flag = true;
				}
				paperPath.LineTo(p2);
			}
		}
		return paperPath;
	}

	internal void AppendSvgArcBeziers(Point center, double rx, double ry, double rotation, double startAngle, double sweep)
	{
		AppendArcBeziers(center, rx, ry, rotation, startAngle, sweep);
	}

	public Geometry ToGeometry()
	{
		PathGeometry pathGeometry = new PathGeometry
		{
			FillRule = FillRule.Nonzero
		};
		PathFigure pathFigure = null;
		foreach (Command command in commands)
		{
			switch (command.Kind)
			{
			case Kind.Move:
				pathFigure = new PathFigure
				{
					StartPoint = command.A,
					IsFilled = true,
					IsClosed = false
				};
				pathGeometry.Figures.Add(pathFigure);
				break;
			case Kind.Line:
				pathFigure.Segments.Add(new LineSegment
				{
					Point = command.A
				});
				break;
			case Kind.Quad:
				pathFigure.Segments.Add(new QuadraticBezierSegment
				{
					Point1 = command.A,
					Point2 = command.B
				});
				break;
			case Kind.Cubic:
				pathFigure.Segments.Add(new BezierSegment
				{
					Point1 = command.A,
					Point2 = command.B,
					Point3 = command.C
				});
				break;
			case Kind.Close:
				if ((object)pathFigure != null)
				{
					pathFigure.IsClosed = true;
				}
				pathFigure = null;
				break;
			}
		}
		return pathGeometry;
	}

	public CanvasGeometry ToCanvasGeometry(ICanvasResourceCreator creator)
	{
		using (CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(creator))
		{
			canvasPathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
			bool flag = false;
			foreach (Command command in commands)
			{
				switch (command.Kind)
				{
				case Kind.Move:
					if (flag)
					{
						canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
					}
					canvasPathBuilder.BeginFigure(V(command.A));
					flag = true;
					_ = command.A;
					break;
				case Kind.Line:
					canvasPathBuilder.AddLine(V(command.A));
					_ = command.A;
					break;
				case Kind.Quad:
					canvasPathBuilder.AddQuadraticBezier(V(command.A), V(command.B));
					_ = command.B;
					break;
				case Kind.Cubic:
					canvasPathBuilder.AddCubicBezier(V(command.A), V(command.B), V(command.C));
					_ = command.C;
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
			return CanvasGeometry.CreatePath(canvasPathBuilder);
		}
		static Vector2 V(Point p)
		{
			return new Vector2((float)p.X, (float)p.Y);
		}
	}
}
