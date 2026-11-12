using System;
using System.Threading;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Media.Playback;

namespace HeyClicky.App.UI;

public sealed class RoundedVideoView : Grid
{
	private readonly Image image = new Image
	{
		Stretch = Stretch.Fill,
		IsHitTestVisible = false
	};

	private readonly DispatcherQueue dq;

	private MediaPlayer? player;

	private CanvasImageSource? target;

	private CanvasRenderTarget? frame;

	private double scale = 1.0;

	private double targetScale;

	private int pending;

	private bool warned;

	public RoundedVideoView(double width, double height, double cornerRadius)
	{
		base.Width = width;
		base.Height = height;
		base.IsHitTestVisible = false;
		base.Background = Ui.Brush(Colors.Black);
		dq = DispatcherQueue.GetForCurrentThread();
		base.Children.Add(image);
		Ui.ClipRounded(this, cornerRadius);
	}

	public void Attach(MediaPlayer? mp)
	{
		MediaPlayer mediaPlayer = player;
		if ((object)mediaPlayer != null)
		{
			mediaPlayer.VideoFrameAvailable -= OnFrame;
			try
			{
				mediaPlayer.IsVideoFrameServerEnabled = false;
			}
			catch
			{
			}
		}
		player = mp;
		if ((object)mp != null)
		{
			mp.IsVideoFrameServerEnabled = true;
			mp.VideoFrameAvailable += OnFrame;
		}
	}

	private void OnFrame(MediaPlayer sender, object args)
	{
		if (Interlocked.Exchange(ref pending, 1) == 1)
		{
			return;
		}
		dq.TryEnqueue(delegate
		{
			Interlocked.Exchange(ref pending, 0);
			if (sender == player)
			{
				Draw(sender);
			}
		});
	}

	private void Draw(MediaPlayer mp)
	{
		try
		{
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			scale = base.XamlRoot?.RasterizationScale ?? scale;
			int num = Math.Max(1, (int)Math.Round(base.Width * scale));
			int num2 = Math.Max(1, (int)Math.Round(base.Height * scale));
			if ((object)target == null || Math.Abs(targetScale - scale) > 0.001)
			{
				target = new CanvasImageSource(sharedDevice, (float)base.Width, (float)base.Height, (float)(96.0 * scale));
				targetScale = scale;
				image.Source = target;
			}
			MediaPlaybackSession playbackSession = mp.PlaybackSession;
			double num3 = playbackSession.NaturalVideoWidth;
			double num4 = playbackSession.NaturalVideoHeight;
			double num5 = ((num3 > 0.0 && num4 > 0.0) ? Math.Max((double)num / num3, (double)num2 / num4) : 1.0);
			int num6 = ((num3 > 0.0) ? Math.Max(num, (int)Math.Ceiling(num3 * num5)) : num);
			int num7 = ((num4 > 0.0) ? Math.Max(num2, (int)Math.Ceiling(num4 * num5)) : num2);
			if ((object)frame == null || frame.SizeInPixels.Width != (uint)num6 || frame.SizeInPixels.Height != (uint)num7)
			{
				frame = new CanvasRenderTarget(sharedDevice, num6, num7, 96f);
			}
			mp.CopyFrameToVideoSurface(frame);
			using CanvasDrawingSession canvasDrawingSession = target.CreateDrawingSession(Colors.Black);
			canvasDrawingSession.DrawImage(frame, new Rect(0.0, 0.0, base.Width, base.Height), new Rect((double)(num6 - num) / 2.0, (double)(num7 - num2) / 2.0, num, num2));
		}
		catch (Exception ex) when (CanvasDevice.GetSharedDevice().IsDeviceLost(ex.HResult))
		{
			target = null;
			frame = null;
		}
		catch (Exception ex2)
		{
			if (!warned)
			{
				warned = true;
				Log.Warn("video", "frame: " + ex2.Message);
			}
		}
	}
}
