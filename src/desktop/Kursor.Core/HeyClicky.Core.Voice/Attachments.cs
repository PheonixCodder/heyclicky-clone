using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.Core.Voice;

public static class Attachments
{
	public const string FilesOnlyWords = "Please review and act on the attached files.";

	private const int ImageLaneMaxFileCount = 2;

	private const long ImageLaneMaxTotalBytes = 5000000L;

	private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"avif", "bmp", "gif", "heic", "heif", "jpeg", "jpg", "png", "tif", "tiff",
		"webp"
	};

	public static IReadOnlyList<string> NormalizedPaths(IEnumerable<string>? paths)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> list = new List<string>();
		foreach (string item in paths ?? Array.Empty<string>())
		{
			string text = item?.Trim() ?? "";
			if (text.Length != 0 && Path.IsPathFullyQualified(text))
			{
				string fullPath;
				try
				{
					fullPath = Path.GetFullPath(text);
				}
				catch
				{
					continue;
				}
				if (hashSet.Add(fullPath))
				{
					list.Add(fullPath);
				}
			}
		}
		return list;
	}

	public static bool IsImage(string path)
	{
		return ImageExtensions.Contains(Path.GetExtension(path).TrimStart('.'));
	}

	public static bool RequiresAgent(IReadOnlyList<string> paths)
	{
		IReadOnlyList<string> readOnlyList = NormalizedPaths(paths);
		if (readOnlyList.Count == 0)
		{
			return true;
		}
		bool num = readOnlyList.All(IsImage);
		long num2 = ((IEnumerable<string>)readOnlyList).Sum((Func<string, long>)FileSize);
		if (num && readOnlyList.Count <= 2)
		{
			return num2 > 5000000;
		}
		return true;
	}

	public static string SummaryLine(IReadOnlyList<string> paths)
	{
		return string.Join(", ", NormalizedPaths(paths).Select(delegate(string path)
		{
			string text = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
			string value = (IsImage(path) ? "image" : ((text.Length == 0) ? "file" : text));
			return $"{Path.GetFileName(path)} ({value}, {FormatBytes(FileSize(path))})";
		}));
	}

	public static string WordsWithSummary(string words, IReadOnlyList<string> paths, bool imagesRideAlong)
	{
		IReadOnlyList<string> readOnlyList = NormalizedPaths(paths);
		if (readOnlyList.Count == 0)
		{
			return words;
		}
		string value = (imagesRideAlong ? "The images are attached for you to look at; for work with them (editing, saving, sending) start a job for it with sessions_spawn." : "You can't open these files yourself: for anything with them, start a job for it with sessions_spawn (it gets the files).");
		string value2 = (string.IsNullOrWhiteSpace(words) ? "[They attached files with no message." : "[They attached files.");
		return $"{words}\n\n{value2} Files: {SummaryLine(readOnlyList)}. {value}]";
	}

	public static async Task<IReadOnlyList<byte[]>> AttachedImagesAsync(IReadOnlyList<string> paths)
	{
		IReadOnlyList<string> readOnlyList = NormalizedPaths(paths);
		if (readOnlyList.Count == 0 || RequiresAgent(readOnlyList))
		{
			return Array.Empty<byte[]>();
		}
		List<byte[]> images = new List<byte[]>();
		foreach (string item in readOnlyList.Where(IsImage).Take(2))
		{
			byte[] array = await JpegAsync(item).ConfigureAwait(continueOnCapturedContext: false);
			if (array != null)
			{
				images.Add(array);
			}
		}
		return images;
	}

	public static async Task<byte[]?> JpegAsync(string path, double quality = 0.85, int maxDimension = 4096)
	{
		_ = 6;
		try
		{
			byte[] source = await File.ReadAllBytesAsync(path).ConfigureAwait(continueOnCapturedContext: false);
			using InMemoryRandomAccessStream input = new InMemoryRandomAccessStream();
			await input.WriteAsync(source.AsBuffer());
			input.Seek(0uL);
			BitmapDecoder obj = await BitmapDecoder.CreateAsync(input);
			uint pixelWidth = obj.PixelWidth;
			uint pixelHeight = obj.PixelHeight;
			double num = Math.Min(1.0, (double)maxDimension / (double)Math.Max(pixelWidth, pixelHeight));
			BitmapTransform transform = new BitmapTransform
			{
				ScaledWidth = Math.Max(1u, (uint)Math.Round((double)pixelWidth * num)),
				ScaledHeight = Math.Max(1u, (uint)Math.Round((double)pixelHeight * num)),
				InterpolationMode = BitmapInterpolationMode.Fant
			};
			byte[] data = (await obj.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb)).DetachPixelData();
			using InMemoryRandomAccessStream output = new InMemoryRandomAccessStream();
			BitmapPropertySet encodingOptions = new BitmapPropertySet { ["ImageQuality"] = new BitmapTypedValue(quality, PropertyType.Single) };
			BitmapEncoder bitmapEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output, encodingOptions);
			for (int i = 0; i < data.Length; i += 4)
			{
				byte b = data[i + 3];
				if (b != byte.MaxValue)
				{
					int num2 = 255 - b;
					data[i] = (byte)Math.Min(255, data[i] + num2);
					data[i + 1] = (byte)Math.Min(255, data[i + 1] + num2);
					data[i + 2] = (byte)Math.Min(255, data[i + 2] + num2);
					data[i + 3] = byte.MaxValue;
				}
			}
			bitmapEncoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform.ScaledWidth, transform.ScaledHeight, 96.0, 96.0, data);
			await bitmapEncoder.FlushAsync();
			byte[] result = new byte[output.Size];
			output.Seek(0uL);
			await output.ReadAsync(result.AsBuffer(), (uint)result.Length, InputStreamOptions.None);
			return result;
		}
		catch (Exception ex)
		{
			Log.Warn("Attachments", "image " + Path.GetFileName(path) + " not read: " + ex.Message);
			return null;
		}
	}

	public static long FileSize(string path)
	{
		try
		{
			return new FileInfo(path).Length;
		}
		catch
		{
			return 0L;
		}
	}

	public static string FormatBytes(long bytes)
	{
		if (bytes <= 0)
		{
			return "Zero KB";
		}
		if (bytes < 1000)
		{
			return $"{bytes} bytes";
		}
		string[] array = new string[4] { "KB", "MB", "GB", "TB" };
		double num = bytes;
		int num2 = -1;
		do
		{
			num /= 1000.0;
			num2++;
		}
		while (num >= 1000.0 && num2 < array.Length - 1);
		string text = ((num2 == 0 || num >= 100.0) ? "0" : ((num >= 10.0) ? "0.#" : "0.#"));
		return num.ToString(text, CultureInfo.InvariantCulture) + " " + array[num2];
	}
}
