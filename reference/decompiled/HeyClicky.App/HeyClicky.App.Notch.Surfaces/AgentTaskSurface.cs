using HeyClicky.App.Portraits;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class AgentTaskSurface : NotchSurface
{
	private readonly Grid view;

	public override VerticalAlignment ContentAlignment => VerticalAlignment.Center;

	public override FrameworkElement View => view;

	public static string StripTitle(string title)
	{
		string text = title.Trim();
		if (text.Length > 25)
		{
			return text.Substring(0, 24) + "…";
		}
		return text;
	}

	public AgentTaskSurface(NotchLiveEvent.AgentTask e, Color agentColor)
	{
		PortraitView portraitView = ClickyFace.NotchBadge(e.TaskId);
		view = Ui.Row(10.0, new UIElement[1] { portraitView }, new UIElement[1] { ActivityIndicators.AgentDots(agentColor) }, 6.0);
		view.Margin = new Thickness(16.0, 0.0, 16.0, 0.0);
		ToolTipService.SetToolTip(view, e.Title);
	}
}
