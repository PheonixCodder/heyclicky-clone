using HeyClicky.Agents.Jobs;

namespace HeyClicky.App.Dock;

public readonly record struct DockWindowKey(string Key, string RailId, string? ClickySlug, ClickyJob? Job)
{
	public bool IsJob => (object)Job != null;
}
