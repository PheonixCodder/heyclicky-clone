using System;
using System.Collections.Generic;

namespace HeyClicky.App.Portraits;

public sealed record RosterClicky(string Slug, string Name, string RoleText, string OneLiner, IReadOnlyList<string> IntroMessages, IReadOnlyList<string> SuggestedAsks, string? RevealLine, DateTimeOffset CreatedAt);
