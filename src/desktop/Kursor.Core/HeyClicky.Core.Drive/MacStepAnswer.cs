using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Drive;

public abstract record MacStepAnswer
{
	public sealed record Drove(string Report) : MacStepAnswer();

	public sealed record Declined : MacStepAnswer;

	public sealed record NoAnswer : MacStepAnswer;

	public sealed record NotYet : MacStepAnswer;

	public sealed record BeingDriven : MacStepAnswer;

	public sealed record CouldNotStart : MacStepAnswer;

	public static readonly MacStepAnswer DeclinedAnswer = new Declined();

	public static readonly MacStepAnswer NoAnswerAnswer = new NoAnswer();

	public static readonly MacStepAnswer NotYetAnswer = new NotYet();

	public static readonly MacStepAnswer BeingDrivenAnswer = new BeingDriven();

	public static readonly MacStepAnswer CouldNotStartAnswer = new CouldNotStart();

	[CompilerGenerated]
	protected MacStepAnswer(MacStepAnswer original)
	{
	}

	protected MacStepAnswer()
	{
	}
}
