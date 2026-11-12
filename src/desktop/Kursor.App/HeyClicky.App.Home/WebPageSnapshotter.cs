using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using Windows.Storage.Streams;

namespace HeyClicky.App.Home;

public static class WebPageSnapshotter
{
	private const double ViewportWidth = 1280.0;

	private const double ViewportHeight = 800.0;

	private const int SnapshotPixelWidth = 720;

	private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(0.6);

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8L);

	private static OverlayWindow? window;

	private static WebView2? view;

	private static Task<bool>? ready;

	private static readonly SemaphoreSlim Turn = new SemaphoreSlim(1, 1);

	private static TaskCompletionSource<bool>? navigation;

	public static bool IsWebPageFile(string path)
	{
		switch (Path.GetExtension(path).ToLowerInvariant())
		{
		case ".html":
		case ".htm":
		case ".xhtml":
			return true;
		default:
			return false;
		}
	}

	public static async Task<ImageSource?> SnapshotAsync(string path)
	{
		await Turn.WaitAsync();
		try
		{
			if (await (ready ?? (ready = CreateAsync())))
			{
				CoreWebView2 core = view?.CoreWebView2;
				if ((object)core != null)
				{
					navigation = new TaskCompletionSource<bool>();
					Task<bool> loading = navigation.Task;
					core.Navigate(new Uri(Path.GetFullPath(path)).AbsoluteUri);
					bool num = await Task.WhenAny(loading, Task.Delay(Timeout)) == loading && loading.Result;
					navigation = null;
					if (!num)
					{
						core.Stop();
						core.Navigate("about:blank");
						Log.Info("home", "page preview: " + Path.GetFileName(path) + " didn't load in time");
						return null;
					}
					await Task.Delay(SettleDelay);
					using InMemoryRandomAccessStream png = new InMemoryRandomAccessStream();
					await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, png);
					core.Navigate("about:blank");
					png.Seek(0uL);
					BitmapImage bitmap = new BitmapImage
					{
						DecodePixelWidth = 720
					};
					await bitmap.SetSourceAsync(png);
					return bitmap;
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			Log.Info("home", "page preview of " + Path.GetFileName(path) + " failed: " + ex.Message);
			return null;
		}
		finally
		{
			Turn.Release();
		}
	}

	private static async Task<bool> CreateAsync()
	{
		_ = 1;
		try
		{
			window = new OverlayWindow("HeyClicky page preview", excludeFromCapture: true, clickThroughEverywhere: false, activatable: false, topmost: false);
			view = new WebView2
			{
				Width = 1280.0,
				Height = 800.0,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			window.Root.Children.Add(view);
			double scale = Displays.Primary().Scale;
			int num = (int)Math.Ceiling(1280.0 * scale);
			int h = (int)Math.Ceiling(800.0 * scale);
			window.PlacePx(GetSystemMetrics(76) - num - 400, GetSystemMetrics(77), num, h);
			window.ShowNoActivate();
			CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions
			{
				AdditionalBrowserArguments = "--disable-features=CalculateNativeWinOcclusion --autoplay-policy=user-gesture-required"
			};
			CoreWebView2Environment coreWebView2Environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, AppPaths.Combine("WebView2", "PagePreviews"), options);
			CoreWebView2ControllerOptions coreWebView2ControllerOptions = coreWebView2Environment.CreateCoreWebView2ControllerOptions();
			coreWebView2ControllerOptions.IsInPrivateModeEnabled = true;
			await view.EnsureCoreWebView2Async(coreWebView2Environment, coreWebView2ControllerOptions);
			CoreWebView2 core = view.CoreWebView2;
			core.IsMuted = true;
			core.Settings.AreDefaultContextMenusEnabled = false;
			core.Settings.AreDevToolsEnabled = false;
			core.Settings.IsStatusBarEnabled = false;
			core.NavigationStarting += delegate(CoreWebView2 _, CoreWebView2NavigationStartingEventArgs e)
			{
				if (!e.Uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && !e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
				{
					e.Cancel = true;
				}
			};
			core.NewWindowRequested += delegate(CoreWebView2 _, CoreWebView2NewWindowRequestedEventArgs e)
			{
				e.Handled = true;
			};
			core.NavigationCompleted += delegate(CoreWebView2 _, CoreWebView2NavigationCompletedEventArgs e)
			{
				if (!core.Source.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
				{
					navigation?.TrySetResult(e.IsSuccess);
				}
			};
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("home", "page previews unavailable (WebView2): " + ex.Message);
			return false;
		}
	}

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int index);
}
