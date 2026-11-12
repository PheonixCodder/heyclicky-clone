using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace HeyClicky.App.Demo;

public static class Snapshot
{
	private struct WinRect
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct BitmapInfoHeader
	{
		public uint biSize;

		public int biWidth;

		public int biHeight;

		public ushort biPlanes;

		public ushort biBitCount;

		public uint biCompression;

		public uint biSizeImage;

		public int biXPelsPerMeter;

		public int biYPelsPerMeter;

		public uint biClrUsed;

		public uint biClrImportant;
	}

	public static string? Directory => Environment.GetEnvironmentVariable("HEYCLICKY_SNAPSHOT_DIR");

	public static async Task SaveAsync(UIElement root, string name)
	{
		string dir = Directory;
		if (string.IsNullOrEmpty(dir))
		{
			return;
		}
		try
		{
			System.IO.Directory.CreateDirectory(dir);
			RenderTargetBitmap rtb = new RenderTargetBitmap();
			await rtb.RenderAsync(root);
			IBuffer pixels = await rtb.GetPixelsAsync();
			string path = Path.Combine(dir, name + ".png");
			using IRandomAccessStream stream = await (await StorageFile.GetFileFromPathAsync(await EnsureFile(path))).OpenAsync(FileAccessMode.ReadWrite);
			BitmapEncoder obj = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
			double num = root.XamlRoot?.RasterizationScale ?? 1.0;
			obj.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)rtb.PixelWidth, (uint)rtb.PixelHeight, 96.0 * num, 96.0 * num, pixels.ToArray());
			await obj.FlushAsync();
			Log.Info("snapshot", $"saved {path} {rtb.PixelWidth}x{rtb.PixelHeight}");
		}
		catch (Exception ex)
		{
			Log.Error("snapshot", name, ex);
		}
	}

	[DllImport("user32.dll")]
	private static extern bool PrintWindow(nint hwnd, nint dc, uint flags);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, out WinRect r);

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint hwnd);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint hwnd, nint dc);

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleBitmap(nint dc, int w, int h);

	[DllImport("gdi32.dll")]
	private static extern nint SelectObject(nint dc, nint obj);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint obj);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern int GetDIBits(nint dc, nint bmp, uint start, uint lines, byte[] bits, ref BitmapInfoHeader info, uint usage);

	public static async Task SaveWindowAsync(nint hwnd, string name)
	{
		string directory = Directory;
		if (string.IsNullOrEmpty(directory) || hwnd == 0)
		{
			return;
		}
		try
		{
			System.IO.Directory.CreateDirectory(directory);
			GetWindowRect(hwnd, out var r);
			int w = r.Right - r.Left;
			int h = r.Bottom - r.Top;
			if (w > 0 && h > 0)
			{
				byte[] pixels = new byte[w * h * 4];
				nint dC = GetDC(0);
				nint dc = CreateCompatibleDC(dC);
				nint num = CreateCompatibleBitmap(dC, w, h);
				try
				{
					nint obj = SelectObject(dc, num);
					PrintWindow(hwnd, dc, 2u);
					SelectObject(dc, obj);
					BitmapInfoHeader bitmapInfoHeader = default(BitmapInfoHeader);
					bitmapInfoHeader.biSize = (uint)Marshal.SizeOf<BitmapInfoHeader>();
					bitmapInfoHeader.biWidth = w;
					bitmapInfoHeader.biHeight = -h;
					bitmapInfoHeader.biPlanes = 1;
					bitmapInfoHeader.biBitCount = 32;
					BitmapInfoHeader info = bitmapInfoHeader;
					GetDIBits(dc, num, 0u, (uint)h, pixels, ref info, 0u);
				}
				finally
				{
					DeleteObject(num);
					DeleteDC(dc);
					ReleaseDC(0, dC);
				}
				for (int i = 3; i < pixels.Length; i += 4)
				{
					pixels[i] = byte.MaxValue;
				}
				string path = Path.Combine(directory, name + ".png");
				using IRandomAccessStream stream = await (await StorageFile.GetFileFromPathAsync(await EnsureFile(path))).OpenAsync(FileAccessMode.ReadWrite);
				BitmapEncoder obj2 = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
				obj2.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)w, (uint)h, 96.0, 96.0, pixels);
				await obj2.FlushAsync();
				Log.Info("snapshot", $"saved {path} {w}x{h} (window)");
			}
		}
		catch (Exception ex)
		{
			Log.Error("snapshot", name, ex);
		}
	}

	private static async Task<string> EnsureFile(string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		await File.WriteAllBytesAsync(path, Array.Empty<byte>());
		return path;
	}
}
