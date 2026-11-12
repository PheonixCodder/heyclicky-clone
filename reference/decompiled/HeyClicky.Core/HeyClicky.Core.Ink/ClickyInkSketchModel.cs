using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkSketchModel(IReadOnlyList<Piece> Pieces)
{
	public abstract record Shape
	{
		public sealed record Box(Rect Rect) : Shape();

		public sealed record Circle(Point Center, double Radius) : Shape();

		public sealed record Line(Point From, Point To) : Shape();

		public sealed record Arrow(Point From, Point To) : Shape();

		public sealed record Path(IReadOnlyList<Point> Points) : Shape()
		{
			public bool Equals(Path? other)
			{
				if ((object)other != null)
				{
					return Points.SequenceEqual(other.Points);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return Points.Count;
			}
		}

		public sealed record Text(Point At) : Shape();

		public sealed record Icon(Point At, string Name, double Size) : Shape();

		public sealed record Dot(Point At) : Shape();

		public sealed record Badge(Point At) : Shape();
	}

	public enum Size
	{
		Small,
		Normal,
		Big
	}

	public sealed record Piece
	{
		public Shape Shape { get; set; } = new Shape.Dot(new Point(0f, 0f));

		public string? Label { get; set; }

		public string? Color { get; set; }

		public bool IsFilled { get; set; }

		public bool IsDashed { get; set; }

		public bool IsCurved { get; set; }

		public Size Size { get; set; } = Size.Normal;
	}

	public const double CanvasWidth = 380.0;

	public const double DefaultHeight = 160.0;

	public static readonly IReadOnlySet<string> ColorNames = new HashSet<string>
	{
		"ink", "black", "blue", "red", "green", "orange", "purple", "pink", "yellow", "gray",
		"grey"
	};

	private static readonly Dictionary<string, ClickyInkSketchModel> Cache = new Dictionary<string, ClickyInkSketchModel>();

	public const double LeadSeconds = 0.15;

	public const double PieceSeconds = 0.45;

	public static ClickyInkSketchModel Cached(string text)
	{
		lock (Cache)
		{
			if (Cache.TryGetValue(text, out ClickyInkSketchModel value))
			{
				return value;
			}
			ClickyInkSketchModel clickyInkSketchModel = Parse(text);
			if (Cache.Count > 60)
			{
				Cache.Clear();
			}
			Cache[text] = clickyInkSketchModel;
			return clickyInkSketchModel;
		}
	}

	public static ClickyInkSketchModel Parse(string text)
	{
		List<Piece> list = new List<Piece>();
		string[] array = text.Split(new char[7] { '\n', '\r', '\v', '\f', '\u0085', '\u2028', '\u2029' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			List<string> list2 = Tokenize(array[i]);
			if (list2.Count == 0)
			{
				continue;
			}
			Piece piece = MakePiece(list2[0].Lowered(), list2.Skip(1).ToList());
			if ((object)piece != null)
			{
				list.Add(piece);
				if (list.Count >= 40)
				{
					break;
				}
			}
		}
		return new ClickyInkSketchModel(list);
	}

	private static List<string> Tokenize(string line)
	{
		List<string> list = new List<string>();
		string text = "";
		bool flag = false;
		string[] array = line.Characters();
		foreach (string text2 in array)
		{
			bool flag2;
			switch (text2)
			{
			case "\"":
			case "“":
			case "”":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			if (flag2)
			{
				if (flag)
				{
					list.Add("\"" + text);
					text = "";
				}
				else if (text.Length > 0)
				{
					list.Add(text);
					text = "";
				}
				flag = !flag;
			}
			else if (text2 == " " && !flag)
			{
				if (text.Length > 0)
				{
					list.Add(text);
					text = "";
				}
			}
			else
			{
				text += text2;
			}
		}
		if (text.Length > 0)
		{
			list.Add(flag ? ("\"" + text) : text);
		}
		return list;
	}

	private static Piece? MakePiece(string command, List<string> tokens)
	{
		List<double> numbers = new List<double>();
		List<Point> list = new List<Point>();
		List<string> list2 = new List<string>();
		Piece piece = new Piece();
		foreach (string token in tokens)
		{
			if (token.HasPrefix("\""))
			{
				string text = token;
				string text2 = text.Substring(1, text.Length - 1).TrimWs();
				if (text2.Length > 0)
				{
					piece.Label = text2.PrefixCharacters(60);
				}
				continue;
			}
			string text3 = token.Lowered();
			if (ColorNames.Contains(text3))
			{
				piece.Color = text3;
				continue;
			}
			switch (text3)
			{
			case "fill":
			case "filled":
			case "fillin":
				piece.IsFilled = true;
				continue;
			case "dash":
			case "dotted":
			case "dashed":
				piece.IsDashed = true;
				continue;
			case "bent":
			case "bend":
			case "curved":
			case "curve":
				piece.IsCurved = true;
				continue;
			case "bold":
			case "large":
			case "big":
				piece.Size = Size.Big;
				continue;
			case "tiny":
			case "small":
				piece.Size = Size.Small;
				continue;
			}
			double? num;
			if (text3.Contains(','))
			{
				string[] array = text3.Split(',', StringSplitOptions.RemoveEmptyEntries);
				if (array.Length == 2)
				{
					num = InkText.ParseDouble(array[0]);
					if (num.HasValue)
					{
						double valueOrDefault = num.GetValueOrDefault();
						num = InkText.ParseDouble(array[1]);
						if (num.HasValue)
						{
							double valueOrDefault2 = num.GetValueOrDefault();
							list.Add(new Point(valueOrDefault, valueOrDefault2));
							continue;
						}
					}
				}
			}
			num = InkText.ParseDouble(text3);
			if (num.HasValue)
			{
				double valueOrDefault3 = num.GetValueOrDefault();
				numbers.Add(valueOrDefault3);
			}
			else
			{
				list2.Add(token);
			}
		}
		switch (command)
		{
		case "box":
		case "rect":
		case "card":
		case "rectangle":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault11 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault12 = num.GetValueOrDefault();
					num = N(2);
					if (num.HasValue)
					{
						double valueOrDefault13 = num.GetValueOrDefault();
						num = N(3);
						if (num.HasValue)
						{
							double valueOrDefault14 = num.GetValueOrDefault();
							if (valueOrDefault13 > 0.0 && valueOrDefault14 > 0.0)
							{
								piece.Shape = new Shape.Box(new Rect(valueOrDefault11, valueOrDefault12, valueOrDefault13, valueOrDefault14));
								break;
							}
						}
					}
				}
			}
			return null;
		}
		case "ring":
		case "oval":
		case "circle":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault8 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault9 = num.GetValueOrDefault();
					num = N(2);
					if (num.HasValue)
					{
						double valueOrDefault10 = num.GetValueOrDefault();
						if (valueOrDefault10 > 0.0)
						{
							piece.Shape = new Shape.Circle(new Point(valueOrDefault8, valueOrDefault9), valueOrDefault10);
							break;
						}
					}
				}
			}
			return null;
		}
		case "line":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault15 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault16 = num.GetValueOrDefault();
					num = N(2);
					if (num.HasValue)
					{
						double valueOrDefault17 = num.GetValueOrDefault();
						num = N(3);
						if (num.HasValue)
						{
							double valueOrDefault18 = num.GetValueOrDefault();
							piece.Shape = new Shape.Line(new Point(valueOrDefault15, valueOrDefault16), new Point(valueOrDefault17, valueOrDefault18));
							break;
						}
					}
				}
			}
			return null;
		}
		case "arrow":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault23 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault24 = num.GetValueOrDefault();
					num = N(2);
					if (num.HasValue)
					{
						double valueOrDefault25 = num.GetValueOrDefault();
						num = N(3);
						if (num.HasValue)
						{
							double valueOrDefault26 = num.GetValueOrDefault();
							piece.Shape = new Shape.Arrow(new Point(valueOrDefault23, valueOrDefault24), new Point(valueOrDefault25, valueOrDefault26));
							break;
						}
					}
				}
			}
			return null;
		}
		case "path":
		case "poly":
		case "curve":
		case "polyline":
		{
			List<Point> list3 = list.ToList();
			if (list3.Count == 0)
			{
				for (int i = 0; i < numbers.Count - 1; i += 2)
				{
					list3.Add(new Point(numbers[i], numbers[i + 1]));
				}
			}
			if (list3.Count < 2)
			{
				return null;
			}
			piece.Shape = new Shape.Path(list3.Take(40).ToList());
			if (command == "curve")
			{
				piece.IsCurved = true;
			}
			break;
		}
		case "text":
		case "label":
		case "write":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault21 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault22 = num.GetValueOrDefault();
					if (piece.Label != null)
					{
						piece.Shape = new Shape.Text(new Point(valueOrDefault21, valueOrDefault22));
						break;
					}
				}
			}
			return null;
		}
		case "icon":
		case "symbol":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault6 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault7 = num.GetValueOrDefault();
					if (list2.Count != 0)
					{
						piece.Shape = new Shape.Icon(new Point(valueOrDefault6, valueOrDefault7), list2[0], Math.Max(10.0, Math.Min(64.0, N(2) ?? 24.0)));
						break;
					}
				}
			}
			return null;
		}
		case "dot":
		case "point":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault19 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault20 = num.GetValueOrDefault();
					piece.Shape = new Shape.Dot(new Point(valueOrDefault19, valueOrDefault20));
					break;
				}
			}
			return null;
		}
		case "num":
		case "step":
		case "number":
		case "badge":
		{
			double? num = N(0);
			if (num.HasValue)
			{
				double valueOrDefault4 = num.GetValueOrDefault();
				num = N(1);
				if (num.HasValue)
				{
					double valueOrDefault5 = num.GetValueOrDefault();
					piece.Shape = new Shape.Badge(new Point(valueOrDefault4, valueOrDefault5));
					if (piece.Label == null && list2.Count > 0)
					{
						piece.Label = list2[0];
					}
					break;
				}
			}
			return null;
		}
		default:
			return null;
		}
		return piece;
		double? N(int index)
		{
			if (index >= numbers.Count)
			{
				return null;
			}
			return numbers[index];
		}
	}

	public static double Step(int count)
	{
		return Math.Min(0.28, 3.2 / (double)Math.Max(count, 1));
	}

	public static double DrawingSeconds(string text)
	{
		int count = Parse(text).Pieces.Count;
		return 0.15 + (double)Math.Max(count - 1, 0) * Step(count) + 0.45 + 0.3;
	}

	public bool Equals(ClickyInkSketchModel? other)
	{
		if ((object)other != null)
		{
			return Pieces.SequenceEqual(other.Pieces);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Pieces.Count;
	}

	[CompilerGenerated]
	private ClickyInkSketchModel(ClickyInkSketchModel original)
	{
		Pieces = original.Pieces;
	}
}
