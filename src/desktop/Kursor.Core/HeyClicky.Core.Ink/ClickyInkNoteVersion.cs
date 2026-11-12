using System;

namespace HeyClicky.Core.Ink;

public sealed record ClickyInkNoteVersion(DateTimeOffset At, string Text, bool IsEdit, bool? IsPartial = null, bool? StartsNewEdit = null);
