using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI;

namespace HeyClicky.App.Settings;

public sealed class MoneyBarChart : Grid
{
	public static readonly Color AquaBlueText = DS.Hex(10668031u);

	private readonly double chartHeight;

	private readonly bool wholeDollars;

	private readonly HashSet<int> callouts;

	private readonly List<(GelSurface Bar, TextBlock Callout)> bars = new List<(GelSurface, TextBlock)>();

	public MoneyBarChart(IReadOnlyList<int> amountsUsdCents, IReadOnlyList<string> labels, IEnumerable<int> calloutIndices, bool wholeDollars = false, double chartHeight = 96.0)
	{
		MoneyBarChart moneyBarChart = this;
		this.chartHeight = chartHeight;
		this.wholeDollars = wholeDollars;
		callouts = calloutIndices.ToHashSet();
		base.ColumnSpacing = 5.0;
		base.Padding = new Thickness(0.0, 16.0, 0.0, 0.0);
		AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
		for (int i = 0; i < amountsUsdCents.Count; i++)
		{
			base.ColumnDefinitions.Add(new ColumnDefinition());
			GelSurface gelSurface = new GelSurface(GelAppearance.PrimaryBlue, 5.0)
			{
				Height = chartHeight,
				VerticalAlignment = VerticalAlignment.Bottom
			};
			TextBlock textBlock = new TextBlock
			{
				FontSize = 9.5,
				FontWeight = FontWeights.Bold,
				Foreground = Ui.Brush(AquaBlueText),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Bottom,
				IsTextScaleFactorEnabled = false,
				TextWrapping = TextWrapping.NoWrap
			};
			Typography.SetNumeralAlignment(textBlock, FontNumeralAlignment.Tabular);
			Grid item = new Grid
			{
				Height = chartHeight,
				Children = 
				{
					(UIElement)gelSurface,
					(UIElement)textBlock
				}
			};
			TextBlock item2 = Ui.Text((i < labels.Count) ? labels[i] : "", 9.5, FontWeights.Medium, SK.TextTertiary, TextAlignment.Center, 1);
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 5.0,
				Children = 
				{
					(UIElement)item,
					(UIElement)item2
				}
			};
			Grid.SetColumn(stackPanel, i);
			base.Children.Add(stackPanel);
			bars.Add((gelSurface, textBlock));
		}
		base.Loaded += delegate
		{
			moneyBarChart.Update(amountsUsdCents, animate: false);
		};
	}

	public void Update(IReadOnlyList<int> amountsUsdCents, bool animate = true)
	{
		int num = Math.Max(1, amountsUsdCents.DefaultIfEmpty(1).Max());
		for (int i = 0; i < bars.Count && i < amountsUsdCents.Count; i++)
		{
			int num2 = amountsUsdCents[i];
			float num3 = (float)Math.Max(0.03, (double)num2 / (double)num);
			(GelSurface Bar, TextBlock Callout) tuple = bars[i];
			GelSurface item = tuple.Bar;
			TextBlock item2 = tuple.Callout;
			item.Opacity = ((num2 > 0) ? 1.0 : 0.25);
			item.CenterPoint = new Vector3((float)(item.ActualWidth / 2.0), (float)chartHeight, 0f);
			Vector3 vector = new Vector3(1f, num3, 1f);
			if (!animate || Anim.Instant || Anim.ReduceMotion)
			{
				Anim.Stop(item, "Scale");
				item.Scale = vector;
			}
			else
			{
				SpringVector3NaturalMotionAnimation springVector3NaturalMotionAnimation = new SwiftSpring(0.35, 0.85).Vector3(Ui.Compositor, vector);
				springVector3NaturalMotionAnimation.Target = "Scale";
				item.StartAnimation(springVector3NaturalMotionAnimation);
			}
			bool flag = callouts.Contains(i) && num2 > 0;
			item2.Text = (flag ? (wholeDollars ? ReferralFormatting.WholeDollars(num2) : ReferralFormatting.CompactDollars(num2)) : "");
			item2.Margin = new Thickness(-40.0, 0.0, -40.0, 0.0);
			item2.Translation = new Vector3(0f, 0f - (float)(chartHeight * (double)num3 + 4.0), 0f);
		}
	}
}
