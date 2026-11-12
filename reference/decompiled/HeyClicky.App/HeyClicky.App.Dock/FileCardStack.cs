using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Dock;

public sealed class FileCardStack : Grid
{
	public const int ShownCardCount = 3;

	public const double NoteRoomTaken = 132.0;

	private static readonly double[] TiltDegrees = new double[3] { 5.0, -6.0, 2.5 };

	private static readonly double[] VerticalOffsets = new double[3] { -12.0, 22.0, -36.0 };

	private static readonly double[] HorizontalOffsets = new double[3] { 26.0, 40.0, 52.0 };

	private const double ArrivalDistance = 178.0;

	private static readonly Dictionary<string, DateTimeOffset> arrivals = new Dictionary<string, DateTimeOffset>();

	private readonly List<(FileCard Card, Grid Slot, int Position)> cards = new List<(FileCard, Grid, int)>();

	private readonly Action<string> open;

	private string? hovered;

	public static FileCardStack? Make(IReadOnlyList<string> entries, string? workspace, Action<string> open)
	{
		if (entries.Count != 0)
		{
			return new FileCardStack(entries, workspace, open);
		}
		return null;
	}

	private FileCardStack(IReadOnlyList<string> entries, string? workspace, Action<string> open)
	{
		FileCardStack fileCardStack = this;
		this.open = open;
		base.Width = 152.0;
		base.HorizontalAlignment = HorizontalAlignment.Right;
		List<string> list = entries.Take(3).ToList();
		string arrivalKey = string.Join("\n", entries);
		DateTimeOffset value;
		bool flag = (arrivals.TryGetValue(arrivalKey, out value) && DateTimeOffset.Now - value < TimeSpan.FromSeconds(3L)) || Anim.ReduceMotion;
		for (int i = 0; i < list.Count; i++)
		{
			string entry = list[i];
			FileCard fileCard = new FileCard(entry, workspace, CardReadyTitle(entry, workspace), delegate
			{
				fileCardStack.Open(entry);
			})
			{
				IsHitTestVisible = false
			};
			double y = VerticalOffsets[i] * (double)((list.Count != 1) ? 1 : 0);
			Grid grid = new Grid
			{
				Width = 138.0,
				Height = 166.0,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 0.0 - HorizontalOffsets[i], 0.0),
				RenderTransform = new TranslateTransform
				{
					Y = y
				}
			};
			fileCard.CenterPoint = new Vector3(69f, 83f, 0f);
			fileCard.Rotation = (float)(TiltDegrees[i] + (double)((!flag) ? 10 : 0));
			fileCard.Translation = new Vector3(flag ? 0f : 178f, 0f, 0f);
			grid.Children.Add(fileCard);
			Pressable pressable = new Pressable
			{
				Width = Math.Max(0.0, 138.0 - HorizontalOffsets[i]),
				Height = 166.0,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			pressable.Click += delegate
			{
				fileCardStack.Open(entry);
			};
			pressable.HoverChanged += delegate(bool h)
			{
				fileCardStack.Hover(entry, h);
			};
			AutomationProperties.SetName(pressable, FileCard.AccessibilityText(entry, workspace));
			pressable.ContextFlyout = Menu(entry, workspace);
			grid.Children.Add(pressable);
			Canvas.SetZIndex(grid, list.Count - i);
			base.Children.Add(grid);
			cards.Add((fileCard, grid, i));
		}
		if (entries.Count > 3)
		{
			Border border = new Border
			{
				Background = Ui.Brush(Colors.White),
				Padding = new Thickness(7.0, 0.0, 7.0, 0.0),
				Height = 20.0,
				IsHitTestVisible = false,
				HorizontalAlignment = HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Center,
				Child = Ui.Text($"+{entries.Count - 3}", 11.0, FontWeights.Bold, ArcColors.Ink.WithAlpha(0.7)),
				Margin = new Thickness(0.0, 140.0, 108.0, 0.0)
			}.Capsule();
			Canvas.SetZIndex(border, 200);
			base.Children.Add(border);
		}
		if (flag)
		{
			arrivals[arrivalKey] = DateTimeOffset.Now;
			return;
		}
		base.Loaded += delegate
		{
			foreach (var card2 in fileCardStack.cards)
			{
				FileCard card = card2.Card;
				int item = card2.Position;
				int p = item;
				DockTimer.After(0.096 + (double)p * 0.07, delegate
				{
					SwiftSpring spring = new SwiftSpring(0.55, 0.82);
					Anim.TranslationSpring(card, Vector3.Zero, spring);
					RotateSpring(card, TiltDegrees[p], spring);
				});
			}
			arrivals[arrivalKey] = DateTimeOffset.Now;
			if (arrivals.Count > 40)
			{
				foreach (string item2 in (from a in arrivals
					where DateTimeOffset.Now - a.Value > TimeSpan.FromSeconds(30L)
					select a.Key).ToList())
				{
					arrivals.Remove(item2);
				}
			}
		};
		base.Unloaded += delegate
		{
			arrivals[arrivalKey] = DateTimeOffset.Now;
		};
	}

	private static void RotateSpring(UIElement e, double degrees, SwiftSpring spring)
	{
		if (Anim.ReduceMotion || Anim.Instant)
		{
			e.Rotation = (float)degrees;
			return;
		}
		SpringScalarNaturalMotionAnimation springScalarNaturalMotionAnimation = spring.Scalar(Ui.Compositor, (float)degrees);
		springScalarNaturalMotionAnimation.Target = "Rotation";
		e.StartAnimation(springScalarNaturalMotionAnimation);
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
			Grid item2 = card.Slot;
			int item3 = card.Position;
			bool flag = item.Entry == hovered;
			item.SetHovered(flag);
			Canvas.SetZIndex(item2, flag ? 100 : (cards.Count - item3));
			if (!Anim.ReduceMotion)
			{
				Anim.TranslationSpring(item, new Vector3(flag ? (-18) : 0, 0f, 0f), spring);
				Anim.ScaleSpring(item, flag ? 1.05f : 1f, spring, null);
				RotateSpring(item, flag ? 0.0 : TiltDegrees[item3], spring);
			}
		}
	}

	private void Open(string entry)
	{
		open(entry);
	}

	internal static MenuFlyout Menu(string entry, string? workspace, Action? open = null)
	{
		MenuFlyout menuFlyout = new MenuFlyout();
		MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
		{
			Text = "Open"
		};
		menuFlyoutItem.Click += delegate
		{
			if (open != null)
			{
				open();
			}
			else
			{
				DockFiles.Open(entry, workspace);
			}
		};
		menuFlyout.Items.Add(menuFlyoutItem);
		if (!CardReadyOutput.IsWebLink(entry))
		{
			MenuFlyoutItem menuFlyoutItem2 = new MenuFlyoutItem
			{
				Text = "Show in File Explorer"
			};
			menuFlyoutItem2.Click += delegate
			{
				DockFiles.Reveal(entry, workspace);
			};
			menuFlyout.Items.Add(menuFlyoutItem2);
		}
		return menuFlyout;
	}

	internal static string CardReadyTitle(string entry, string? workspace)
	{
		return CardReadyOutput.CardButtonTitle(CardReadyOutput.Make(new global::_003C_003Ez__ReadOnlySingleElementList<string>(entry), null, null, (IReadOnlyList<string> e) => e, (string e) => DockFiles.Resolve(e, workspace), FileKind));
	}

	public static (string KindLabel, string Symbol, uint Tint) FileKind(string path)
	{
		DockFileStyle dockFileStyle = DockFileStyle.For(path);
		uint item = (uint)((dockFileStyle.Tint.R << 16) | (dockFileStyle.Tint.G << 8) | dockFileStyle.Tint.B);
		string kind = dockFileStyle.Kind;
		return (KindLabel: dockFileStyle.Kind, Symbol: kind switch
		{
			"PDF" => "doc.richtext.fill", 
			"Image" => "photo.fill", 
			"Spreadsheet" => "tablecells.fill", 
			"Document" => "doc.text.fill", 
			"Markdown" => "text.alignleft", 
			"Text" => "doc.plaintext.fill", 
			"Web page" => "globe", 
			"Slides" => "rectangle.on.rectangle.fill", 
			"Video" => "film.fill", 
			"Audio" => "waveform", 
			"Data" => "curlybraces", 
			"Code" => "chevron.left.forwardslash.chevron.right", 
			"Archive" => "doc.zipper", 
			"Folder" => "folder.fill", 
			"Link" => "link", 
			_ => "doc.fill", 
		}, Tint: item);
	}
}
