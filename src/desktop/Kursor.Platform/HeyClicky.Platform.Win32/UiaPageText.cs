using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace HeyClicky.Platform.Win32;

public static class UiaPageText
{
	public sealed record PageText(string Text, bool IsTruncated);

	public sealed record FocusTarget(bool HasFocus, int ProcessId, bool IsPassword, bool IsAddressBar, string ControlType, string? AppName);

	private const int NodeCap = 3000;

	private static readonly TimeSpan WallClock = TimeSpan.FromSeconds(1.5);

	private static readonly string[] PasswordManagers = new string[6] { "1Password", "Bitwarden", "KeePass", "KeePassXC", "Dashlane", "LastPass" };

	private static readonly string[] PrivateBrowsingTitles = new string[3] { "InPrivate", "Incognito", "Private Browsing" };

	internal static bool IsSensitiveWindow(nint window)
	{
		if (window == 0)
		{
			return false;
		}
		try
		{
			ForegroundApp? app = ForegroundAppInfo.Capture(new HWND(window));
			if (app != null)
			{
				foreach (string pm in PasswordManagers)
				{
					if (app.ProcessName.Contains(pm, StringComparison.OrdinalIgnoreCase) || app.AppName.Contains(pm, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				foreach (string pb in PrivateBrowsingTitles)
				{
					if (app.WindowTitle.Contains(pb, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static bool IsPasswordElement(IUIAutomationElement? element)
	{
		if (element == null)
		{
			return false;
		}
		try
		{
			object val = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
			if (val is bool b && b)
			{
				return true;
			}
			return (bool)element.CurrentIsPassword;
		}
		catch
		{
			return false;
		}
	}

	public static Task<PageText?> ReadAsync(nint window, int maximumCharacters, CancellationToken ct = default(CancellationToken))
	{
		if (IsSensitiveWindow(window))
		{
			return Task.FromResult<PageText?>(null);
		}
		return UIAutomationHelpers.RunAsync((IUIAutomation automation) => Read(automation, window, maximumCharacters), ct);
	}

	private static PageText? Read(IUIAutomation automation, nint window, int maximumCharacters)
	{
		if (IsSensitiveWindow(window))
		{
			return null;
		}
		DateTime deadline = DateTime.UtcNow + WallClock;
		IUIAutomation2 iUIAutomation = automation as IUIAutomation2;
		uint transactionTimeout = 0u;
		if (iUIAutomation != null)
		{
			transactionTimeout = iUIAutomation.TransactionTimeout;
			iUIAutomation.TransactionTimeout = 1500u;
		}
		try
		{
			IUIAutomationElement iUIAutomationElement = automation.ElementFromHandle(new HWND(window));
			if (iUIAutomationElement == null)
			{
				return null;
			}
			IUIAutomationElement iUIAutomationElement2 = FindDocument(automation, iUIAutomationElement);
			if (iUIAutomationElement2 == null)
			{
				return null;
			}
			PageText pageText = ReadTextPattern(iUIAutomationElement2, maximumCharacters);
			if ((object)pageText != null)
			{
				return pageText;
			}
			return Walk(automation, iUIAutomationElement2, maximumCharacters, deadline);
		}
		finally
		{
			if (iUIAutomation != null)
			{
				iUIAutomation.TransactionTimeout = transactionTimeout;
			}
		}
	}

	private static IUIAutomationElement? FindDocument(IUIAutomation automation, IUIAutomationElement root)
	{
		IUIAutomationCondition condition = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ControlTypePropertyId, 50030);
		IUIAutomationElementArray iUIAutomationElementArray = root.FindAll(TreeScope.TreeScope_Descendants, condition);
		IUIAutomationElement iUIAutomationElement = null;
		double num = -1.0;
		for (int i = 0; i < (iUIAutomationElementArray?.Length ?? 0); i++)
		{
			IUIAutomationElement element = iUIAutomationElementArray.GetElement(i);
			RECT currentBoundingRectangle = element.CurrentBoundingRectangle;
			double num2 = (double)Math.Max(0, currentBoundingRectangle.right - currentBoundingRectangle.left) * (double)Math.Max(0, currentBoundingRectangle.bottom - currentBoundingRectangle.top);
			if (num2 > num)
			{
				iUIAutomationElement = element;
				num = num2;
			}
		}
		if (iUIAutomationElement != null)
		{
			return iUIAutomationElement;
		}
		IUIAutomationCondition condition2 = automation.CreateAndCondition(automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ControlTypePropertyId, 50004), automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId, true));
		return root.FindFirst(TreeScope.TreeScope_Descendants, condition2);
	}

	private static PageText? ReadTextPattern(IUIAutomationElement document, int maximumCharacters)
	{
		try
		{
			if (IsPasswordElement(document))
			{
				return null;
			}
			if (!(document.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId) is IUIAutomationTextPattern iUIAutomationTextPattern))
			{
				return null;
			}
			string text = iUIAutomationTextPattern.DocumentRange.GetText(maximumCharacters + 1).ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			bool flag = text.Length > maximumCharacters;
			return new PageText(Normalize(flag ? text.Substring(0, maximumCharacters) : text), flag);
		}
		catch (COMException)
		{
			return null;
		}
	}

	private static PageText? Walk(IUIAutomation automation, IUIAutomationElement document, int maximumCharacters, DateTime deadline)
	{
		IUIAutomationTreeWalker controlViewWalker = automation.ControlViewWalker;
		StringBuilder stringBuilder = new StringBuilder();
		Stack<IUIAutomationElement> stack = new Stack<IUIAutomationElement>();
		stack.Push(document);
		int num = 0;
		bool flag = false;
		while (stack.Count > 0)
		{
			if (DateTime.UtcNow > deadline || num >= 3000 || stringBuilder.Length >= maximumCharacters)
			{
				flag = true;
				break;
			}
			IUIAutomationElement iUIAutomationElement = stack.Pop();
			num++;
			try
			{
				if (IsPasswordElement(iUIAutomationElement))
				{
					continue;
				}
				UIA_CONTROLTYPE_ID currentControlType = iUIAutomationElement.CurrentControlType;
				if ((currentControlType != UIA_CONTROLTYPE_ID.UIA_HyperlinkControlTypeId && currentControlType != UIA_CONTROLTYPE_ID.UIA_TextControlTypeId) || 1 == 0)
				{
					goto IL_00be;
				}
				string text = iUIAutomationElement.CurrentName.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					stringBuilder.Append(text.Trim()).Append('\n');
				}
				if (currentControlType != UIA_CONTROLTYPE_ID.UIA_TextControlTypeId)
				{
					goto IL_00be;
				}
				goto end_IL_0056;
				IL_00be:
				List<IUIAutomationElement> list = new List<IUIAutomationElement>();
				for (IUIAutomationElement iUIAutomationElement2 = controlViewWalker.GetFirstChildElement(iUIAutomationElement); iUIAutomationElement2 != null; iUIAutomationElement2 = controlViewWalker.GetNextSiblingElement(iUIAutomationElement2))
				{
					list.Add(iUIAutomationElement2);
					if (list.Count > 500)
					{
						break;
					}
				}
				for (int num2 = list.Count - 1; num2 >= 0; num2--)
				{
					stack.Push(list[num2]);
				}
				end_IL_0056:;
			}
			catch (COMException)
			{
			}
		}
		if (stringBuilder.Length == 0)
		{
			return null;
		}
		string text2 = stringBuilder.ToString();
		bool isTruncated = flag || text2.Length > maximumCharacters;
		return new PageText(Normalize((text2.Length > maximumCharacters) ? text2.Substring(0, maximumCharacters) : text2), isTruncated);
	}

	private static string Normalize(string text)
	{
		return text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("￼", "")
			.Trim();
	}

	public static Task<FocusTarget?> FocusedTargetAsync(CancellationToken ct = default(CancellationToken))
	{
		return UIAutomationHelpers.RunAsync(delegate(IUIAutomation automation)
		{
			IUIAutomationElement iUIAutomationElement;
			try
			{
				iUIAutomationElement = automation.GetFocusedElement();
			}
			catch (COMException)
			{
				iUIAutomationElement = null;
			}
			if (iUIAutomationElement == null)
			{
				return new FocusTarget(HasFocus: false, 0, IsPassword: false, IsAddressBar: false, "", null);
			}
			int currentProcessId = iUIAutomationElement.CurrentProcessId;
			string text = iUIAutomationElement.CurrentControlType.ToString().Replace("UIA_", "").Replace("ControlTypeId", "");
			BOOL currentIsPassword = iUIAutomationElement.CurrentIsPassword;
			string text2 = iUIAutomationElement.CurrentName.ToString() ?? "";
			string text3 = iUIAutomationElement.CurrentAutomationId.ToString() ?? "";
			string text4 = iUIAutomationElement.CurrentClassName.ToString() ?? "";
			string text5 = null;
			try
			{
				using Process process = Process.GetProcessById(currentProcessId);
				text5 = process.ProcessName;
			}
			catch
			{
			}
			bool isAddressBar = text5 != null && ForegroundAppInfo.IsBrowser(text5) && text == "Edit" && (text3 == "urlbar-input" || text4 == "OmniboxViewViews" || text2.Contains("Address and search bar", StringComparison.OrdinalIgnoreCase) || text2.Contains("Search or enter address", StringComparison.OrdinalIgnoreCase) || text2.Contains("Search or type web address", StringComparison.OrdinalIgnoreCase) || text2.Contains("search or type a URL", StringComparison.OrdinalIgnoreCase));
			return new FocusTarget(HasFocus: true, currentProcessId, currentIsPassword, isAddressBar, text, text5);
		}, ct);
	}
}
