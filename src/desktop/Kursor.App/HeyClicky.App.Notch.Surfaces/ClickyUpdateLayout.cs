using System;
using System.Collections.Generic;
using System.Linq;
using HeyClicky.App.Design;
using HeyClicky.App.Notch.Peek;
using HeyClicky.Core.Notch;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Text;

namespace HeyClicky.App.Notch.Surfaces;

public static class ClickyUpdateLayout
{
	public const double Width = 560.0;

	public const double EdgeGap = 20.0;

	public const double SideInset = 12.0;

	public const double BottomInset = 14.0;

	public const double ContentTopGap = 4.0;

	public const double ButtonHeight = 34.0;

	public const int DetailLineLimit = 4;

	public const int SummaryLineLimit = 3;

	public const double TitleFontSize = 14.0;

	public const double DetailFontSize = 11.0;

	private const double TitleToDetailGap = 3.0;

	private const double WordsToButtonsGap = 9.0;

	private const double IconSize = 32.0;

	public const double PreviewGap = 14.0;

	public const double PreviewMaxWidth = 132.0;

	private static double? stampHeight;

	private static TextBlock? measurer;

	private static readonly HashSet<string> IngWordsThatAreThings = new HashSet<string>
	{
		"morning", "evening", "meeting", "wedding", "building", "spring", "string", "thing", "things", "ceiling",
		"pricing", "marketing", "shopping", "booking", "bookings", "listing", "listings", "clothing", "housing", "training",
		"funding", "hiring", "onboarding", "branding", "setting", "settings", "earnings", "savings", "ranking", "rankings",
		"landing", "parking", "holdings", "opening", "openings", "reading", "running", "spending", "billing", "shipping",
		"packing", "trending", "breaking", "upcoming", "outstanding", "pending"
	};

	private static double DetailTextWidth => 458.0;

	private static double DetailTextWidthBesidePreview => DetailTextWidth - 132.0 - 14.0;

	public static double ContentTopPadding => 28.0;

	public static double ContentHeight(NotchUpdateKind kind, string? detail, bool hasPreview = false)
	{
		double num = LineHeight(11.0, FontWeights.Normal);
		if (kind != NotchUpdateKind.Done && detail != null)
		{
			int num2 = ((detail.Length == 0) ? 1 : Math.Min(4, MeasuredLineCount(detail, 11.0, DetailTextWidth)));
			double val = LineHeight(14.0, FontWeights.SemiBold) + 3.0 + (double)num2 * num;
			return Math.Ceiling(Math.Max(32.0, val)) + 9.0 + 34.0;
		}
		string text = (string.IsNullOrEmpty(detail) ? null : detail);
		int num3 = ((text == null) ? 1 : Math.Min(3, MeasuredLineCount(text, 11.0, hasPreview ? DetailTextWidthBesidePreview : DetailTextWidth)));
		double val2 = FinishedTitleRowHeight() + ((text == null) ? 1.0 : 3.0) + (double)num3 * num;
		return Math.Ceiling(Math.Max(32.0, val2)) + ((text == null) ? 12.0 : 9.0) + 34.0;
	}

	private static double FinishedTitleRowHeight()
	{
		double? num = stampHeight;
		if (!num.HasValue)
		{
			FrameworkElement frameworkElement = ChatStamp.Create(ChatStamp.Status.Ready, "done", 0);
			frameworkElement.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			stampHeight = Math.Ceiling(frameworkElement.DesiredSize.Height);
		}
		return Math.Max(LineHeight(14.0, FontWeights.SemiBold), stampHeight.Value);
	}

	public static double PanelHeight(NotchUpdateKind kind, string? detail, bool hasPreview)
	{
		return 34.0 + ContentHeight(kind, detail, hasPreview) + 20.0;
	}

	public static (double W, double H) PreviewSize(string? detail)
	{
		double num = ContentHeight(NotchUpdateKind.Done, detail, hasPreview: true);
		return (W: Math.Min(132.0, Math.Round(num * 1.3)), H: num);
	}

	private static double LineHeight(double size, FontWeight weight)
	{
		if ((object)measurer == null)
		{
			measurer = new TextBlock
			{
				IsTextScaleFactorEnabled = false,
				TextWrapping = TextWrapping.Wrap
			};
		}
		measurer.FontSize = size;
		measurer.FontWeight = weight;
		measurer.FontFamily = DS.Fonts.For(size);
		measurer.Text = "Ag";
		measurer.Width = double.NaN;
		measurer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		return Math.Ceiling(measurer.DesiredSize.Height);
	}

	private static int MeasuredLineCount(string text, double size, double width)
	{
		double num = LineHeight(size, FontWeights.Normal);
		measurer.FontWeight = FontWeights.Normal;
		measurer.Text = text;
		measurer.Width = width;
		measurer.Measure(new Size(width, double.PositiveInfinity));
		return Math.Max(1, (int)Math.Round(measurer.DesiredSize.Height / num));
	}

	public static string? Detail(NotchUpdateKind kind, NotchUpdateContent content)
	{
		if (kind != NotchUpdateKind.Done)
		{
			return content.Request?.Detail;
		}
		return SummaryText(content);
	}

	public static string? SummaryText(NotchUpdateContent content)
	{
		string summary = content.Summary;
		if (summary == null)
		{
			return null;
		}
		string text = string.Join(" ", summary.Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)).Replace("**", "").Trim();
		if (text.Length != 0)
		{
			return text;
		}
		return null;
	}

	public static string FinishedTitle(NotchUpdateContent content, string? clickyName)
	{
		NotchUpdateReadyOutput readyOutput = content.ReadyOutput;
		if ((object)readyOutput == null)
		{
			goto IL_005f;
		}
		bool flag;
		if (!readyOutput.IsLink)
		{
			if (readyOutput.OpensFullReplyOnly)
			{
				string buttonTitle = readyOutput.ButtonTitle;
				if (buttonTitle == null || buttonTitle.Length <= 0)
				{
					goto IL_005f;
				}
			}
			flag = (object)content.ReadyOutput != null;
		}
		else
		{
			if (content.RoutineTitle == null)
			{
				string linkTitle = readyOutput.LinkTitle;
				if (linkTitle != null)
				{
					return linkTitle;
				}
			}
			flag = true;
		}
		goto IL_0061;
		IL_005f:
		flag = false;
		goto IL_0061;
		IL_0061:
		string text = (content.RoutineTitle ?? content.Title).Trim();
		if (text.Length == 0 || (content.RoutineTitle == null && text == clickyName))
		{
			string text2 = clickyName ?? "Your job";
			if (!flag)
			{
				return text2 + " is done";
			}
			return text2 + " has something ready";
		}
		string text3 = text.Split(' ').FirstOrDefault() ?? "";
		if (text3.Length > 4 && text3.EndsWith("ing", StringComparison.Ordinal) && char.IsUpper(text3[0]))
		{
			string buttonTitle = text3;
			if (buttonTitle.Substring(1, buttonTitle.Length - 1).All(char.IsLower) && !IngWordsThatAreThings.Contains(text3.ToLowerInvariant()))
			{
				ReadOnlySpan<char> readOnlySpan = "Finished ";
				ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(char.ToLowerInvariant(text[0]));
				buttonTitle = text;
				return string.Concat(readOnlySpan, readOnlySpan2, buttonTitle.Substring(1, buttonTitle.Length - 1));
			}
		}
		if (!flag)
		{
			return text + " is done";
		}
		return text + " is ready";
	}
}
