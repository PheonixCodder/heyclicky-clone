using System;
using System.IO;

namespace HeyClicky.Core.Auth;

public static class AtomicFile
{
	public static ReadOnlyMemory<byte> ReadJson(string path)
	{
		byte[] array = File.ReadAllBytes(path);
		return (array.Length >= 3 && array[0] == 239 && array[1] == 187 && array[2] == 191) ? array.AsMemory(3) : ((Memory<byte>)array);
	}

	public static void KeepUnreadable(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Copy(path, path + ".unreadable", overwrite: true);
			}
		}
		catch
		{
		}
	}

	public static void WriteAllBytes(string path, ReadOnlySpan<byte> bytes)
	{
		string? directoryName = Path.GetDirectoryName(path);
		Directory.CreateDirectory(directoryName);
		string text = Path.Combine(directoryName, $".{Path.GetFileName(path)}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp");
		using (FileStream fileStream = new FileStream(text, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
		{
			fileStream.Write(bytes);
		}
		if (File.Exists(path))
		{
			File.Replace(text, path, null, ignoreMetadataErrors: true);
		}
		else
		{
			File.Move(text, path);
		}
	}
}
