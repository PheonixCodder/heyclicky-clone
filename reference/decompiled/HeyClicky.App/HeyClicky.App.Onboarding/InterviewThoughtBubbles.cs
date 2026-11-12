using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Onboarding;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace HeyClicky.App.Onboarding;

internal sealed class InterviewThoughtBubbles : Grid
{
	private sealed class Row
	{
		private const double Omega = 19.634954084936208;

		private const double Seconds = 0.42;

		public readonly Grid View = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};

		private readonly Grid bubble = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};

		private readonly Grid words = new Grid
		{
			Margin = new Thickness(11.0, 7.0, 11.0, 13.0)
		};

		private readonly TextBlock highlight = Ui.Text("", 13.0, FontWeights.Medium, DS.White(0.55), TextAlignment.Left, 0, wrap: true);

		private readonly TextBlock ink = Ui.Text("", 13.0, FontWeights.Medium, DS.Black(0.86), TextAlignment.Left, 0, wrap: true);

		private readonly Canvas dots = new Canvas
		{
			IsHitTestVisible = false
		};

		private FrameworkElement? plate;

		private FrameworkElement? typing;

		private bool? hasTail;

		private float glideFrom;

		private long glideStartedAt;

		public string? Text { get; private set; }

		public double Height { get; private set; }

		private static bool StandsStill
		{
			get
			{
				if (!Anim.Instant)
				{
					return Anim.ReduceMotion;
				}
				return true;
			}
		}

		public Row()
		{
			highlight.RenderTransform = new TranslateTransform
			{
				Y = 1.0
			};
			words.Children.Add(highlight);
			words.Children.Add(ink);
			bubble.Children.Add(words);
			View.Children.Add(bubble);
			dots.Children.Add(Dot(7.0, -6.0, 0.0));
			dots.Children.Add(Dot(5.0, -13.0, 5.0));
			dots.VerticalAlignment = VerticalAlignment.Bottom;
			dots.HorizontalAlignment = HorizontalAlignment.Left;
			View.Children.Add(dots);
		}

		private static Ellipse Dot(double size, double left, double belowBottom)
		{
			Ellipse obj = new Ellipse
			{
				Width = size,
				Height = size,
				Fill = Ui.Brush(DS.Hex(16768443u)),
				Stroke = Ui.Brush(DS.Hex(10114328u).WithAlpha(0.7)),
				StrokeThickness = 1.0
			};
			Canvas.SetLeft(obj, left);
			Canvas.SetTop(obj, belowBottom);
			return obj;
		}

		public void Set(string? text, bool tail, InterviewThoughtBubbles owner)
		{
			if (text != Text || Height == 0.0)
			{
				Text = text;
				Size size = owner.Measure(text);
				bubble.Width = size.Width + 22.0;
				Grid grid = bubble;
				double num2 = (View.Height = size.Height + 14.0 + 6.0);
				double height = (grid.Height = num2);
				Height = height;
				if (text == null)
				{
					words.Visibility = Visibility.Collapsed;
					if ((object)typing == null)
					{
						typing = TypingDots(size);
						bubble.Children.Add(typing);
					}
				}
				else
				{
					if ((object)typing != null)
					{
						bubble.Children.Remove(typing);
						typing = null;
					}
					words.Visibility = Visibility.Visible;
					TextBlock textBlock = highlight;
					string text3 = (ink.Text = text);
					textBlock.Text = text3;
				}
			}
			if (tail != hasTail)
			{
				hasTail = tail;
				if ((object)plate != null)
				{
					bubble.Children.Remove(plate);
				}
				LinearGradientBrush fill = Ui.Vertical((0.0, DS.Hex(16771542u)), (0.42, DS.Hex(16763289u)), (1.0, DS.Hex(16771542u)));
				LinearGradientBrush stroke = Ui.Vertical((0.0, DS.Hex(10114328u)), (1.0, DS.Hex(6447714u)));
				plate = ChatBlockViews.BubblePlate(fill, stroke, tail, mirrored: false, 12.0, 6.0);
				if (!tail)
				{
					plate.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
				}
				bubble.Children.Insert(0, plate);
				dots.Visibility = ((!tail) ? Visibility.Collapsed : Visibility.Visible);
			}
		}

		private static FrameworkElement TypingDots(Size size)
		{
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5.0,
				Margin = new Thickness(11.0, 7.0, 11.0, 13.0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center
			};
			for (int i = 0; i < 3; i++)
			{
				Ellipse ellipse = new Ellipse
				{
					Width = 6.0,
					Height = 6.0,
					Fill = Ui.Brush(DS.Black(0.7))
				};
				stackPanel.Children.Add(ellipse);
				if (!Anim.Instant && !Anim.ReduceMotion)
				{
					double phase = (double)i * 0.18;
					Anim.LoopScalar(ellipse, "Opacity", 1.2, (double t) => (float)(0.35 + 0.65 * Math.Max(0.0, Math.Sin(Math.PI * 2.0 * (t / 1.2 - phase)))));
				}
			}
			AutomationProperties.SetName(stackPanel, "HeyClicky is thinking");
			return stackPanel;
		}

		private static double Remaining(double seconds)
		{
			if (!(seconds >= 0.42))
			{
				return (1.0 + 19.634954084936208 * seconds) * Math.Exp(-19.634954084936208 * seconds);
			}
			return 0.0;
		}

		public void Enter()
		{
			if (!StandsStill)
			{
				Glide(Height);
				View.Opacity = 0.0;
				Compositor compositor = Ui.Compositor;
				ScalarKeyFrameAnimation scalarKeyFrameAnimation = compositor.CreateScalarKeyFrameAnimation();
				for (int i = 1; i <= 24; i++)
				{
					scalarKeyFrameAnimation.InsertKeyFrame((float)i / 24f, (float)(1.0 - Remaining(0.42 * (double)i / 24.0)), compositor.CreateLinearEasingFunction());
				}
				scalarKeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.42);
				scalarKeyFrameAnimation.Target = "Opacity";
				View.StartAnimation(scalarKeyFrameAnimation);
			}
		}

		public void Glide(double distance)
		{
			if (!StandsStill)
			{
				double num = Environment.TickCount64;
				float num2 = (glideFrom = (float)(distance + (double)glideFrom * Remaining((num - (double)glideStartedAt) / 1000.0)));
				glideStartedAt = (long)num;
				Compositor compositor = Ui.Compositor;
				Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
				vector3KeyFrameAnimation.InsertKeyFrame(0f, new Vector3(0f, num2, 0f));
				for (int i = 1; i <= 24; i++)
				{
					vector3KeyFrameAnimation.InsertKeyFrame((float)i / 24f, new Vector3(0f, (float)((double)num2 * Remaining(0.42 * (double)i / 24.0)), 0f), compositor.CreateLinearEasingFunction());
				}
				vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.42);
				vector3KeyFrameAnimation.Target = "Translation";
				View.StartAnimation(vector3KeyFrameAnimation);
			}
		}
	}

	private const double FontSize = 13.0;

	private const double HorizontalPadding = 11.0;

	private const double VerticalPadding = 7.0;

	private const double TailDrop = 6.0;

	private const double Corner = 12.0;

	private const double BottomRoom = 14.0;

	private readonly double textWidth;

	private readonly double lineHeight;

	private readonly InterviewBubblePagination pagination = new InterviewBubblePagination();

	private readonly StackPanel stack = new StackPanel
	{
		Padding = new Thickness(14.0, 2.0, 2.0, 0.0),
		VerticalAlignment = VerticalAlignment.Bottom
	};

	private readonly ScrollViewer scroller;

	private readonly List<Row> rows = new List<Row>();

	private readonly TextBlock measurer = Ui.Text("", 13.0, FontWeights.Medium, DS.Black(0.86), TextAlignment.Left, 0, wrap: true);

	private string shownText = "";

	private bool shownLoading;

	public InterviewThoughtBubbles(double maximumWidth, double maximumHeight)
	{
		textWidth = maximumWidth - 22.0;
		measurer.Text = "Ag";
		measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		lineHeight = Math.Ceiling(measurer.DesiredSize.Height);
		stack.Children.Add(new Border
		{
			Height = 14.0
		});
		scroller = new ScrollViewer
		{
			Content = stack,
			MaxHeight = maximumHeight,
			Width = maximumWidth + 16.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalAlignment = VerticalAlignment.Bottom,
			IsHitTestVisible = false
		};
		base.Children.Add(scroller);
	}

	public void Show(string text, bool isLoading)
	{
		if (text == shownText && isLoading == shownLoading)
		{
			return;
		}
		shownText = text;
		shownLoading = isLoading;
		pagination.Update(text, (string candidate) => Measure(candidate).Height <= lineHeight * 6.0);
		List<string> list;
		if (pagination.Bubbles.Count <= 0)
		{
			if (!isLoading)
			{
				list = new List<string>();
			}
			else
			{
				int num = 1;
				list = new List<string>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<string> span = CollectionsMarshal.AsSpan(list);
				int index = 0;
				span[index] = null;
			}
		}
		else
		{
			list = pagination.Bubbles.Select((string b) => b).ToList();
		}
		IReadOnlyList<string> readOnlyList = list;
		if (rows.Count > readOnlyList.Count)
		{
			goto IL_0117;
		}
		if (rows.Count > 0 && readOnlyList.Count > 0)
		{
			string text2 = rows[0].Text;
			if (text2 != null)
			{
				string text3 = readOnlyList[0];
				if (text3 != null && !text3.StartsWith(text2, StringComparison.Ordinal))
				{
					goto IL_0117;
				}
			}
		}
		goto IL_016b;
		IL_0117:
		foreach (Row row2 in rows)
		{
			stack.Children.Remove(row2.View);
		}
		rows.Clear();
		goto IL_016b;
		IL_016b:
		List<double> list2 = Bottoms();
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			bool tail = i == readOnlyList.Count - 1;
			if (i < rows.Count)
			{
				rows[i].Set(readOnlyList[i], tail, this);
				continue;
			}
			Row row = new Row();
			row.Set(readOnlyList[i], tail, this);
			rows.Add(row);
			stack.Children.Insert(stack.Children.Count - 1, row.View);
			row.Enter();
		}
		List<double> list3 = Bottoms();
		for (int j = 0; j < list2.Count && j < rows.Count; j++)
		{
			double num2 = list3[j] - list2[j];
			if (Math.Abs(num2) > 0.25)
			{
				rows[j].Glide(num2);
			}
		}
		scroller.UpdateLayout();
		scroller.ChangeView(null, scroller.ScrollableHeight, null, disableAnimation: true);
	}

	private List<double> Bottoms()
	{
		List<double> list = new List<double>(rows.Count);
		double num = 14.0;
		for (int num2 = rows.Count - 1; num2 >= 0; num2--)
		{
			list.Insert(0, num);
			num += rows[num2].Height;
		}
		return list;
	}

	private Size Measure(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return new Size(32.0, lineHeight);
		}
		measurer.Text = text;
		measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		double width = Math.Min(textWidth, Math.Ceiling(measurer.DesiredSize.Width) + 1.0);
		measurer.Measure(new Size(width, double.PositiveInfinity));
		return new Size(width, Math.Max(lineHeight, Math.Ceiling(measurer.DesiredSize.Height)));
	}
}
