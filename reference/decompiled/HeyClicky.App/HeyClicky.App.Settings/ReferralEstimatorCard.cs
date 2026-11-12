using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Billing;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI;

namespace HeyClicky.App.Settings;

public sealed class ReferralEstimatorCard : StackPanel
{
	public sealed record Plan(string Key, string Name, int MonthlyPriceUsdCents, int DefaultReferralsPerMonth);

	public sealed class State
	{
		public Dictionary<string, double> ReferralsByPlan { get; } = new Dictionary<string, double>();

		public double? MonthsRetained { get; set; }
	}

	private static readonly (GelAppearance Gel, Color Glow)[] PlanGels = new(GelAppearance, Color)[2]
	{
		(GelAppearance.PrimaryBlue, DS.Hex(10336511u)),
		(GelAppearance.Peach, DS.Hex(16763289u))
	};

	private readonly IReadOnlyList<Plan> plans;

	private readonly State state;

	private readonly double commissionPercent;

	private readonly int commissionMonths;

	private readonly TextBlock total;

	private readonly TextBlock summary;

	private readonly MoneyBarChart chart;

	private readonly List<(Run Value, Plan Plan)> planValues = new List<(Run, Plan)>();

	private Run? monthsValue;

	public string TotalText => total.Text;

	private int Months => (int)Math.Round(state.MonthsRetained ?? 1.0);

	public ReferralEstimatorCard(IReadOnlyList<Plan> plans, int maxReferralsPerMonth, int defaultMonthsRetained, double commissionPercent, int commissionMonths, State state)
	{
		ReferralEstimatorCard referralEstimatorCard = this;
		this.plans = plans;
		this.state = state;
		this.commissionPercent = commissionPercent;
		this.commissionMonths = commissionMonths;
		foreach (Plan plan2 in plans)
		{
			if (!state.ReferralsByPlan.ContainsKey(plan2.Key))
			{
				state.ReferralsByPlan[plan2.Key] = plan2.DefaultReferralsPerMonth;
			}
		}
		State state2 = state;
		double? monthsRetained = state2.MonthsRetained;
		double valueOrDefault = monthsRetained.GetValueOrDefault();
		if (!monthsRetained.HasValue)
		{
			valueOrDefault = Math.Min(defaultMonthsRetained, commissionMonths);
			double? monthsRetained2 = valueOrDefault;
			state2.MonthsRetained = monthsRetained2;
		}
		base.Spacing = 18.0;
		base.Padding = new Thickness(16.0);
		TextBlock item = SK.MicroLabel("IN YOUR FIRST YEAR", 9.5, FontWeights.Bold, 80);
		total = new TextBlock
		{
			FontSize = 38.0,
			FontWeight = FontWeights.Black,
			IsTextScaleFactorEnabled = false,
			Foreground = Ui.Vertical((0.0, DS.Hex(15594751u)), (0.5, DS.Hex(10668031u)), (1.0, DS.Hex(10079224u)))
		};
		Typography.SetNumeralAlignment(total, FontNumeralAlignment.Tabular);
		base.Children.Add(new StackPanel
		{
			Spacing = 2.0,
			Children = 
			{
				(UIElement)item,
				(UIElement)total
			}
		});
		ReferralEarningsMixEstimate referralEarningsMixEstimate = Estimate();
		chart = new MoneyBarChart(referralEarningsMixEstimate.FirstYearMonthlyUsdCents, (from m in Enumerable.Range(1, 12)
			select ((m == 1 || m == 12) ? true : false) ? $"Month {m}" : m.ToString()).ToList(), new global::_003C_003Ez__ReadOnlySingleElementList<int>(11), wholeDollars: true, 84.0);
		base.Children.Add(chart);
		for (int i = 0; i < plans.Count; i++)
		{
			Plan plan = plans[i];
			var (gel, glow) = PlanGels[i % PlanGels.Length];
			base.Children.Add(SliderRow("New " + plan.Name + " referrals per month", ReferralFormatting.CompactDollars(plan.MonthlyPriceUsdCents) + "/mo plan", "/ month", state.ReferralsByPlan[plan.Key], 0.0, Math.Max(1, maxReferralsPerMonth), gel, glow, out Run valueRun, delegate(double v)
			{
				state.ReferralsByPlan[plan.Key] = v;
				referralEstimatorCard.Recompute();
			}));
			planValues.Add((valueRun, plan));
		}
		base.Children.Add(SliderRow("How long they stick around", "how long they stick around", "/ friend", state.MonthsRetained.Value, 1.0, Math.Max(2, commissionMonths), GelAppearance.Mint, DS.Hex(13234383u), out Run valueRun2, delegate(double v)
		{
			state.MonthsRetained = v;
			referralEstimatorCard.Recompute();
		}));
		monthsValue = valueRun2;
		summary = Ui.Text("", 12.0, FontWeights.Medium, SK.TextSecondary, TextAlignment.Left, 0, wrap: true);
		summary.LineHeight = 17.0;
		base.Children.Add(summary);
		Recompute(animate: false);
	}

	private int ReferralsFor(Plan p)
	{
		return (int)Math.Round(state.ReferralsByPlan.GetValueOrDefault(p.Key));
	}

	private ReferralEarningsMixEstimate Estimate()
	{
		return ReferralEarningsEstimator.Estimate(plans.Select((Plan p) => new ReferralEarningsPlanMix(p.MonthlyPriceUsdCents, ReferralsFor(p))).ToList(), Months, commissionPercent, commissionMonths);
	}

	private void Recompute(bool animate = true)
	{
		ReferralEarningsMixEstimate referralEarningsMixEstimate = Estimate();
		total.Text = ReferralFormatting.WholeDollars(referralEarningsMixEstimate.FirstYearUsdCents);
		if (animate)
		{
			chart.Update(referralEarningsMixEstimate.FirstYearMonthlyUsdCents);
		}
		foreach (var planValue in planValues)
		{
			Run item = planValue.Value;
			Plan item2 = planValue.Plan;
			item.Text = ReferralFormatting.Referrals(ReferralsFor(item2), item2.Name);
		}
		if ((object)monthsValue != null)
		{
			monthsValue.Text = ReferralFormatting.Months(Months);
		}
		List<string> list = (from p in plans
			where ReferralsFor(p) > 0
			select ReferralFormatting.People(ReferralsFor(p)) + " on " + p.Name).ToList();
		summary.Text = ((list.Count == 0) ? "Move a slider to see what your referrals could earn." : $"Refer {string.Join(" and ", list)} a month who each stay {ReferralFormatting.Months(Months)}. {ReferralFormatting.FirstYearExplanation(referralEarningsMixEstimate, Months, commissionMonths)}");
		AutomationProperties.SetName(total, "In your first year: " + total.Text);
	}

	private static FrameworkElement SliderRow(string title, string? detail, string unit, double value, double min, double max, GelAppearance gel, Color glow, out Run valueRun, Action<double> changed)
	{
		TextBlock textBlock = new TextBlock
		{
			FontSize = 20.0,
			FontWeight = FontWeights.Bold,
			IsTextScaleFactorEnabled = false,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxLines = 1
		};
		Typography.SetNumeralAlignment(textBlock, FontNumeralAlignment.Tabular);
		valueRun = new Run
		{
			Foreground = Ui.Brush(SK.TextPrimary)
		};
		textBlock.Inlines.Add(valueRun);
		textBlock.Inlines.Add(new Run
		{
			Text = " " + unit,
			Foreground = Ui.Brush(SK.TextTertiary)
		});
		Grid grid = new Grid
		{
			ColumnSpacing = 8.0
		};
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.Children.Add(textBlock);
		if (detail != null)
		{
			TextBlock textBlock2 = Ui.Text(detail, 11.5, FontWeights.Medium, SK.TextTertiary, TextAlignment.Left, 1);
			textBlock2.VerticalAlignment = VerticalAlignment.Bottom;
			textBlock2.Margin = new Thickness(0.0, 0.0, 0.0, 3.0);
			Grid.SetColumn(textBlock2, 1);
			grid.Children.Add(textBlock2);
		}
		GelSlider gelSlider = new GelSlider(value, min, max, gel, glow, title)
		{
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		};
		gelSlider.ValueChanged += changed;
		return new StackPanel
		{
			Spacing = 7.0,
			Children = 
			{
				(UIElement)grid,
				(UIElement)gelSlider
			}
		};
	}
}
