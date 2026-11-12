using System;

namespace HeyClicky.App.Notch.Peek;

public sealed record NotchUpdateReadyOutput(string ButtonTitle, string? Glyph, bool OpensFullReplyOnly, bool IsLink, string? LinkTitle, Action Open);
