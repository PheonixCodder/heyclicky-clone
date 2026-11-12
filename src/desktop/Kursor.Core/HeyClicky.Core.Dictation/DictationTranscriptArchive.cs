using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Core.Configuration;

namespace HeyClicky.Core.Dictation;

public static class DictationTranscriptArchive
{
	private const int RetentionDays = 14;

	private static readonly object Gate = new object();

	public static string Directory => Path.Combine(AppBundleConfiguration.AppDataDirectory, "DictationTranscripts");

	public static void DeleteAll()
	{
		lock (Gate)
		{
			try
			{
				if (System.IO.Directory.Exists(Directory))
				{
					System.IO.Directory.Delete(Directory, recursive: true);
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Dictation", "transcript archive delete: " + ex.Message);
			}
		}
	}

	public static void AppendFinishedTranscript(string transcript, string? targetAppName)
	{
		string text = transcript.Trim();
		if (text.Length == 0)
		{
			return;
		}
		DateTimeOffset at = DateTimeOffset.Now;
		Task.Run(delegate
		{
			try
			{
				lock (Gate)
				{
					System.IO.Directory.CreateDirectory(Directory);
					JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
					{
						["at"] = at.ToString("O"),
						["text"] = text
					};
					if (!string.IsNullOrEmpty(targetAppName))
					{
						jsonObject["app"] = targetAppName;
					}
					File.AppendAllText(Path.Combine(Directory, $"{at:yyyy-MM-dd}.jsonl"), jsonObject.ToJsonString() + "\n");
					foreach (string item in System.IO.Directory.EnumerateFiles(Directory, "*.jsonl"))
					{
						if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(item), "yyyy-MM-dd", null, DateTimeStyles.None, out var result) && result < at.Date.AddDays(-14.0))
						{
							File.Delete(item);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Warn("Dictation", "transcript archive: " + ex.Message);
			}
		});
	}
}
