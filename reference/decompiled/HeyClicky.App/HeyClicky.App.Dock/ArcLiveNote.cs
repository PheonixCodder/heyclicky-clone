using HeyClicky.App.Ink;
using HeyClicky.Core.Ink;

namespace HeyClicky.App.Dock;

internal sealed record ArcLiveNote(string NotePath, ClickyInkCardSubject Subject, ClickyInkNoteTarget Target, ClickyHomeInkNote Ink);
