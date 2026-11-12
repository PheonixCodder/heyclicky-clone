using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Ink;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace HeyClicky.App.Ink;

public static class ClickyInkRunStamp
{
	private static readonly HashSet<string> editing = new HashSet<string>();

	internal static void SetEditing(string blockID, bool isEditing)
	{
		if (isEditing)
		{
			editing.Add(blockID);
		}
		else
		{
			editing.Remove(blockID);
		}
	}

	public static FrameworkElement Create(ClickyInkLiveNote.Block block, double clock, ClickyInkStyle style, double width, ClickyInkActions actions)
	{
		bool isWindowPreview = ClickyInkEnvironment.Current.IsWindowPreview;
		Grid host = new Grid
		{
			Width = Math.Max(0.0, width),
			HorizontalAlignment = HorizontalAlignment.Left
		};
		Build(editorAppears: false);
		return host;
		void Build(bool editorAppears)
		{
			host.Children.Clear();
			bool flag = editing.Contains(block.Id) && !isWindowPreview;
			ClickyInkRunLine clickyInkRunLine = new ClickyInkRunLine(block.Source);
			ClickyInkRoutineControl clickyInkRoutineControl = ClickyInkDashboardViewMath.RunStampRoutine(block.Source, actions.Routines);
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 1.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			string ranAt = clickyInkRunLine.RanAt;
			if (ranAt != null)
			{
				stackPanel.Children.Add(InkTextView.Plain(ranAt, ClickyPaperFonts.Typed(11.5, FontWeights.SemiBold), style.Ink.Opacity(0.8)));
			}
			ClickyInkHStack clickyInkHStack = new ClickyInkHStack(8.0, InkVerticalAlignment.Center);
			if ((object)clickyInkRoutineControl != null)
			{
				clickyInkHStack.Children.Add(ClickyInkRoutineStatusLine.Create(clickyInkRoutineControl, style));
			}
			else
			{
				string next = clickyInkRunLine.Next;
				if (next != null)
				{
					clickyInkHStack.Children.Add(InkTextView.Plain("next: " + next, ClickyPaperFonts.Hand(15.0), style.Quiet));
				}
			}
			string extra = clickyInkRunLine.Extra;
			if (extra != null)
			{
				clickyInkHStack.Children.Add(InkTextView.Plain(extra, ClickyPaperFonts.Hand(15.0), style.Quiet).With(delegate(InkTextView t)
				{
					t.LineLimit = 1;
				}));
			}
			if (clickyInkHStack.Children.Count > 0)
			{
				stackPanel.Children.Add(clickyInkHStack);
			}
			ClickyInkHStack clickyInkHStack2 = new ClickyInkHStack(10.0, InkVerticalAlignment.Center, ClickyInkRunStampMark.Create(clickyInkRunLine.Number, block.Source, clock, block.LandsAt), ClickyInkHStack.Flexible(stackPanel), new ClickyInkSpacer());
			if (actions.Routines.Count > 0 && !isWindowPreview)
			{
				TextBlock item = ClickyPaperFonts.Typed(12.0, FontWeights.SemiBold).Apply(new TextBlock
				{
					Text = (flag ? "Done" : "Edit"),
					Foreground = Ui.Brush(style.Fresh),
					IsTextScaleFactorEnabled = false,
					Margin = new Thickness(6.0, 3.0, 6.0, 3.0)
				});
				Pressable pressable = new Pressable
				{
					Background = Ui.Transparent,
					Children = { (UIElement)item }
				};
				AutomationProperties.SetName(pressable, flag ? "Done editing the routine" : "Edit the routine");
				pressable.Click += delegate
				{
					if (!editing.Remove(block.Id))
					{
						editing.Add(block.Id);
					}
					Build(editorAppears: true);
				};
				clickyInkHStack2.Children.Add(pressable);
			}
			StackPanel stackPanel2 = new StackPanel
			{
				Spacing = 10.0,
				Children = { (UIElement)clickyInkHStack2 }
			};
			if (flag)
			{
				FrameworkElement editor = ClickyInkRoutineEditor.Create(actions.Routines, style, actions, delegate
				{
					Build(editorAppears: false);
				});
				stackPanel2.Children.Add(editor);
				if (editorAppears)
				{
					SwiftSpring spring = new SwiftSpring(0.32, 0.85);
					ElementCompositionPreview.SetIsTranslationEnabled(editor, value: true);
					editor.Opacity = 0.0;
					editor.Translation = new Vector3(0f, -12f, 0f);
					editor.Loaded += delegate
					{
						Anim.OpacitySpring(editor, 1f, spring);
						Anim.TranslationSpring(editor, Vector3.Zero, spring);
					};
				}
			}
			host.Children.Add(stackPanel2);
		}
	}
}
