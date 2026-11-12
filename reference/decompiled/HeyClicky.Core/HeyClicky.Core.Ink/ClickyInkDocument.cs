using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkDocument : IEquatable<ClickyInkDocument>
{
	public sealed class Page : IEquatable<Page>
	{
		public string Name { get; set; }

		public bool IsBoard { get; set; }

		public List<Block> Blocks { get; set; }

		public Page(string name, bool isBoard, List<Block> blocks)
		{
			Name = name;
			IsBoard = isBoard;
			Blocks = blocks;
		}

		public bool Equals(Page? other)
		{
			if (other != null && Name == other.Name && IsBoard == other.IsBoard)
			{
				return Blocks.SequenceEqual(other.Blocks);
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Page);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, IsBoard, Blocks.Count);
		}
	}

	public sealed class Block : IEquatable<Block>
	{
		public Kind Kind { get; set; }

		public string Text { get; set; }

		public Dictionary<string, string> Attributes { get; set; }

		public List<Item> Items { get; set; }

		public List<string> Columns { get; set; }

		public string? this[string key]
		{
			get
			{
				if (!Attributes.TryGetValue(key, out string value))
				{
					return null;
				}
				return value;
			}
		}

		public Block(Kind kind, string text = "", Dictionary<string, string>? attributes = null, List<Item>? items = null, List<string>? columns = null)
		{
			Kind = kind;
			Text = text;
			Attributes = attributes ?? new Dictionary<string, string>();
			Items = items ?? new List<Item>();
			Columns = columns ?? new List<string>();
		}

		public Block Clone()
		{
			return new Block(Kind, Text, new Dictionary<string, string>(Attributes), Items.Select((Item i) => i.Clone()).ToList(), Columns.ToList());
		}

		public bool Equals(Block? other)
		{
			if ((object)other != null && Kind == other.Kind && Text == other.Text && Columns.SequenceEqual(other.Columns) && Items.SequenceEqual(other.Items))
			{
				return AttributesEqual(Attributes, other.Attributes);
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Block);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Kind, Text, Items.Count, Attributes.Count);
		}

		public static bool operator ==(Block? a, Block? b)
		{
			return a?.Equals(b) ?? ((object)b == null);
		}

		public static bool operator !=(Block? a, Block? b)
		{
			return !(a == b);
		}

		internal static bool AttributesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
		{
			if (a.Count != b.Count)
			{
				return false;
			}
			foreach (var (key, text3) in a)
			{
				if (!b.TryGetValue(key, out string value) || value != text3)
				{
					return false;
				}
			}
			return true;
		}
	}

	public sealed class Item : IEquatable<Item>
	{
		public string Text { get; set; }

		public string? Detail { get; set; }

		public List<string> Cells { get; set; }

		public Mark? Mark { get; set; }

		public bool? IsChecked { get; set; }

		public string? Url { get; set; }

		public int? Number { get; set; }

		public Item(string text, string? detail = null, List<string>? cells = null, Mark? mark = null, bool? isChecked = null, string? url = null, int? number = null)
		{
			Text = text;
			Detail = detail;
			Cells = cells ?? new List<string>();
			Mark = mark;
			IsChecked = isChecked;
			Url = url;
			Number = number;
		}

		public Item Clone()
		{
			return new Item(Text, Detail, Cells.ToList(), Mark, IsChecked, Url, Number);
		}

		public bool Equals(Item? other)
		{
			if ((object)other != null && Text == other.Text && Detail == other.Detail && Cells.SequenceEqual(other.Cells) && Mark == other.Mark && IsChecked == other.IsChecked && Url == other.Url)
			{
				return Number == other.Number;
			}
			return false;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Item);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Text, Detail, Mark, IsChecked, Url, Number);
		}

		public static bool operator ==(Item? a, Item? b)
		{
			return a?.Equals(b) ?? ((object)b == null);
		}

		public static bool operator !=(Item? a, Item? b)
		{
			return !(a == b);
		}
	}

	public enum Mark
	{
		New,
		Needs,
		Done,
		Pick,
		Now,
		Gone
	}

	public enum Kind
	{
		Heading,
		Paragraph,
		Answer,
		Quote,
		List,
		Checklist,
		Numbered,
		Table,
		Code,
		Rule,
		Figure,
		Sticky,
		Ticket,
		Receipt,
		Photo,
		Place,
		Day,
		Letter,
		Clip,
		File,
		Link,
		Ask,
		Question,
		Chart,
		Note,
		Spark,
		Status,
		Progress,
		Tally,
		Streak,
		Countdown,
		Pipeline,
		Run,
		Quiet,
		Review,
		Tiles,
		Form,
		Connect,
		Items,
		Quiz,
		Todo,
		Gallery,
		Event,
		Criteria,
		Drawing,
		Unknown
	}

	public string? Title { get; set; }

	public List<Page> Pages { get; set; }

	public static ClickyInkDocument Empty => new ClickyInkDocument(null, new List<Page>());

	public List<(int Page, Block Block)> AllBlocks
	{
		get
		{
			List<(int, Block)> list = new List<(int, Block)>();
			for (int i = 0; i < Pages.Count; i++)
			{
				foreach (Block block in Pages[i].Blocks)
				{
					list.Add((i, block));
				}
			}
			return list;
		}
	}

	public ClickyInkDocument(string? title = null, List<Page>? pages = null)
	{
		Title = title;
		Pages = pages ?? new List<Page>();
	}

	public bool Equals(ClickyInkDocument? other)
	{
		if ((object)other != null && Title == other.Title)
		{
			return Pages.SequenceEqual(other.Pages);
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ClickyInkDocument);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Title, Pages.Count);
	}

	public static bool operator ==(ClickyInkDocument? a, ClickyInkDocument? b)
	{
		return a?.Equals(b) ?? ((object)b == null);
	}

	public static bool operator !=(ClickyInkDocument? a, ClickyInkDocument? b)
	{
		return !(a == b);
	}
}
