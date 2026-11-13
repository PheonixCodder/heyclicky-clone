using System;
using System.Threading.Tasks;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents.Spreadsheet;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Documents;

public sealed class SpreadsheetDocumentView : Grid
{
	private readonly MessagesPalette look = MessagesPalette.Current;

	public string Path { get; }

	public SpreadsheetPreview? Preview { get; private set; }

	public event Action<SpreadsheetPreview>? PreviewReady;

	public SpreadsheetDocumentView(string path, Func<FrameworkElement>? fallback)
	{
		Path = path;
		base.Children.Add(LoadingView(null));
		LoadAsync(fallback);
	}

	private async Task LoadAsync(Func<FrameworkElement>? fallback)
	{
		try
		{
			SpreadsheetWorkbook spreadsheetWorkbook = await SpreadsheetAssetCache.LoadAsync(Path);
			base.Children.Clear();
			if (spreadsheetWorkbook != null)
			{
				Preview = new SpreadsheetPreview(spreadsheetWorkbook);
				base.Children.Add(Preview);
				this.PreviewReady?.Invoke(Preview);
				return;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("documents", "spreadsheet load failed for " + Path + ": " + ex.Message);
			base.Children.Clear();
		}
		using (MessagesPalette.Use(look))
		{
			base.Children.Add(fallback?.Invoke() ?? LoadingView("This document couldn’t be loaded."));
		}
	}

	private static FrameworkElement LoadingView(string? error)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 12.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		if (error == null)
		{
			stackPanel.Children.Add(new ProgressRing
			{
				IsActive = true,
				Width = 16.0,
				Height = 16.0,
				HorizontalAlignment = HorizontalAlignment.Center
			});
		}
		stackPanel.Children.Add(Ui.Text(error ?? "Loading document…", 13.0, FontWeights.Normal, MessagesPalette.Current.SecondaryText, TextAlignment.Center));
		return stackPanel;
	}
}
