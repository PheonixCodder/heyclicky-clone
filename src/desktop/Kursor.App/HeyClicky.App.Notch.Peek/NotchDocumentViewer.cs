using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace HeyClicky.App.Notch.Peek;

public sealed class NotchDocumentViewer : Grid
{
	private readonly Border pane;

	private readonly Grid footer = new Grid();

	private readonly INotchHost host;

	private readonly DispatcherQueue dq;

	private FileSystemWatcher? watcher;

	private DispatcherQueueTimer? mountTimer;

	private DispatcherQueueTimer? reloadTimer;

	private string? shownPath;

	private int version;

	private readonly bool showsFollowUp;

	private readonly MessagesPalette look = MessagesPalette.Current;

	public NotchDocumentViewer(INotchHost host, bool showsFollowUp = true)
	{
		this.host = host;
		this.showsFollowUp = showsFollowUp;
		dq = DispatcherQueue.GetForCurrentThread();
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		base.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		pane = new Border
		{
			Background = Ui.Brush(look.SidebarFill),
			CornerRadius = new CornerRadius(12.0),
			BorderBrush = Ui.Brush(look.InkAt(0.08)),
			BorderThickness = new Thickness(1.0),
			Margin = new Thickness(4.0, 6.0, 4.0, 4.0)
		};
		base.Children.Add(pane);
		Grid.SetRow(footer, 1);
		base.Children.Add(footer);
		base.Unloaded += delegate
		{
			StopWatching();
		};
	}

	public void Show(string? path, string? threadId = null)
	{
		if (path == shownPath)
		{
			return;
		}
		shownPath = path;
		int v = ++version;
		pane.Child = null;
		StopWatching();
		ShowFollowUpFooter(path, threadId);
		if (path == null)
		{
			return;
		}
		mountTimer?.Stop();
		mountTimer = dq.CreateTimer().Rooted();
		mountTimer.Interval = NotchPeekLayout.DocumentMountDelay;
		mountTimer.IsRepeating = false;
		mountTimer.Tick += delegate
		{
			if (v == version)
			{
				Mount(path, v);
			}
		};
		mountTimer.Start();
	}

	private void Mount(string path, int v)
	{
		if (v != version)
		{
			return;
		}
		using (MessagesPalette.Use(look))
		{
			INotchDocumentRenderer notchDocumentRenderer = NotchExtensions.DocumentRenderers.FirstOrDefault((INotchDocumentRenderer r) => r.CanRender(path));
			INotchDocumentRenderer notchDocumentRenderer2 = notchDocumentRenderer ?? NotchDocumentRenderers.For(path);
			try
			{
				pane.Child = notchDocumentRenderer2.Render(path, delegate
				{
					dq.TryEnqueue(delegate
					{
						if (v == version)
						{
							Mount(path, v);
						}
					});
				});
			}
			catch (Exception ex)
			{
				Log.Warn("notch-doc", "render " + Path.GetFileName(path) + " failed: " + ex.Message);
				pane.Child = NotchDocumentRenderers.Message("This document couldn’t be loaded.");
			}
			Ui.ClipRounded(pane, 12.0);
			if (notchDocumentRenderer == null)
			{
				Watch(path, v);
			}
			Log.Info("notch-doc", $"showing {Path.GetFileName(path)} ({ChatBlockViews.Kind(path)})");
		}
	}

	private void ShowFollowUpFooter(string? path, string? threadId)
	{
		footer.Children.Clear();
		if (showsFollowUp && path != null)
		{
			ChatHandOff chatHandOff = ReplyEntry(path, threadId);
			if ((object)chatHandOff != null)
			{
				footer.Children.Add(new NotchChatComposer(host, chatHandOff.Id));
			}
		}
	}

	internal static ChatHandOff? ReplyEntry(string path, string? threadId)
	{
		ChatHandOffs handOffs = NotchChat.HandOffs;
		if (handOffs == null)
		{
			return null;
		}
		string documentPath;
		try
		{
			documentPath = Path.GetFullPath(path);
		}
		catch
		{
			return null;
		}
		List<ChatHandOff> source = handOffs.Entries.Where((ChatHandOff e) => e.Kind == ChatHandOffKind.CameBack && e.Outcome.GetValueOrDefault() == ChatHandOffOutcome.Finished).Reverse().ToList();
		ChatHandOff chatHandOff = source.FirstOrDefault(Made);
		if ((object)chatHandOff != null)
		{
			return chatHandOff;
		}
		if (threadId == null)
		{
			return null;
		}
		string jobId = handOffs.Directory?.JobForThread(threadId)?.Id;
		return source.FirstOrDefault((ChatHandOff e) => e.ThreadId == threadId || (jobId != null && e.JobId == jobId));
		bool Made(ChatHandOff e)
		{
			return (e.ArtifactEntries ?? Array.Empty<string>()).Any(delegate(string artifact)
			{
				string text = ArtifactPaths.Resolve(artifact, null);
				return !ArtifactPaths.IsWebLink(text) && string.Equals(text, documentPath, StringComparison.OrdinalIgnoreCase);
			});
		}
	}

	private void Watch(string path, int v)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(path);
			if (directoryName == null || !Directory.Exists(directoryName))
			{
				return;
			}
			watcher = new FileSystemWatcher(directoryName, Path.GetFileName(path))
			{
				NotifyFilter = (NotifyFilters.Size | NotifyFilters.LastWrite),
				EnableRaisingEvents = true
			};
			watcher.Changed += delegate
			{
				dq.TryEnqueue(delegate
				{
					if (v == version)
					{
						reloadTimer?.Stop();
						reloadTimer = dq.CreateTimer().Rooted();
						reloadTimer.Interval = TimeSpan.FromMilliseconds(300L, 0L);
						reloadTimer.IsRepeating = false;
						reloadTimer.Tick += delegate
						{
							if (v == version)
							{
								Mount(path, v);
							}
						};
						reloadTimer.Start();
					}
				});
			};
		}
		catch (Exception ex)
		{
			Log.Warn("notch-doc", "watch: " + ex.Message);
		}
	}

	private void StopWatching()
	{
		watcher?.Dispose();
		watcher = null;
	}
}
