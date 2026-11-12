using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Documents.Markdown;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerDocumentModel
{
	private static readonly List<MarkdownViewerDocumentModel> RecentDocuments = new List<MarkdownViewerDocumentModel>();

	private CancellationTokenSource? loadCancellation;

	private Guid loadGeneration = Guid.NewGuid();

	private string? fullyLoadedSource;

	public string FilePath { get; }

	public string MarkdownSource { get; private set; } = "";

	public MarkdownViewerPreparedDocument? Prepared { get; private set; }

	public string? LoadingError { get; private set; }

	public string DisplayTitle => Path.GetFileName(FilePath);

	public string? BaseDirectory => Path.GetDirectoryName(FilePath);

	public event Action? Changed;

	public static MarkdownViewerDocumentModel RetainedDocument(string path)
	{
		string full = Path.GetFullPath(path);
		MarkdownViewerDocumentModel markdownViewerDocumentModel = RecentDocuments.FirstOrDefault((MarkdownViewerDocumentModel d) => string.Equals(d.FilePath, full, StringComparison.OrdinalIgnoreCase));
		if (markdownViewerDocumentModel != null)
		{
			RecentDocuments.Remove(markdownViewerDocumentModel);
			RecentDocuments.Insert(0, markdownViewerDocumentModel);
			return markdownViewerDocumentModel;
		}
		MarkdownViewerDocumentModel markdownViewerDocumentModel2 = new MarkdownViewerDocumentModel(full);
		RecentDocuments.Insert(0, markdownViewerDocumentModel2);
		if (RecentDocuments.Count > 3)
		{
			RecentDocuments.RemoveAt(RecentDocuments.Count - 1);
		}
		return markdownViewerDocumentModel2;
	}

	private MarkdownViewerDocumentModel(string path)
	{
		FilePath = path;
	}

	public void ReloadFromDisk()
	{
		loadCancellation?.Cancel();
		CancellationToken ct = (loadCancellation = new CancellationTokenSource()).Token;
		Guid generation = (loadGeneration = Guid.NewGuid());
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		string previousSource = fullyLoadedSource;
		string path = FilePath;
		string baseDirectory = BaseDirectory;
		LoadingError = null;
		Task.Run(async delegate
		{
			_ = 3;
			try
			{
				string source;
				using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					using StreamReader reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
					source = await reader.ReadToEndAsync(ct);
				}
				ct.ThrowIfCancellationRequested();
				if (!(source == previousSource))
				{
					IReadOnlyList<MarkdownViewerBlock> blocks = MarkdownViewerParser.ParseBlocks(source);
					ct.ThrowIfCancellationRequested();
					MarkdownViewerPreparedDocument prepared = await MarkdownViewerPreparedDocument.PrepareAsync(blocks, baseDirectory, null, ct);
					ct.ThrowIfCancellationRequested();
					Publish(dq, generation, delegate
					{
						MarkdownSource = source;
						Prepared = prepared;
						if (prepared.RemoteImageSourcesToLoad.Count == 0)
						{
							fullyLoadedSource = source;
						}
					});
					if (prepared.RemoteImageSourcesToLoad.Count != 0)
					{
						Dictionary<string, byte[]> remoteImagesBySource = await MarkdownViewerPreparedDocument.DownloadAsync(prepared.RemoteImageSourcesToLoad, ct);
						ct.ThrowIfCancellationRequested();
						MarkdownViewerPreparedDocument updated = await MarkdownViewerPreparedDocument.PrepareAsync(blocks, baseDirectory, remoteImagesBySource, ct);
						ct.ThrowIfCancellationRequested();
						Publish(dq, generation, delegate
						{
							Prepared = updated;
							fullyLoadedSource = source;
						});
					}
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex2)
			{
				if (!ct.IsCancellationRequested)
				{
					Log.Warn("markdown", "couldn't load " + Path.GetFileName(path) + ": " + ex2.Message);
					Publish(dq, generation, delegate
					{
						LoadingError = "This document couldn’t be loaded.";
					});
				}
			}
		}, ct);
	}

	private void Publish(DispatcherQueue? dq, Guid generation, Action apply)
	{
		if ((object)dq == null || dq.HasThreadAccess)
		{
			Run();
		}
		else
		{
			dq.TryEnqueue(Run);
		}
		void Run()
		{
			if (!(loadGeneration != generation))
			{
				apply();
				this.Changed?.Invoke();
			}
		}
	}

	public void CancelLoading()
	{
		loadGeneration = Guid.NewGuid();
		loadCancellation?.Cancel();
		loadCancellation = null;
	}
}
