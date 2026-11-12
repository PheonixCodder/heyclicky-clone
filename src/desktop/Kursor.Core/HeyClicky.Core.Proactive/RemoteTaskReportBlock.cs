using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public abstract record RemoteTaskReportBlock
{
	public sealed record Bullet(string Title, IReadOnlyList<string> DetailLines) : RemoteTaskReportBlock()
	{
		public IReadOnlyList<string> DetailLines { get; set; } = DetailLines;

		internal Bullet _003CClone_003E_0024() => this with { };
	}

	public sealed record Paragraph(string Text) : RemoteTaskReportBlock();

	public sealed record Spacer : RemoteTaskReportBlock;

	public static IReadOnlyList<RemoteTaskReportBlock> Parse(string summary)
	{
		List<RemoteTaskReportBlock> blocks = new List<RemoteTaskReportBlock>();
		Bullet current = null;
		List<string> paragraph = new List<string>();
		string[] array = summary.Replace("\r", "").Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].TrimEnd();
			string text2 = text.TrimStart();
			if (text2.Length == 0)
			{
				FlushParagraph();
				FlushBullet();
				if (blocks.Count > 0)
				{
					List<RemoteTaskReportBlock> list = blocks;
					if (!(list[list.Count - 1] is Spacer))
					{
						blocks.Add(new Spacer());
					}
				}
				continue;
			}
			bool flag = text2.StartsWith("- ") || text2.StartsWith("* ") || text2.StartsWith("• ");
			bool flag2 = text.Length - text2.Length >= 2;
			if (flag && !flag2)
			{
				FlushParagraph();
				FlushBullet();
				string text3 = text2;
				current = new Bullet(text3.Substring(2, text3.Length - 2).Trim(), Array.Empty<string>());
			}
			else if ((object)current != null)
			{
				Bullet bullet = current._003CClone_003E_0024();
				IReadOnlyList<string> detailLines = current.DetailLines;
				string element;
				if (!flag)
				{
					element = text2;
				}
				else
				{
					string text3 = text2;
					element = text3.Substring(2, text3.Length - 2).Trim();
				}
				bullet.DetailLines = detailLines.Append(element).ToList();
				current = bullet;
			}
			else
			{
				paragraph.Add(text2);
			}
		}
		FlushParagraph();
		FlushBullet();
		while (blocks.Count > 0)
		{
			List<RemoteTaskReportBlock> list2 = blocks;
			if (!(list2[list2.Count - 1] is Spacer))
			{
				break;
			}
			blocks.RemoveAt(blocks.Count - 1);
		}
		return blocks;
		void FlushBullet()
		{
			if ((object)current != null)
			{
				blocks.Add(current);
				current = null;
			}
		}
		void FlushParagraph()
		{
			if (paragraph.Count > 0)
			{
				blocks.Add(new Paragraph(string.Join(" ", paragraph)));
				paragraph.Clear();
			}
		}
	}
}
