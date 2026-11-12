using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HeyClicky.Core.Notch;

public static class ComposerAttachmentPolicy
{
	public const int MaxAttachmentCount = 10;

	public static readonly string CapNotice = $"You can attach up to {10} files";

	public static readonly TimeSpan CapNoticeDuration = TimeSpan.FromSeconds(2.5);

	private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".heic", ".tiff", ".tif", ".bmp", ".webp" };

	public static (IReadOnlyList<string> Accepted, bool ExceededCap) Admit(int attachedCount, IEnumerable<string> incoming)
	{
		List<string> list = incoming.Where((string p) => !string.IsNullOrEmpty(p)).ToList();
		int num = Math.Max(0, 10 - attachedCount);
		return (Accepted: list.Take(num).ToList(), ExceededCap: list.Count > num);
	}

	public static IReadOnlyList<string> PastableImageFiles(IEnumerable<string> copiedPaths)
	{
		return copiedPaths.Where((string p) => !string.IsNullOrEmpty(p) && ImageExtensions.Contains(Path.GetExtension(p))).ToList();
	}
}
