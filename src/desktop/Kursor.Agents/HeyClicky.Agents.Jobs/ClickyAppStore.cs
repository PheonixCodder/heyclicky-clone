using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HeyClicky.Agents.Jobs;

public sealed class ClickyAppStore
{
	public const int MaximumWorksPerClicky = 60;

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private readonly object gate = new object();

	private readonly string rootFolder;

	private readonly Dictionary<string, List<ClickyWorkPiece>> worksBySlug = new Dictionary<string, List<ClickyWorkPiece>>(StringComparer.Ordinal);

	private readonly HashSet<string> loadedSlugs = new HashSet<string>(StringComparer.Ordinal);

	public event Action<string>? Changed;

	public ClickyAppStore(string rootFolder)
	{
		this.rootFolder = rootFolder;
	}

	public string FolderPath(string slug)
	{
		string text = new string(slug.Select(delegate(char c)
		{
			bool flag = char.IsAsciiLetterOrDigit(c);
			if (!flag)
			{
				bool flag2 = ((c == '-' || c == '_') ? true : false);
				flag = flag2;
			}
			return (!flag) ? '_' : c;
		}).ToArray());
		return Path.Combine(rootFolder, (text.Length == 0) ? "clicky" : text);
	}

	public IReadOnlyList<ClickyWorkPiece> Works(string slug)
	{
		LoadIfNeeded(slug);
		lock (gate)
		{
			List<ClickyWorkPiece> value;
			return worksBySlug.TryGetValue(slug, out value) ? value.ToList() : new List<ClickyWorkPiece>();
		}
	}

	public ClickyWorkShelf Shelf(string slug)
	{
		return new ClickyWorkShelf(Works(slug));
	}

	public void LoadIfNeeded(string slug)
	{
		lock (gate)
		{
			if (!loadedSlugs.Add(slug))
			{
				return;
			}
		}
		List<ClickyWorkPiece> pieces = new List<ClickyWorkPiece>();
		try
		{
			string path = Path.Combine(FolderPath(slug), "works.json");
			if (File.Exists(path))
			{
				pieces = JsonSerializer.Deserialize<List<ClickyWorkPiece>>(File.ReadAllText(path), Json) ?? new List<ClickyWorkPiece>();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "works.json for " + slug + " unreadable: " + ex.Message);
		}
		Merge(pieces, slug, persist: false);
	}

	public void Merge(IReadOnlyList<ClickyWorkPiece> pieces, string slug, bool persist = true)
	{
		if (pieces.Count == 0)
		{
			return;
		}
		if (persist)
		{
			LoadIfNeeded(slug);
		}
		List<ClickyWorkPiece> list;
		lock (gate)
		{
			Dictionary<string, ClickyWorkPiece> dictionary = new Dictionary<string, ClickyWorkPiece>(StringComparer.Ordinal);
			if (worksBySlug.TryGetValue(slug, out List<ClickyWorkPiece> value))
			{
				foreach (ClickyWorkPiece item in value)
				{
					dictionary[item.Id] = item;
				}
			}
			bool flag = false;
			foreach (ClickyWorkPiece piece in pieces)
			{
				ClickyWorkPiece clickyWorkPiece = piece;
				if (!dictionary.ContainsKey(clickyWorkPiece.Id))
				{
					string clientTurnKey = clickyWorkPiece.ClientTurnKey;
					if (clientTurnKey != null)
					{
						ClickyWorkPiece clickyWorkPiece2 = dictionary.Values.FirstOrDefault((ClickyWorkPiece w) => w.ClientTurnKey == clientTurnKey);
						if ((object)clickyWorkPiece2 != null)
						{
							clickyWorkPiece = clickyWorkPiece.Reidentified(clickyWorkPiece2.Id, clickyWorkPiece2.FinishedAt);
						}
					}
				}
				if (dictionary.TryGetValue(clickyWorkPiece.Id, out var value2))
				{
					ClickyWorkPiece clickyWorkPiece3 = clickyWorkPiece with
					{
						Reply = ((clickyWorkPiece.Reply.Length < value2.Reply.Length) ? value2.Reply : clickyWorkPiece.Reply),
						ArtifactPaths = ((clickyWorkPiece.ArtifactPaths.Count == 0) ? value2.ArtifactPaths : clickyWorkPiece.ArtifactPaths),
						Summary = ((clickyWorkPiece.Summary.Length == 0) ? value2.Summary : clickyWorkPiece.Summary),
						Title = ((clickyWorkPiece.Title.Length == 0) ? value2.Title : clickyWorkPiece.Title)
					};
					if (!clickyWorkPiece3.Equals(value2))
					{
						dictionary[clickyWorkPiece.Id] = clickyWorkPiece3;
						flag = true;
					}
				}
				else
				{
					dictionary[clickyWorkPiece.Id] = clickyWorkPiece;
					flag = true;
				}
			}
			if (!flag)
			{
				return;
			}
			list = dictionary.Values.OrderByDescending((ClickyWorkPiece w) => w.FinishedAt).Take(60).ToList();
			worksBySlug[slug] = list;
		}
		if (persist)
		{
			Save(slug, list);
		}
		try
		{
			this.Changed?.Invoke(slug);
		}
		catch (Exception ex)
		{
			Log.Warn("Agents", "ClickyAppStore.Changed: " + ex.Message);
		}
	}

	private void Save(string slug, List<ClickyWorkPiece> works)
	{
		string folder = FolderPath(slug);
		Task.Run(delegate
		{
			try
			{
				Directory.CreateDirectory(folder);
				string text = Path.Combine(folder, "works.json");
				string text2 = text + ".tmp";
				File.WriteAllText(text2, JsonSerializer.Serialize(works, Json));
				File.Move(text2, text, overwrite: true);
			}
			catch (Exception ex)
			{
				Log.Warn("Agents", "works.json for " + slug + " not saved: " + ex.Message);
			}
		});
	}

	public void ForgetAll()
	{
		List<string> list;
		lock (gate)
		{
			list = worksBySlug.Keys.ToList();
			worksBySlug.Clear();
			loadedSlugs.Clear();
		}
		try
		{
			Directory.Delete(rootFolder, recursive: true);
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (Exception ex2)
		{
			Log.Warn("Agents", "forget all works: " + ex2.Message);
		}
		foreach (string item in list)
		{
			try
			{
				this.Changed?.Invoke(item);
			}
			catch
			{
			}
		}
	}

	public void Forget(string slug)
	{
		lock (gate)
		{
			worksBySlug.Remove(slug);
			loadedSlugs.Add(slug);
		}
		try
		{
			Directory.Delete(FolderPath(slug), recursive: true);
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (Exception ex2)
		{
			Log.Warn("Agents", "forget works for " + slug + ": " + ex2.Message);
		}
		try
		{
			this.Changed?.Invoke(slug);
		}
		catch
		{
		}
	}
}
