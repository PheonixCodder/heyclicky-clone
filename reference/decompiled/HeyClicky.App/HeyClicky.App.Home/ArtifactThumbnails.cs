using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;

namespace HeyClicky.App.Home;

public static class ArtifactThumbnails
{
	private sealed record Entry(string Revision, int PixelWidth, ImageSource Image);

	private const int MaxEntries = 128;

	private static readonly Dictionary<string, Entry> Cache = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

	private static readonly LinkedList<string> Recency = new LinkedList<string>();

	private static readonly Dictionary<string, Task<ImageSource?>> InFlight = new Dictionary<string, Task<ImageSource>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Guid> LatestRefresh = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

	private static readonly SemaphoreSlim DecodeSlots = new SemaphoreSlim(2, 2);

	private static readonly HashSet<string> RasterExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".tiff", ".tif", ".heic", ".webp", ".bmp" };

	public static ImageSource? Cached(string path)
	{
		if (!Cache.TryGetValue(path, out Entry value))
		{
			return null;
		}
		return value.Image;
	}

	public static async Task<ImageSource?> GetAsync(string path, int pixelWidth)
	{
		Guid refreshId = Guid.NewGuid();
		LatestRefresh[path] = refreshId;
		try
		{
			string revision = await Task.Run(() => Revision(path));
			if (revision == null)
			{
				return Cached(path);
			}
			if (Cache.TryGetValue(path, out Entry value) && value.Revision == revision && value.PixelWidth >= pixelWidth)
			{
				Touch(path);
				return value.Image;
			}
			string key = $"{path}|{revision}|{pixelWidth}";
			Task<ImageSource> value2;
			bool owns = !InFlight.TryGetValue(key, out value2);
			if (owns)
			{
				value2 = (InFlight[key] = GenerateAsync(path, pixelWidth));
			}
			ImageSource imageSource;
			try
			{
				imageSource = await value2;
			}
			finally
			{
				if (owns)
				{
					InFlight.Remove(key);
				}
			}
			if ((object)imageSource != null && LatestRefresh.GetValueOrDefault(path) == refreshId)
			{
				Store(path, new Entry(revision, pixelWidth, imageSource));
			}
			return imageSource ?? Cached(path);
		}
		finally
		{
			if (LatestRefresh.GetValueOrDefault(path) == refreshId)
			{
				LatestRefresh.Remove(path);
			}
		}
	}

	private static string? Revision(string path)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			return fileInfo.Exists ? $"{fileInfo.LastWriteTimeUtc.Ticks}|{fileInfo.Length}" : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static void Store(string path, Entry entry)
	{
		Cache[path] = entry;
		Touch(path);
		while (Cache.Count > 128)
		{
			LinkedListNode<string> last = Recency.Last;
			if (last != null)
			{
				Recency.RemoveLast();
				Cache.Remove(last.Value);
				continue;
			}
			break;
		}
	}

	private static void Touch(string path)
	{
		Recency.Remove(path);
		Recency.AddFirst(path);
	}

	private static async Task<ImageSource?> GenerateAsync(string path, int pixelWidth)
	{
		bool flag = WebPageSnapshotter.IsWebPageFile(path);
		ImageSource imageSource = default(ImageSource);
		if (flag)
		{
			imageSource = await WebPageSnapshotter.SnapshotAsync(path);
			flag = (object)imageSource != null;
		}
		if (flag)
		{
			return imageSource;
		}
		await DecodeSlots.WaitAsync();
		try
		{
			StorageFile storageFile = await StorageFile.GetFileFromPathAsync(path);
			BitmapImage bitmap = new BitmapImage();
			if (RasterExtensions.Contains(Path.GetExtension(path)))
			{
				bitmap.DecodePixelWidth = pixelWidth;
				using IRandomAccessStreamWithContentType stream = await storageFile.OpenReadAsync();
				await bitmap.SetSourceAsync(stream);
				return bitmap;
			}
			if (path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
			{
				PdfDocument pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);
				if (pdfDocument.PageCount == 0)
				{
					return null;
				}
				using PdfPage page = pdfDocument.GetPage(0u);
				using InMemoryRandomAccessStream rendered = new InMemoryRandomAccessStream();
				await page.RenderToStreamAsync(rendered, new PdfPageRenderOptions
				{
					DestinationWidth = (uint)pixelWidth
				});
				rendered.Seek(0uL);
				await bitmap.SetSourceAsync(rendered);
				return bitmap;
			}
			using StorageItemThumbnail thumbnail = await storageFile.GetThumbnailAsync(ThumbnailMode.DocumentsView, (uint)pixelWidth, ThumbnailOptions.ResizeThumbnail);
			if ((object)thumbnail == null || thumbnail.Type != 0)
			{
				return null;
			}
			await bitmap.SetSourceAsync(thumbnail);
			return bitmap;
		}
		catch (Exception ex)
		{
			Log.Info("home", "no thumbnail for " + Path.GetFileName(path) + ": " + ex.Message);
			return null;
		}
		finally
		{
			DecodeSlots.Release();
		}
	}
}
