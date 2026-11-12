using System;
using System.IO;
using System.Text.Json;

namespace HeyClicky.Core.Voice;

public sealed record PageContext(string Kind, string Reference, string? Text, bool IsTruncated, int? PageCount, bool AccessDenied)
{
	public string ThreadSafeReference
	{
		get
		{
			if (Kind == "web_page")
			{
				if (!Uri.TryCreate(Reference, UriKind.Absolute, out Uri result) || result.Host.Length <= 0)
				{
					return "a web page";
				}
				return result.Host;
			}
			string fileName = Path.GetFileName(Reference.TrimEnd('\\', '/'));
			if (fileName.Length <= 0)
			{
				return Reference;
			}
			return fileName;
		}
	}

	public static PageContext ReferenceOnly(PageReference reference)
	{
		return new PageContext(reference.IsWebPage ? "web_page" : "file", reference.Reference, null, IsTruncated: false, null, AccessDenied: false);
	}

	internal void Write(Utf8JsonWriter writer, bool includeAccessDenied = true)
	{
		writer.WriteStartObject("page");
		writer.WriteString("kind", Kind);
		writer.WriteString("reference", Reference);
		if (!string.IsNullOrEmpty(Text))
		{
			writer.WriteString("text", Text);
			writer.WriteBoolean("truncated", IsTruncated);
		}
		int? pageCount = PageCount;
		if (pageCount.HasValue)
		{
			int valueOrDefault = pageCount.GetValueOrDefault();
			writer.WriteNumber("page_count", valueOrDefault);
		}
		if (AccessDenied && includeAccessDenied)
		{
			writer.WriteBoolean("access_denied", value: true);
		}
		writer.WriteEndObject();
	}
}
