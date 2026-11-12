using System;

namespace HeyClicky.Core.Notch;

public sealed record NotchUpdate(string RailKey, NotchUpdateKind Kind)
{
	public Guid Id { get; } = Guid.NewGuid();

	public bool ClosesOnItsOwn => Kind == NotchUpdateKind.Done;
}
