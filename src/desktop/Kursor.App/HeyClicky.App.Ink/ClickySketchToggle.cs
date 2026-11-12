using System;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public sealed class ClickySketchToggle : Pressable
{
	public static readonly Size SwitchSize = new Size(46f, 24f);

	private readonly long seed;

	private readonly PaperDrawing track = new PaperDrawing();

	private readonly Grid knob = new Grid
	{
		Width = 18.0,
		Height = 18.0,
		HorizontalAlignment = HorizontalAlignment.Left,
		VerticalAlignment = VerticalAlignment.Top
	};

	private bool isOn;

	public bool IsOn
	{
		get
		{
			return isOn;
		}
		set
		{
			if (isOn != value)
			{
				isOn = value;
				base.AccessibleToggleState = value;
				Draw(animated: true);
				this.Toggled?.Invoke(value);
			}
		}
	}

	public event Action<bool>? Toggled;

	public ClickySketchToggle(bool isOn, long seed = 6L, string accessibilityTitle = "")
	{
		this.seed = seed;
		this.isOn = isOn;
		base.Width = SwitchSize.Width + 6.0;
		base.Height = SwitchSize.Height + 6.0;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.VerticalAlignment = VerticalAlignment.Top;
		Grid grid = new Grid
		{
			Margin = new Thickness(3.0)
		};
		grid.Children.Add(track);
		grid.Children.Add(knob);
		base.Children.Add(grid);
		Draw(animated: false);
		base.Click += delegate
		{
			IsOn = !this.isOn;
		};
		AutomationProperties.SetName(this, accessibilityTitle);
		base.AccessibleToggleState = isOn;
	}

	private void Draw(bool animated)
	{
		Size switchSize = SwitchSize;
		track.Children.Clear();
		PaperPath p = new ClickySketchRect(switchSize.Height / 2.0, seed, 0.4).PathIn(new Rect(0.0, 0.0, switchSize.Width, switchSize.Height));
		track.Fill(p, isOn ? ClickySketchInk.Teal : PaperColor.Hex("#E9EFF7"));
		track.Stroke(p, new PaperStroke(isOn ? ClickySketchInk.TealLine : PaperColor.Hex("#97ACC6"), 1.3));
		knob.Children.Clear();
		PaperPath p2 = new ClickySketchRect(9.0, seed + 2, 0.35).PathIn(new Rect(0f, 0f, 18f, 18f));
		knob.Children.Add(new PaperDrawing().Fill(p2, PaperColor.Hex("#FBFDFF")).Stroke(p2, new PaperStroke(isOn ? ClickySketchInk.TealLine : PaperColor.Hex("#97ACC6"), 1.3)));
		Vector3 vector = new Vector3((float)(isOn ? (switchSize.Width - 22.0) : 4.0), 3f, 0f);
		if (animated && !Anim.ReduceMotion)
		{
			Anim.TranslationSpring(knob, vector, new SwiftSpring(0.28, 0.75));
			return;
		}
		ElementCompositionPreview.SetIsTranslationEnabled(knob, value: true);
		knob.Translation = vector;
	}
}
