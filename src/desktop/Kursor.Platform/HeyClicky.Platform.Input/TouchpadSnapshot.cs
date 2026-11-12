namespace HeyClicky.Platform.Input;

public readonly record struct TouchpadSnapshot(bool Present, int Fingers, long Touch, double DownAt, double LiftedAt)
{
	public bool IsTouching => Fingers > 0;

	public static readonly TouchpadSnapshot None;
}
