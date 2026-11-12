using System;

namespace HeyClicky.App.Notch.Peek;

public static class ChatRowLayout
{
	public const double IconSize = 22.0;

	public const double ContentLeadingInset = 30.0;

	public const double ReplyBubbleMaximumWidth = 420.0;

	public const double MessageMaximumWidth = 390.0;

	public const double MessageFreeSide = 56.0;

	public const double CardWidth = 96.0;

	public const double CardCornerRadius = 6.0;

	public static double CardHeight => Math.Ceiling(61.020242914979754);
}
