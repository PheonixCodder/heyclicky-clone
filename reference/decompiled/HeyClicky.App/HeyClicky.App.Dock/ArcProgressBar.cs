using HeyClicky.Agents.Jobs;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.UI;

namespace HeyClicky.App.Dock;

public sealed class ArcProgressBar : Grid
{
	private readonly bool isLarge;

	private readonly bool showsLabel;

	private readonly bool isCompact;

	private readonly Color ink;

	private GelTrackView? track;

	private TextBlock? label;

	private bool hadPlan;

	private double TroughHeight => isLarge ? 16 : (isCompact ? 9 : 12);

	private double LabelSize => isLarge ? 21 : (isCompact ? 11 : 14);

	public ArcProgressBar(ClickyArcPlan? plan, Color ink, bool isLarge, bool showsLabel = true, bool isCompact = false)
	{
		this.isLarge = isLarge;
		this.showsLabel = showsLabel;
		this.isCompact = isCompact;
		this.ink = ink;
		Build(plan);
	}

	private void Build(ClickyArcPlan? plan)
	{
		base.Children.Clear();
		base.RowDefinitions.Clear();
		base.ColumnDefinitions.Clear();
		bool flag = (hadPlan = (object)plan != null && !plan.IsEmpty);
		track = (flag ? new GelTrackView(plan.Steps.Count, plan.DoneCount, plan.CurrentIndex, TroughHeight, ink) : new GelTrackView(1, 0, null, TroughHeight, ink, isIndeterminate: true));
		string text = ((!showsLabel) ? null : ((!flag) ? "Working" : (isLarge ? $"{plan.DoneCount} of {plan.Steps.Count} steps" : $"{plan.DoneCount}/{plan.Steps.Count}")));
		label = ((text == null) ? null : Ui.Text(text, LabelSize, FontWeights.SemiBold, ink.WithAlpha(isLarge ? 0.6 : 0.55)));
		if ((object)label != null)
		{
			label.FontFamily = DS.Fonts.Display;
			Typography.SetNumeralAlignment(label, FontNumeralAlignment.Tabular);
		}
		if (isLarge)
		{
			base.RowSpacing = 10.0;
			base.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			base.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			base.Children.Add(track);
			if ((object)label != null)
			{
				label.HorizontalAlignment = HorizontalAlignment.Center;
				Grid.SetRow(label, 1);
				base.Children.Add(label);
			}
			return;
		}
		base.ColumnSpacing = (isCompact ? 9 : 12);
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		base.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		track.VerticalAlignment = VerticalAlignment.Center;
		base.Children.Add(track);
		if ((object)label != null)
		{
			label.VerticalAlignment = VerticalAlignment.Center;
			Grid.SetColumn(label, 1);
			base.Children.Add(label);
		}
	}

	public void Update(ClickyArcPlan? plan)
	{
		bool flag = (object)plan != null && !plan.IsEmpty;
		if (flag != hadPlan || (object)track == null)
		{
			Build(plan);
		}
		else if (flag)
		{
			track.Update(plan.Steps.Count, plan.DoneCount, plan.CurrentIndex);
			if ((object)label != null)
			{
				label.Text = (isLarge ? $"{plan.DoneCount} of {plan.Steps.Count} steps" : $"{plan.DoneCount}/{plan.Steps.Count}");
			}
		}
	}
}
