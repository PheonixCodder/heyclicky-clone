using System;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerContent : Grid
{
	private readonly MarkdownViewerDocumentModel model;

	private readonly MessagesPalette look = MessagesPalette.Current;

	private readonly MarkdownViewerPalette colors = MarkdownViewerPalette.Current;

	private readonly Grid page = new Grid();

	private readonly MarkdownViewerTextView textView = new MarkdownViewerTextView(usesPageLayout: true);

	private readonly FrameworkElement loading;

	private readonly TextBlock loadingText;

	private readonly ProgressRing loadingRing;

	private MarkdownViewerFileWatcher? watcher;

	private MarkdownViewerPreparedDocument? installed;

	public string FilePath => model.FilePath;

	internal MarkdownViewerTextView TextView => textView;

	public MarkdownViewerContent(string path)
	{
		model = MarkdownViewerDocumentModel.RetainedDocument(path);
		base.RequestedTheme = ((!look.IsDark) ? ElementTheme.Light : ElementTheme.Dark);
		page.Background = Ui.Brush(colors.PageBackground);
		textView.WholeDocumentText = () => model.MarkdownSource;
		page.Children.Add(new ScrollViewer
		{
			Content = textView,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled
		});
		page.Children.Add(CopyWholeDocumentButton());
		page.Visibility = Visibility.Collapsed;
		(loading, loadingText, loadingRing) = LoadingView();
		base.Children.Add(page);
		base.Children.Add(loading);
		base.Loaded += delegate
		{
			model.Changed += Refresh;
			Refresh();
			model.ReloadFromDisk();
			if (watcher == null)
			{
				watcher = new MarkdownViewerFileWatcher(model.FilePath, model.ReloadFromDisk);
			}
		};
		base.Unloaded += delegate
		{
			model.Changed -= Refresh;
			watcher?.Dispose();
			watcher = null;
			model.CancelLoading();
		};
	}

	private void Refresh()
	{
		MarkdownViewerPreparedDocument prepared = model.Prepared;
		if (prepared != null)
		{
			if (prepared != installed)
			{
				textView.Install(prepared);
				installed = prepared;
			}
			page.Visibility = Visibility.Visible;
			loading.Visibility = Visibility.Collapsed;
		}
		else
		{
			page.Visibility = Visibility.Collapsed;
			loading.Visibility = Visibility.Visible;
			loadingText.Text = model.LoadingError ?? "Loading document…";
			loadingRing.Visibility = ((model.LoadingError != null) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private (FrameworkElement, TextBlock, ProgressRing) LoadingView()
	{
		ProgressRing progressRing = new ProgressRing
		{
			IsActive = true,
			Width = 16.0,
			Height = 16.0,
			HorizontalAlignment = HorizontalAlignment.Center
		};
		TextBlock textBlock = Ui.Text("Loading document…", 13.0, FontWeights.Normal, look.SecondaryText, TextAlignment.Center);
		return (new StackPanel
		{
			Spacing = 12.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)progressRing,
				(UIElement)textBlock
			}
		}, textBlock, progressRing);
	}

	private FrameworkElement CopyWholeDocumentButton()
	{
		TextBlock glyph = Icons.Glyph("\ue8c8", 11.0, colors.SecondaryTextColor);
		glyph.FontWeight = FontWeights.Medium;
		TextBlock copied = Ui.Text("Copied", 11.0, FontWeights.Medium, colors.SecondaryTextColor);
		copied.Visibility = Visibility.Collapsed;
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 5.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel.Children.Add(glyph);
		stackPanel.Children.Add(copied);
		Pressable button = new Pressable
		{
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			Background = Ui.Brush(colors.RaisedChipBackgroundColor),
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 18.0, 20.0, 0.0)
		};
		button.Children.Add(stackPanel);
		button.SizeChanged += delegate(object _, SizeChangedEventArgs e)
		{
			button.CornerRadius = new CornerRadius(e.NewSize.Height / 2.0);
		};
		AutomationProperties.SetName(button, "Copy markdown");
		DispatcherTimer reset = null;
		button.Click += delegate
		{
			DataPackage dataPackage = new DataPackage();
			dataPackage.SetText(model.MarkdownSource);
			Clipboard.SetContent(dataPackage);
			glyph.Text = "\ue73e";
			copied.Visibility = Visibility.Visible;
			reset?.Stop();
			reset = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1.4)
			};
			reset.Tick += delegate
			{
				reset?.Stop();
				glyph.Text = "\ue8c8";
				copied.Visibility = Visibility.Collapsed;
			};
			reset.Start();
		};
		return button.Tip("Copy markdown");
	}
}
