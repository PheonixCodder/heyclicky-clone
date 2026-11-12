using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace HeyClicky.App.Portraits;

public sealed class PPath
{
	private enum K : byte
	{
		M,
		L,
		C,
		Q,
		Z
	}

	private readonly record struct Cmd(K Kind, Vector2 A, Vector2 B, Vector2 C);

	[CompilerGenerated]
	private sealed class _003CCubicExtrema_003Ed__25 : IEnumerable<double>, IEnumerable, IEnumerator<double>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private double _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private double p0;

		public double _003C_003E3__p0;

		private double p1;

		public double _003C_003E3__p1;

		private double p2;

		public double _003C_003E3__p2;

		private double p3;

		public double _003C_003E3__p3;

		private double[] _003C_003E7__wrap1;

		private int _003C_003E7__wrap2;

		double IEnumerator<double>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CCubicExtrema_003Ed__25(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E7__wrap1 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
			{
				_003C_003E1__state = -1;
				double num = 0.0 - p0 + 3.0 * p1 - 3.0 * p2 + p3;
				double num2 = 2.0 * (p0 - 2.0 * p1 + p2);
				double num3 = p1 - p0;
				if (Math.Abs(num) < 1E-09)
				{
					if (Math.Abs(num2) > 1E-09)
					{
						double num4 = (0.0 - num3) / num2;
						if (num4 > 0.0 && num4 < 1.0)
						{
							_003C_003E2__current = num4;
							_003C_003E1__state = 1;
							return true;
						}
					}
					goto IL_00e4;
				}
				double num5 = num2 * num2 - 4.0 * num * num3;
				if (num5 < 0.0)
				{
					return false;
				}
				double num6 = Math.Sqrt(num5);
				_003C_003E7__wrap1 = new double[2]
				{
					(0.0 - num2 + num6) / (2.0 * num),
					(0.0 - num2 - num6) / (2.0 * num)
				};
				_003C_003E7__wrap2 = 0;
				goto IL_019d;
			}
			case 1:
				_003C_003E1__state = -1;
				goto IL_00e4;
			case 2:
				{
					_003C_003E1__state = -1;
					goto IL_018f;
				}
				IL_00e4:
				return false;
				IL_018f:
				_003C_003E7__wrap2++;
				goto IL_019d;
				IL_019d:
				if (_003C_003E7__wrap2 < _003C_003E7__wrap1.Length)
				{
					double num7 = _003C_003E7__wrap1[_003C_003E7__wrap2];
					if (num7 > 0.0 && num7 < 1.0)
					{
						_003C_003E2__current = num7;
						_003C_003E1__state = 2;
						return true;
					}
					goto IL_018f;
				}
				_003C_003E7__wrap1 = null;
				return false;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<double> IEnumerable<double>.GetEnumerator()
		{
			_003CCubicExtrema_003Ed__25 _003CCubicExtrema_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CCubicExtrema_003Ed__ = this;
			}
			else
			{
				_003CCubicExtrema_003Ed__ = new _003CCubicExtrema_003Ed__25(0);
			}
			_003CCubicExtrema_003Ed__.p0 = _003C_003E3__p0;
			_003CCubicExtrema_003Ed__.p1 = _003C_003E3__p1;
			_003CCubicExtrema_003Ed__.p2 = _003C_003E3__p2;
			_003CCubicExtrema_003Ed__.p3 = _003C_003E3__p3;
			return _003CCubicExtrema_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<double>)this).GetEnumerator();
		}
	}

	private readonly List<Cmd> cmds = new List<Cmd>();

	private Vector2 current;

	private Vector2 figureStart;

	private bool open;

	private const double Kappa = 0.5522847498;

	public bool IsEmpty => cmds.Count == 0;

	public PPath M(double x, double y)
	{
		Vector2 a = V(x, y);
		cmds.Add(new Cmd(K.M, a, default(Vector2), default(Vector2)));
		current = (figureStart = a);
		open = true;
		return this;
	}

	public PPath L(double x, double y)
	{
		EnsureOpen();
		Vector2 a = V(x, y);
		cmds.Add(new Cmd(K.L, a, default(Vector2), default(Vector2)));
		current = a;
		return this;
	}

	public PPath C(double x1, double y1, double x2, double y2, double x, double y)
	{
		EnsureOpen();
		cmds.Add(new Cmd(K.C, V(x1, y1), V(x2, y2), V(x, y)));
		current = V(x, y);
		return this;
	}

	public PPath Q(double cx, double cy, double x, double y)
	{
		EnsureOpen();
		cmds.Add(new Cmd(K.Q, V(cx, cy), V(x, y), default(Vector2)));
		current = V(x, y);
		return this;
	}

	public PPath Z()
	{
		if (open)
		{
			cmds.Add(new Cmd(K.Z, default(Vector2), default(Vector2), default(Vector2)));
			current = figureStart;
			open = false;
		}
		return this;
	}

	private void EnsureOpen()
	{
		if (!open)
		{
			M(current.X, current.Y);
		}
	}

	private static Vector2 V(double x, double y)
	{
		return new Vector2((float)x, (float)y);
	}

	public PPath Ellipse(double x, double y, double w, double h)
	{
		double num = w / 2.0;
		double num2 = h / 2.0;
		double num3 = x + num;
		double num4 = y + num2;
		double num5 = num * 0.5522847498;
		double num6 = num2 * 0.5522847498;
		M(num3 + num, num4);
		C(num3 + num, num4 + num6, num3 + num5, num4 + num2, num3, num4 + num2);
		C(num3 - num5, num4 + num2, num3 - num, num4 + num6, num3 - num, num4);
		C(num3 - num, num4 - num6, num3 - num5, num4 - num2, num3, num4 - num2);
		C(num3 + num5, num4 - num2, num3 + num, num4 - num6, num3 + num, num4);
		return Z();
	}

	public PPath RoundedRect(double x, double y, double w, double h, double r)
	{
		r = Math.Min(r, Math.Min(w, h) / 2.0);
		double num = r * 0.4477152502;
		M(x + r, y);
		L(x + w - r, y);
		C(x + w - num, y, x + w, y + num, x + w, y + r);
		L(x + w, y + h - r);
		C(x + w, y + h - num, x + w - num, y + h, x + w - r, y + h);
		L(x + r, y + h);
		C(x + num, y + h, x, y + h - num, x, y + h - r);
		L(x, y + r);
		C(x, y + num, x + num, y, x + r, y);
		return Z();
	}

	public static PPath Oval(double x, double y, double w, double h)
	{
		return new PPath().Ellipse(x, y, w, h);
	}

	public static PPath Rounded(double x, double y, double w, double h, double r)
	{
		return new PPath().RoundedRect(x, y, w, h, r);
	}

	public PPath Append(PPath other)
	{
		foreach (Cmd cmd in other.cmds)
		{
			cmds.Add(cmd);
		}
		current = other.current;
		figureStart = other.figureStart;
		open = other.open;
		return this;
	}

	public PPath Transformed(Matrix3x2 m)
	{
		PPath pPath = new PPath();
		foreach (Cmd cmd in cmds)
		{
			pPath.cmds.Add(new Cmd(cmd.Kind, Vector2.Transform(cmd.A, m), Vector2.Transform(cmd.B, m), Vector2.Transform(cmd.C, m)));
		}
		pPath.current = Vector2.Transform(current, m);
		pPath.figureStart = Vector2.Transform(figureStart, m);
		pPath.open = open;
		return pPath;
	}

	public PathGeometry ToXaml()
	{
		PathGeometry pathGeometry = new PathGeometry
		{
			FillRule = FillRule.Nonzero
		};
		PathFigure pathFigure = null;
		foreach (Cmd cmd in cmds)
		{
			switch (cmd.Kind)
			{
			case K.M:
				pathFigure = new PathFigure
				{
					StartPoint = P(cmd.A),
					IsFilled = true,
					IsClosed = false
				};
				pathGeometry.Figures.Add(pathFigure);
				break;
			case K.L:
				pathFigure.Segments.Add(new LineSegment
				{
					Point = P(cmd.A)
				});
				break;
			case K.C:
				pathFigure.Segments.Add(new BezierSegment
				{
					Point1 = P(cmd.A),
					Point2 = P(cmd.B),
					Point3 = P(cmd.C)
				});
				break;
			case K.Q:
				pathFigure.Segments.Add(new QuadraticBezierSegment
				{
					Point1 = P(cmd.A),
					Point2 = P(cmd.B)
				});
				break;
			case K.Z:
				if ((object)pathFigure != null)
				{
					pathFigure.IsClosed = true;
				}
				break;
			}
		}
		return pathGeometry;
		static Point P(Vector2 v)
		{
			return new Point(v.X, v.Y);
		}
	}

	public CanvasGeometry ToWin2D(ICanvasResourceCreator device)
	{
		using CanvasPathBuilder canvasPathBuilder = new CanvasPathBuilder(device);
		canvasPathBuilder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
		bool flag = false;
		foreach (Cmd cmd in cmds)
		{
			switch (cmd.Kind)
			{
			case K.M:
				if (flag)
				{
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
				}
				canvasPathBuilder.BeginFigure(cmd.A);
				flag = true;
				break;
			case K.L:
				canvasPathBuilder.AddLine(cmd.A);
				break;
			case K.C:
				canvasPathBuilder.AddCubicBezier(cmd.A, cmd.B, cmd.C);
				break;
			case K.Q:
				canvasPathBuilder.AddQuadraticBezier(cmd.A, cmd.B);
				break;
			case K.Z:
				if (flag)
				{
					canvasPathBuilder.EndFigure(CanvasFigureLoop.Closed);
					flag = false;
				}
				break;
			}
		}
		if (flag)
		{
			canvasPathBuilder.EndFigure(CanvasFigureLoop.Open);
		}
		return CanvasGeometry.CreatePath(canvasPathBuilder);
	}

	public (double X, double Y, double W, double H) Bounds()
	{
		double minX = double.MaxValue;
		double minY = double.MaxValue;
		double maxX = double.MinValue;
		double maxY = double.MinValue;
		Vector2 vector = default(Vector2);
		Vector2 vector2 = default(Vector2);
		foreach (Cmd cmd in cmds)
		{
			switch (cmd.Kind)
			{
			case K.M:
				vector = (vector2 = cmd.A);
				Add(vector);
				break;
			case K.L:
				Add(cmd.A);
				vector = cmd.A;
				break;
			case K.Q:
			{
				Vector2 vector4 = vector;
				Vector2 a2 = cmd.A;
				Vector2 b2 = cmd.B;
				for (int i = 1; i <= 24; i++)
				{
					float num3 = (float)i / 24f;
					Add((1f - num3) * (1f - num3) * vector4 + 2f * (1f - num3) * num3 * a2 + num3 * num3 * b2);
				}
				vector = b2;
				break;
			}
			case K.C:
			{
				Vector2 vector3 = vector;
				Vector2 a = cmd.A;
				Vector2 b = cmd.B;
				Vector2 c = cmd.C;
				Add(c);
				foreach (double item in CubicExtrema(vector3.X, a.X, b.X, c.X).Concat(CubicExtrema(vector3.Y, a.Y, b.Y, c.Y)))
				{
					float num2 = 1f - (float)item;
					Add(num2 * num2 * num2 * vector3 + 3f * num2 * num2 * (float)item * a + 3f * num2 * (float)(item * item) * b + (float)(item * item * item) * c);
				}
				vector = c;
				break;
			}
			case K.Z:
				vector = vector2;
				break;
			}
		}
		if (!(minX > maxX))
		{
			return (X: minX, Y: minY, W: maxX - minX, H: maxY - minY);
		}
		return (X: 0.0, Y: 0.0, W: 0.0, H: 0.0);
		void Add(Vector2 p)
		{
			minX = Math.Min(minX, p.X);
			minY = Math.Min(minY, p.Y);
			maxX = Math.Max(maxX, p.X);
			maxY = Math.Max(maxY, p.Y);
		}
	}

	[IteratorStateMachine(typeof(_003CCubicExtrema_003Ed__25))]
	private static IEnumerable<double> CubicExtrema(double p0, double p1, double p2, double p3)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CCubicExtrema_003Ed__25(-2)
		{
			_003C_003E3__p0 = p0,
			_003C_003E3__p1 = p1,
			_003C_003E3__p2 = p2,
			_003C_003E3__p3 = p3
		};
	}

	public static PPath ParseSvg(string data)
	{
		PPath pPath = new PPath();
		int i = 0;
		while (i < data.Length)
		{
			char c = data[i];
			if (char.IsWhiteSpace(c) || c == ',')
			{
				i++;
				continue;
			}
			i++;
			if ((uint)c <= 76u)
			{
				if (c != 'C')
				{
					if (c != 'L')
					{
						continue;
					}
					while (true)
					{
						double? num = Num();
						if (!num.HasValue)
						{
							break;
						}
						double valueOrDefault = num.GetValueOrDefault();
						num = Num();
						if (!num.HasValue)
						{
							break;
						}
						double valueOrDefault2 = num.GetValueOrDefault();
						pPath.L(valueOrDefault, valueOrDefault2);
					}
					continue;
				}
				while (true)
				{
					double? num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault3 = num.GetValueOrDefault();
					num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault4 = num.GetValueOrDefault();
					num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault5 = num.GetValueOrDefault();
					num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault6 = num.GetValueOrDefault();
					num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault7 = num.GetValueOrDefault();
					num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault8 = num.GetValueOrDefault();
					pPath.C(valueOrDefault3, valueOrDefault4, valueOrDefault5, valueOrDefault6, valueOrDefault7, valueOrDefault8);
				}
				continue;
			}
			switch (c)
			{
			case 'M':
				while (true)
				{
					double? num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault9 = num.GetValueOrDefault();
					num = Num();
					if (!num.HasValue)
					{
						break;
					}
					double valueOrDefault10 = num.GetValueOrDefault();
					pPath.M(valueOrDefault9, valueOrDefault10);
				}
				break;
			case 'Z':
			case 'z':
				pPath.Z();
				break;
			}
		}
		return pPath;
		double? Num()
		{
			for (; i < data.Length && (data[i] == ' ' || data[i] == ',' || data[i] == '\n' || data[i] == '\r' || data[i] == '\t'); i++)
			{
			}
			int num2 = i;
			if (i < data.Length && (data[i] == '-' || data[i] == '+'))
			{
				i++;
			}
			for (; i < data.Length && (char.IsDigit(data[i]) || data[i] == '.'); i++)
			{
			}
			if (i < data.Length && (data[i] == 'e' || data[i] == 'E'))
			{
				i++;
				if (i < data.Length && (data[i] == '-' || data[i] == '+'))
				{
					i++;
				}
				for (; i < data.Length && char.IsDigit(data[i]); i++)
				{
				}
			}
			if (num2 == i)
			{
				return null;
			}
			return double.Parse(data.AsSpan(num2, i - num2), CultureInfo.InvariantCulture);
		}
	}
}
