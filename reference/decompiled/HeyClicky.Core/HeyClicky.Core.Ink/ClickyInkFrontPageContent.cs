using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkFrontPageContent
{
	private readonly Dictionary<string, ClickyInkGroup> groupsByID = new Dictionary<string, ClickyInkGroup>();

	private readonly Dictionary<string, List<ClickyInkGroup>> strays = new Dictionary<string, List<ClickyInkGroup>>();

	public ClickyInkLiveNote Note { get; }

	public ClickyInkDashboardBoard Board { get; }

	public ClickyInkFrontPagePlanner.Plan Plan { get; }

	public double Clock { get; }

	public List<ClickyInkLiveNote.Block> StatBlocks
	{
		get
		{
			IEnumerable<ClickyInkLiveNote.Block> second = (from g in Board.Sections.SelectMany((ClickyInkDashboardBoard.Section s) => s.Groups)
				where Plan.StatGroupIDs.Contains(g.Id)
				select g).SelectMany((ClickyInkGroup g) => g.Blocks);
			return Board.Strip.Concat(second).Take(8).ToList();
		}
	}

	public List<ClickyInkGroup> MoveGroups => Board.Move?.Groups ?? new List<ClickyInkGroup>();

	public ClickyInkFrontPageContent(ClickyInkLiveNote note, ClickyInkDashboardBoard board, ClickyInkFrontPagePlanner.Plan plan, double clock)
	{
		Note = note;
		Board = board;
		Plan = plan;
		Clock = clock;
		foreach (ClickyInkDashboardBoard.Section section in board.Sections)
		{
			foreach (ClickyInkGroup group in section.Groups)
			{
				groupsByID[group.Id] = group;
				if (!plan.PlacedGroupIDs.Contains(group.Id) && !plan.StatGroupIDs.Contains(group.Id))
				{
					string value;
					string key = (plan.StoryOfPage.TryGetValue(section.Id, out value) ? value : plan.LastStoryID);
					if (!strays.TryGetValue(key, out List<ClickyInkGroup> value2))
					{
						value2 = (strays[key] = new List<ClickyInkGroup>());
					}
					value2.Add(group);
				}
			}
		}
	}

	public ClickyInkGroup? Group(string id)
	{
		if (!groupsByID.TryGetValue(id, out ClickyInkGroup value))
		{
			return null;
		}
		return value;
	}

	public List<ClickyInkGroup> Groups(ClickyInkFrontPagePlanner.Chunk chunk)
	{
		List<ClickyInkGroup> list = new List<ClickyInkGroup>();
		foreach (ClickyInkFrontPagePlanner.Piece piece in chunk.Pieces)
		{
			if (!groupsByID.TryGetValue(piece.GroupID, out ClickyInkGroup value))
			{
				continue;
			}
			if (piece.ItemIDs != null || piece.TakesOtherItems)
			{
				HashSet<string> kept = new HashSet<string>(piece.ItemIDs ?? new List<string>());
				HashSet<string> claimed = piece.OtherClaimedItemIDs;
				ClickyInkLiveNote.Block block = value.Blocks[0].Clone();
				block.Items = block.Items.Where((ClickyInkLiveNote.Item item) => kept.Contains(item.Id) || (piece.TakesOtherItems && !claimed.Contains(item.Id))).ToList();
				if (piece.Part > 0)
				{
					block.Id += $"-part{piece.Part}";
					block.Words = ClickyInkLiveNote.Words.Empty;
				}
				if (block.Items.Count == 0)
				{
					continue;
				}
				ClickyInkGroup clickyInkGroup = value._003CClone_003E_0024();
				List<ClickyInkLiveNote.Block> list2 = new List<ClickyInkLiveNote.Block>();
				list2.Add(block);
				list2.AddRange(value.Blocks.Skip(1));
				clickyInkGroup.Blocks = list2;
				value = clickyInkGroup;
			}
			list.Add(value);
		}
		if (chunk.IsLastOfStory && strays.TryGetValue(chunk.StoryID, out List<ClickyInkGroup> value2))
		{
			list.AddRange(value2);
		}
		return list;
	}

	public ClickyInkLiveNote.Words? StoryName(string storyID)
	{
		if (Board.Run == null)
		{
			ClickyInkLiveNote.Page page = Note.Pages.FirstOrDefault((ClickyInkLiveNote.Page p) => p.Id == storyID);
			if (page != null)
			{
				return page.Name;
			}
		}
		object obj = Board.Sections.FirstOrDefault((ClickyInkDashboardBoard.Section s) => s.Id == storyID)?.Name;
		if (obj == null)
		{
			ClickyInkLiveNote.Page? page2 = Note.Pages.FirstOrDefault((ClickyInkLiveNote.Page p) => p.Id == storyID);
			if (page2 == null)
			{
				return null;
			}
			obj = page2.Name;
		}
		return (ClickyInkLiveNote.Words?)obj;
	}

	public (int New, int Needs) Counts(string storyID, double clock)
	{
		ClickyInkDashboardBoard.Section section = Board.Sections.FirstOrDefault((ClickyInkDashboardBoard.Section s) => s.Id == storyID);
		if ((object)section == null)
		{
			return (New: 0, Needs: 0);
		}
		List<ClickyInkLiveNote.Item> source = (from i in section.Groups.SelectMany((ClickyInkGroup g) => g.Blocks).SelectMany((ClickyInkLiveNote.Block b) => b.Items)
			where !i.IsStruck(clock)
			select i).ToList();
		return (New: (!section.IsNamedNew) ? source.Count((ClickyInkLiveNote.Item i) => i.Source.Mark == ClickyInkDocument.Mark.New) : 0, Needs: source.Count((ClickyInkLiveNote.Item i) => i.Source.Mark == ClickyInkDocument.Mark.Needs));
	}

	public bool HasBadStatus(string storyID)
	{
		return Board.Sections.FirstOrDefault((ClickyInkDashboardBoard.Section s) => s.Id == storyID)?.Groups.SelectMany((ClickyInkGroup g) => g.Blocks).Any((ClickyInkLiveNote.Block b) => b.Source.Kind == ClickyInkDocument.Kind.Status && ClickyInkStatus.State(b.Source["state"] ?? b.Source["status"]) == ClickyInkStatusState.Bad) ?? false;
	}
}
