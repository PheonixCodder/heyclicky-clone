using System;
using System.Collections.Generic;
using HeyClicky.App.Design;
using HeyClicky.App.Documents;
using HeyClicky.App.Home;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchFullReply : Grid
{
	private const double TextInset = 12.0;

	private readonly INotchHost host;

	private readonly string entryId;

	private readonly StackPanel column = new StackPanel
	{
		Spacing = 14.0
	};

	public NotchFullReply(INotchHost host, string entryId)
	{
		NotchFullReply notchFullReply = this;
		this.host = host;
		this.entryId = entryId;
		ChatHandOff entry = NotchChat.HandOffs?.Entry(entryId);
		string text = ChatReplyPreview.FullReplyMarkdown(entry);
		base.Margin = new Thickness(-18.0, -6.0, -18.0, -6.0);
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(34.0)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		Grid grid = new Grid
		{
			IsHitTestVisible = false
		};
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(34.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(56.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = new Border
		{
			Background = Ui.Vertical((0.0, Colors.Black), (1.0, HomeTheme.PaneFill))
		};
		Border border2 = new Border
		{
			Background = Ui.Brush(HomeTheme.PaneFill)
		};
		Grid.SetRow(border, 1);
		Grid.SetRow(border2, 2);
		grid.Children.Add(new Border
		{
			Background = Ui.Brush(Colors.Black)
		});
		grid.Children.Add(border);
		grid.Children.Add(border2);
		Grid.SetRowSpan(grid, 3);
		base.Children.Add(grid);
		Pressable pressable = new Pressable
		{
			Width = 26.0,
			Height = 26.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom
		};
		Ellipse disc = new Ellipse
		{
			Fill = Ui.Brush(DS.White(0.09))
		};
		TextBlock x = Icons.Glyph("\ue711", 11.0, DS.White(0.7));
		x.FontWeight = FontWeights.Bold;
		pressable.Children.Add(disc);
		pressable.Children.Add(x);
		pressable.HoverChanged += delegate(bool h)
		{
			disc.Fill = Ui.Brush(DS.White(h ? 0.16 : 0.09));
			x.Foreground = Ui.Brush(DS.White(h ? 0.95 : 0.7));
		};
		pressable.Click += delegate
		{
			(host.Services.Presenter as NotchPresenter)?.Tabs.CloseFullReply(NotchChatFullReplyCloseVia.CloseButton);
		};
		pressable.Tip("Back to the chat (esc)");
		pressable.Margin = new Thickness(0.0, 0.0, 30.0, 2.0);
		base.Children.Add(pressable);
		double width = NotchPeekLayout.FullReplyContentWidth(text) - 24.0;
		column.Width = width;
		column.Margin = new Thickness(0.0, 8.0, 0.0, 20.0);
		column.HorizontalAlignment = HorizontalAlignment.Center;
		column.VerticalAlignment = VerticalAlignment.Top;
		if ((object)entry != null)
		{
			TextBlock textBlock = Ui.Text(entry.TaskTitle ?? entry.Title, 15.0, FontWeights.SemiBold, HomeTheme.PrimaryText, TextAlignment.Left, 2);
			textBlock.TextWrapping = TextWrapping.Wrap;
			TextBlock item = Ui.Text(((entry.TaskTitle != null && entry.TaskTitle != entry.Title) ? (entry.Title + "  ·  ") : "") + ChatBubbles.ShortTime(entry.CreatedAt), 11.0, FontWeights.Normal, HomeTheme.TertiaryText);
			column.Children.Add(new StackPanel
			{
				Spacing = 2.0,
				Children = 
				{
					(UIElement)textBlock,
					(UIElement)item
				}
			});
		}
		if (text != null)
		{
			column.Children.Add(new MarkdownViewerInlineDocument(text));
		}
		IReadOnlyList<string> readOnlyList = entry?.ArtifactEntries;
		if (readOnlyList != null && readOnlyList.Count > 0)
		{
			FrameworkElement frameworkElement = NotchExtensions.LiveWork?.FileCards(readOnlyList, Open) ?? ChatHandOffRow.FilePills(readOnlyList, Open);
			frameworkElement.Margin = new Thickness(0.0, -8.0, 0.0, 0.0);
			column.Children.Add(frameworkElement);
		}
		if ((object)entry != null)
		{
			column.Children.Add(Footer(entry));
			IReadOnlyList<string> readOnlyList2 = NotchChat.HandOffs?.ShownFollowUps(entry.Id) ?? Array.Empty<string>();
			if (readOnlyList2.Count > 0)
			{
				WrapPanel wrapPanel = new WrapPanel
				{
					HorizontalSpacing = 6.0,
					VerticalSpacing = 6.0
				};
				foreach (string item2 in readOnlyList2)
				{
					wrapPanel.Children.Add(Pill(entry, item2));
				}
				column.Children.Add(wrapPanel);
			}
		}
		ScrollViewer scrollViewer = new ScrollViewer
		{
			Content = column,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(30.0, 0.0, 30.0, 0.0)
		};
		Grid.SetRow(scrollViewer, 1);
		base.Children.Add(scrollViewer);
		column.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			NotchTabs notchTabs = (host.Services.Presenter as NotchPresenter)?.Tabs;
			double num = Math.Ceiling(e.NewSize.Height + notchFullReply.column.Margin.Top + notchFullReply.column.Margin.Bottom);
			if (notchTabs != null && notchTabs.ExpandedReplyEntryId == entryId && notchTabs.FullReplyContentHeight != num)
			{
				notchTabs.FullReplyContentHeight = num;
				host.Relayout();
			}
		};
		FrameworkElement frameworkElement2 = FollowUpFooter(entry);
		Grid.SetRow(frameworkElement2, 2);
		base.Children.Add(frameworkElement2);
		base.Loaded += delegate
		{
			ChatHandOffs handOffs = NotchChat.HandOffs;
			if (handOffs != null)
			{
				handOffs.Changed += OnHandOffsChanged;
			}
			notchFullReply.CloseIfEntryGone();
		};
		base.Unloaded += delegate
		{
			ChatHandOffs handOffs2 = NotchChat.HandOffs;
			if (handOffs2 != null)
			{
				handOffs2.Changed -= OnHandOffsChanged;
			}
		};
		void OnHandOffsChanged()
		{
			notchFullReply.DispatcherQueue.TryEnqueue(notchFullReply.CloseIfEntryGone);
		}
		void Open(string artifact)
		{
			NotchPeekSurface.PeekAction("chat_artifact_opened", NotchChatAnalytics.ChatArtifactOpened("full_reply", artifact));
			(host.Actions as INotchActions)?.OpenChatArtifact(artifact, entry.ThreadId, entry.ClickySlug);
		}
	}

	private void CloseIfEntryGone()
	{
		if ((object)NotchChat.HandOffs?.Entry(entryId) == null)
		{
			NotchTabs notchTabs = (host.Services.Presenter as NotchPresenter)?.Tabs;
			if (notchTabs != null && notchTabs.ExpandedReplyEntryId == entryId)
			{
				notchTabs.CloseFullReply(null);
			}
		}
	}

	private static FrameworkElement Footer(ChatHandOff entry)
	{
		TextBlock textBlock = Ui.Text(ChatBubbles.ShortTime(entry.CreatedAt), 11.0, FontWeights.Normal, HomeTheme.TertiaryText);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		Pressable pressable = new Pressable
		{
			Width = 22.0,
			Height = 22.0
		};
		TextBlock glyph = Icons.Glyph("\ue8c8", 11.0, HomeTheme.TertiaryText);
		pressable.Children.Add(glyph);
		pressable.HoverChanged += delegate(bool h)
		{
			glyph.Foreground = Ui.Brush(h ? HomeTheme.PrimaryText : HomeTheme.TertiaryText);
		};
		pressable.Click += delegate
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(entry.FullReply ?? entry.ReplyText ?? "");
			Clipboard.SetContent(dataPackage);
			glyph.Text = "\ue73e";
		};
		pressable.Tip("Copy");
		StackPanel stackPanel = Ui.H(6.0, textBlock, pressable);
		stackPanel.Margin = new Thickness(0.0, -8.0, 0.0, 0.0);
		return stackPanel;
	}

	private FrameworkElement FollowUpFooter(ChatHandOff? entry)
	{
		Grid grid = new Grid();
		if ((object)entry != null)
		{
			grid.Children.Add(new NotchChatComposer(host, entry.Id)
			{
				Margin = new Thickness(18.0, 0.0, 18.0, 6.0)
			});
		}
		else
		{
			grid.Height = 64.0;
		}
		grid.Children.Add(new Border
		{
			Height = 1.0,
			Background = Ui.Brush(DS.White(0.08)),
			VerticalAlignment = VerticalAlignment.Top,
			IsHitTestVisible = false
		});
		return grid;
	}

	private FrameworkElement Pill(ChatHandOff entry, string followUp)
	{
		Pressable p = new Pressable
		{
			Height = 24.0,
			CornerRadius = new CornerRadius(12.0),
			Background = Ui.Brush(DS.White(0.07)),
			BorderBrush = Ui.Brush(DS.White(0.1)),
			BorderThickness = new Thickness(1.0)
		};
		TextBlock label = Ui.Text(followUp, 12.0, FontWeights.Medium, HomeTheme.SecondaryText, TextAlignment.Left, 1);
		label.Margin = new Thickness(11.0, 0.0, 11.0, 0.0);
		label.VerticalAlignment = VerticalAlignment.Center;
		p.Children.Add(label);
		p.HoverChanged += delegate(bool h)
		{
			p.Background = Ui.Brush(DS.White(h ? 0.13 : 0.07));
			p.BorderBrush = Ui.Brush(DS.White(h ? 0.18 : 0.1));
			label.Foreground = Ui.Brush(h ? HomeTheme.PrimaryText : HomeTheme.SecondaryText);
		};
		p.Tip("Send \"" + followUp + "\"");
		AutomationProperties.SetName(p, "Ask: " + followUp);
		p.Click += delegate
		{
			(host.Actions as INotchActions)?.SendChatFollowUp(entry, followUp);
		};
		return p;
	}
}
