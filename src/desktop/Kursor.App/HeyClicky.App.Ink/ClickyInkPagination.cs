using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HeyClicky.Core.Ink;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace HeyClicky.App.Ink;

public static class ClickyInkPagination
{
	public sealed record PartsInfo(int Count, IReadOnlyDictionary<string, int> PartOf, double Scale = 1.0)
	{
		public IReadOnlyDictionary<string, int> ItemPartOf { get; init; } = new Dictionary<string, int>();
	}

	private static readonly HashSet<ClickyInkDocument.Kind> SplittableKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.List,
		ClickyInkDocument.Kind.Checklist,
		ClickyInkDocument.Kind.Numbered
	};

	private static readonly Dictionary<string, PartsInfo> Cache = new Dictionary<string, PartsInfo>();

	public static string ItemKey(string blockID, string itemID)
	{
		return blockID + "|" + itemID;
	}

	public static PartsInfo Parts(ClickyInkLiveNote note, int pageIndex, ClickyHomeSpreadPaper paper)
	{
		ClickyInkLiveNote.Page page = note.Pages[pageIndex];
		string key = $"{note.Title?.Current ?? ""}|{note.Pages.Count}|{page.Id}|{note.VersionCount}|{InkText.DoubleDescription(note.PenFreeAt)}|{paper}";
		if (Cache.TryGetValue(key, out PartsInfo value))
		{
			return value;
		}
		double clock = note.FinishedClock + 30.0;
		ClickyInkStyle style = new ClickyInkStyle(paper.Ink, paper.Quiet, paper.Deep, note.LatestEdit(clock));
		List<ClickyInkGroup> list = ClickyInkGroup.Groups(page, clock);
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		int part = 0;
		double num = 0.0;
		List<double> list2 = list.Select((ClickyInkGroup g) => Measure(g)).ToList();
		double num2 = ClickyInkLayout.PageSize.Height - 26.0 - ClickyInkLayout.ContentTopFor(pageIndex == 0 && (object)note.Title != null, isContinuation: false);
		double num3 = list2.Sum();
		if (num3 > num2 + 6.0 && num3 <= num2 * 1.3)
		{
			return Store(new PartsInfo(1, page.Blocks.ToDictionary((ClickyInkLiveNote.Block b) => b.Id, (ClickyInkLiveNote.Block _) => 0), num2 / num3));
		}
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		for (int i = 0; i < list.Count; i++)
		{
			ClickyInkGroup clickyInkGroup = list[i];
			double num4 = list2[i];
			if (SplittableKinds.Contains(clickyInkGroup.Kind) && clickyInkGroup.Blocks.Count == 1 && clickyInkGroup.Blocks[0].Items.Count > 1 && num4 > Available() - num + 6.0 && num4 > Available() - 40.0)
			{
				ClickyInkLiveNote.Block block = clickyInkGroup.Blocks[0];
				for (int j = 0; j < block.Items.Count; j++)
				{
					ClickyInkLiveNote.Item item = block.Items[j];
					ClickyInkLiveNote.Block block2 = block.Clone();
					int num5 = 1;
					List<ClickyInkLiveNote.Item> list3 = new List<ClickyInkLiveNote.Item>(num5);
					CollectionsMarshal.SetCount(list3, num5);
					Span<ClickyInkLiveNote.Item> span = CollectionsMarshal.AsSpan(list3);
					int index = 0;
					span[index] = item;
					block2.Items = list3;
					ClickyInkGroup clickyInkGroup2 = clickyInkGroup with
					{
						Blocks = new List<ClickyInkLiveNote.Block> { block2 }
					};
					double num6 = Measure(clickyInkGroup2, 0.0) + ((j == 0) ? 0.0 : 7.0);
					if (num > 0.0 && num + num6 > Available() + 6.0)
					{
						part++;
						num = 0.0;
					}
					if (j == 0)
					{
						dictionary[block.Id] = part;
					}
					dictionary2[ItemKey(block.Id, item.Id)] = part;
					num += num6;
				}
				num += 14.0;
				continue;
			}
			if (num > 0.0 && num + num4 > Available() + 6.0)
			{
				part++;
				num = 0.0;
			}
			num += num4;
			foreach (ClickyInkLiveNote.Block block3 in clickyInkGroup.Blocks)
			{
				dictionary[block3.Id] = part;
			}
		}
		if (part > 0 && num < (ClickyInkLayout.PageSize.Height - 200.0) * 0.28 && num3 <= num2 * 1.45)
		{
			return Store(new PartsInfo(1, page.Blocks.ToDictionary((ClickyInkLiveNote.Block b) => b.Id, (ClickyInkLiveNote.Block _) => 0), num2 / num3));
		}
		int value2 = 0;
		foreach (ClickyInkLiveNote.Block block4 in page.Blocks)
		{
			if (dictionary.TryGetValue(block4.Id, out var value3))
			{
				value2 = value3;
			}
			else
			{
				dictionary[block4.Id] = value2;
			}
		}
		return Store(new PartsInfo(part + 1, dictionary)
		{
			ItemPartOf = dictionary2
		});
		double Available()
		{
			return ClickyInkLayout.PageSize.Height - 26.0 - ClickyInkLayout.ContentTopFor(pageIndex == 0 && part == 0 && (object)note.Title != null, part > 0);
		}
		double Measure(ClickyInkGroup group, double bottomPadding = 14.0)
		{
			using (ClickyInkEnvironment.Use(ClickyInkEnvironment.Default))
			{
				FrameworkElement item2 = ClickyInkGroupView.Make(group, clock, style, paper, new ClickyInkActions());
				Grid grid = new Grid();
				grid.Width = 378.0;
				grid.Padding = new Thickness(0.0, 0.0, 0.0, bottomPadding);
				grid.Children.Add(item2);
				grid.Measure(new Size(378.0, double.PositiveInfinity));
				return grid.DesiredSize.Height;
			}
		}
		PartsInfo Store(PartsInfo result)
		{
			Cache[key] = result;
			if (Cache.Count > 200)
			{
				Cache.Clear();
			}
			return result;
		}
	}

	public static List<(int Page, int Part)> Sheets(ClickyInkLiveNote note, ClickyHomeSpreadPaper? paper = null)
	{
		return Enumerable.Range(0, note.Pages.Count).SelectMany((int index) => from p in Enumerable.Range(0, Parts(note, index, paper ?? ClickyHomeSpreadPaper.Note).Count)
			select (index: index, p: p)).ToList();
	}
}
