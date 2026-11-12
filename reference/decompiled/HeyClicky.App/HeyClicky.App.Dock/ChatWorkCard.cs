using System;
using System.Linq;
using System.Numerics;
using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace HeyClicky.App.Dock;

internal sealed class ChatWorkCard : Grid
{
	private readonly ArcDock dock;

	private readonly string key;

	private readonly bool hidesFinalizing;

	private string look = "";

	private string shape = "";

	private PlanRows? planRows;

	private ArcProgressBar? bar;

	private bool appeared;

	private bool listening;

	private ChatWorkCardMode Mode(ArcCardContent content)
	{
		bool isWorking = content.IsWorking;
		bool isFinalizing = content.IsFinalizing;
		CardFooterRequest request = content.Request;
		return ChatWorkCardRules.Mode(isWorking, isFinalizing, (object)request != null && request.Kind != CardFooterRequest.Kinds.Ready, hidesFinalizing);
	}

	private bool Shows(ArcCardContent content)
	{
		return Mode(content) != ChatWorkCardMode.Hidden;
	}

	public ChatWorkCard(ArcDock dock, string key, ArcCardContent content, bool hidesFinalizing = false)
	{
		ChatWorkCard chatWorkCard = this;
		this.dock = dock;
		this.key = key;
		this.hidesFinalizing = hidesFinalizing;
		base.MaxWidth = 420.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.RequestedTheme = ElementTheme.Light;
		if (Shows(content))
		{
			Build(content);
		}
		else
		{
			base.Visibility = Visibility.Collapsed;
		}
		base.Loaded += delegate
		{
			if (!chatWorkCard.listening)
			{
				chatWorkCard.listening = true;
				dock.ContentChanged += chatWorkCard.OnContentChanged;
			}
			chatWorkCard.OnContentChanged();
			if (chatWorkCard.Visibility == Visibility.Visible)
			{
				chatWorkCard.Appear();
			}
		};
		base.Unloaded += delegate
		{
			if (!chatWorkCard.IsLoaded && chatWorkCard.listening)
			{
				chatWorkCard.listening = false;
				dock.ContentChanged -= chatWorkCard.OnContentChanged;
			}
		};
	}

	private void Appear()
	{
		if (!appeared && base.IsLoaded)
		{
			appeared = true;
			if (!Anim.ReduceMotion && !Anim.Instant)
			{
				base.Opacity = 0.0;
				Anim.Opacity(this, 1f, 0.25);
				base.CenterPoint = Vector3.Zero;
				Compositor compositor = Ui.Compositor;
				Vector3KeyFrameAnimation vector3KeyFrameAnimation = compositor.CreateVector3KeyFrameAnimation();
				vector3KeyFrameAnimation.InsertKeyFrame(0f, new Vector3(0.97f, 0.97f, 1f));
				vector3KeyFrameAnimation.InsertKeyFrame(1f, Vector3.One, Easing.EaseOut(compositor));
				vector3KeyFrameAnimation.Duration = TimeSpan.FromSeconds(0.25);
				vector3KeyFrameAnimation.Target = "Scale";
				StartAnimation(vector3KeyFrameAnimation);
			}
		}
	}

	private string Look(ArcCardContent c)
	{
		string[] obj = new string[5]
		{
			Mode(c).ToString(),
			"|",
			c.Shape,
			"|",
			null
		};
		ClickyArcPlan plan = c.Plan;
		obj[4] = (((object)plan != null && !plan.IsEmpty) ? "" : (c.LiveLine?.ToString() + "|" + string.Join(",", (c.LiveLine?.Icons ?? Array.Empty<ClickyLiveLineIcon>()).Select((ClickyLiveLineIcon i) => i.Host ?? i.Toolkit)) + "+" + c.LiveLine?.MoreIcons + "|" + c.CurrentStepLine));
		return string.Concat(obj);
	}

	private void OnContentChanged()
	{
		ArcCardContent arcCardContent = dock.ContentForKey(key);
		if ((object)arcCardContent == null || !Shows(arcCardContent))
		{
			base.Visibility = Visibility.Collapsed;
			return;
		}
		if (base.Visibility != 0)
		{
			base.Visibility = Visibility.Visible;
			Appear();
		}
		if (Look(arcCardContent) == look)
		{
			return;
		}
		if ((object)planRows != null && arcCardContent.Shape == shape)
		{
			ClickyArcPlan plan = arcCardContent.Plan;
			if ((object)plan != null && !plan.IsEmpty)
			{
				planRows.Update(plan, arcCardContent.LiveLine);
				bar?.Update(plan);
				look = Look(arcCardContent);
				return;
			}
		}
		Build(arcCardContent);
	}

	private void Build(ArcCardContent content)
	{
		look = Look(content);
		shape = content.Shape;
		planRows = null;
		bar = null;
		base.Children.Clear();
		if (Mode(content) == ChatWorkCardMode.Ask)
		{
			CardFooterRequest request = content.Request;
			if ((object)request != null)
			{
				base.Children.Add(new FooterRequestRow(dock, content, request, 30.0, "chat_live_card", buttonsOnly: true)
				{
					Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
				});
				return;
			}
		}
		LinearGradientBrush fill = Ui.Vertical((0.0, DS.Hex(15526893u)), (1.0, DS.Hex(16250871u)));
		base.Children.Add(ChatBlockViews.BubblePlate(fill, null, tail: true, mirrored: false));
		base.Children.Add(ChatBlockViews.BubblePlate(Ui.Brush(content.Paper.TitleWash.WithAlpha(0.45)), null, tail: true, mirrored: false));
		Color ink = content.Paper.Ink;
		if (!content.IsWorking)
		{
			return;
		}
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 9.0,
			Padding = new Thickness(14.0, 12.0, 14.0, 19.0)
		};
		ClickyArcPlan plan = content.Plan;
		if ((object)plan != null && !plan.IsEmpty)
		{
			planRows = new PlanRows(plan, content.LiveLine, ink, chat: true);
			stackPanel.Children.Add(planRows);
			bar = new ArcProgressBar(plan, ink, isLarge: false, showsLabel: true, isCompact: true)
			{
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
			};
			stackPanel.Children.Add(bar);
		}
		else
		{
			StackPanel stackPanel2 = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 8.0
			};
			stackPanel2.Children.Add(new ProgressRing
			{
				IsActive = true,
				Width = 16.0,
				Height = 16.0,
				MinWidth = 16.0,
				MinHeight = 16.0,
				Foreground = Ui.Brush(ArcColors.Working),
				VerticalAlignment = VerticalAlignment.Center
			});
			ClickyLiveLine liveLine = content.LiveLine;
			if ((object)liveLine != null)
			{
				stackPanel2.Children.Add(new LiveLineView(liveLine, ink, 12.5)
				{
					VerticalAlignment = VerticalAlignment.Center
				});
			}
			else
			{
				TextBlock textBlock = Ui.Text(content.CurrentStepLine ?? "Getting started…", 12.5, FontWeights.Medium, ink.WithAlpha(0.7), TextAlignment.Left, 2);
				textBlock.VerticalAlignment = VerticalAlignment.Center;
				stackPanel2.Children.Add(textBlock);
			}
			stackPanel.Children.Add(stackPanel2);
			stackPanel.Children.Add(new ArcProgressBar(null, ink, isLarge: false, showsLabel: false, isCompact: true)
			{
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
			});
		}
		base.Children.Add(stackPanel);
	}
}
