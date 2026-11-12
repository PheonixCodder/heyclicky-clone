using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace HeyClicky.Core.Brain;

public static class BrainRequestCompression
{
	public const string AcceptsGzipHeader = "x-clicky-accepts-gzip";

	public static readonly IReadOnlySet<string> GzipPaths = new HashSet<string> { "/brain/turn", "/brain/compact", "/brain/session-end", "/brain/prewarm" };

	public const int MinimumBytesToCompress = 8192;

	private static volatile bool workerAcceptsGzip;

	public static bool WorkerAcceptsGzip => workerAcceptsGzip;

	public static void NoteResponse(HttpResponseMessage? response, string path)
	{
		if (response != null && GzipPaths.Contains(path))
		{
			workerAcceptsGzip = response.Headers.TryGetValues("x-clicky-accepts-gzip", out IEnumerable<string> values) && values.FirstOrDefault() == "1";
		}
	}

	internal static void SetWorkerAcceptsGzipForTest(bool accepts)
	{
		workerAcceptsGzip = accepts;
	}

	public static bool ShouldCompress(string path, long length)
	{
		if (workerAcceptsGzip && GzipPaths.Contains(path))
		{
			return length >= 8192;
		}
		return false;
	}

	public static (HttpContent Content, int Bytes, bool IsGzip) Content(byte[] json, int length, string path)
	{
		if (ShouldCompress(path, length))
		{
			byte[] array = Gzip(json.AsSpan(0, length));
			if (array != null && array.Length < length)
			{
				ByteArrayContent byteArrayContent = new ByteArrayContent(array);
				byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				byteArrayContent.Headers.ContentEncoding.Add("gzip");
				return (Content: byteArrayContent, Bytes: array.Length, IsGzip: true);
			}
		}
		ByteArrayContent byteArrayContent2 = new ByteArrayContent(json, 0, length);
		byteArrayContent2.Headers.ContentType = new MediaTypeHeaderValue("application/json");
		return (Content: byteArrayContent2, Bytes: length, IsGzip: false);
	}

	public static byte[]? Gzip(ReadOnlySpan<byte> data)
	{
		if (data.IsEmpty)
		{
			return null;
		}
		try
		{
			using MemoryStream memoryStream = new MemoryStream(data.Length / 3 + 64);
			using (GZipStream gZipStream = new GZipStream(memoryStream, new ZLibCompressionOptions
			{
				CompressionLevel = 4
			}, leaveOpen: true))
			{
				gZipStream.Write(data);
			}
			return memoryStream.ToArray();
		}
		catch (Exception ex)
		{
			Log.Warn("Brain", "gzip failed: " + ex.Message);
			return null;
		}
	}
}
