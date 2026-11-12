using System;
using System.Globalization;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.OneBrain;
using HeyClicky.App.Portraits;
using HeyClicky.App.Proactive;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class MorningSuggestionsSurface : NotchSurface
{
	private readonly Grid view;

	public static bool ShowsPaper
	{
		get
		{
			GoodMorningBrief shared = GoodMorningBrief.Shared;
			if (shared != null)
			{
				if (shared.NotchHeadline == null)
				{
					return shared.HasPaperToday;
				}
				return true;
			}
			return false;
		}
	}

	public override FrameworkElement View => view;

	public MorningSuggestionsSurface(NotchLiveEvent.MorningSuggestions e)
	{
		Canvas constellation = new Canvas
		{
			Width = 161.2,
			Height = 59.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		PortraitAppearance who2 = PortraitAppearance.HomeMascot("#26C281");
		PortraitAppearance who3 = PortraitAppearance.HomeMascot("#3380FF");
		PortraitAppearance who4 = PortraitAppearance.HomeMascot("#FF4C4C");
		Face(88.0, -64.0, -5.0, -9.0, who2, 0.0, 3.2);
		Face(86.0, 64.0, -6.0, 8.0, who4, 0.12, 3.7);
		Face(120.0, 0.0, 4.0, -2.0, who3, 0.24, 3.4);
		string text = PaperHeadline();
		if (text != null)
		{
			TextBlock textBlock = Ui.Text(("This " + DateTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("en-US")) + " morning").ToUpperInvariant(), 10.0, FontWeights.SemiBold, DS.Colors.TextSecondary, TextAlignment.Left, 1);
			textBlock.CharacterSpacing = 80;
			textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
			TextBlock textBlock2 = Ui.Text(text, 15.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 2, wrap: true);
			textBlock2.Margin = new Thickness(0.0, 0.0, 0.0, 3.0);
			StackPanel stackPanel = new StackPanel
			{
				Width = 320.0,
				VerticalAlignment = VerticalAlignment.Center,
				Children = 
				{
					(UIElement)textBlock,
					(UIElement)textBlock2
				}
			};
			string text2 = PaperSubtitle();
			if (text2 != null)
			{
				TextBlock textBlock3 = Ui.Text(text2, 12.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Left, 1);
				textBlock3.TextTrimming = TextTrimming.CharacterEllipsis;
				stackPanel.Children.Add(textBlock3);
			}
			GelButton gelButton = new GelButton("Read it", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, "\ue736", null, null);
			gelButton.Click += delegate
			{
				ProactiveCoordinator current = ProactiveCoordinator.Current;
				if (current != null)
				{
					current.PlayMorningPaperFromNotch();
				}
				else
				{
					base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MorningSuggestions));
				}
			};
			AutomationProperties.SetHelpText(gelButton, "Draws this morning's paper on your screen.");
			GelButton gelButton2 = new GelButton("Not now", GelAppearance.SecondaryOutline, GelButton.Sizing.Normal, 100.0, null, null, null);
			gelButton2.Click += delegate
			{
				ProactiveCoordinator current2 = ProactiveCoordinator.Current;
				if (current2 != null)
				{
					current2.DismissMorningNoticeFromExplicitClose();
				}
				else
				{
					base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MorningSuggestions));
				}
			};
			AutomationProperties.SetHelpText(gelButton2, "Closes this card. Today's ideas stay in Suggestions.");
			StackPanel stackPanel2 = Ui.H(8.0, gelButton, gelButton2);
			stackPanel2.Margin = new Thickness(0.0, 10.0, 0.0, 0.0);
			stackPanel.Children.Add(stackPanel2);
			StackPanel stackPanel3 = Ui.H(16.0, constellation, stackPanel);
			stackPanel3.HorizontalAlignment = HorizontalAlignment.Center;
			stackPanel3.VerticalAlignment = VerticalAlignment.Top;
			stackPanel3.Margin = new Thickness(0.0, 36.0, 0.0, 0.0);
			view = new Grid
			{
				Children = { (UIElement)stackPanel3 }
			};
			return;
		}
		int count = e.Count;
		FrameworkElement frameworkElement = Ui.Shrink(Ui.Text((count == 1) ? "I can do 1 thing for you today" : $"I can do {count} things for you today", 14.0, FontWeights.SemiBold, DS.Colors.TextPrimary, TextAlignment.Left, 1));
		frameworkElement.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
		TextBlock textBlock4 = Ui.Text("Pick what you'd like me to take on.", 11.0, FontWeights.Normal, DS.Colors.TextSecondary, TextAlignment.Left, 1);
		textBlock4.Margin = new Thickness(0.0, 0.0, 0.0, 7.0);
		GelButton gelButton3 = new GelButton("Dismiss", GelAppearance.SecondaryOutline, GelButton.Sizing.Normal, 100.0, null, null, null);
		gelButton3.Click += delegate
		{
			ProactiveCoordinator current3 = ProactiveCoordinator.Current;
			if (current3 != null)
			{
				current3.DismissMorningNoticeFromExplicitClose();
			}
			else
			{
				base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MorningSuggestions));
			}
		};
		GelButton gelButton4 = new GelButton("Show me", GelAppearance.PrimaryBlue, GelButton.Sizing.Normal, 100.0, "\ue82f", null, null);
		gelButton4.Click += delegate
		{
			ProactiveCoordinator current4 = ProactiveCoordinator.Current;
			if (current4 != null)
			{
				current4.OpenSuggestionsFromMorningNotch();
			}
			else
			{
				base.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.MorningSuggestions));
			}
		};
		StackPanel stackPanel4 = new StackPanel
		{
			Width = 250.0,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)frameworkElement,
				(UIElement)textBlock4,
				(UIElement)Ui.H(8.0, gelButton3, gelButton4)
			}
		};
		StackPanel stackPanel5 = Ui.H(14.0, constellation, stackPanel4);
		stackPanel5.HorizontalAlignment = HorizontalAlignment.Center;
		stackPanel5.VerticalAlignment = VerticalAlignment.Top;
		stackPanel5.Margin = new Thickness(0.0, 36.0, 0.0, 0.0);
		view = new Grid
		{
			Children = { (UIElement)stackPanel5 }
		};
		void Face(double size, double x, double y, double tilt, PortraitAppearance who, double delay, double duration)
		{
			double num = 0.62;
			double num2 = size * num;
			double num3 = size * 0.74 * num;
			double num4 = size * 0.9 * num;
			PortraitView portraitView = new PortraitView(who, num4, num4, PortraitExpression.Happy, showsAvatarBackground: false, who.HairColor.Hair(), size * 0.08 * num)
			{
				AnimatesWhileIdle = false,
				TracksPointer = false
			};
			Grid grid = new Grid
			{
				Width = num2,
				Height = num3,
				Children = { (UIElement)portraitView }
			};
			portraitView.HorizontalAlignment = HorizontalAlignment.Center;
			portraitView.VerticalAlignment = VerticalAlignment.Center;
			grid.CenterPoint = new Vector3((float)num2 / 2f, (float)num3 / 2f, 0f);
			grid.Rotation = (float)tilt;
			Grid grid2 = new Grid
			{
				Width = num2,
				Height = num3,
				Children = { (UIElement)grid }
			};
			Canvas.SetLeft(grid2, 80.6 + x * num - num2 / 2.0);
			Canvas.SetTop(grid2, 29.5 + y * num - num3 / 2.0);
			constellation.Children.Add(grid2);
			if (!Anim.Instant && !Anim.ReduceMotion)
			{
				Arrive(grid2, num2, num3, -14.0 * num, delay);
			}
			if (!Anim.Instant)
			{
				double period = duration * 2.0;
				Anim.LoopScalar(grid, "Rotation", period, (double t) => (float)(tilt + 1.0 + Math.Sin(Math.PI * 2.0 * (t / period + 0.75))));
				Anim.LoopVector3(grid, "Translation", period, (double t) => new Vector3(0f, (float)(-1.5 + 1.5 * Math.Sin(Math.PI * 2.0 * (t / period + 0.25))), 0f));
			}
		}
	}

	private static string? PaperHeadline()
	{
		ProactiveCoordinator current = ProactiveCoordinator.Current;
		if (current == null)
		{
			return null;
		}
		string text = GoodMorningBrief.TodaysHeadline(current.CoreSettings, current.UserId)?.Headline;
		if (text != null)
		{
			return text;
		}
		if (!GoodMorningBrief.HasTodaysResearch(current.CoreSettings, current.UserId))
		{
			return null;
		}
		return "Your morning paper is ready";
	}

	private static string? PaperSubtitle()
	{
		ProactiveCoordinator current = ProactiveCoordinator.Current;
		if (current == null)
		{
			return null;
		}
		return GoodMorningBrief.TodaysHeadline(current.CoreSettings, current.UserId)?.Line;
	}

	private static void Arrive(UIElement e, double w, double h, double fromY, double delay)
	{
		Compositor compositor = Ui.Compositor;
		CubicBezierEasingFunction easingFunction = compositor.CreateCubicBezierEasingFunction(new Vector2(0.22f, 1f), new Vector2(0.36f, 1f));
		e.CenterPoint = new Vector3((float)w / 2f, (float)h / 2f, 0f);
		Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation.InsertKeyFrame(0f, new Vector3(0.84f, 0.84f, 1f));
		vector3KeyFrameAnimation.InsertKeyFrame(1f, Vector3.One, easingFunction);
		Run("Scale", Timed(vector3KeyFrameAnimation));
		Vector3KeyFrameAnimation vector3KeyFrameAnimation2 = compositor.CreateVector3KeyFrameAnimation();
		vector3KeyFrameAnimation2.InsertKeyFrame(0f, new Vector3(0f, (float)fromY, 0f));
		vector3KeyFrameAnimation2.InsertKeyFrame(1f, Vector3.Zero, easingFunction);
		Run("Translation", Timed(vector3KeyFrameAnimation2));
		ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
		scalarKeyFrameAnimation.InsertKeyFrame(0f, 0f);
		scalarKeyFrameAnimation.InsertKeyFrame(1f, 1f, easingFunction);
		Run("Opacity", Timed(scalarKeyFrameAnimation));
		void Run(string target, CompositionAnimation a)
		{
			a.Target = target;
			e.StartAnimation(a);
		}
		KeyFrameAnimation Timed(KeyFrameAnimation a)
		{
			a.Duration = TimeSpan.FromSeconds(0.48);
			a.DelayTime = TimeSpan.FromSeconds(delay);
			a.DelayBehavior = AnimationDelayBehavior.SetInitialValueBeforeDelay;
			return a;
		}
	}
}
