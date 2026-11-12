namespace HeyClicky.Core.Documents.Markdown;

public sealed record MarkdownViewerListItem(int IndentLevel, string? OrdinalLabel, bool? CheckboxState, string InlineMarkdown);
