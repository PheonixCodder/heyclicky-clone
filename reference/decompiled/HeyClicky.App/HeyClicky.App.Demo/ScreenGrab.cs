using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.App.Demo;

internal static class ScreenGrab
{
	private struct BITMAPINFOHEADER
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
	private static extern bool BitBlt(nint dst, int x, int y, int w, int h, nint src, int sx, int sy, uint rop);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint obj);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint dc);

	[DllImport("gdi32.dll")]
	private static extern int GetDIBits(nint dc, nint bmp, uint start, uint lines, byte[] bits, ref BITMAPINFOHEADER info, uint usage);

	public static async Task SaveAsync(string path, int x, int y, int w, int h)
	{
		if (w > 0 && h > 0)
		{
			await SavePixelsAsync(path, Grab(x, y, w, h), w, h);
		}
	}

	public static byte[] Grab(int x, int y, int w, int h)
	{
		byte[] array = new byte[w * h * 4];
		nint dC = GetDC(0);
		nint num = CreateCompatibleDC(dC);
		nint num2 = CreateCompatibleBitmap(dC, w, h);
		try
		{
			nint obj = SelectObject(num, num2);
			BitBlt(num, 0, 0, w, h, dC, x, y, 1087111200u);
			SelectObject(num, obj);
			BITMAPINFOHEADER bITMAPINFOHEADER = default(BITMAPINFOHEADER);
			bITMAPINFOHEADER.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
			bITMAPINFOHEADER.biWidth = w;
			bITMAPINFOHEADER.biHeight = -h;
			bITMAPINFOHEADER.biPlanes = 1;
			bITMAPINFOHEADER.biBitCount = 32;
			BITMAPINFOHEADER info = bITMAPINFOHEADER;
			GetDIBits(num, num2, 0u, (uint)h, array, ref info, 0u);
		}
		finally
		{
			DeleteObject(num2);
			DeleteDC(num);
			ReleaseDC(0, dC);
		}
		for (int i = 3; i < array.Length; i += 4)
		{
			array[i] = byte.MaxValue;
		}
		return array;
	}

	public static async Task SavePixelsAsync(string path, byte[] pixels, int w, int h)
	{
		using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
		BitmapEncoder obj = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
		obj.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)w, (uint)h, 96.0, 96.0, pixels);
		await obj.FlushAsync();
		byte[] bytes = new byte[stream.Size];
		stream.Seek(0uL);
		using (DataReader reader = new DataReader(stream.GetInputStreamAt(0uL)))
		{
			await reader.LoadAsync((uint)stream.Size);
			reader.ReadBytes(bytes);
		}
		await File.WriteAllBytesAsync(path, bytes);
	}
}
