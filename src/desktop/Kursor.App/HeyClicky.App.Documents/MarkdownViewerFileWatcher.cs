using System;
using System.IO;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerFileWatcher : IDisposable
{
	private readonly FileSystemWatcher? watcher;

	private readonly DispatcherQueueTimer? debounce;

	public MarkdownViewerFileWatcher(string path, Action onFileChanged)
	{
		MarkdownViewerFileWatcher markdownViewerFileWatcher = this;
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		string directoryName = Path.GetDirectoryName(path);
		string name = Path.GetFileName(path);
		if ((object)dq == null || directoryName == null || !Directory.Exists(directoryName))
		{
			return;
		}
		debounce = dq.CreateTimer();
		debounce.Interval = TimeSpan.FromSeconds(0.15);
		debounce.IsRepeating = false;
		debounce.Tick += delegate
		{
			onFileChanged();
		};
		try
		{
			watcher = new FileSystemWatcher(directoryName)
			{
				NotifyFilter = (NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.CreationTime),
				IncludeSubdirectories = false
			};
			watcher.Changed += delegate(object _, FileSystemEventArgs e)
			{
				if (Matches(e.Name))
				{
					Schedule();
				}
			};
			watcher.Created += delegate(object _, FileSystemEventArgs e)
			{
				if (Matches(e.Name))
				{
					Schedule();
				}
			};
			watcher.Deleted += delegate(object _, FileSystemEventArgs e)
			{
				if (Matches(e.Name))
				{
					Schedule();
				}
			};
			watcher.Renamed += delegate(object _, RenamedEventArgs e)
			{
				if (Matches(e.Name) || Matches(e.OldName))
				{
					Schedule();
				}
			};
			watcher.EnableRaisingEvents = true;
		}
		catch (Exception ex)
		{
			Log.Warn("markdown", "watch " + name + ": " + ex.Message);
		}
		bool Matches(string? changed)
		{
			return string.Equals(changed, name, StringComparison.OrdinalIgnoreCase);
		}
		void Schedule()
		{
			dq.TryEnqueue(delegate
			{
				markdownViewerFileWatcher.debounce.Stop();
				markdownViewerFileWatcher.debounce.Start();
			});
		}
	}

	public void Dispose()
	{
		watcher?.Dispose();
		debounce?.Stop();
	}
}
