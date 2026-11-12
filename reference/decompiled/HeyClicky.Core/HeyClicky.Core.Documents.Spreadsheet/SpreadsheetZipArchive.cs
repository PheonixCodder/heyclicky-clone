using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace HeyClicky.Core.Documents.Spreadsheet;

public sealed class SpreadsheetZipArchive
{
	private readonly record struct Entry(ushort CompressionMethod, int CompressedSize, int UncompressedSize, int LocalHeaderOffset);

	public const int MaximumArchiveBytes = 83886080;

	private const int MaximumEntryBytes = 419430400;

	private readonly byte[] bytes;

	private readonly Dictionary<string, Entry> entriesByName;

	public IEnumerable<string> EntryNames => entriesByName.Keys;

	public SpreadsheetZipArchive(string path)
	{
		using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
		{
			if (fileStream.Length > 83886080)
			{
				throw new SpreadsheetReadException(SpreadsheetReadErrorKind.TooLarge);
			}
			bytes = new byte[fileStream.Length];
			fileStream.ReadExactly(bytes);
		}
		entriesByName = ReadCentralDirectory(bytes);
	}

	public SpreadsheetZipArchive(byte[] data)
	{
		if (data.Length > 83886080)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.TooLarge);
		}
		bytes = data;
		entriesByName = ReadCentralDirectory(bytes);
	}

	public byte[]? Contents(string entryName)
	{
		string text;
		if (!entryName.StartsWith('/'))
		{
			text = entryName;
		}
		else
		{
			text = entryName.Substring(1, entryName.Length - 1);
		}
		string key = text;
		if (!entriesByName.TryGetValue(key, out var value))
		{
			return null;
		}
		int localHeaderOffset = value.LocalHeaderOffset;
		if (localHeaderOffset < 0 || localHeaderOffset + 30 > bytes.Length || ReadUInt32(bytes, localHeaderOffset) != 67324752)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.UnsupportedEntry, entryName);
		}
		int num = ReadUInt16(bytes, localHeaderOffset + 26);
		int num2 = ReadUInt16(bytes, localHeaderOffset + 28);
		long num3 = (long)localHeaderOffset + 30L + num + num2;
		if (num3 + value.CompressedSize > bytes.Length || value.CompressedSize < 0)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.UnsupportedEntry, entryName);
		}
		return value.CompressionMethod switch
		{
			0 => bytes.AsSpan((int)num3, value.CompressedSize).ToArray(), 
			8 => Inflate(bytes, (int)num3, value.CompressedSize, value.UncompressedSize), 
			_ => throw new SpreadsheetReadException(SpreadsheetReadErrorKind.UnsupportedEntry, entryName), 
		};
	}

	private static Dictionary<string, Entry> ReadCentralDirectory(byte[] bytes)
	{
		if (bytes.Length < 22)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.NotAZipArchive);
		}
		int num = Math.Max(0, bytes.Length - 65557);
		int? num2 = null;
		for (int num3 = bytes.Length - 22; num3 >= num; num3--)
		{
			if (ReadUInt32(bytes, num3) == 101010256)
			{
				num2 = num3;
				break;
			}
		}
		if (num2.HasValue)
		{
			int valueOrDefault = num2.GetValueOrDefault();
			int num4 = ReadUInt16(bytes, valueOrDefault + 10);
			long num5 = ReadUInt32(bytes, valueOrDefault + 16);
			Dictionary<string, Entry> dictionary = new Dictionary<string, Entry>(num4);
			for (int i = 0; i < num4; i++)
			{
				if (num5 + 46 > bytes.Length || ReadUInt32(bytes, (int)num5) != 33639248)
				{
					throw new SpreadsheetReadException(SpreadsheetReadErrorKind.NotAZipArchive);
				}
				int num6 = (int)num5;
				ushort compressionMethod = ReadUInt16(bytes, num6 + 10);
				uint val = ReadUInt32(bytes, num6 + 20);
				uint val2 = ReadUInt32(bytes, num6 + 24);
				int num7 = ReadUInt16(bytes, num6 + 28);
				int num8 = ReadUInt16(bytes, num6 + 30);
				int num9 = ReadUInt16(bytes, num6 + 32);
				uint val3 = ReadUInt32(bytes, num6 + 42);
				int num10 = num6 + 46;
				if (num10 + num7 > bytes.Length)
				{
					throw new SpreadsheetReadException(SpreadsheetReadErrorKind.NotAZipArchive);
				}
				Span<byte> span = bytes.AsSpan(num10, num7);
				string @string;
				try
				{
					@string = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(span);
				}
				catch (DecoderFallbackException)
				{
					@string = Encoding.Latin1.GetString(span);
				}
				dictionary[@string] = new Entry(compressionMethod, (int)Math.Min(val, 2147483647u), (int)Math.Min(val2, 2147483647u), (int)Math.Min(val3, 2147483647u));
				num5 = num10 + num7 + num8 + num9;
			}
			return dictionary;
		}
		throw new SpreadsheetReadException(SpreadsheetReadErrorKind.NotAZipArchive);
	}

	private static byte[] Inflate(byte[] bytes, int start, int count, int expectedSize)
	{
		if (expectedSize <= 0)
		{
			return Array.Empty<byte>();
		}
		if (expectedSize > 419430400)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.TooLarge);
		}
		byte[] array = new byte[expectedSize];
		int i = 0;
		try
		{
			using DeflateStream deflateStream = new DeflateStream(new MemoryStream(bytes, start, count, writable: false), CompressionMode.Decompress);
			int num;
			for (; i < expectedSize; i += num)
			{
				num = deflateStream.Read(array, i, expectedSize - i);
				if (num == 0)
				{
					break;
				}
			}
		}
		catch (InvalidDataException)
		{
			i = 0;
		}
		if (i <= 0)
		{
			throw new SpreadsheetReadException(SpreadsheetReadErrorKind.Unreadable);
		}
		if (i >= expectedSize)
		{
			return array;
		}
		return array[..i];
	}

	private static ushort ReadUInt16(byte[] bytes, int offset)
	{
		return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
	}

	private static uint ReadUInt32(byte[] bytes, int offset)
	{
		return (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));
	}
}
