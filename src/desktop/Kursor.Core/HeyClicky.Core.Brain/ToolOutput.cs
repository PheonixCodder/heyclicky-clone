using System.IO;
using System.Text;
using System.Text.Json;

namespace HeyClicky.Core.Brain;

public static class ToolOutput
{
	public static string Status(string status, string? message = null)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using (Utf8JsonWriter utf8JsonWriter = new Utf8JsonWriter(memoryStream))
		{
			utf8JsonWriter.WriteStartObject();
			utf8JsonWriter.WriteString("status", status);
			if (message != null)
			{
				utf8JsonWriter.WriteString("message", message);
			}
			utf8JsonWriter.WriteEndObject();
		}
		return Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
	}

	public static string Error(string message)
	{
		return Status("error", message);
	}
}
