using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Dock;

internal sealed class FileCardRow : Grid
{
	private static readonly double[] TiltDegrees = new double[4] { -3.0, 2.5, -1.5, 3.5 };

	private readonly List<(FileCard Card, double Tilt)> cards = new List<(FileCard, double)>();

	private string? hovered;

	public FileCardRow(IReadOnlyList<string> entries, Action<string> open)
	{
		FileCardRow fileCardRow = this;
		base.HorizontalAlignment = HorizontalAlignment.Left;
		base.Padding = new Thickness(4.0, 6.0, 0.0, 6.0);
		HashSet<string> seen = new HashSet<string>();
		IReadOnlyList<string> readOnlyList = ArcCardContentSource.MainEntries(entries.Where((string e) => e.Length > 0 && seen.Add(e)).ToList(), null);
		DockFlow dockFlow = new DockFlow
		{
			HorizontalSpacing = 14.0,
			VerticalSpacing = 14.0
		};
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			string entry = readOnlyList[i];
			double num = TiltDegrees[i % TiltDegrees.Length];
			FileCard fileCard = new FileCard(entry, null, FileCardStack.CardReadyTitle(entry, null), delegate
			{
				open(entry);
			})
			{
				CenterPoint = new Vector3(69f, 83f, 0f),
				Rotation = (Anim.ReduceMotion ? 0f : ((float)num))
			};
			Pressable pressable = new Pressable
			{
				Width = 138.0,
				Height = 166.0
			};
			pressable.Click += delegate
			{
				open(entry);
			};
			pressable.HoverChanged += delegate(bool h)
			{
				fileCardRow.Hover(entry, h);
			};
			pressable.ContextFlyout = FileCardStack.Menu(entry, null, delegate
			{
				open(entry);
			});
			AutomationProperties.SetName(pressable, FileCard.AccessibilityText(entry, null));
			dockFlow.Children.Add(new Grid
			{
				Children = 
				{
					(UIElement)fileCard,
					(UIElement)pressable
				}
			});
			cards.Add((fileCard, num));
		}
		base.Children.Add(dockFlow);
	}

	private void Hover(string entry, bool hovering)
	{
		if (hovering && hovered != entry)
		{
			FileCardSound.PlayLift();
		}
		if (hovering)
		{
			hovered = entry;
		}
		else if (hovered == entry)
		{
			hovered = null;
		}
		SwiftSpring spring = new SwiftSpring(0.28, 0.8);
		foreach (var card in cards)
		{
			FileCard item = card.Card;
			double item2 = card.Tilt;
			bool flag = item.Entry == hovered;
			item.SetHovered(flag);
			if (item.Parent is UIElement element)
			{
				Canvas.SetZIndex(element, flag ? 1 : 0);
			}
			if (!Anim.ReduceMotion)
			{
				Anim.ScaleSpring(item, flag ? 1.04f : 1f, spring, null);
				SpringScalarNaturalMotionAnimation springScalarNaturalMotionAnimation = spring.Scalar(Ui.Compositor, flag ? 0f : ((float)item2));
				springScalarNaturalMotionAnimation.Target = "Rotation";
				item.StartAnimation(springScalarNaturalMotionAnimation);
			}
		}
	}
}
