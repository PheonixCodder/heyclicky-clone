using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace HeyClicky.Platform.Win32;

public static class HighlightTextHarvester
{
	private sealed record Harvested(string RoleNoun, string? Label, string? Value, string? Url, string? LinkUrl);

	private const int MaximumElements = 6;

	private const int MaximumValueLength = 120;

	private const int MaximumParentWalkDepth = 5;

	private const double DwellSampleSlackPoints = 8.0;

	private static readonly int OwnProcessId = Environment.ProcessId;

	public static Task<string?> SummarizeAsync(IReadOnlyList<HighlightStroke> strokes, IReadOnlyList<DisplayInfo> displays, CancellationToken ct = default(CancellationToken))
	{
		List<PointD> list = strokes.Where((HighlightStroke s) => s.PointsPx.Count >= 2).SelectMany((HighlightStroke s) => s.PointsPx).ToList();
		if (list.Count < 2)
		{
			return Task.FromResult<string>(null);
		}
		double num = list.Min((PointD p) => p.X);
		double num2 = list.Min((PointD p) => p.Y);
		double num3 = list.Max((PointD p) => p.X);
		double num4 = list.Max((PointD p) => p.Y);
		RectD region = new RectD(num, num2, num3 - num, num4 - num2);
		if (region.Width <= 1.0 || region.Height <= 1.0)
		{
			return Task.FromResult<string>(null);
		}
		DisplayInfo displayInfo = displays.FirstOrDefault((DisplayInfo d) => d.BoundsPx.Contains(new PointD(region.X + region.Width / 2.0, region.Y + region.Height / 2.0))) ?? displays.FirstOrDefault();
		if ((object)displayInfo != null)
		{
			double num5 = displayInfo.BoundsPx.Width * displayInfo.BoundsPx.Height;
			if (num5 > 0.0 && region.Width * region.Height / num5 > 0.8)
			{
				return Task.FromResult<string>(null);
			}
		}
		double num6 = displayInfo?.Scale ?? 1.0;
		PointD? dwell = strokes.SelectMany((HighlightStroke s) => s.DwellsPx).Cast<PointD?>().FirstOrDefault();
		List<PointD> samples = SamplePoints(region, dwell, 8.0 * num6);
		return UIAutomationHelpers.RunAsync((IUIAutomation automation) => Harvest(automation, samples, ct), ct);
	}

	internal static List<PointD> SamplePoints(RectD region, PointD? dwell, double dwellSlack)
	{
		List<PointD> list = new List<PointD>(11);
		if (dwell.HasValue)
		{
			PointD valueOrDefault = dwell.GetValueOrDefault();
			if (valueOrDefault.X >= region.X - dwellSlack && valueOrDefault.X <= region.Right + dwellSlack && valueOrDefault.Y >= region.Y - dwellSlack && valueOrDefault.Y <= region.Bottom + dwellSlack)
			{
				list.Add(valueOrDefault);
			}
		}
		list.Add(new PointD(region.X + region.Width / 2.0, region.Y + region.Height / 2.0));
		double[] array = new double[3] { 0.25, 0.5, 0.75 };
		foreach (double num in array)
		{
			double[] array2 = new double[3] { 0.25, 0.5, 0.75 };
			foreach (double num2 in array2)
			{
				list.Add(new PointD(region.X + region.Width * num, region.Y + region.Height * num2));
			}
		}
		return list;
	}

	private static string? Harvest(IUIAutomation automation, List<PointD> samples, CancellationToken ct)
	{
		IUIAutomationCacheRequest iUIAutomationCacheRequest = automation.CreateCacheRequest();
		UIA_PROPERTY_ID[] array = new UIA_PROPERTY_ID[7]
		{
			UIA_PROPERTY_ID.UIA_NamePropertyId,
			UIA_PROPERTY_ID.UIA_ControlTypePropertyId,
			UIA_PROPERTY_ID.UIA_ProcessIdPropertyId,
			UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId,
			UIA_PROPERTY_ID.UIA_ValueValuePropertyId,
			UIA_PROPERTY_ID.UIA_IsPasswordPropertyId,
			UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId
		};
		foreach (UIA_PROPERTY_ID propertyId in array)
		{
			iUIAutomationCacheRequest.AddProperty(propertyId);
		}
		List<Harvested> list = new List<Harvested>();
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		long num = Environment.TickCount64 + 800;
		foreach (PointD sample in samples)
		{
			if (ct.IsCancellationRequested || Environment.TickCount64 > num)
			{
				break;
			}
			IUIAutomationElement iUIAutomationElement;
			try
			{
				iUIAutomationElement = automation.ElementFromPointBuildCache(new Point((int)sample.X, (int)sample.Y), iUIAutomationCacheRequest);
			}
			catch (COMException)
			{
				continue;
			}
			if (iUIAutomationElement == null || CachedInt(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ProcessIdPropertyId) == OwnProcessId)
			{
				continue;
			}
			string item = $"{CachedInt(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ControlTypePropertyId)}|{Cached(iUIAutomationElement, UIA_PROPERTY_ID.UIA_NamePropertyId)}|{CachedRectKey(iUIAutomationElement)}";
			object cachedPropertyValue = iUIAutomationElement.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId);
			bool flag = cachedPropertyValue is bool && (bool)cachedPropertyValue;
			if (!hashSet.Add(item) && !flag)
			{
				continue;
			}
			Harvested harvested = Describe(automation, iUIAutomationElement, sample, iUIAutomationCacheRequest);
			if ((object)harvested == null)
			{
				continue;
			}
			string item2 = $"{harvested.RoleNoun}|{harvested.Label}|{harvested.Url}|{harvested.LinkUrl}|{harvested.Value}";
			if (hashSet2.Add(item2))
			{
				list.Add(harvested);
				if (list.Count >= 6)
				{
					break;
				}
			}
		}
		if (list.Count != 0)
		{
			return ComposeSummary(list);
		}
		return null;
	}

	private static Harvested? Describe(IUIAutomation automation, IUIAutomationElement element, PointD point, IUIAutomationCacheRequest cache)
	{
		UIA_CONTROLTYPE_ID uIA_CONTROLTYPE_ID = (UIA_CONTROLTYPE_ID)CachedInt(element, UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
		object cachedPropertyValue = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
		bool flag = cachedPropertyValue is bool && (bool)cachedPropertyValue;
		string text = (flag ? null : Cached(element, UIA_PROPERTY_ID.UIA_ValueValuePropertyId));
		string text2 = ((uIA_CONTROLTYPE_ID == UIA_CONTROLTYPE_ID.UIA_HyperlinkControlTypeId) ? NormalizedUrl(text) : null);
		string text3 = NearestAncestorLinkUrl(automation, element, cache);
		string text4 = Truncate(Cached(element, UIA_PROPERTY_ID.UIA_NamePropertyId));
		string text5 = null;
		if (!flag)
		{
			cachedPropertyValue = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId);
			if (cachedPropertyValue is bool && (bool)cachedPropertyValue)
			{
				text5 = Truncate(LineAt(element, point));
			}
		}
		if (text5 == null && text2 == null)
		{
			text5 = Truncate(text);
		}
		if (text5 != null && text5 == text4)
		{
			text5 = null;
		}
		string text6 = text2 ?? text3;
		string linkUrl = ((text2 != null && text3 != null && text3 != text2) ? text3 : null);
		if (text4 == null && text5 == null && text6 == null)
		{
			return null;
		}
		return new Harvested(RoleNoun(uIA_CONTROLTYPE_ID), text4, text5, text6, linkUrl);
	}

	private static string? LineAt(IUIAutomationElement element, PointD point)
	{
		try
		{
			if (!(element.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId) is IUIAutomationTextPattern iUIAutomationTextPattern))
			{
				return null;
			}
			IUIAutomationTextRange iUIAutomationTextRange = iUIAutomationTextPattern.RangeFromPoint(new Point((int)point.X, (int)point.Y));
			if (iUIAutomationTextRange == null)
			{
				return null;
			}
			iUIAutomationTextRange.ExpandToEnclosingUnit(TextUnit.TextUnit_Line);
			return Text(iUIAutomationTextRange.GetText(240));
		}
		catch (COMException)
		{
			return null;
		}
		catch (InvalidCastException)
		{
			return null;
		}
	}

	private static string? NearestAncestorLinkUrl(IUIAutomation automation, IUIAutomationElement element, IUIAutomationCacheRequest cache)
	{
		try
		{
			IUIAutomationTreeWalker controlViewWalker = automation.ControlViewWalker;
			IUIAutomationElement parentElementBuildCache = controlViewWalker.GetParentElementBuildCache(element, cache);
			for (int i = 0; i < 5; i++)
			{
				if (parentElementBuildCache == null)
				{
					break;
				}
				if (CachedInt(parentElementBuildCache, UIA_PROPERTY_ID.UIA_ControlTypePropertyId) == 50005)
				{
					string text = NormalizedUrl(Cached(parentElementBuildCache, UIA_PROPERTY_ID.UIA_ValueValuePropertyId));
					if (text != null)
					{
						return text;
					}
				}
				parentElementBuildCache = controlViewWalker.GetParentElementBuildCache(parentElementBuildCache, cache);
			}
		}
		catch (COMException)
		{
		}
		return null;
	}

	private static string ComposeSummary(List<Harvested> elements)
	{
		IEnumerable<string> values = elements.Select(delegate(Harvested e)
		{
			string text = "a " + e.RoleNoun;
			if (!string.IsNullOrEmpty(e.Label))
			{
				text = text + " \"" + e.Label + "\"";
			}
			if (!string.IsNullOrEmpty(e.Value))
			{
				text = text + " (value: \"" + e.Value + "\")";
			}
			if (e.Url != null)
			{
				text = text + " → " + e.Url;
			}
			if (e.LinkUrl != null)
			{
				text = text + " (linked from " + e.LinkUrl + ")";
			}
			return text;
		});
		return "Accessibility data inside the highlighted region (use these exact strings/URLs when relevant, don't re-read them from the image): " + string.Join("; ", values) + ".";
	}

	internal static string RoleNoun(UIA_CONTROLTYPE_ID controlType)
	{
		switch (controlType)
		{
		case UIA_CONTROLTYPE_ID.UIA_HyperlinkControlTypeId:
			return "link";
		case UIA_CONTROLTYPE_ID.UIA_ButtonControlTypeId:
		case UIA_CONTROLTYPE_ID.UIA_SplitButtonControlTypeId:
			return "button";
		case UIA_CONTROLTYPE_ID.UIA_TextControlTypeId:
			return "text";
		case UIA_CONTROLTYPE_ID.UIA_ComboBoxControlTypeId:
		case UIA_CONTROLTYPE_ID.UIA_EditControlTypeId:
			return "text field";
		case UIA_CONTROLTYPE_ID.UIA_DocumentControlTypeId:
			return "document";
		case UIA_CONTROLTYPE_ID.UIA_ImageControlTypeId:
			return "image";
		case UIA_CONTROLTYPE_ID.UIA_CheckBoxControlTypeId:
			return "checkbox";
		case UIA_CONTROLTYPE_ID.UIA_RadioButtonControlTypeId:
			return "radio button";
		case UIA_CONTROLTYPE_ID.UIA_HeaderControlTypeId:
		case UIA_CONTROLTYPE_ID.UIA_HeaderItemControlTypeId:
			return "heading";
		case UIA_CONTROLTYPE_ID.UIA_DataItemControlTypeId:
			return "table cell";
		case UIA_CONTROLTYPE_ID.UIA_ListItemControlTypeId:
			return "list item";
		case UIA_CONTROLTYPE_ID.UIA_MenuItemControlTypeId:
			return "menu item";
		case UIA_CONTROLTYPE_ID.UIA_TabItemControlTypeId:
			return "tab";
		default:
			return "element";
		}
	}

	private static string? NormalizedUrl(string? raw)
	{
		string text = raw?.Trim();
		if (!string.IsNullOrEmpty(text) && text.Contains("://", StringComparison.Ordinal))
		{
			return text;
		}
		return null;
	}

	internal static string? Truncate(string? text)
	{
		if (text == null)
		{
			return null;
		}
		string text2 = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		if (text2.Length == 0)
		{
			return null;
		}
		if (text2.Length <= 120)
		{
			return text2;
		}
		return text2.Substring(0, 120) + "…";
	}

	private static string? Cached(IUIAutomationElement element, UIA_PROPERTY_ID property)
	{
		try
		{
			return (element.GetCachedPropertyValue(property) is string text && text.Trim().Length > 0) ? text.Trim() : null;
		}
		catch (COMException)
		{
			return null;
		}
	}

	private static int CachedInt(IUIAutomationElement element, UIA_PROPERTY_ID property)
	{
		try
		{
			return (element.GetCachedPropertyValue(property) is int num) ? num : 0;
		}
		catch (COMException)
		{
			return 0;
		}
	}

	private static string CachedRectKey(IUIAutomationElement element)
	{
		try
		{
			return (element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId) is double[] array && array.Length == 4) ? $"{array[0]:0},{array[1]:0},{array[2]:0},{array[3]:0}" : "";
		}
		catch (COMException)
		{
			return "";
		}
	}

	private unsafe static string Text(BSTR value)
	{
		if (value.Value == null)
		{
			return "";
		}
		try
		{
			return Marshal.PtrToStringBSTR((nint)value.Value) ?? "";
		}
		finally
		{
			Marshal.FreeBSTR((nint)value.Value);
		}
	}
}
