namespace HeyClicky.Platform.Overlay;

internal struct SpringState1D
{
	public double Value;

	public double Velocity;

	public void Advance(double target, double seconds, in OverlayCursorSpring spring)
	{
		(double, double) tuple = spring.ReleasedDisplacement(seconds);
		(double, double) tuple2 = spring.ReleasedVelocity(seconds);
		double num = Value - target;
		Value = target + num * tuple.Item1 + Velocity * tuple2.Item1;
		Velocity = num * tuple.Item2 + Velocity * tuple2.Item2;
	}
}
