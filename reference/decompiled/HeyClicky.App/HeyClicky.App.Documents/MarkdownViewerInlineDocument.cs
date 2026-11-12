using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Home;
using HeyClicky.Core.Documents.Markdown;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerInlineDocument : Grid
{
	private sealed record CacheEntry(IReadOnlyList<MarkdownViewerBlock> Blocks, MarkdownViewerPreparedDocument Prepared);

	public const double TextScale = 0.875;

	private static readonly List<(string Key, CacheEntry Entry)> RenderCache = new List<(string, CacheEntry)>();

	private readonly MarkdownViewerTextView textView = new MarkdownViewerTextView(usesPageLayout: false, 0.875);

	private string markdown = "";

	private string? baseDirectory;

	private CancellationTokenSource? imageLoad;

	public string Markdown
	{
		get
		{
			return markdown;
		}
		set
		{
			SetMarkdown(value, baseDirectory);
		}
	}

	public MarkdownViewerInlineDocument(string markdown, string? baseDirectory = null)
	{
		base.RequestedTheme = ((!MessagesPalette.Current.IsDark) ? ElementTheme.Light : ElementTheme.Dark);
		base.Children.Add(textView);
		textView.WholeDocumentText = () => this.markdown;
		this.baseDirectory = baseDirectory;
		Markdown = markdown;
		base.Unloaded += delegate
		{
			imageLoad?.Cancel();
		};
	}

	public void SetMarkdown(string newMarkdown, string? newBaseDirectory)
	{
		if (newMarkdown == markdown && newBaseDirectory == baseDirectory && textView.ActualHeight > 0.0)
		{
			return;
		}
		markdown = newMarkdown;
		baseDirectory = newBaseDirectory;
		imageLoad?.Cancel();
		CancellationTokenSource cts = (imageLoad = new CancellationTokenSource());
		string key = baseDirectory + "\u001f" + markdown;
		CacheEntry item = RenderCache.FirstOrDefault<(string, CacheEntry)>(((string Key, CacheEntry Entry) e) => e.Key == key).Item2;
		if ((object)item != null)
		{
			textView.Install(item.Prepared);
			LoadRemoteImages(item, cts.Token);
			return;
		}
		IReadOnlyList<MarkdownViewerBlock> blocks = MarkdownViewerParser.ParseBlocks(markdown);
		Task<MarkdownViewerPreparedDocument> preparing = MarkdownViewerPreparedDocument.PrepareAsync(blocks, baseDirectory);
		if (preparing.IsCompletedSuccessfully)
		{
			Show(key, blocks, preparing.Result, cts.Token);
		}
		else
		{
			FinishAsync();
		}
		async Task FinishAsync()
		{
			try
			{
				MarkdownViewerPreparedDocument prepared = await preparing;
				if (!cts.IsCancellationRequested)
				{
					Show(key, blocks, prepared, cts.Token);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("markdown", "inline document: " + ex.Message);
			}
		}
	}

	private void Show(string key, IReadOnlyList<MarkdownViewerBlock> blocks, MarkdownViewerPreparedDocument prepared, CancellationToken ct)
	{
		CacheEntry cacheEntry = new CacheEntry(blocks, prepared);
		RenderCache.Insert(0, (key, cacheEntry));
		if (RenderCache.Count > 8)
		{
			RenderCache.RemoveAt(RenderCache.Count - 1);
		}
		textView.Install(prepared);
		LoadRemoteImages(cacheEntry, ct);
	}

	private void LoadRemoteImages(CacheEntry entry, CancellationToken ct)
	{
		IReadOnlyList<string> sources = entry.Prepared.RemoteImageSourcesToLoad;
		if (sources.Count == 0)
		{
			return;
		}
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		string directory = baseDirectory;
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				Dictionary<string, byte[]> dictionary = await MarkdownViewerPreparedDocument.DownloadAsync(sources, ct);
				if (dictionary.Count != 0 && !ct.IsCancellationRequested)
				{
					MarkdownViewerPreparedDocument updated = await MarkdownViewerPreparedDocument.PrepareAsync(entry.Blocks, directory, dictionary, ct);
					dq.TryEnqueue(delegate
					{
						if (!ct.IsCancellationRequested)
						{
							textView.Install(updated);
						}
					});
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				Log.Warn("markdown", "inline document images: " + ex2.Message);
			}
		}, ct);
	}
}
