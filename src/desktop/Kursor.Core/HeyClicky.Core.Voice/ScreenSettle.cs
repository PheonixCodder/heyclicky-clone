using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.Core.Voice;

public static class ScreenSettle
{
	private const int SampleWidth = 192;

	private const int PixelDifferenceThreshold = 12;

	private const double SettleChangedFraction = 0.005;

	private const double MaximumWaitSeconds = 4.0;

	private const int SampleIntervalMilliseconds = 150;

	private const int RequiredCalmSamples = 2;

	private const double NoVisibleChangeGiveUpSeconds = 1.5;

	public static async Task<byte[]?> GraySampleAsync(byte[] jpeg)
	{
		_ = 2;
		try
		{
			using InMemoryRandomAccessStream input = new InMemoryRandomAccessStream();
			await input.WriteAsync(jpeg.AsBuffer());
			input.Seek(0uL);
			BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(input);
			int width = Math.Min(192, (int)bitmapDecoder.PixelWidth);
			int height = Math.Max(1, (int)Math.Round((double)bitmapDecoder.PixelHeight * (double)width / (double)Math.Max(1u, bitmapDecoder.PixelWidth)));
			BitmapTransform transform = new BitmapTransform
			{
				ScaledWidth = (uint)width,
				ScaledHeight = (uint)height,
				InterpolationMode = BitmapInterpolationMode.Linear
			};
			byte[] array = (await bitmapDecoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
			byte[] array2 = new byte[width * height];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = (byte)(array[i * 4] * 29 + array[i * 4 + 1] * 150 + array[i * 4 + 2] * 77 >> 8);
			}
			return array2;
		}
		catch (Exception ex)
		{
			Log.Warn("ScreenSettle", "screenshot not sampled: " + ex.Message);
			return null;
		}
	}

	public static double ChangedFraction(byte[] first, byte[] second)
	{
		if (first.Length != second.Length || first.Length == 0)
		{
			return 1.0;
		}
		int num = 0;
		for (int i = 0; i < first.Length; i++)
		{
			if (Math.Abs(first[i] - second[i]) > 12)
			{
				num++;
			}
		}
		return (double)num / (double)first.Length;
	}

	public static async Task<bool> LookTheSameAsync(byte[] firstJpeg, byte[] secondJpeg, double tolerance)
	{
		byte[] first = await GraySampleAsync(firstJpeg).ConfigureAwait(continueOnCapturedContext: false);
		byte[] array = await GraySampleAsync(secondJpeg).ConfigureAwait(continueOnCapturedContext: false);
		return first != null && array != null && ChangedFraction(first, array) <= tolerance;
	}

	public static async Task WaitForScreenToSettleAsync(Func<Task<byte[]?>> sample, TimeSpan fallback, CancellationToken ct)
	{
		long started = Environment.TickCount64;
		byte[] clickSample = await SampleAsync(sample).ConfigureAwait(continueOnCapturedContext: false);
		if (clickSample == null)
		{
			await Task.Delay(fallback, ct).ConfigureAwait(continueOnCapturedContext: false);
			return;
		}
		await Task.Delay(250, ct).ConfigureAwait(continueOnCapturedContext: false);
		byte[] previous = clickSample;
		bool changedSinceClick = false;
		int calm = 0;
		int samples = 0;
		while (Elapsed() < 4.0)
		{
			ct.ThrowIfCancellationRequested();
			byte[] array = await SampleAsync(sample).ConfigureAwait(continueOnCapturedContext: false);
			if (array == null)
			{
				double num = fallback.TotalSeconds - Elapsed();
				if (num > 0.0)
				{
					await Task.Delay(TimeSpan.FromSeconds(num), ct).ConfigureAwait(continueOnCapturedContext: false);
				}
				return;
			}
			samples++;
			if (ChangedFraction(clickSample, array) > 0.005)
			{
				changedSinceClick = true;
			}
			calm = ((ChangedFraction(previous, array) <= 0.005) ? (calm + 1) : 0);
			previous = array;
			if (calm >= 2 && (changedSinceClick || Elapsed() >= 1.5))
			{
				Log.Info("ScreenSettle", $"screen settled after {Elapsed() * 1000.0:0} ms ({samples} samples, changed={changedSinceClick})");
				return;
			}
			await Task.Delay(150, ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		Log.Info("ScreenSettle", $"screen still changing after {4.0:0} s: going on");
		double Elapsed()
		{
			return (double)(Environment.TickCount64 - started) / 1000.0;
		}
	}

	private static async Task<byte[]?> SampleAsync(Func<Task<byte[]?>> sample)
	{
		_ = 1;
		try
		{
			byte[] array = await sample().ConfigureAwait(continueOnCapturedContext: false);
			return (array == null) ? null : (await GraySampleAsync(array).ConfigureAwait(continueOnCapturedContext: false));
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			return null;
		}
	}
}
