using System.IO;
using System.Linq;
using System.Threading;

namespace HeyClicky.Agents.Backend;

public sealed class UserMemoryCache
{
	private readonly string directory = Path.Combine(stateDirectory, "user-memory");

	private readonly Lock gate = new Lock();

	public UserMemoryCache(string stateDirectory)
	{
	}

	public string? Resolve(string? userId, UserMemoryFetchStatus status, string? markdown)
	{
		if (status != 0)
		{
			if (status != UserMemoryFetchStatus.NoProfile)
			{
				string text = Read(userId);
				Log.Info("Agents", (text == null) ? "user memory: fetch failed, no cached copy" : $"user memory: fetch failed, using the cached copy ({text.Length} chars)");
				return text;
			}
		}
		else if (!string.IsNullOrWhiteSpace(markdown))
		{
			Store(userId, markdown);
			Log.Info("Agents", $"user memory: fetched fresh ({markdown.Length} chars), cached");
			return markdown;
		}
		Clear(userId);
		Log.Info("Agents", "user memory: the Worker reports no profile; injecting nothing");
		return null;
	}

	public string? Read(string? userId)
	{
		string text = PathFor(userId);
		if (text == null)
		{
			return null;
		}
		using (gate.EnterScope())
		{
			try
			{
				object result;
				if (File.Exists(text))
				{
					string text2 = File.ReadAllText(text);
					if (text2 != null && !string.IsNullOrWhiteSpace(text2))
					{
						result = text2;
						goto IL_0037;
					}
				}
				result = null;
				goto IL_0037;
				IL_0037:
				return (string?)result;
			}
			catch (IOException)
			{
				return null;
			}
		}
	}

	private void Store(string? userId, string markdown)
	{
		string text = PathFor(userId);
		if (text == null)
		{
			return;
		}
		using (gate.EnterScope())
		{
			try
			{
				Directory.CreateDirectory(directory);
				File.WriteAllText(text + ".tmp", markdown);
				File.Move(text + ".tmp", text, overwrite: true);
			}
			catch (IOException ex)
			{
				Log.Warn("Agents", "user memory cache write failed: " + ex.Message);
			}
		}
	}

	public void Clear(string? userId)
	{
		string text = PathFor(userId);
		if (text == null)
		{
			return;
		}
		using (gate.EnterScope())
		{
			try
			{
				File.Delete(text);
			}
			catch (IOException)
			{
			}
		}
	}

	private string? PathFor(string? userId)
	{
		if (string.IsNullOrWhiteSpace(userId))
		{
			return null;
		}
		string text = new string(userId.Where(delegate(char c)
		{
			bool flag = char.IsAsciiLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '_') ? true : false);
				flag = flag2;
			}
			return flag;
		}).ToArray());
		if (text.Length != 0)
		{
			return Path.Combine(directory, text + ".md");
		}
		return null;
	}
}
