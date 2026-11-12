using System;
using System.Numerics;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace HeyClicky.App.UI;

public static class IntroMark
{
	public static Path Create(double size, Color fill, double rotationDegrees = 0.0)
	{
		Path path = new Path
		{
			Data = Geometry(size),
			Fill = Ui.Brush(fill),
			Width = size,
			Height = size
		};
		if (rotationDegrees != 0.0)
		{
			path.RenderTransform = new RotateTransform
			{
				Angle = rotationDegrees,
				CenterX = size / 2.0,
				CenterY = size / 2.0
			};
		}
		return path;
	}

	public static PathGeometry Geometry(double size, double cornerFraction = 0.09)
	{
		double num = size / 2.0;
		double num2 = size / 2.0;
		double num3 = size * 0.5;
		double num4 = size * Math.Sqrt(3.0) / 4.0;
		return Rounded(new Vector2[3]
		{
			new Vector2((float)(num + num4), (float)num2),
			new Vector2((float)(num - num4), (float)(num2 + num3)),
			new Vector2((float)(num - num4), (float)(num2 - num3))
		}, size * cornerFraction);
	}

	public static Path Cursor(double size, Color fill, double rotationDegrees)
	{
		double num = size * Math.Sqrt(3.0) / 2.0;
		double num2 = size / 2.0;
		double num3 = size / 2.0;
		PathGeometry data = Rounded(new Vector2[3]
		{
			new Vector2((float)num2, (float)(num3 - num / 1.5)),
			new Vector2((float)(num2 + size / 2.0), (float)(num3 + num / 3.0)),
			new Vector2((float)(num2 - size / 2.0), (float)(num3 + num / 3.0))
		}, size * 0.09);
		return new Path
		{
			Data = data,
			Fill = Ui.Brush(fill),
			Width = size,
			Height = size,
			RenderTransform = new RotateTransform
			{
				Angle = rotationDegrees,
				CenterX = size / 2.0,
				CenterY = size / 2.0
			}
		};
	}

	private static PathGeometry Rounded(Vector2[] v, double r)
	{
		double num = r * Math.Sqrt(3.0);
		PathFigure pathFigure = new PathFigure
		{
			IsClosed = true,
			IsFilled = true
		};
		PathSegmentCollection pathSegmentCollection = new PathSegmentCollection();
		for (int i = 0; i < 3; i++)
		{
			Vector2 vector = v[(i + 2) % 3];
			Vector2 vector2 = v[i];
			Vector2 vector3 = v[(i + 1) % 3];
			Vector2 vector4 = Vector2.Normalize(vector2 - vector);
			Vector2 vector5 = Vector2.Normalize(vector3 - vector2);
			Vector2 p2 = vector2 - vector4 * (float)num;
			Vector2 p3 = vector2 + vector5 * (float)num;
			if (i == 0)
			{
				pathFigure.StartPoint = P(p2);
			}
			else
			{
				pathSegmentCollection.Add(new LineSegment
				{
					Point = P(p2)
				});
			}
			pathSegmentCollection.Add(new ArcSegment
			{
				Point = P(p3),
				Size = new Size(r, r),
				SweepDirection = SweepDirection.Clockwise
			});
		}
		pathFigure.Segments = pathSegmentCollection;
		return new PathGeometry
		{
			Figures = { pathFigure }
		};
		static Point P(Vector2 p)
		{
			return new Point(p.X, p.Y);
		}
	}
}
