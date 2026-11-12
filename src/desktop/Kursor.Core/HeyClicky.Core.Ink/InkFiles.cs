using System;
using System.IO;
using System.Text;

namespace HeyClicky.Core.Ink;

public static class InkFiles
{
	private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public static string? ReadUtf8(string path)
	{
		try
		{
			return File.ReadAllText(path, StrictUtf8);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static void WriteUtf8Atomically(string path, string text)
	{
		string text2 = Path.Combine(Path.GetDirectoryName(path) ?? ".", "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
		File.WriteAllText(text2, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		File.Move(text2, path, overwrite: true);
	}

	public static void WriteBytesAtomically(string path, byte[] bytes)
	{
		string directoryName = Path.GetDirectoryName(path);
		if (directoryName != null)
		{
			Directory.CreateDirectory(directoryName);
		}
		string text = Path.Combine(directoryName ?? ".", "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
		File.WriteAllBytes(text, bytes);
		File.Move(text, path, overwrite: true);
	}

	public static (DateTimeOffset ModifiedAt, long Size)? Stat(string path)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			if (!fileInfo.Exists)
			{
				return null;
			}
			return (new DateTimeOffset(fileInfo.LastWriteTimeUtc, TimeSpan.Zero), fileInfo.Length);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static double SecondsSince1970(DateTimeOffset date)
	{
		return (date - DateTimeOffset.UnixEpoch).TotalSeconds;
	}

	public static DateTimeOffset FromSecondsSince1970(double seconds)
	{
		return DateTimeOffset.UnixEpoch.AddTicks((long)Math.Round(seconds * 10000000.0));
	}
}
