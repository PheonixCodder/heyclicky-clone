namespace HeyClicky.Core.Documents.Markdown;

public sealed record MarkdownInlineRun(string Text, bool IsStrong = false, bool IsEmphasized = false, bool IsCode = false, bool IsStrikethrough = false, string? Link = null);
