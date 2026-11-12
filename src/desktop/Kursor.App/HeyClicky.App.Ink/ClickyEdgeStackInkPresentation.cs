using System.Runtime.CompilerServices;

namespace HeyClicky.App.Ink;

public abstract record ClickyEdgeStackInkPresentation
{
	public sealed record Rest : ClickyEdgeStackInkPresentation;

	public sealed record Fanned(bool IsFront) : ClickyEdgeStackInkPresentation();

	public sealed record Pulled : ClickyEdgeStackInkPresentation;

	public bool LetsThePenMove
	{
		get
		{
			if (!(this is Pulled))
			{
				if (this is Fanned fanned)
				{
					return fanned.IsFront;
				}
				return false;
			}
			return true;
		}
	}

	public static readonly ClickyEdgeStackInkPresentation AtRest = new Rest();

	public static readonly ClickyEdgeStackInkPresentation PulledOut = new Pulled();

	public static ClickyEdgeStackInkPresentation InFan(bool isFront)
	{
		return new Fanned(isFront);
	}

	[CompilerGenerated]
	protected ClickyEdgeStackInkPresentation(ClickyEdgeStackInkPresentation original)
	{
	}

	protected ClickyEdgeStackInkPresentation()
	{
	}
}
