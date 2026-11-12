using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.App.Ink;

public static class ClickyInkImages
{
	public const int MaximumCachedImages = 48;

	private const int MaximumPixelSide = 1200;

	private static readonly HttpClient Http = CreateHttp();

	private static readonly Dictionary<string, ClickyInkPicture> Cache = new Dictionary<string, ClickyInkPicture>();

	private static readonly Dictionary<string, Task<ClickyInkPicture?>> Loads = new Dictionary<string, Task<ClickyInkPicture>>();

	private static readonly HashSet<string> Failed = new HashSet<string>();

	public static Func<string, byte[]?> Loader { get; set; } = delegate(string source)
	{
		if (!Path.IsPathRooted(source) && !source.StartsWith('~'))
		{
			return (byte[]?)null;
		}
		string text;
		if (!source.StartsWith('~'))
		{
			text = source;
		}
		else
		{
			text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), source.Substring(1, source.Length - 1).TrimStart('/', '\\'));
		}
		string path = text;
		try
		{
			return File.Exists(path) ? File.ReadAllBytes(path) : null;
		}
		catch (Exception)
		{
			return (byte[]?)null;
		}
	};

	public static Func<string, CancellationToken, Task<byte[]?>>? PhotoSearch { get; set; }

	public static string WorkerBaseUrl { get; set; } = "https://api.heyclicky.com";

	private static HttpClient CreateHttp()
	{
		HttpClient httpClient = new HttpClient();
		httpClient.Timeout = TimeSpan.FromSeconds(20L);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0) HeyClicky");
		return httpClient;
	}

	public static ClickyInkPicture? Image(string? source)
	{
		if (string.IsNullOrEmpty(source))
		{
			return null;
		}
		lock (Cache)
		{
			ClickyInkPicture value;
			return Cache.TryGetValue(source, out value) ? value : null;
		}
	}

	public static void Remember(ClickyInkPicture picture, string source)
	{
		lock (Cache)
		{
			if (Cache.Count >= 48)
			{
				Cache.Clear();
			}
			Cache[source] = picture;
		}
	}

	public static Task<ClickyInkPicture?> LoadAsync(string? source)
	{
		if (string.IsNullOrEmpty(source))
		{
			return Task.FromResult<ClickyInkPicture>(null);
		}
		ClickyInkPicture clickyInkPicture = Image(source);
		if ((object)clickyInkPicture != null)
		{
			return Task.FromResult(clickyInkPicture);
		}
		if (Failed.Contains(source))
		{
			return Task.FromResult<ClickyInkPicture>(null);
		}
		if (Loads.TryGetValue(source, out Task<ClickyInkPicture> value))
		{
			return value;
		}
		DispatcherQueue dispatcher = DispatcherQueue.GetForCurrentThread();
		Task<ClickyInkPicture> task = LoadUncachedAsync(source, dispatcher);
		Loads[source] = task;
		task.ContinueWith(delegate(Task<ClickyInkPicture?> t)
		{
			dispatcher.TryEnqueue(delegate
			{
				Loads.Remove(source);
				if (!t.IsCompletedSuccessfully || (object)t.Result == null)
				{
					Failed.Add(source);
				}
			});
		}, TaskScheduler.Default);
		return task;
	}

	private static async Task<ClickyInkPicture?> LoadUncachedAsync(string source, DispatcherQueue dispatcher)
	{
		byte[] bytes = await Task.Run(async delegate
		{
			_ = 1;
			try
			{
				byte[] array = Loader(source);
				if (array != null)
				{
					return array;
				}
				if (source.StartsWith("http", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(source, UriKind.Absolute, out Uri result))
				{
					return await Http.GetByteArrayAsync(result).ConfigureAwait(continueOnCapturedContext: false);
				}
				Func<string, CancellationToken, Task<byte[]>> photoSearch = PhotoSearch;
				return (photoSearch == null) ? null : (await photoSearch(source, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false));
			}
			catch (Exception ex)
			{
				Log.Warn("ink", "picture " + source + ": " + ex.Message);
				return (byte[])null;
			}
		});
		if (bytes == null || bytes.Length == 0)
		{
			return null;
		}
		(byte[], byte[], int, int)? tuple = await Task.Run(() => Decode(bytes));
		if (!tuple.HasValue)
		{
			return null;
		}
		(byte[], byte[], int, int) value = tuple.Value;
		byte[] pixels = value.Item1;
		byte[] gray = value.Item2;
		int width = value.Item3;
		int height = value.Item4;
		TaskCompletionSource<ClickyInkPicture?> done = new TaskCompletionSource<ClickyInkPicture>();
		if (!dispatcher.TryEnqueue(delegate
		{
			try
			{
				ClickyInkPicture clickyInkPicture = new ClickyInkPicture(Bitmap(pixels, width, height), Bitmap(gray, width, height), width, height);
				Remember(clickyInkPicture, source);
				done.SetResult(clickyInkPicture);
			}
			catch (Exception exception)
			{
				done.SetException(exception);
			}
		}))
		{
			return null;
		}
		return await done.Task;
	}

	private static WriteableBitmap Bitmap(byte[] pixels, int width, int height)
	{
		WriteableBitmap writeableBitmap = new WriteableBitmap(width, height);
		using (Stream stream = writeableBitmap.PixelBuffer.AsStream())
		{
			stream.Write(pixels, 0, pixels.Length);
		}
		writeableBitmap.Invalidate();
		return writeableBitmap;
	}

	private static (byte[] Pixels, byte[] Gray, int Width, int Height)? Decode(byte[] bytes)
	{
		try
		{
			CanvasDevice sharedDevice = CanvasDevice.GetSharedDevice();
			using IRandomAccessStream stream = new MemoryStream(bytes).AsRandomAccessStream();
			using CanvasBitmap canvasBitmap = CanvasBitmap.LoadAsync(sharedDevice, stream, 96f).AsTask().GetAwaiter()
				.GetResult();
			BitmapSize sizeInPixels = canvasBitmap.SizeInPixels;
			double num = Math.Min(1.0, 1200.0 / (double)Math.Max(sizeInPixels.Width, sizeInPixels.Height));
			int num2 = Math.Max(1, (int)Math.Round((double)sizeInPixels.Width * num));
			int num3 = Math.Max(1, (int)Math.Round((double)sizeInPixels.Height * num));
			using CanvasRenderTarget canvasRenderTarget = new CanvasRenderTarget(sharedDevice, num2, num3, 96f);
			using (CanvasDrawingSession canvasDrawingSession = canvasRenderTarget.CreateDrawingSession())
			{
				canvasDrawingSession.Clear(Colors.Transparent);
				canvasDrawingSession.DrawImage(canvasBitmap, new Rect(0f, 0f, num2, num3), new Rect(0f, 0f, sizeInPixels.Width, sizeInPixels.Height), 1f, CanvasImageInterpolation.HighQualityCubic);
			}
			byte[] pixelBytes = canvasRenderTarget.GetPixelBytes();
			byte[] array = new byte[pixelBytes.Length];
			for (int i = 0; i + 3 < pixelBytes.Length; i += 4)
			{
				byte b = (byte)Math.Round(0.0722 * (double)(int)pixelBytes[i] + 0.7152 * (double)(int)pixelBytes[i + 1] + 0.2126 * (double)(int)pixelBytes[i + 2]);
				array[i] = (array[i + 1] = (array[i + 2] = b));
				array[i + 3] = pixelBytes[i + 3];
			}
			return (pixelBytes, array, num2, num3);
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "picture decode: " + ex.Message);
			return null;
		}
	}
}
