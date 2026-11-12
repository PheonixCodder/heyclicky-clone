using System.Collections.Generic;

namespace HeyClicky.Core.Documents.Markdown;

public abstract record MarkdownViewerBlock
{
	public sealed record Heading(int Level, string InlineMarkdown) : MarkdownViewerBlock();

	public sealed record Paragraph(string InlineMarkdown) : MarkdownViewerBlock();

	public sealed record CodeBlock(string? Language, string Code) : MarkdownViewerBlock();

	public sealed record Blockquote(IReadOnlyList<MarkdownViewerBlock> ChildBlocks) : MarkdownViewerBlock();

	public sealed record List(IReadOnlyList<MarkdownViewerListItem> Items) : MarkdownViewerBlock();

	public sealed record Table(IReadOnlyList<string> HeaderCells, IReadOnlyList<IReadOnlyList<string>> Rows) : MarkdownViewerBlock();

	public sealed record HorizontalRule : MarkdownViewerBlock;

	public sealed record Image(string AlternateText, string Source) : MarkdownViewerBlock();
}
