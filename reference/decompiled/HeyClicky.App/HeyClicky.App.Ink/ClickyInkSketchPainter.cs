using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.Ink;

public static class ClickyInkSketchPainter
{
	public static Color ColorOf(string? name, Color ink)
	{
		switch (name)
		{
		case "blue":
			return PaperColor.Hex("#2F6FD6");
		case "red":
			return PaperColor.Hex("#D84A44");
		case "green":
			return PaperColor.Hex("#2E9A5E");
		case "orange":
			return PaperColor.Hex("#E2782C");
		case "purple":
			return PaperColor.Hex("#7E57C8");
		case "pink":
			return PaperColor.Hex("#D9508F");
		case "yellow":
			return PaperColor.Hex("#C99A06");
		case "gray":
		case "grey":
			return PaperColor.Hex("#80848C");
		default:
			return ink;
		}
	}

	public static PaperFont Font(ClickyInkSketchModel.Size size, bool labelOnShape)
	{
		return size switch
		{
			ClickyInkSketchModel.Size.Small => ClickyPaperFonts.Hand(labelOnShape ? 12 : 13), 
			ClickyInkSketchModel.Size.Normal => ClickyPaperFonts.Hand(labelOnShape ? 15 : 16), 
			_ => ClickyPaperFonts.Hand(labelOnShape ? 19 : 22), 
		};
	}

	public static void Draw(ClickyInkSketchModel.Piece piece, double progress, int seed, Color ink, Canvas canvas)
	{
		ClickyInkSketchRandom random = new ClickyInkSketchRandom(seed);
		Color color = ColorOf(piece.Color, ink);
		PaperStroke paperStroke = new PaperStroke(color, (piece.Size == ClickyInkSketchModel.Size.Big) ? 2.6 : 2.0, PenLineCap.Round, PenLineJoin.Round, (!piece.IsDashed) ? null : new double[2] { 6.0, 6.0 });
		double num = Math.Min(1.0, progress / 0.8);
		double progress2 = Math.Max(0.0, (progress - 0.35) / 0.65);
		ClickyInkSketchModel.Shape shape = piece.Shape;
		if (!(shape is ClickyInkSketchModel.Shape.Box { Rect: var rect }))
		{
			if (!(shape is ClickyInkSketchModel.Shape.Circle circle))
			{
				if (!(shape is ClickyInkSketchModel.Shape.Line line))
				{
					if (!(shape is ClickyInkSketchModel.Shape.Arrow arrow))
					{
						if (!(shape is ClickyInkSketchModel.Shape.Path path))
						{
							if (!(shape is ClickyInkSketchModel.Shape.Text text))
							{
								if (!(shape is ClickyInkSketchModel.Shape.Icon icon))
								{
									if (!(shape is ClickyInkSketchModel.Shape.Dot dot))
									{
										if (shape is ClickyInkSketchModel.Shape.Badge badge)
										{
											Fill(canvas, HandCircle(badge.At, 11.0, ref random), color.Opacity(0.9 * progress));
											string label = piece.Label;
											if (label != null)
											{
												DrawWords(label, badge.At, new Point(0.5, 0.5), 24.0, ClickyPaperFonts.Hand(14.0), PaperColor.White, progress2, canvas);
											}
										}
									}
									else
									{
										double num2 = ((piece.Size == ClickyInkSketchModel.Size.Big) ? 6.0 : ((piece.Size == ClickyInkSketchModel.Size.Small) ? 2.5 : 4.0));
										Fill(canvas, new PaperPath().AddEllipse(new Rect(dot.At.X - num2, dot.At.Y - num2, num2 * 2.0, num2 * 2.0)), color.Opacity(progress));
										string label2 = piece.Label;
										if (label2 != null)
										{
											DrawWords(label2, new Point(dot.At.X + num2 + 5.0, dot.At.Y), new Point(0.0, 0.5), 160.0, Font(piece.Size, labelOnShape: false), ink, progress2, canvas);
										}
									}
									return;
								}
								TextBlock textBlock = ClickyInkIcon.Glyph(icon.Name, icon.Size * 0.92, color);
								if ((object)textBlock != null)
								{
									Grid grid = new Grid
									{
										Width = icon.Size,
										Height = icon.Size,
										Opacity = progress,
										Children = { (UIElement)textBlock }
									};
									Canvas.SetLeft(grid, icon.At.X - icon.Size / 2.0);
									Canvas.SetTop(grid, icon.At.Y - icon.Size / 2.0);
									canvas.Children.Add(grid);
								}
								string label3 = piece.Label;
								if (label3 != null)
								{
									DrawWords(label3, new Point(icon.At.X, icon.At.Y + icon.Size / 2.0 + 9.0), new Point(0.5, 0.5), 120.0, Font(ClickyInkSketchModel.Size.Small, labelOnShape: true), ink, progress2, canvas);
								}
							}
							else
							{
								DrawWords(piece.Label ?? "", text.At, new Point(0.0, 0.5), 380.0 - text.At.X, Font(piece.Size, labelOnShape: false), color, progress, canvas);
							}
						}
						else
						{
							IReadOnlyList<Point> points = path.Points;
							PaperPath paperPath = HandPath(points, piece.IsCurved, ref random);
							if (piece.IsFilled)
							{
								Fill(canvas, paperPath, color.Opacity(0.16 * num));
							}
							Stroke(canvas, paperPath.Trimmed(0.0, num), paperStroke);
							string label4 = piece.Label;
							if (label4 != null && points.Count > 0)
							{
								DrawWords(label4, new Point(points[points.Count - 1].X + 6.0, points[points.Count - 1].Y), new Point(0.0, 0.5), 160.0, Font(piece.Size, labelOnShape: false), color, progress2, canvas);
							}
						}
						return;
					}
					double bow = (piece.IsCurved ? 0.2 : 0.03);
					PaperPath paperPath2 = HandLine(arrow.From, arrow.To, bow, ref random);
					Stroke(canvas, paperPath2.Trimmed(0.0, num), paperStroke);
					if (num >= 1.0)
					{
						Point point = BowControl(arrow.From, arrow.To, bow);
						double num3 = Math.Atan2(arrow.To.Y - point.Y, arrow.To.X - point.X);
						PaperPath paperPath3 = new PaperPath();
						double[] array = new double[2] { -1.0, 1.0 };
						for (int i = 0; i < array.Length; i++)
						{
							double num4 = array[i] * 0.5;
							paperPath3.MoveTo(arrow.To);
							paperPath3.LineTo(arrow.To.X - 10.0 * Math.Cos(num3 + num4), arrow.To.Y - 10.0 * Math.Sin(num3 + num4));
						}
						Stroke(canvas, paperPath3, new PaperStroke(color, paperStroke.Width, PenLineCap.Round));
					}
					string label5 = piece.Label;
					if (label5 != null)
					{
						DrawMidLabel(label5, arrow.From, arrow.To, ink, progress2, canvas);
					}
				}
				else
				{
					PaperPath paperPath4 = HandLine(line.From, line.To, piece.IsCurved ? 0.18 : 0.03, ref random);
					Stroke(canvas, paperPath4.Trimmed(0.0, num), paperStroke);
					string label6 = piece.Label;
					if (label6 != null)
					{
						DrawMidLabel(label6, line.From, line.To, ink, progress2, canvas);
					}
				}
			}
			else
			{
				PaperPath paperPath5 = HandCircle(circle.Center, circle.Radius, ref random);
				if (piece.IsFilled)
				{
					Fill(canvas, paperPath5, color.Opacity(0.16 * num));
				}
				Stroke(canvas, paperPath5.Trimmed(0.0, num), paperStroke);
				string label7 = piece.Label;
				if (label7 != null)
				{
					DrawWords(label7, circle.Center, new Point(0.5, 0.5), circle.Radius * 1.8, Font(piece.Size, labelOnShape: true), piece.IsFilled ? color : ink, progress2, canvas);
				}
			}
		}
		else
		{
			PaperPath paperPath6 = HandRect(rect, ref random);
			if (piece.IsFilled)
			{
				Fill(canvas, paperPath6, color.Opacity(0.16 * num));
			}
			Stroke(canvas, paperPath6.Trimmed(0.0, num), paperStroke);
			string label8 = piece.Label;
			if (label8 != null)
			{
				DrawWords(label8, new Point(rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0), new Point(0.5, 0.5), rect.Width - 8.0, Font(piece.Size, labelOnShape: true), piece.IsFilled ? color : ink, progress2, canvas);
			}
		}
	}

	private static void Fill(Canvas canvas, PaperPath path, Color color)
	{
		canvas.Children.Add(new Path
		{
			Data = path.ToGeometry(),
			Fill = Ui.Brush(color)
		});
	}

	private static void Stroke(Canvas canvas, PaperPath path, PaperStroke stroke)
	{
		Path path2 = new Path
		{
			Data = path.ToGeometry(),
			Stroke = Ui.Brush(stroke.Color),
			StrokeThickness = stroke.Width,
			StrokeStartLineCap = stroke.Cap,
			StrokeEndLineCap = stroke.Cap,
			StrokeDashCap = stroke.Cap,
			StrokeLineJoin = stroke.Join
		};
		double[] dash = stroke.Dash;
		if (dash != null)
		{
			DoubleCollection doubleCollection = new DoubleCollection();
			double[] array = dash;
			foreach (double num in array)
			{
				doubleCollection.Add(num / stroke.Width);
			}
			path2.StrokeDashArray = doubleCollection;
		}
		canvas.Children.Add(path2);
	}

	private static void DrawMidLabel(string label, Point from, Point to, Color ink, double progress, Canvas canvas)
	{
		Point point = new Point((from.X + to.X) / 2.0, (from.Y + to.Y) / 2.0 - 11.0);
		DrawWords(label, point, new Point(0.5, 0.5), 150.0, Font(ClickyInkSketchModel.Size.Small, labelOnShape: false), ink.Opacity(0.8), progress, canvas);
	}

	private static void DrawWords(string words, Point point, Point anchor, double maxWidth, PaperFont font, Color color, double progress, Canvas canvas)
	{
		if (!(progress <= 0.0) && words.Length != 0)
		{
			InkTextView inkTextView = new InkTextView(ClickyInkTextContent.Plain(words, null), font, color);
			inkTextView.Measure(new Size(Math.Max(20.0, maxWidth), 200.0));
			Size desiredSize = inkTextView.DesiredSize;
			double length = point.X - desiredSize.Width * anchor.X;
			double length2 = point.Y - desiredSize.Height * anchor.Y;
			inkTextView.Width = desiredSize.Width;
			inkTextView.Height = desiredSize.Height;
			Grid grid = new Grid
			{
				Width = desiredSize.Width,
				Height = desiredSize.Height,
				Children = { (UIElement)inkTextView },
				Clip = new RectangleGeometry
				{
					Rect = new Rect(-2.0, -4.0, Math.Max(0.0, (desiredSize.Width + 4.0) * progress), desiredSize.Height + 8.0)
				}
			};
			Canvas.SetLeft(grid, length);
			Canvas.SetTop(grid, length2);
			canvas.Children.Add(grid);
		}
	}

	private static double Wobble(ref ClickyInkSketchRandom random, double amount = 1.2)
	{
		return (random.Next() * 2.0 - 1.0) * amount;
	}

	private static PaperPath HandRect(Rect rect, ref ClickyInkSketchRandom random)
	{
		Point[] array = new Point[4]
		{
			new Point(rect.X, rect.Y),
			new Point(rect.X + rect.Width, rect.Y),
			new Point(rect.X + rect.Width, rect.Y + rect.Height),
			new Point(rect.X, rect.Y + rect.Height)
		};
		PaperPath paperPath = new PaperPath();
		Point p = new Point(array[0].X + Wobble(ref random), array[0].Y + Wobble(ref random));
		paperPath.MoveTo(p);
		for (int i = 1; i <= 4; i++)
		{
			Point point = array[i - 1];
			Point point2 = array[i % 4];
			Point to = ((i == 4) ? new Point(point2.X + 5.0 + Wobble(ref random), point2.Y + Wobble(ref random)) : new Point(point2.X + Wobble(ref random), point2.Y + Wobble(ref random)));
			Point control = new Point((point.X + point2.X) / 2.0 + Wobble(ref random, 1.8), (point.Y + point2.Y) / 2.0 + Wobble(ref random, 1.8));
			paperPath.QuadTo(to, control);
		}
		return paperPath;
	}

	private static PaperPath HandCircle(Point center, double radius, ref ClickyInkSketchRandom random)
	{
		PaperPath paperPath = new PaperPath();
		double num = random.Next() * Math.PI * 2.0;
		for (int i = 0; i <= 39; i++)
		{
			double num2 = num + (double)i / 36.0 * Math.PI * 2.0;
			double num3 = radius * (1.0 + (random.Next() * 2.0 - 1.0) * 0.025);
			Point p = new Point(center.X + num3 * Math.Cos(num2), center.Y + num3 * Math.Sin(num2));
			if (i == 0)
			{
				paperPath.MoveTo(p);
			}
			else
			{
				paperPath.LineTo(p);
			}
		}
		return paperPath;
	}

	private static Point BowControl(Point from, Point to, double bow)
	{
		Point point = new Point((from.X + to.X) / 2.0, (from.Y + to.Y) / 2.0);
		double num = to.X - from.X;
		double num2 = to.Y - from.Y;
		double num3 = Math.Max(1.0, Math.Sqrt(num * num + num2 * num2));
		return new Point(point.X - num2 / num3 * num3 * bow, point.Y + num / num3 * num3 * bow);
	}

	private static PaperPath HandLine(Point from, Point to, double bow, ref ClickyInkSketchRandom random)
	{
		Point point = BowControl(from, to, bow);
		PaperPath paperPath = new PaperPath();
		paperPath.MoveTo(from.X + Wobble(ref random, 0.8), from.Y + Wobble(ref random, 0.8));
		paperPath.QuadTo(to, new Point(point.X + Wobble(ref random, 1.5), point.Y + Wobble(ref random, 1.5)));
		return paperPath;
	}

	private static PaperPath HandPath(IReadOnlyList<Point> points, bool curved, ref ClickyInkSketchRandom random)
	{
		PaperPath paperPath = new PaperPath();
		if (points.Count == 0)
		{
			return paperPath;
		}
		paperPath.MoveTo(points[0]);
		if (curved && points.Count > 2)
		{
			for (int i = 1; i < points.Count; i++)
			{
				Point control = points[i - 1];
				Point p = points[i];
				Point to = new Point((control.X + p.X) / 2.0, (control.Y + p.Y) / 2.0);
				paperPath.QuadTo(to, control);
				if (i == points.Count - 1)
				{
					paperPath.LineTo(p);
				}
			}
		}
		else
		{
			foreach (Point item in points.Skip(1))
			{
				paperPath.LineTo(item.X + Wobble(ref random, 0.8), item.Y + Wobble(ref random, 0.8));
			}
		}
		return paperPath;
	}
}
