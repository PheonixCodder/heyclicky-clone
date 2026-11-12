using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Storage.Streams;

namespace HeyClicky.Drawing.Media;

internal static class PhotoInspection
{
	private const double FaceHeightFraction = 0.06;

	public static async Task<(int Width, int Height)?> PixelSizeAsync(byte[] bytes)
	{
		_ = 1;
		try
		{
			using IRandomAccessStream stream = new MemoryStream(bytes).AsRandomAccessStream();
			BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
			int width = (int)decoder.OrientedPixelWidth;
			int height = (int)decoder.OrientedPixelHeight;
			for (uint frame = 1u; frame < decoder.FrameCount; frame++)
			{
				BitmapFrame bitmapFrame = await decoder.GetFrameAsync(frame);
				if (bitmapFrame.PixelWidth > width)
				{
					width = (int)bitmapFrame.PixelWidth;
					height = (int)bitmapFrame.PixelHeight;
				}
			}
			return (width > 0 && height > 0) ? new(int, int)?((width, height)) : (((int, int)?)null);
		}
		catch
		{
			return null;
		}
	}

	public static async Task<bool> ShowsPeopleAsync(byte[] bytes)
	{
		_ = 3;
		try
		{
			if (!FaceDetector.IsSupported)
			{
				return false;
			}
			using IRandomAccessStream stream = new MemoryStream(bytes).AsRandomAccessStream();
			BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(stream);
			double num = Math.Min(1.0, 640.0 / (double)Math.Max(1u, Math.Max(bitmapDecoder.OrientedPixelWidth, bitmapDecoder.OrientedPixelHeight)));
			BitmapTransform transform = new BitmapTransform
			{
				ScaledWidth = (uint)Math.Max(1.0, Math.Round((double)bitmapDecoder.OrientedPixelWidth * num)),
				ScaledHeight = (uint)Math.Max(1.0, Math.Round((double)bitmapDecoder.OrientedPixelHeight * num)),
				InterpolationMode = BitmapInterpolationMode.Fant
			};
			using SoftwareBitmap bitmap = await bitmapDecoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.RespectExifOrientation, ColorManagementMode.DoNotColorManage);
			SoftwareBitmap grey = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Gray8);
			try
			{
				return (await (await FaceDetector.CreateAsync()).DetectFacesAsync(grey)).Any((DetectedFace face) => (double)face.FaceBox.Height > 0.06 * (double)grey.PixelHeight);
			}
			finally
			{
				if ((object)grey != null)
				{
					((IDisposable)grey).Dispose();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Drawing", "photo face check failed: " + ex.Message);
			return false;
		}
	}
}
