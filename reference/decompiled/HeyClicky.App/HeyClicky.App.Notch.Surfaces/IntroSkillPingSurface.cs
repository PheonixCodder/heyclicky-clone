using System;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class IntroSkillPingSurface : NotchSurface
{
	private readonly Grid empty = new Grid();

	private readonly Grid overlay = new Grid
	{
		IsHitTestVisible = false
	};

	private readonly Grid floatHost = new Grid
	{
		Width = 140.0,
		Height = 140.0,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private readonly Grid wiggle = new Grid
	{
		Width = 140.0,
		Height = 140.0
	};

	private readonly Grid halo;

	private readonly Path mark;

	private readonly Grid markHost = new Grid
	{
		Width = 34.0,
		Height = 34.0,
		HorizontalAlignment = HorizontalAlignment.Center,
		VerticalAlignment = VerticalAlignment.Center
	};

	private NotchLiveEvent.IntroSkillPing state;

	private Color color;

	private readonly DropShadow markShadow;

	public override FrameworkElement View => empty;

	public override FrameworkElement? Overlay => overlay;

	private double PositiveBias => Math.Max(0.0, state.ScaleBias);

	public IntroSkillPingSurface(NotchLiveEvent.IntroSkillPing e, Color fallbackColor)
	{
		state = e;
		string glyphColorHex = e.GlyphColorHex;
		color = ((glyphColorHex != null) ? DS.CursorColor(glyphColorHex) : fallbackColor);
		halo = Ui.GlowSpot(color, 1.0, 66.0, 0.25);
		mark = IntroMark.Create(34.0, color);
		Grid grid = new Grid
		{
			Width = 34.0,
			Height = 34.0,
			IsHitTestVisible = false
		};
		markShadow = Ui.Compositor.CreateDropShadow();
		markShadow.Color = color;
		markShadow.Opacity = 0.36f;
		markShadow.BlurRadius = 10f;
		markShadow.Mask = mark.GetAlphaMask();
		SpriteVisual spriteVisual = Ui.Compositor.CreateSpriteVisual();
		spriteVisual.Size = new Vector2(34f, 34f);
		spriteVisual.Shadow = markShadow;
		ElementCompositionPreview.SetElementChildVisual(grid, spriteVisual);
		markHost.Children.Add(grid);
		markHost.Children.Add(mark);
		wiggle.Children.Add(halo);
		wiggle.Children.Add(markHost);
		wiggle.CenterPoint = new Vector3(70f, 70f, 0f);
		floatHost.Children.Add(wiggle);
		overlay.Children.Add(floatHost);
		overlay.SizeChanged += delegate
		{
			Position();
		};
		ApplyBias(animated: false);
		if (!Anim.Instant)
		{
			Anim.LoopVector3(floatHost, "Translation", 3.5, (double t) => new Vector3(0f, (float)(-3.0 * Math.Cos((t / 3.5 + 3.0 / 14.0) * 2.0 * Math.PI)), 0f));
		}
	}

	public override bool CanUpdate(NotchPresentation p)
	{
		if (p is NotchPresentation.LiveEvent liveEvent)
		{
			return liveEvent.Event is NotchLiveEvent.IntroSkillPing;
		}
		return false;
	}

	public override void Update(NotchPresentation p)
	{
		if (!(p is NotchPresentation.LiveEvent { Event: NotchLiveEvent.IntroSkillPing @event }))
		{
			return;
		}
		bool num = @event.WiggleToken != state.WiggleToken;
		state = @event;
		string glyphColorHex = @event.GlyphColorHex;
		if (glyphColorHex != null)
		{
			Color color = DS.CursorColor(glyphColorHex);
			if (color != this.color)
			{
				this.color = color;
				mark.Fill = Ui.Brush(color);
				markShadow.Color = color;
			}
		}
		ApplyBias(animated: true);
		Position();
		if (num)
		{
			Wiggle(@event.LastToggleAdded);
		}
	}

	private void Position()
	{
		double num = ((overlay.ActualHeight > 0.0) ? overlay.ActualHeight : 104.0);
		double num2 = ((overlay.ActualWidth > 0.0) ? overlay.ActualWidth : 60.0);
		double num3 = Math.Max(64.0, num - (32.0 + PositiveBias * 8.0));
		floatHost.Margin = new Thickness(num2 / 2.0 - 70.0, num3 - 70.0, 0.0, 0.0);
	}

	private void ApplyBias(bool animated)
	{
		float num = (float)Math.Clamp(1.0 + state.ScaleBias, 0.78, 1.42);
		double num2 = Math.Max(0.07, 0.12 + state.ScaleBias * 0.1);
		float num3 = (float)((46.0 + PositiveBias * 18.0) / 46.0) * num;
		halo.CenterPoint = new Vector3(33f, 33f, 0f);
		if (animated && !Anim.Instant)
		{
			Anim.Scale(markHost, num, 0.18, Easing.EaseOut(Ui.Compositor), new Vector3(17f, 17f, 0f));
			Anim.Scale(halo, num3, 0.18, Easing.EaseOut(Ui.Compositor), null);
			Anim.Opacity(halo, (float)num2, 0.18);
		}
		else
		{
			markHost.CenterPoint = new Vector3(17f, 17f, 0f);
			markHost.Scale = new Vector3(num, num, 1f);
			halo.Scale = new Vector3(num3, num3, 1f);
			halo.Opacity = num2;
		}
	}

	private void Wiggle(bool adding)
	{
		if (!Anim.Instant)
		{
			(float, double)[] frames = ((!adding) ? new(float, double)[4]
			{
				(1f, 0.04),
				(0.84f, 0.12),
				(0.94f, 0.12),
				(1f, 0.22)
			} : new(float, double)[4]
			{
				(0.86f, 0.04),
				(1.12f, 0.14),
				(1.04f, 0.18),
				(1f, 0.3)
			});
			(float, double)[] frames2 = ((!adding) ? new(float, double)[5]
			{
				(0f, 0.04),
				(7f, 0.08),
				(-8f, 0.1),
				(3f, 0.08),
				(0f, 0.18)
			} : new(float, double)[5]
			{
				(0f, 0.04),
				(-7f, 0.1),
				(5f, 0.12),
				(-2f, 0.1),
				(0f, 0.26)
			});
			(float, double)[] array = ((!adding) ? new(float, double)[4]
			{
				(0f, 0.04),
				(-2f, 0.1),
				(2f, 0.14),
				(0f, 0.2)
			} : new(float, double)[4]
			{
				(-7f, 0.04),
				(-11f, 0.12),
				(5f, 0.26),
				(0f, 0.24)
			});
			(float, double)[] array2 = ((!adding) ? new(float, double)[4]
			{
				(0f, 0.04),
				(-2f, 0.08),
				(2f, 0.1),
				(0f, 0.2)
			} : new(float, double)[4]
			{
				(0f, 0.04),
				(2f, 0.1),
				(-1f, 0.14),
				(0f, 0.28)
			});
			Compositor compositor = Ui.Compositor;
			CubicBezierEasingFunction ease = Easing.EaseInOut(compositor);
			Vector3KeyFrameAnimation sa = compositor.CreateVector3KeyFrameAnimation();
			Insert(frames, delegate(float k, float v)
			{
				sa.InsertKeyFrame(k, new Vector3(v, v, 1f), ease);
			}, out var duration);
			sa.Duration = duration;
			sa.Target = "Scale";
			wiggle.StartAnimation(sa);
			Anim.KeyframesScalar(wiggle, "Rotation", frames2);
			double num = Math.Max(array2.Sum(((float v, double s) f) => f.s), array.Sum(((float v, double s) f) => f.s));
			Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
			LinearEasingFunction easingFunction = compositor.CreateLinearEasingFunction();
			for (double num2 = 0.0; num2 <= num + 1E-06; num2 += 0.02)
			{
				vector3KeyFrameAnimation.InsertKeyFrame((float)(num2 / num), new Vector3(Sample(array2, num2), Sample(array, num2), 0f), easingFunction);
			}
			vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(num);
			vector3KeyFrameAnimation.Target = "Translation";
			wiggle.StartAnimation(vector3KeyFrameAnimation);
		}
	}

	private static void Insert((float v, double s)[] frames, Action<float, float> add, out TimeSpan duration)
	{
		double num = frames.Sum(((float v, double s) f) => f.s);
		double num2 = 0.0;
		for (int i = 0; i < frames.Length; i++)
		{
			(float v, double s) tuple = frames[i];
			float item = tuple.v;
			double item2 = tuple.s;
			num2 += item2;
			add((float)(num2 / num), item);
		}
		duration = TimeSpan.FromSeconds(num);
	}

	private static float Sample((float v, double s)[] frames, double t)
	{
		float num = 0f;
		double num2 = 0.0;
		for (int i = 0; i < frames.Length; i++)
		{
			var (num3, num4) = frames[i];
			if (t <= num2 + num4)
			{
				double num5 = ((num4 <= 0.0) ? 1.0 : ((t - num2) / num4));
				num5 = ((num5 < 0.5) ? (2.0 * num5 * num5) : (1.0 - Math.Pow(-2.0 * num5 + 2.0, 2.0) / 2.0));
				return (float)((double)num + (double)(num3 - num) * num5);
			}
			num = num3;
			num2 += num4;
		}
		return num;
	}
}
