using System.Collections.Generic;

namespace HeyClicky.App.Documents;

public sealed record MarkdownViewerTableContent(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows, string PlainText);
