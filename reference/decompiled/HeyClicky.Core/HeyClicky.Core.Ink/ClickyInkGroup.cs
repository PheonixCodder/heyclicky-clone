using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkGroup
{
	public ClickyInkDocument.Kind Kind { get; init; }

	public List<ClickyInkLiveNote.Block> Blocks { get; init; }

	public string Id
	{
		get
		{
			if (Blocks.Count <= 0)
			{
				return Guid.NewGuid().ToString();
			}
			return Blocks[0].Id;
		}
	}

	public bool IsMadeLink
	{
		get
		{
			if (Kind == ClickyInkDocument.Kind.Link && Blocks.Count > 0)
			{
				return Blocks[0].Source["_made"] != null;
			}
			return false;
		}
	}

	public bool IsSupportingFiles
	{
		get
		{
			if (Kind == ClickyInkDocument.Kind.File && Blocks.Count > 0)
			{
				return Blocks[0].Source["_supporting_files"] != null;
			}
			return false;
		}
	}

	public double LandsAt
	{
		get
		{
			if (Blocks.Count != 0)
			{
				return Blocks.Min((ClickyInkLiveNote.Block b) => b.LandsAt);
			}
			return 0.0;
		}
	}

	public double? VanishesAt
	{
		get
		{
			if (!Blocks.Any((ClickyInkLiveNote.Block b) => !b.VanishesAt.HasValue))
			{
				return Blocks.Max((ClickyInkLiveNote.Block b) => b.VanishesAt.Value);
			}
			return null;
		}
	}

	public const string SupportingFilesAttribute = "_supporting_files";

	public ClickyInkGroup(ClickyInkDocument.Kind kind, List<ClickyInkLiveNote.Block> blocks)
	{
		Kind = kind;
		Blocks = blocks;
	}

	public static List<ClickyInkGroup> Groups(ClickyInkLiveNote.Page page, double clock)
	{
		List<ClickyInkLiveNote.Block> list = (from b in page.Blocks
			where b.IsPresent(clock)
			select b.DashboardResolved(clock)).ToList();
		HashSet<ClickyInkDocument.Kind> hashSet = new HashSet<ClickyInkDocument.Kind>
		{
			ClickyInkDocument.Kind.Figure,
			ClickyInkDocument.Kind.Ask,
			ClickyInkDocument.Kind.Link,
			ClickyInkDocument.Kind.Sticky,
			ClickyInkDocument.Kind.Photo,
			ClickyInkDocument.Kind.Status
		};
		List<ClickyInkGroup> list2 = new List<ClickyInkGroup>();
		HashSet<string> hashSet2 = ClickyInkMadeLink.Ids(page, list);
		foreach (ClickyInkLiveNote.Block item in list)
		{
			ClickyInkLiveNote.Block block = item;
			ClickyInkDocument.Kind kind = block.Source.Kind;
			if (kind == ClickyInkDocument.Kind.Link && hashSet2.Contains(block.Id))
			{
				block = WithAttribute(block, "_made", "1", lastStamp: true);
				List<ClickyInkGroup> list3 = list2;
				ClickyInkDocument.Kind kind2 = kind;
				int num = 1;
				List<ClickyInkLiveNote.Block> list4 = new List<ClickyInkLiveNote.Block>(num);
				CollectionsMarshal.SetCount(list4, num);
				Span<ClickyInkLiveNote.Block> span = CollectionsMarshal.AsSpan(list4);
				int index = 0;
				span[index] = block;
				list3.Add(new ClickyInkGroup(kind2, list4));
				continue;
			}
			string text = block.Source["_tiles"];
			if (text != null)
			{
				if (list2.Count > 0)
				{
					List<ClickyInkGroup> list5 = list2;
					if (list5[list5.Count - 1].Kind == ClickyInkDocument.Kind.Tiles)
					{
						List<ClickyInkGroup> list6 = list2;
						if (list6[list6.Count - 1].Blocks.Count > 0)
						{
							List<ClickyInkGroup> list7 = list2;
							if (list7[list7.Count - 1].Blocks[0].Source["_tiles"] == text)
							{
								List<ClickyInkGroup> list8 = list2;
								list8[list8.Count - 1].Blocks.Add(block);
								continue;
							}
						}
					}
				}
				List<ClickyInkGroup> list9 = list2;
				int index = 1;
				List<ClickyInkLiveNote.Block> list10 = new List<ClickyInkLiveNote.Block>(index);
				CollectionsMarshal.SetCount(list10, index);
				Span<ClickyInkLiveNote.Block> span = CollectionsMarshal.AsSpan(list10);
				int num = 0;
				span[num] = block;
				list9.Add(new ClickyInkGroup(ClickyInkDocument.Kind.Tiles, list10));
			}
			else
			{
				int num2 = list2.FindLastIndex((ClickyInkGroup g) => g.Kind == kind && !g.IsMadeLink);
				if (hashSet.Contains(kind) && num2 >= 0 && (num2 == list2.Count - 1 || block.GoneAt.HasValue || list2.Skip(num2 + 1).All((ClickyInkGroup g) => g.Blocks.All((ClickyInkLiveNote.Block b) => b.GoneAt.HasValue))))
				{
					list2[num2].Blocks.Add(block);
					continue;
				}
				List<ClickyInkGroup> list11 = list2;
				ClickyInkDocument.Kind kind3 = kind;
				int num = 1;
				List<ClickyInkLiveNote.Block> list12 = new List<ClickyInkLiveNote.Block>(num);
				CollectionsMarshal.SetCount(list12, num);
				Span<ClickyInkLiveNote.Block> span = CollectionsMarshal.AsSpan(list12);
				int index = 0;
				span[index] = block;
				list11.Add(new ClickyInkGroup(kind3, list12));
			}
		}
		list2 = MovingSupportingFilesToTheEnd(list2);
		for (int i = 0; i < list2.Count; i++)
		{
			if (list2[i].Kind == ClickyInkDocument.Kind.Link && !list2[i].IsMadeLink)
			{
				string text2 = ((i > 0 && list2[i - 1].Kind == ClickyInkDocument.Kind.Heading) ? list2[i - 1].Blocks[0].Words.Current : null);
				string text3 = ((i == 0) ? page.Name.Current : null);
				if ((text2 != null && NamesSources(text2)) || (text3 != null && NamesSources(text3)))
				{
					list2[i].Blocks[0] = WithAttribute(list2[i].Blocks[0], "_labelled", "1", lastStamp: false);
				}
			}
		}
		return list2;
	}

	private static ClickyInkLiveNote.Block WithAttribute(ClickyInkLiveNote.Block block, string key, string value, bool lastStamp)
	{
		ClickyInkLiveNote.Block block2 = block.Clone();
		int index = (lastStamp ? (block2.Stamps.Count - 1) : 0);
		ClickyInkDocument.Block block3 = block2.Stamps[index].Value.Clone();
		block3.Attributes[key] = value;
		block2.Stamps[index] = block2.Stamps[index]with
		{
			Value = block3
		};
		return block2;
	}

	private static List<ClickyInkGroup> MovingSupportingFilesToTheEnd(List<ClickyInkGroup> groups)
	{
		List<ClickyInkGroup> list = groups.Where((ClickyInkGroup g) => g.Kind == ClickyInkDocument.Kind.File).ToList();
		if (list.Count <= 1)
		{
			return groups;
		}
		ISet<string> supportingPaths = ClickyInkPaths.MainArtifacts.SupportingPaths(list.Select(PathOf).ToList());
		HashSet<string> mainIDs = (from g in list.Where(IsMain)
			select g.Id).ToHashSet();
		if (mainIDs.Count == 0)
		{
			mainIDs.Add(list[0].Id);
		}
		List<ClickyInkLiveNote.Block> list2 = list.Where((ClickyInkGroup g) => !mainIDs.Contains(g.Id)).SelectMany((ClickyInkGroup g) => g.Blocks).ToList();
		if (list2.Count == 0)
		{
			return groups;
		}
		ClickyInkLiveNote.Block item = WithAttribute(list2[0], "_supporting_files", "1", lastStamp: true);
		List<ClickyInkGroup> list3 = groups.Where((ClickyInkGroup g) => g.Kind != ClickyInkDocument.Kind.File || mainIDs.Contains(g.Id)).ToList();
		List<ClickyInkLiveNote.Block> list4 = new List<ClickyInkLiveNote.Block>();
		list4.Add(item);
		list4.AddRange(list2.Skip(1));
		list3.Add(new ClickyInkGroup(ClickyInkDocument.Kind.File, list4));
		return list3;
		bool IsMain(ClickyInkGroup group)
		{
			string text = PathOf(group);
			if (ClickyInkPaths.IsWebLink(text))
			{
				return true;
			}
			if (ClickyInkPaths.MainArtifacts.IsMeaningfulFile(text))
			{
				return !supportingPaths.Contains(ClickyInkPaths.Standardized(text));
			}
			return false;
		}
		static string PathOf(ClickyInkGroup group)
		{
			return ClickyInkPaths.ExpandingTilde(group.Blocks[0].Source["path"] ?? "");
		}
	}

	public static bool NamesSources(string name)
	{
		string words = ClickyInkParser.Emphasis.Strip(name).ToLowerInvariant();
		if (new string[6] { "source", "link", "reference", "reading", "read more", "citation" }.Any((string w) => words.Contains(w, StringComparison.Ordinal)))
		{
			return words.CharacterCount() <= 32;
		}
		return false;
	}

	public bool Equals(ClickyInkGroup? other)
	{
		if ((object)other != null && Kind == other.Kind)
		{
			return Blocks.SequenceEqual(other.Blocks);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Kind, Blocks.Count);
	}

	[CompilerGenerated]
	private ClickyInkGroup(ClickyInkGroup original)
	{
		Kind = original.Kind;
		Blocks = original.Blocks;
	}
}
