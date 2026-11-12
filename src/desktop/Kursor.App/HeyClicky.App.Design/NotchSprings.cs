namespace HeyClicky.App.Design;

public static class NotchSprings
{
	public static readonly SwiftSpring Open = new SwiftSpring(0.42, 0.82);

	public static readonly SwiftSpring Close = new SwiftSpring(0.45, 1.0);

	public static readonly SwiftSpring Content = new SwiftSpring(0.34, 0.86);

	public static readonly SwiftSpring TextInputOpen = new SwiftSpring(0.2, 0.9);

	public static readonly SwiftSpring Control = new SwiftSpring(0.18, 0.84);

	public const float ContentTransitionScale = 0.92f;
}
