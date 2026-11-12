using System;
using System.Collections.Generic;
using System.Numerics;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Peek;

public sealed class ChatMessageRow : Grid
{
	private readonly bool outgoing;

	private readonly MessagesPalette look = MessagesPalette.Current;

	private readonly string frame;

	private readonly StackPanel? bubbleStack;

	private readonly List<string> texts = new List<string>();

	private bool lastOfRun;

	public ChatMessageRow(ConversationMessage message, bool isLastOfRun, Action<string> openAttachment)
	{
		outgoing = message.IsUser;
		frame = Frame(message);
		lastOfRun = isLastOfRun;
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 3.0,
			HorizontalAlignment = (outgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left),
			MaxWidth = 390.0
		};
		if (outgoing)
		{
			IReadOnlyList<string> attachmentPaths = message.AttachmentPaths;
			if (attachmentPaths != null && attachmentPaths.Count > 0)
			{
				ChatSentAttachments chatSentAttachments = new ChatSentAttachments(attachmentPaths, openAttachment)
				{
					HorizontalAlignment = HorizontalAlignment.Right
				};
				if (!message.BubbleIsOnlyAboutAttachments)
				{
					chatSentAttachments.Margin = new Thickness(0.0, 0.0, 0.0, 3.0);
				}
				stackPanel.Children.Add(chatSentAttachments);
			}
		}
		if (!message.BubbleIsOnlyAboutAttachments)
		{
			IReadOnlyList<string> bubbleTexts = message.BubbleTexts;
			StackPanel obj = new StackPanel
			{
				Spacing = 2.0,
				HorizontalAlignment = (outgoing ? HorizontalAlignment.Right : HorizontalAlignment.Left)
			};
			StackPanel stackPanel2 = obj;
			bubbleStack = obj;
			StackPanel stackPanel3 = stackPanel2;
			for (int i = 0; i < bubbleTexts.Count; i++)
			{
				stackPanel3.Children.Add(ChatBubbles.Bubble(bubbleTexts[i], outgoing, isLastOfRun && i == bubbleTexts.Count - 1, look));
			}
			texts.AddRange(bubbleTexts);
			stackPanel.Children.Add(stackPanel3);
		}
		string actionCaption = message.ActionCaption;
		if (actionCaption != null)
		{
			StackPanel stackPanel4 = new StackPanel
			{
				Spacing = 2.0,
				Margin = new Thickness(6.0, 0.0, 6.0, 0.0)
			};
			foreach (ChatActionCaption.Item item in ChatActionCaption.Items(actionCaption))
			{
				TextBlock textBlock = Icons.Glyph(ChatBubbles.Glyph(item.Symbol), 9.0, look.TertiaryText);
				textBlock.FontWeight = FontWeights.SemiBold;
				textBlock.Width = 11.0;
				textBlock.VerticalAlignment = VerticalAlignment.Top;
				textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
				TextBlock textBlock2 = Ui.Text(item.Text, 10.5, FontWeights.Normal, look.TertiaryText, TextAlignment.Left, 2);
				textBlock2.TextWrapping = TextWrapping.Wrap;
				stackPanel4.Children.Add(Ui.H(5.0, textBlock, textBlock2));
			}
			stackPanel.Children.Add(stackPanel4);
		}
		stackPanel.Margin = (outgoing ? new Thickness(56.0, 0.0, 0.0, 0.0) : new Thickness(0.0, 0.0, 56.0, 0.0));
		base.Children.Add(stackPanel);
	}

	private static string Frame(ConversationMessage m)
	{
		global::_003C_003Ey__InlineArray4<object> buffer = default(global::_003C_003Ey__InlineArray4<object>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 0) = m.IsUser;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 1) = m.BubbleIsOnlyAboutAttachments;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 2) = m.ActionCaption;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray4<object>, object>(ref buffer, 3) = string.Join("\u001e", m.AttachmentPaths ?? Array.Empty<string>());
		return string.Join("\u001f", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray4<object>, object>(in buffer, 4));
	}

	public bool TryGrow(ConversationMessage message, bool isLastOfRun)
	{
		if ((object)bubbleStack == null || Frame(message) != frame)
		{
			return false;
		}
		IReadOnlyList<string> bubbleTexts = message.BubbleTexts;
		if (bubbleTexts.Count < texts.Count)
		{
			return false;
		}
		for (int i = 0; i < texts.Count; i++)
		{
			if (bubbleTexts[i] != texts[i])
			{
				return false;
			}
		}
		if (texts.Count > 0)
		{
			int num = texts.Count - 1;
			bool num2 = lastOfRun;
			bool flag = isLastOfRun && num == bubbleTexts.Count - 1;
			if (num2 != flag)
			{
				bubbleStack.Children[num] = ChatBubbles.Bubble(texts[num], outgoing, flag, look);
			}
		}
		for (int j = texts.Count; j < bubbleTexts.Count; j++)
		{
			FrameworkElement frameworkElement = ChatBubbles.Bubble(bubbleTexts[j], outgoing, isLastOfRun && j == bubbleTexts.Count - 1, look);
			bubbleStack.Children.Add(frameworkElement);
			GrowIn(frameworkElement);
			texts.Add(bubbleTexts[j]);
		}
		lastOfRun = isLastOfRun;
		return true;
	}

	private void GrowIn(FrameworkElement bubble)
	{
		if (!Anim.ReduceMotion)
		{
			bubble.Opacity = 0.0;
			bubble.SizeChanged += Go;
		}
		void Go(object sender, SizeChangedEventArgs e)
		{
			bubble.SizeChanged -= Go;
			SwiftSpring spring = new SwiftSpring(0.34, 0.86);
			Anim.ScaleUpFrom(bubble, 0.92f, spring, new Vector3(outgoing ? ((float)e.NewSize.Width) : 0f, (float)e.NewSize.Height, 0f));
			Anim.OpacitySpring(bubble, 1f, spring);
		}
	}
}
