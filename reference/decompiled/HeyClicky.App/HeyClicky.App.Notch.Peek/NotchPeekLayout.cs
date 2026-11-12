using System;
using HeyClicky.Core.Notch;

namespace HeyClicky.App.Notch.Peek;

public static class NotchPeekLayout
{
	public const double ContentWidth = 500.0;

	public const double EdgeInset = 20.0;

	public const double BottomEdgeInset = 18.0;

	public const double NotchContentHorizontalPadding = 18.0;

	public const double NotchSideContentInset = 8.0;

	public const double NotchBottomContentInset = 6.0;

	public const double SidePadding = 12.0;

	public const double BottomPadding = 12.0;

	public const double PanelWidth = 536.0;

	public const double MenuBarBandHeight = 8.0;

	public const double HeaderRowHeight = 30.0;

	public const double HeaderRowTopPadding = 4.0;

	public const double BlackToGreyFadeHeight = 56.0;

	private const double ArcCardWidth = 494.0;

	private const double ArcCardHeight = 314.0;

	private const double ArcFrontScale = 0.7;

	public const double ArcCardTopGap = 20.0;

	public const double ArcCardBottomPadding = 22.0;

	public const double ArcCardSidePadding = 28.0;

	public const double FooterPillHeight = 32.0;

	public const double FooterHeight = 44.0;

	public const double GreetingBodyHeight = 64.0;

	public const double FullReplyMinimumBodyHeight = 140.0;

	public const double SettingsPanelWidth = 820.0;

	public const double SettingsBodyHeight = 610.0;

	public const double DocumentBodyHeight = 640.0;

	public static readonly TimeSpan DocumentMountDelay = TimeSpan.FromMilliseconds(380L, 0L);

	public static readonly TimeSpan SettingsSettleDelay = TimeSpan.FromMilliseconds(360L, 0L);

	public const double PillTopInset = 10.0;

	public const double TranscriptFadeHeight = 28.0;

	public const double PillHeight = 42.0;

	public const double CollapsedTextBoxWidth = 150.0;

	public const double ComposerBottomPadding = 12.0;

	public const double ComposerHeight = 64.0;

	public const double TopFadeHeight = 52.0;

	public const double VoicePillHeight = 24.0;

	public const double VoicePillScale = 0.86;

	private static double FallbackFrontCardHeight => 219.79999999999998;

	public static double BodyHeight => Math.Round(84.0 + FallbackFrontCardHeight + 22.0 + 44.0 - 8.0);

	public static double ChatBodyFrameHeight => BodyHeight - 6.0;

	public static double ChatPanelHeight => 38.0 + BodyHeight;

	public static (double W, double H) PanelSize(NotchTabs tabs, ChatHandOffs? handOffs, (double W, double H)? frontCardSize)
	{
		double num = Math.Max(536.0, 300.0);
		switch (tabs.Selected)
		{
		case NotchTab.Chat:
		{
			string expandedReplyEntryId = tabs.ExpandedReplyEntryId;
			if (expandedReplyEntryId != null)
			{
				double? fullReplyContentHeight = tabs.FullReplyContentHeight;
				if (fullReplyContentHeight.HasValue)
				{
					double valueOrDefault2 = fullReplyContentHeight.GetValueOrDefault();
					double num3 = 34.0;
					double val = num3 + valueOrDefault2 + FullReplyFooter(handOffs, expandedReplyEntryId) + 6.0;
					double val2 = 678.0;
					return (W: ChatReplyPreview.UsesWideLayout(ChatReplyPreview.FullReplyMarkdown(handOffs?.Entry(expandedReplyEntryId))) ? Math.Max(num, 820.0) : num, H: Math.Min(val2, Math.Max(num3 + 140.0, val)));
				}
			}
			return (W: Math.Max(num, NotchChatSize.ContentWidth + 36.0), H: 38.0 + NotchChatSize.BodyHeight);
		}
		case NotchTab.Document:
			return (W: Math.Max(num, 820.0), H: 678.0);
		case NotchTab.Settings:
			return (W: Math.Max(num, 820.0), H: 648.0);
		default:
		{
			double num2 = 138.0;
			if (frontCardSize.HasValue)
			{
				(double, double) valueOrDefault = frontCardSize.GetValueOrDefault();
				num = Math.Max(num, valueOrDefault.Item1 + 56.0);
				num2 += 20.0 + valueOrDefault.Item2 + 22.0;
			}
			else
			{
				num2 += 20.0;
			}
			return (W: num, H: num2);
		}
		}
	}

	public static double FullReplyFooter(ChatHandOffs? handOffs, string entryId)
	{
		return 64.0;
	}

	public static double FullReplyContentWidth(string? markdown)
	{
		if (!ChatReplyPreview.UsesWideLayout(markdown))
		{
			return 500.0;
		}
		return 784.0;
	}
}
