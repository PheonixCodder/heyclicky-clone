using System.Numerics;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class TypingDots : StackPanel
{
	public const double DotDiameter = 8.0;

	public const double DotSpacing = 5.0;

	private const double Cycle = 1.1;

	private const float Resting = 0.38f;

	public TypingDots(Color color, double scale = 1.0)
	{
		base.Orientation = Orientation.Horizontal;
		base.Spacing = 5.0 * scale;
		base.VerticalAlignment = VerticalAlignment.Center;
		base.IsHitTestVisible = false;
		double num = 8.0 * scale;
		for (int i = 0; i < 3; i++)
		{
			Ellipse ellipse = new Ellipse
			{
				Width = num,
				Height = num,
				Fill = Ui.Brush(color),
				CenterPoint = new Vector3((float)num / 2f, (float)num / 2f, 0f)
			};
			base.Children.Add(ellipse);
			int num2 = i;
			double lag = 1.1 * (double)num2 / 6.0;
			if (Anim.ReduceMotion)
			{
				ellipse.Opacity = ((num2 == 1) ? 0.75 : 0.3799999952316284);
				continue;
			}
			ellipse.Opacity = 0.3799999952316284;
			Anim.LoopScalar(ellipse, "Opacity", 1.1, (double t) => (float)Lerp3(0.3799999952316284, 1.0, 0.3799999952316284, Phase(t - lag)));
			Anim.LoopVector3(ellipse, "Scale", 1.1, delegate(double t)
			{
				float num3 = (float)Lerp3(0.86, 1.08, 0.86, Phase(t - lag));
				return new Vector3(num3, num3, 1f);
			});
		}
	}

	private static double Phase(double t)
	{
		double num = (t % 1.1 + 1.1) % 1.1 / 1.1;
		return num * num * (3.0 - 2.0 * num);
	}

	private static double Lerp3(double a, double b, double c, double u)
	{
		if (!(u < 0.5))
		{
			return b + (c - b) * ((u - 0.5) / 0.5);
		}
		return a + (b - a) * (u / 0.5);
	}
}
