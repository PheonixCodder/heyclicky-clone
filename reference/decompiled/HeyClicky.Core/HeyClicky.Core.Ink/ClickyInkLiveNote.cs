using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Ink;

public sealed class ClickyInkLiveNote : IEquatable<ClickyInkLiveNote>
{
	public sealed class Page : IEquatable<Page>
	{
		public string Id { get; set; }

		public Words Name { get; set; }

		public bool IsBoard { get; set; }

		public List<Block> Blocks { get; set; }

		public double LandsAt { get; set; }

		public Page(string id, Words name, bool isBoard, List<Block> blocks, double landsAt)
		{
			Id = id;
			Name = name;
			IsBoard = isBoard;
			Blocks = blocks;
			LandsAt = landsAt;
		}

		public Page Clone()
		{
			return new Page(Id, Name, IsBoard, Blocks.ToList(), LandsAt);
		}

		public bool Equals(Page? other)
		{
			if (this != other)
			{
				if (other != null && Id == other.Id && Name.Equals(other.Name) && IsBoard == other.IsBoard && LandsAt.Equals(other.LandsAt))
				{
					return Blocks.SequenceEqual(other.Blocks);
				}
				return false;
			}
			return true;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Page);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Blocks.Count);
		}
	}

	public sealed class Block : IEquatable<Block>
	{
		public string Id { get; set; }

		public List<Stamp<ClickyInkDocument.Block>> Stamps { get; set; }

		public Words Words { get; set; }

		public List<Item> Items { get; set; }

		public double LandsAt { get; set; }

		public double? GoneAt { get; set; }

		public double? VanishesAt { get; set; }

		public int? BornInEdit { get; set; }

		public int? GoneInEdit { get; set; }

		public bool IsPreview { get; set; }

		public List<double> PenArrivals { get; set; } = new List<double>();

		public double? NumberShownSince { get; set; }

		public double? NumberLineWrittenAt { get; set; }

		public ClickyInkDocument.Block Source
		{
			get
			{
				List<Stamp<ClickyInkDocument.Block>> stamps = Stamps;
				return stamps[stamps.Count - 1].Value;
			}
		}

		public Block(string id, List<Stamp<ClickyInkDocument.Block>> stamps, Words words, List<Item> items, double landsAt, int? bornInEdit = null)
		{
			Id = id;
			Stamps = stamps;
			Words = words;
			Items = items;
			LandsAt = landsAt;
			BornInEdit = bornInEdit;
		}

		public Block Clone()
		{
			return new Block(Id, Stamps.ToList(), Words, Items.ToList(), LandsAt, BornInEdit)
			{
				GoneAt = GoneAt,
				VanishesAt = VanishesAt,
				GoneInEdit = GoneInEdit,
				IsPreview = IsPreview,
				PenArrivals = PenArrivals.ToList(),
				NumberShownSince = NumberShownSince,
				NumberLineWrittenAt = NumberLineWrittenAt
			};
		}

		public ClickyInkDocument.Block SourceAt(double clock)
		{
			return Stamp<ClickyInkDocument.Block>.ValueIn(Stamps, clock);
		}

		public bool IsPresent(double clock)
		{
			if (clock >= LandsAt)
			{
				double? vanishesAt = VanishesAt;
				if (vanishesAt.HasValue)
				{
					double valueOrDefault = vanishesAt.GetValueOrDefault();
					return clock < valueOrDefault + 0.45;
				}
				return true;
			}
			return false;
		}

		public bool IsStruck(double clock)
		{
			double? goneAt = GoneAt;
			if (goneAt.HasValue)
			{
				double valueOrDefault = goneAt.GetValueOrDefault();
				return clock >= valueOrDefault;
			}
			return false;
		}

		public bool Equals(Block? other)
		{
			if (this != other)
			{
				if (other != null && Id == other.Id && Stamps.SequenceEqual(other.Stamps) && Words.Equals(other.Words) && Items.SequenceEqual(other.Items) && LandsAt.Equals(other.LandsAt) && GoneAt == other.GoneAt && VanishesAt == other.VanishesAt && BornInEdit == other.BornInEdit && GoneInEdit == other.GoneInEdit && IsPreview == other.IsPreview && PenArrivals.SequenceEqual(other.PenArrivals) && NumberShownSince == other.NumberShownSince)
				{
					return NumberLineWrittenAt == other.NumberLineWrittenAt;
				}
				return false;
			}
			return true;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Block);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Stamps.Count, Items.Count);
		}
	}

	public sealed class Item : IEquatable<Item>
	{
		public string Id { get; set; }

		public List<Stamp<ClickyInkDocument.Item>> Stamps { get; set; }

		public Words Words { get; set; }

		public List<Words> Cells { get; set; }

		public Words? Detail { get; set; }

		public double LandsAt { get; set; }

		public double? GoneAt { get; set; }

		public double? VanishesAt { get; set; }

		public int? GoneInEdit { get; set; }

		public bool IsPreview { get; set; }

		public ClickyInkDocument.Item Source
		{
			get
			{
				List<Stamp<ClickyInkDocument.Item>> stamps = Stamps;
				return stamps[stamps.Count - 1].Value;
			}
		}

		public double ShownSince
		{
			get
			{
				if (Stamps.Count <= 0)
				{
					return LandsAt;
				}
				List<Stamp<ClickyInkDocument.Item>> stamps = Stamps;
				return stamps[stamps.Count - 1].At;
			}
		}

		public Item(string id, List<Stamp<ClickyInkDocument.Item>> stamps, Words words, List<Words> cells, Words? detail, double landsAt)
		{
			Id = id;
			Stamps = stamps;
			Words = words;
			Cells = cells;
			Detail = detail;
			LandsAt = landsAt;
		}

		public Item Clone()
		{
			return new Item(Id, Stamps.ToList(), Words, Cells.ToList(), Detail, LandsAt)
			{
				GoneAt = GoneAt,
				VanishesAt = VanishesAt,
				GoneInEdit = GoneInEdit,
				IsPreview = IsPreview
			};
		}

		public ClickyInkDocument.Item SourceAt(double clock)
		{
			return Stamp<ClickyInkDocument.Item>.ValueIn(Stamps, clock);
		}

		public double? Since(Func<ClickyInkDocument.Item, bool> holds)
		{
			if (Stamps.Count != 0)
			{
				List<Stamp<ClickyInkDocument.Item>> stamps = Stamps;
				if (holds(stamps[stamps.Count - 1].Value))
				{
					List<Stamp<ClickyInkDocument.Item>> stamps2 = Stamps;
					double at = stamps2[stamps2.Count - 1].At;
					int num = Stamps.Count - 2;
					while (num >= 0 && holds(Stamps[num].Value))
					{
						at = Stamps[num].At;
						num--;
					}
					return at;
				}
			}
			return null;
		}

		public bool IsPresent(double clock)
		{
			if (clock >= LandsAt)
			{
				double? vanishesAt = VanishesAt;
				if (vanishesAt.HasValue)
				{
					double valueOrDefault = vanishesAt.GetValueOrDefault();
					return clock < valueOrDefault + 0.45;
				}
				return true;
			}
			return false;
		}

		public bool IsStruck(double clock)
		{
			double? goneAt = GoneAt;
			if (goneAt.HasValue)
			{
				double valueOrDefault = goneAt.GetValueOrDefault();
				return clock >= valueOrDefault;
			}
			return false;
		}

		public bool Equals(Item? other)
		{
			if (this != other)
			{
				if (other != null && Id == other.Id && Stamps.SequenceEqual(other.Stamps) && Words.Equals(other.Words) && Cells.SequenceEqual(other.Cells) && object.Equals(Detail, other.Detail) && LandsAt.Equals(other.LandsAt) && GoneAt == other.GoneAt && VanishesAt == other.VanishesAt && GoneInEdit == other.GoneInEdit)
				{
					return IsPreview == other.IsPreview;
				}
				return false;
			}
			return true;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as Item);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Stamps.Count);
		}
	}

	public readonly record struct Stamp<TValue>(double At, TValue Value, int? Edit = null)
	{
		public static TValue ValueIn(IReadOnlyList<Stamp<TValue>> stamps, double clock)
		{
			for (int num = stamps.Count - 1; num >= 0; num--)
			{
				if (stamps[num].At <= clock)
				{
					return stamps[num].Value;
				}
			}
			return stamps[0].Value;
		}
	}

	public sealed record Words
	{
		public IReadOnlyList<Segment> Segments { get; init; }

		public double? AsOf { get; init; }

		public double? ReservedUntil { get; init; }

		public string Current => string.Concat(from s in Segments
			where !s.Struck.HasValue && !s.VanishesAt.HasValue
			select s.Text);

		public double FinishedAt
		{
			get
			{
				List<InkRange> list = (from s in Segments
					where s.Written.HasValue
					select s.Written.Value).ToList();
				double? asOf = AsOf;
				if (asOf.HasValue)
				{
					double asOf2 = asOf.GetValueOrDefault();
					List<InkRange> list2 = list.Where((InkRange w) => w.LowerBound <= asOf2).ToList();
					if (list2.Count == 0)
					{
						if (list.Count != 0)
						{
							return list.MinBy((InkRange w) => w.LowerBound).UpperBound;
						}
						return 0.0;
					}
					return list2.Max((InkRange w) => w.UpperBound);
				}
				if (list.Count != 0)
				{
					return list.Max((InkRange w) => w.UpperBound);
				}
				return 0.0;
			}
		}

		public double? StartsAt
		{
			get
			{
				double? num = null;
				foreach (Segment segment in Segments)
				{
					InkRange? written = segment.Written;
					if (written.HasValue)
					{
						InkRange valueOrDefault = written.GetValueOrDefault();
						if (!num.HasValue || valueOrDefault.LowerBound < num)
						{
							num = valueOrDefault.LowerBound;
						}
					}
				}
				return num;
			}
		}

		public static readonly Words Empty = new Words(Array.Empty<Segment>(), null, null);

		public Words(IReadOnlyList<Segment> segments, double? asOf = null, double? reservedUntil = null)
		{
			Segments = segments;
			AsOf = asOf;
			ReservedUntil = reservedUntil;
		}

		public string TextAt(double clock)
		{
			return string.Concat(from s in Segments.Where(delegate(Segment s)
				{
					InkRange? written = s.Written;
					if (written.HasValue)
					{
						InkRange valueOrDefault = written.GetValueOrDefault();
						if (!(clock >= valueOrDefault.LowerBound))
						{
							goto IL_0076;
						}
					}
					written = s.Struck;
					if (written.HasValue)
					{
						InkRange valueOrDefault2 = written.GetValueOrDefault();
						if (clock >= valueOrDefault2.LowerBound)
						{
							goto IL_0076;
						}
					}
					double? vanishesAt = s.VanishesAt;
					if (vanishesAt.HasValue)
					{
						double valueOrDefault3 = vanishesAt.GetValueOrDefault();
						return !(clock >= valueOrDefault3);
					}
					return true;
					IL_0076:
					return false;
				})
				select s.Text);
		}

		public static Words Plain(string text)
		{
			IReadOnlyList<Segment> segments;
			if (text.Length != 0)
			{
				IReadOnlyList<Segment> readOnlyList = new _003C_003Ez__ReadOnlySingleElementList<Segment>(new Segment(text, null, null, null, null, null));
				segments = readOnlyList;
			}
			else
			{
				IReadOnlyList<Segment> readOnlyList = Array.Empty<Segment>();
				segments = readOnlyList;
			}
			return new Words(segments, null, null);
		}

		public bool Equals(Words? other)
		{
			if ((object)this != other)
			{
				if ((object)other != null && AsOf == other.AsOf && ReservedUntil == other.ReservedUntil)
				{
					return Segments.SequenceEqual(other.Segments);
				}
				return false;
			}
			return true;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Segments.Count, AsOf, ReservedUntil);
		}

		[CompilerGenerated]
		private Words(Words original)
		{
			Segments = original.Segments;
			AsOf = original.AsOf;
			ReservedUntil = original.ReservedUntil;
		}
	}

	public record struct Segment(string Text, InkRange? Written = null, InkRange? Struck = null, double? VanishesAt = null, int? Edit = null, int? StruckInEdit = null, bool IsPreview = false);

	public readonly record struct FocusKnown(double From, double Since);

	public record struct PenFocus(double At, double Until, string PlaceID, string? ItemID = null)
	{
		public double KnownSince { get; set; } = double.NegativeInfinity;

		public bool IsLanding { get; set; } = false;
	}

	[CompilerGenerated]
	private sealed class _003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed : IEnumerable<double>, IEnumerable, IEnumerator<double>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private double _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private Words words;

		public Words _003C_003E3__words;

		private IEnumerator<Segment> _003C_003E7__wrap1;

		private Segment _003Csegment_003E5__3;

		double IEnumerator<double>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || (uint)(num - 1) <= 1u)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003Csegment_003E5__3 = default(Segment);
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				InkRange? written;
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					if ((object)words == null)
					{
						return false;
					}
					_003C_003E7__wrap1 = words.Segments.GetEnumerator();
					_003C_003E1__state = -3;
					goto IL_00f2;
				case 1:
					_003C_003E1__state = -3;
					goto IL_00a8;
				case 2:
					{
						_003C_003E1__state = -3;
						goto IL_00e6;
					}
					IL_00e6:
					_003Csegment_003E5__3 = default(Segment);
					goto IL_00f2;
					IL_00f2:
					if (_003C_003E7__wrap1.MoveNext())
					{
						_003Csegment_003E5__3 = _003C_003E7__wrap1.Current;
						written = _003Csegment_003E5__3.Written;
						if (written.HasValue)
						{
							_003C_003E2__current = written.GetValueOrDefault().LowerBound;
							_003C_003E1__state = 1;
							return true;
						}
						goto IL_00a8;
					}
					_003C_003Em__Finally1();
					_003C_003E7__wrap1 = null;
					return false;
					IL_00a8:
					written = _003Csegment_003E5__3.Struck;
					if (written.HasValue)
					{
						_003C_003E2__current = written.GetValueOrDefault().LowerBound;
						_003C_003E1__state = 2;
						return true;
					}
					goto IL_00e6;
				}
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<double> IEnumerable<double>.GetEnumerator()
		{
			_003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed _003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed = this;
			}
			else
			{
				_003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed = new _003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed(0);
			}
			_003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed.words = _003C_003E3__words;
			return _003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<double>)this).GetEnumerator();
		}
	}

	private static readonly HashSet<ClickyInkDocument.Kind> EntityKinds = new HashSet<ClickyInkDocument.Kind>
	{
		ClickyInkDocument.Kind.Link,
		ClickyInkDocument.Kind.File,
		ClickyInkDocument.Kind.Ticket,
		ClickyInkDocument.Kind.Photo,
		ClickyInkDocument.Kind.Place,
		ClickyInkDocument.Kind.Ask,
		ClickyInkDocument.Kind.Clip,
		ClickyInkDocument.Kind.Status,
		ClickyInkDocument.Kind.Question
	};

	public const string TitleFocusID = "title";

	public Words? Title { get; set; }

	public List<Page> Pages { get; set; } = new List<Page>();

	public int VersionCount { get; set; }

	public double PenFreeAt { get; set; } = double.NegativeInfinity;

	public List<double> EditStarts { get; set; } = new List<double>();

	public double? LastVersionAt { get; set; }

	public bool LastVersionWasEdit { get; set; }

	public bool LastVersionWasPartial { get; set; }

	public List<double> VersionArrivals { get; set; } = new List<double>();

	public List<FocusKnown> FocusKnownSince { get; set; } = new List<FocusKnown>();

	public int? PenLastPage { get; set; }

	public int? PenLastBlock { get; set; }

	public double FinishedClock => PenFreeAt + 0.6;

	public ClickyInkLiveNote Clone()
	{
		return new ClickyInkLiveNote
		{
			Title = Title,
			Pages = Pages.ToList(),
			VersionCount = VersionCount,
			PenFreeAt = PenFreeAt,
			EditStarts = EditStarts.ToList(),
			LastVersionAt = LastVersionAt,
			LastVersionWasEdit = LastVersionWasEdit,
			LastVersionWasPartial = LastVersionWasPartial,
			VersionArrivals = VersionArrivals.ToList(),
			FocusKnownSince = FocusKnownSince.ToList(),
			PenLastPage = PenLastPage,
			PenLastBlock = PenLastBlock
		};
	}

	public int? LatestEdit(double clock)
	{
		int num = EditStarts.Count((double s) => s <= clock);
		if (num != 0)
		{
			return num;
		}
		return null;
	}

	public bool Equals(ClickyInkLiveNote? other)
	{
		if ((object)this != other)
		{
			if ((object)other != null && object.Equals(Title, other.Title) && VersionCount == other.VersionCount && PenFreeAt.Equals(other.PenFreeAt) && EditStarts.SequenceEqual(other.EditStarts) && LastVersionAt == other.LastVersionAt && LastVersionWasEdit == other.LastVersionWasEdit && LastVersionWasPartial == other.LastVersionWasPartial && VersionArrivals.SequenceEqual(other.VersionArrivals) && FocusKnownSince.SequenceEqual(other.FocusKnownSince) && PenLastPage == other.PenLastPage && PenLastBlock == other.PenLastBlock)
			{
				return Pages.SequenceEqual(other.Pages);
			}
			return false;
		}
		return true;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as ClickyInkLiveNote);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(VersionCount, Pages.Count, PenFreeAt);
	}

	public static bool operator ==(ClickyInkLiveNote? a, ClickyInkLiveNote? b)
	{
		return a?.Equals(b) ?? ((object)b == null);
	}

	public static bool operator !=(ClickyInkLiveNote? a, ClickyInkLiveNote? b)
	{
		return !(a == b);
	}

	public void Apply(ClickyInkDocument document, double time, bool isEdit = false, bool? startsNewEdit = null, bool isPartial = false)
	{
		double num = Math.Max(0.0, PenFreeAt - time);
		double num2 = ((VersionCount > 0 && num <= 0.0 && (!isPartial || !LastVersionWasPartial)) ? 0.35 : 0.0);
		double num3 = Math.Max(time + num2, PenFreeAt);
		ClickyInkLiveNote clickyInkLiveNote = Clone();
		clickyInkLiveNote.Apply(document, time, isEdit, startsNewEdit, isPartial, 1.0, num2);
		double num4 = Math.Max(0.0, clickyInkLiveNote.PenFreeAt - num3);
		double num5 = ClickyInkPen.Haste(num, num4);
		if (isPartial && num5 <= 1.0 && num < 0.25)
		{
			double? lastVersionAt = LastVersionAt;
			if (lastVersionAt.HasValue)
			{
				double valueOrDefault = lastVersionAt.GetValueOrDefault();
				if (LastVersionWasPartial && num4 > 0.0)
				{
					double num6 = Math.Min(1.2, Math.Max(0.15, time - valueOrDefault));
					num5 = Math.Min(1.0, Math.Max(0.6, num4 / num6));
				}
			}
		}
		Apply(document, time, isEdit, startsNewEdit, isPartial, num5, num2);
	}

	private void Apply(ClickyInkDocument document, double time, bool isEdit, bool? startsNewEdit, bool isPartial, double haste, double travel)
	{
		double num = Math.Max(time + travel, PenFreeAt);
		int num2;
		if (startsNewEdit.HasValue)
		{
			bool valueOrDefault = startsNewEdit == true;
			num2 = ((!valueOrDefault) ? 1 : 0);
		}
		else
		{
			if (LastVersionWasEdit)
			{
				double? lastVersionAt = LastVersionAt;
				if (lastVersionAt.HasValue)
				{
					double valueOrDefault2 = lastVersionAt.GetValueOrDefault();
					num2 = ((time - valueOrDefault2 < 600.0) ? 1 : 0);
					goto IL_0063;
				}
			}
			num2 = 0;
		}
		goto IL_0063;
		IL_0063:
		bool flag = (byte)num2 != 0;
		if (isEdit && (!flag || EditStarts.Count <= 0))
		{
			EditStarts.Add(num);
		}
		LastVersionAt = time;
		LastVersionWasEdit = isEdit;
		LastVersionWasPartial = isPartial;
		VersionArrivals.Add(time);
		FocusKnownSince.Add(new FocusKnown(num, time));
		Scheduler scheduler = new Scheduler(num, time, isEdit ? new int?(EditStarts.Count) : ((int?)null), haste)
		{
			IsPartial = isPartial
		};
		int? penLastPage = PenLastPage;
		if (penLastPage.HasValue)
		{
			int valueOrDefault3 = penLastPage.GetValueOrDefault();
			penLastPage = PenLastBlock;
			if (penLastPage.HasValue)
			{
				int valueOrDefault4 = penLastPage.GetValueOrDefault();
				scheduler.LastWorked = (valueOrDefault3, valueOrDefault4);
			}
		}
		int num3 = document.Pages.Count - 1;
		string title = document.Title;
		if (title != null)
		{
			if ((object)Title == null)
			{
				scheduler.Pause(0.3);
			}
			Title = scheduler.Rewrite(Title, title, 24.0);
		}
		List<Page> oldPages = Pages;
		List<(string, Block)> list = new List<(string, Block)>();
		foreach (Page item2 in oldPages)
		{
			foreach (Block block7 in item2.Blocks)
			{
				if (!block7.GoneAt.HasValue && !block7.VanishesAt.HasValue)
				{
					list.Add((item2.Id, block7));
				}
			}
		}
		HashSet<string> usedOld = new HashSet<string>();
		HashSet<string> usedPages = new HashSet<string>();
		List<Page> list2 = new List<Page>();
		for (int pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
		{
			ClickyInkDocument.Page page = document.Pages[pageIndex];
			Page page2 = oldPages.FirstOrDefault((Page p) => !usedPages.Contains(p.Id) && ClickyInkMatch.Similarity(p.Name.Current, page.Name) > 0.5) ?? oldPages.Skip(pageIndex).FirstOrDefault((Page p) => !usedPages.Contains(p.Id) && pageIndex < oldPages.Count && p.Id == oldPages[pageIndex].Id);
			if (page2 != null)
			{
				usedPages.Add(page2.Id);
			}
			string text = page2?.Id ?? $"p{VersionCount}-{pageIndex}";
			scheduler.FoldTime = scheduler.Cursor;
			bool flag2 = IsSilentPageName(page.Name, pageIndex);
			Words name = scheduler.Rewrite(page2?.Name, page.Name, flag2 ? double.PositiveInfinity : 24.0);
			List<Block> list3 = new List<Block>();
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			for (int i = 0; i < page.Blocks.Count; i++)
			{
				ClickyInkDocument.Block block2 = page.Blocks[i];
				(int, double)? tuple = null;
				for (int j = 0; j < list.Count; j++)
				{
					(string, Block) tuple2 = list[j];
					if (!usedOld.Contains(tuple2.Item2.Id) && ClickyInkMatch.SameFamily(tuple2.Item2.Source.Kind, block2.Kind))
					{
						double num4 = ClickyInkMatch.Similarity(ClickyInkMatch.Signature(tuple2.Item2.Source), ClickyInkMatch.Signature(block2));
						if (tuple2.Item1 == text)
						{
							num4 += 0.08;
						}
						if (tuple2.Item2.Source.Kind == block2.Kind)
						{
							num4 += 0.04;
						}
						if (num4 > (tuple?.Item2 ?? 0.36))
						{
							tuple = (j, num4);
						}
					}
				}
				if (tuple.HasValue)
				{
					(int, double) valueOrDefault5 = tuple.GetValueOrDefault();
					(dictionary[i], _) = valueOrDefault5;
					usedOld.Add(list[valueOrDefault5.Item1].Item2.Id);
				}
			}
			if (page2 != null)
			{
				for (int k = 0; k < page.Blocks.Count; k++)
				{
					ClickyInkDocument.Block newBlock = page.Blocks[k];
					if (dictionary.ContainsKey(k) || EntityKinds.Contains(newBlock.Kind))
					{
						continue;
					}
					int ordinal = page.Blocks.Take(k).Count((ClickyInkDocument.Block b) => b.Kind == newBlock.Kind);
					List<Block> sameKindOld = page2.Blocks.Where((Block b) => !b.GoneAt.HasValue && !b.VanishesAt.HasValue && b.Source.Kind == newBlock.Kind).ToList();
					if (ordinal < sameKindOld.Count && !usedOld.Contains(sameKindOld[ordinal].Id))
					{
						int num5 = list.FindIndex(((string PageId, Block Block) o) => o.Block.Id == sameKindOld[ordinal].Id);
						if (num5 >= 0)
						{
							dictionary[k] = num5;
							usedOld.Add(sameKindOld[ordinal].Id);
						}
					}
				}
			}
			for (int l = 0; l < page.Blocks.Count; l++)
			{
				ClickyInkDocument.Block block3 = page.Blocks[l];
				if (dictionary.TryGetValue(l, out var value))
				{
					Block item = list[value].Item2;
					if (isPartial && pageIndex == num3 && l == page.Blocks.Count - 1 && ClickyInkMatch.IsStillCatchingUp(item.Source, block3))
					{
						list3.Add(item);
						continue;
					}
					scheduler.Position = (pageIndex, l);
					list3.Add(scheduler.Update(item, block3));
				}
				else
				{
					scheduler.Position = (pageIndex, l);
					list3.Add(scheduler.Write(block3, $"b{VersionCount}-{pageIndex}-{l}"));
				}
			}
			if (page2 != null)
			{
				string previousId = null;
				foreach (Block block8 in page2.Blocks)
				{
					try
					{
						if (usedOld.Contains(block8.Id))
						{
							continue;
						}
						Block block4 = block8;
						if (isPartial && !block8.GoneAt.HasValue && !block8.VanishesAt.HasValue)
						{
							usedOld.Add(block8.Id);
							int val = PositionAfter(list3, previousId) ?? list3.Count;
							list3.Insert(Math.Min(val, list3.Count), block4);
							continue;
						}
						if (block8.GoneAt.HasValue || block8.VanishesAt.HasValue)
						{
							penLastPage = scheduler.Edit;
							bool flag3 = penLastPage.HasValue && block8.GoneInEdit == scheduler.Edit;
							if (!block4.VanishesAt.HasValue && !flag3)
							{
								block4 = block4.Clone();
								block4.VanishesAt = scheduler.Arrival;
							}
						}
						else
						{
							usedOld.Add(block8.Id);
							block4 = scheduler.Strike(block8);
						}
						int valueOrDefault6 = PositionAfter(list3, previousId).GetValueOrDefault();
						list3.Insert(Math.Min(valueOrDefault6, list3.Count), block4);
					}
					finally
					{
						previousId = block8.Id;
					}
				}
			}
			list2.Add(new Page(text, name, page.IsBoard, list3, page2?.LandsAt ?? scheduler.Cursor));
		}
		foreach (Page item3 in oldPages)
		{
			if (usedPages.Contains(item3.Id))
			{
				continue;
			}
			Page page3 = item3.Clone();
			page3.Blocks.RemoveAll((Block b) => usedOld.Contains(b.Id));
			if (page3.Blocks.Count == 0)
			{
				continue;
			}
			if (isPartial)
			{
				list2.Add(page3);
				continue;
			}
			page3.Blocks = page3.Blocks.Select(delegate(Block block)
			{
				Block block5 = block.Clone();
				if (!block5.GoneAt.HasValue && !block5.IsPreview)
				{
					block5.GoneAt = scheduler.Arrival;
				}
				Block block6 = block5;
				double? vanishesAt = block6.VanishesAt;
				double valueOrDefault7 = vanishesAt.GetValueOrDefault();
				if (!vanishesAt.HasValue)
				{
					valueOrDefault7 = scheduler.Arrival + 0.3;
					double? vanishesAt2 = valueOrDefault7;
					block6.VanishesAt = vanishesAt2;
				}
				return block5;
			}).ToList();
			list2.Add(page3);
		}
		Pages = list2;
		VersionCount++;
		PenFreeAt = scheduler.Cursor;
		(int, int)? lastWorked = scheduler.LastWorked;
		if (lastWorked.HasValue)
		{
			(int, int) valueOrDefault8 = lastWorked.GetValueOrDefault();
			PenLastPage = valueOrDefault8.Item1;
			PenLastBlock = valueOrDefault8.Item2;
		}
	}

	private static int? PositionAfter(List<Block> blocks, string? previousId)
	{
		if (previousId == null)
		{
			return null;
		}
		int num = blocks.FindIndex((Block b) => b.Id == previousId);
		if (num >= 0)
		{
			return num + 1;
		}
		return null;
	}

	public void Finish(double time)
	{
		if (!(PenFreeAt > time))
		{
			return;
		}
		double factor = ClickyInkPen.AfterDoneHaste(PenFreeAt - time);
		if (!(factor > 1.0))
		{
			return;
		}
		Title = (((object)Title == null) ? null : WarpWords(Title));
		Pages = Pages.Select((Page page) => new Page(page.Id, WarpWords(page.Name), page.IsBoard, page.Blocks.Select(delegate(Block block)
		{
			Block block2 = block.Clone();
			block2.Stamps = WarpStamps<ClickyInkDocument.Block>(block.Stamps);
			block2.Words = WarpWords(block.Words);
			block2.LandsAt = Warp(block.LandsAt);
			block2.PenArrivals = block.PenArrivals.Select(Warp).ToList();
			double? goneAt = block.GoneAt;
			double? goneAt2;
			if (goneAt.HasValue)
			{
				double valueOrDefault = goneAt.GetValueOrDefault();
				goneAt2 = Warp(valueOrDefault);
			}
			else
			{
				goneAt2 = null;
			}
			block2.GoneAt = goneAt2;
			goneAt = block.VanishesAt;
			double? vanishesAt;
			if (goneAt.HasValue)
			{
				double valueOrDefault2 = goneAt.GetValueOrDefault();
				vanishesAt = Warp(valueOrDefault2);
			}
			else
			{
				vanishesAt = null;
			}
			block2.VanishesAt = vanishesAt;
			block2.Items = block.Items.Select(delegate(Item item)
			{
				Item item2 = item.Clone();
				item2.Stamps = WarpStamps<ClickyInkDocument.Item>(item.Stamps);
				item2.Words = WarpWords(item.Words);
				item2.Cells = item.Cells.Select(WarpWords).ToList();
				item2.Detail = (((object)item.Detail == null) ? null : WarpWords(item.Detail));
				item2.LandsAt = Warp(item.LandsAt);
				double? goneAt3 = item.GoneAt;
				double? goneAt4;
				if (goneAt3.HasValue)
				{
					double valueOrDefault3 = goneAt3.GetValueOrDefault();
					goneAt4 = Warp(valueOrDefault3);
				}
				else
				{
					goneAt4 = null;
				}
				item2.GoneAt = goneAt4;
				goneAt3 = item.VanishesAt;
				double? vanishesAt2;
				if (goneAt3.HasValue)
				{
					double valueOrDefault4 = goneAt3.GetValueOrDefault();
					vanishesAt2 = Warp(valueOrDefault4);
				}
				else
				{
					vanishesAt2 = null;
				}
				item2.VanishesAt = vanishesAt2;
				return item2;
			}).ToList();
			return block2;
		}).ToList(), Warp(page.LandsAt))).ToList();
		EditStarts = EditStarts.Select(Warp).ToList();
		PenFreeAt = Warp(PenFreeAt);
		List<FocusKnown> list = FocusKnownSince.Where((FocusKnown k) => k.From <= time).ToList();
		list.Add(new FocusKnown(time, time));
		list.AddRange(from k in FocusKnownSince
			where k.From > time
			select new FocusKnown(Warp(k.From), Math.Max(k.Since, time)));
		FocusKnownSince = list;
		double Warp(double value)
		{
			if (!(value <= time))
			{
				return time + (value - time) / factor;
			}
			return value;
		}
		InkRange WarpRange(InkRange range)
		{
			return new InkRange(Warp(range.LowerBound), Warp(range.UpperBound));
		}
		List<Stamp<T>> WarpStamps<T>(List<Stamp<T>> stamps) where T : notnull
		{
			return stamps.Select((Stamp<T> stamp) => stamp with
			{
				At = Warp(stamp.At)
			}).ToList();
		}
		Words WarpWords(Words words)
		{
			return words with
			{
				Segments = words.Segments.Select(delegate(Segment segment)
				{
					Segment result = segment;
					InkRange? written = segment.Written;
					InkRange? written2;
					if (written.HasValue)
					{
						InkRange valueOrDefault5 = written.GetValueOrDefault();
						written2 = WarpRange(valueOrDefault5);
					}
					else
					{
						written2 = null;
					}
					result.Written = written2;
					written = segment.Struck;
					InkRange? struck;
					if (written.HasValue)
					{
						InkRange valueOrDefault6 = written.GetValueOrDefault();
						struck = WarpRange(valueOrDefault6);
					}
					else
					{
						struck = null;
					}
					result.Struck = struck;
					double? vanishesAt3 = segment.VanishesAt;
					double? vanishesAt4;
					if (vanishesAt3.HasValue)
					{
						double valueOrDefault7 = vanishesAt3.GetValueOrDefault();
						vanishesAt4 = Warp(valueOrDefault7);
					}
					else
					{
						vanishesAt4 = null;
					}
					result.VanishesAt = vanishesAt4;
					return result;
				}).ToList()
			};
		}
	}

	public double KnownSince(double time)
	{
		for (int num = FocusKnownSince.Count - 1; num >= 0; num--)
		{
			if (FocusKnownSince[num].From <= time + 1E-09)
			{
				return FocusKnownSince[num].Since;
			}
		}
		return double.NegativeInfinity;
	}

	public static bool IsSilentPageName(string name, int pageIndex)
	{
		if (pageIndex == 0)
		{
			return ClickyInkParser.Emphasis.Strip(name).TrimWs().Lowered() == "in short";
		}
		return false;
	}

	public List<int> PageIndices(double clock)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < Pages.Count; i++)
		{
			Page page = Pages[i];
			if (clock >= page.LandsAt && page.Blocks.Any((Block b) => b.IsPresent(clock)))
			{
				list.Add(i);
			}
		}
		return list;
	}

	public static string PageFocusID(string pageId)
	{
		return "page:" + pageId;
	}

	public List<PenFocus> PenFocusList()
	{
		List<(double Start, double End, string PlaceId, string? ItemId, bool Lands)> marks = new List<(double, double, string, string, bool)>();
		AddWords(Title, "title");
		foreach (Page page in Pages)
		{
			int count = marks.Count;
			AddWords(page.Name, PageFocusID(page.Id));
			for (int i = count; i < marks.Count; i++)
			{
				if (marks[i].Start < page.LandsAt + 1.0)
				{
					List<(double Start, double End, string PlaceId, string? ItemId, bool Lands)> list = marks;
					int index = i;
					(double, double, string, string, bool) value = marks[i];
					value.Item5 = true;
					list[index] = value;
				}
			}
			foreach (Block block in page.Blocks)
			{
				marks.Add((block.LandsAt, block.LandsAt + 0.4, block.Id, null, true));
				foreach (double penArrival in block.PenArrivals)
				{
					(string, bool)? tuple = FirstChangedRow(block, penArrival, 1.2000000000000002);
					marks.Add((penArrival, penArrival + 0.2, block.Id, tuple?.Item1, tuple?.Item2 ?? false));
				}
				foreach (Stamp<ClickyInkDocument.Block> item in block.Stamps.Skip(1))
				{
					(string, bool)? tuple2 = FirstChangedRow(block, item.At, 0.3);
					marks.Add((item.At, item.At + 0.2, block.Id, tuple2?.Item1, tuple2?.Item2 ?? false));
				}
				double? goneAt = block.GoneAt;
				if (goneAt.HasValue)
				{
					double valueOrDefault = goneAt.GetValueOrDefault();
					marks.Add((valueOrDefault, valueOrDefault + 0.3, block.Id, null, false));
				}
				AddWords(block.Words, block.Id);
				foreach (Item item2 in block.Items)
				{
					marks.Add((item2.LandsAt, item2.LandsAt + 0.1, block.Id, item2.Id, true));
					foreach (Stamp<ClickyInkDocument.Item> item3 in item2.Stamps.Skip(1))
					{
						marks.Add((item3.At, item3.At + 0.3, block.Id, item2.Id, false));
					}
					goneAt = item2.GoneAt;
					if (goneAt.HasValue)
					{
						double valueOrDefault2 = goneAt.GetValueOrDefault();
						marks.Add((valueOrDefault2, valueOrDefault2 + 0.3, block.Id, item2.Id, false));
					}
					AddWords(item2.Words, block.Id, item2.Id);
					foreach (Words cell in item2.Cells)
					{
						AddWords(cell, block.Id, item2.Id);
					}
					AddWords(item2.Detail, block.Id, item2.Id);
				}
			}
		}
		List<(double, double, string, string, bool)> list2 = (from m in marks
			orderby m.Start, m.End
			select m).ToList();
		List<PenFocus> list3 = new List<PenFocus>();
		foreach (var item4 in list2)
		{
			if (list3.Count > 0)
			{
				if (list3[list3.Count - 1].PlaceID == item4.Item3)
				{
					if (list3[list3.Count - 1].ItemID == item4.Item4)
					{
						PenFocus value2 = list3[list3.Count - 1];
						value2.Until = Math.Max(value2.Until, item4.Item2);
						list3[list3.Count - 1] = value2;
						continue;
					}
				}
			}
			list3.Add(new PenFocus(item4.Item1, item4.Item2, item4.Item3, item4.Item4)
			{
				KnownSince = KnownSince(item4.Item1),
				IsLanding = item4.Item5
			});
		}
		return list3;
		void AddWords(Words? words, string placeId, string? itemId = null)
		{
			if ((object)words == null)
			{
				return;
			}
			foreach (Segment segment in words.Segments)
			{
				InkRange? written = segment.Written;
				if (written.HasValue)
				{
					InkRange valueOrDefault3 = written.GetValueOrDefault();
					if (valueOrDefault3.UpperBound > valueOrDefault3.LowerBound)
					{
						marks.Add((valueOrDefault3.LowerBound, valueOrDefault3.UpperBound, placeId, itemId, false));
					}
				}
				written = segment.Struck;
				if (written.HasValue)
				{
					InkRange valueOrDefault4 = written.GetValueOrDefault();
					marks.Add((valueOrDefault4.LowerBound, valueOrDefault4.UpperBound, placeId, itemId, false));
				}
			}
		}
	}

	public static (string Id, bool IsNew)? FirstChangedRow(Block block, double time, double window)
	{
		if (block.Items.Count == 0)
		{
			return null;
		}
		InkRange inkRange = new InkRange(time - 0.001, time + window);
		if (Starts(block.Words).Any(((InkRange)inkRange).Contains))
		{
			return null;
		}
		(double, string)? tuple = null;
		foreach (Item item2 in block.Items)
		{
			List<double> list = Starts(item2.Words).Concat(Starts(item2.Detail)).Concat(item2.Cells.SelectMany(Starts)).ToList();
			list.Add(item2.LandsAt);
			list.AddRange(from s in item2.Stamps.Skip(1)
				select s.At);
			double? goneAt = item2.GoneAt;
			if (goneAt.HasValue)
			{
				double valueOrDefault = goneAt.GetValueOrDefault();
				list.Add(valueOrDefault);
			}
			List<double> list2 = list.Where(((InkRange)inkRange).Contains).ToList();
			if (list2.Count != 0)
			{
				double num = list2.Min();
				if (num < (tuple?.Item1 ?? double.PositiveInfinity))
				{
					tuple = (num, item2.Id);
				}
			}
		}
		if (tuple.HasValue)
		{
			(double At, string Id) f = tuple.GetValueOrDefault();
			Item item = block.Items.FirstOrDefault((Item i) => i.Id == f.Id);
			if (item == null)
			{
				return null;
			}
			return (item.Id, item.LandsAt >= time - 0.001);
		}
		return null;
		[IteratorStateMachine(typeof(_003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed))]
		static IEnumerable<double> Starts(Words? words)
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new _003C_003CFirstChangedRow_003Eg__Starts_007C76_0_003Ed(-2)
			{
				_003C_003E3__words = words
			};
		}
	}
}
