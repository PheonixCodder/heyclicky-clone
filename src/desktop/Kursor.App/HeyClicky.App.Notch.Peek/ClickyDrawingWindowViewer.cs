using System;
using System.Numerics;
using HeyClicky.App.Home;
using HeyClicky.App.Shell;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch.Peek;

public sealed class ClickyDrawingWindowViewer
{
	private OverlayWindow? window;

	private ClickyDrawingWindowView? view;

	private ClickyLessonInlinePlayer? player;

	private int presentation;

	private volatile bool isOpen;

	public static ClickyDrawingWindowViewer Shared { get; } = new ClickyDrawingWindowViewer();

	public bool IsOpen => isOpen;

	public bool IsRecording { get; set; }

	public async void Present(StoredLesson stored, LessonService service)
	{
		try
		{
			Displays.Monitor monitor = Displays.UnderCursor();
			Dismiss(animated: false);
			int current = ++presentation;
			ClickyLessonInlinePlayer newPlayer = new ClickyLessonInlinePlayer(stored, service);
			double scale = monitor.Scale;
			Vector2 screenSize = new Vector2((float)((double)monitor.Bounds.Width / scale), (float)((double)monitor.Bounds.Height / scale));
			Vector2 stageSize = ClickyDrawingWindowView.StageSize(screenSize, stored);
			if (!(await newPlayer.LoadAsync((int)Math.Round((double)stageSize.X * scale))) || current != presentation)
			{
				newPlayer.Dispose();
				if (current == presentation)
				{
					Log.Warn("lessons", "drawing window: the playback engine didn't load; replaying full screen");
					service.PlayAsync(stored);
				}
				return;
			}
			if ((object)window == null)
			{
				window = new OverlayWindow("Kursor drawing", service.ExcludesFromCapture, clickThroughEverywhere: false, activatable: true);
				window.Activated += delegate(object _, WindowActivatedEventArgs e)
				{
					if (e.WindowActivationState != WindowActivationState.Deactivated)
					{
						view?.TakeKeyboardFocus();
					}
				};
			}
			player = newPlayer;
			view = new ClickyDrawingWindowView(newPlayer, screenSize, stageSize, scale, delegate
			{
				Dismiss(animated: true);
			}, delegate
			{
				Dismiss(animated: false);
				service.PlayAsync(stored);
			});
			window.Root.Children.Clear();
			window.Root.Children.Add(view);
			window.PlacePx(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height);
			window.ShowActivated();
			isOpen = true;
			view.TakeKeyboardFocus();
			view.Appear();
			newPlayer.Play();
			NotchPeekSurface.PeekAction("drawing_window_opened");
			Log.Info("lessons", $"drawing window: \"{stored.Data.Title}\" ({stored.Data.Lines.Count} lines, stage {stageSize.X:0}x{stageSize.Y:0} pt)");
		}
		catch (Exception ex)
		{
			Log.Error("lessons", "drawing window present failed", ex);
			try
			{
				Dismiss(animated: false);
			}
			catch
			{
			}
		}
	}

	public void Dismiss(bool animated)
	{
		presentation++;
		OverlayWindow shownWindow = window;
		if ((object)shownWindow == null)
		{
			return;
		}
		ClickyDrawingWindowView shownView = view;
		if ((object)shownView == null)
		{
			return;
		}
		isOpen = false;
		player?.Dispose();
		player = null;
		IsRecording = false;
		view = null;
		shownView.Closing();
		if (!animated)
		{
			Hide(shownWindow, shownView);
			return;
		}
		shownView.Disappear();
		DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.GetForCurrentThread().CreateTimer().Rooted();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(0.22);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += delegate
		{
			if ((object)view == null)
			{
				Hide(shownWindow, shownView);
			}
		};
		dispatcherQueueTimer.Start();
	}

	private static void Hide(OverlayWindow shownWindow, ClickyDrawingWindowView shownView)
	{
		shownWindow.HideWindow();
		shownWindow.Root.Children.Remove(shownView);
	}
}
