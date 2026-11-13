using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Win32;

public sealed class DictationTextTarget : IDictationTextTarget
{
	private sealed record Anchor(IUIAutomationElement Element, int ControlType, RectD? Frame, string Name, string ProcessName);

	private const string Area = "DictationTarget";

	private const int SurroundingTextCap = 2000;

	private const int LandingCheckMaxCharacters = 200000;

	private const int LearnerMaxCharacters = 20001;

	private static readonly TimeSpan DictationRestoreDelay = TimeSpan.FromSeconds(0.6);

	private static readonly HashSet<string> BlindPasteProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "WeChat", "Weixin", "WXWork", "ms-teams", "ChatGPT", "Codex", "Doubao", "sublime_text" };

	private static readonly HashSet<string> TerminalProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"WindowsTerminal", "OpenConsole", "conhost", "cmd", "powershell", "pwsh", "wezterm-gui", "alacritty", "mintty", "kitty",
		"Hyper", "Tabby", "warp"
	};

	[DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
	private static extern int GetWindowStyle(nint hwnd, int index);

	public async Task<DictationFocus?> CaptureFocusAsync(CancellationToken ct = default(CancellationToken))
	{
		DictationFocus dictationFocus = await UIAutomationHelpers.RunAsync((IUIAutomation automation) => CaptureOnUiaThread(automation), ct).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)dictationFocus == null)
		{
			await Task.Delay(100, ct).ConfigureAwait(continueOnCapturedContext: false);
			dictationFocus = await UIAutomationHelpers.RunAsync((IUIAutomation automation) => CaptureOnUiaThread(automation), ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		return dictationFocus;
	}

	private static DictationFocus? CaptureOnUiaThread(IUIAutomation automation)
	{
		IUIAutomationElement focusedElement = automation.GetFocusedElement();
		if (focusedElement == null)
		{
			return null;
		}
		IUIAutomationElement iUIAutomationElement = FindTextInput(automation, focusedElement);
		if (iUIAutomationElement == null)
		{
			return null;
		}
		int processId = Int(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ProcessIdPropertyId);
		(string ProcessName, string AppName) tuple = ForegroundAppInfo.DescribeProcess(processId);
		string item = tuple.ProcessName;
		string item2 = tuple.AppName;
		int controlType = Int(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
		object currentPropertyValue = iUIAutomationElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
		bool flag = currentPropertyValue is bool && (bool)currentPropertyValue;
		RectD? frame = Rect(iUIAutomationElement);
		string name = (iUIAutomationElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId) as string) ?? "";
		nint num = TopLevelWindow(iUIAutomationElement, processId);
		string siteHost = null;
		if (ForegroundAppInfo.IsBrowserProcess(item, out var isFirefox) && num != 0)
		{
			siteHost = HostOf(UIAutomationHelpers.ReadBrowserAddress(automation, num, isFirefox));
		}
		string beforeText = "";
		string selectedText = "";
		string afterText = "";
		if (!flag)
		{
			(beforeText, selectedText, afterText) = ReadSurroundingText(iUIAutomationElement);
		}
		return new DictationFocus(new Anchor(iUIAutomationElement, controlType, frame, name, item), processId, num, item2, siteHost, RoleName(controlType), flag, IsSingleLine(iUIAutomationElement, controlType, frame), beforeText, selectedText, afterText);
	}

	private static IUIAutomationElement? FindTextInput(IUIAutomation automation, IUIAutomationElement start)
	{
		IUIAutomationTreeWalker controlViewWalker = automation.ControlViewWalker;
		IUIAutomationElement iUIAutomationElement = start;
		List<string> list = new List<string>();
		for (int i = 0; i < 6; i++)
		{
			if (iUIAutomationElement == null)
			{
				break;
			}
			if (AcceptsText(iUIAutomationElement))
			{
				return iUIAutomationElement;
			}
			list.Add(RoleName(Int(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ControlTypePropertyId)) + "/" + (iUIAutomationElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_ClassNamePropertyId) as string));
			try
			{
				iUIAutomationElement = controlViewWalker.GetParentElement(iUIAutomationElement);
			}
			catch (COMException)
			{
				break;
			}
		}
		Log.Info("DictationTarget", "no text input around the focus: " + string.Join(" < ", list));
		return null;
	}

	private static bool AcceptsText(IUIAutomationElement element)
	{
		UIA_CONTROLTYPE_ID uIA_CONTROLTYPE_ID = (UIA_CONTROLTYPE_ID)Int(element, UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
		object currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsValuePatternAvailablePropertyId);
		bool? obj;
		if (!(currentPropertyValue is bool) || !(bool)currentPropertyValue)
		{
			obj = null;
		}
		else
		{
			currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_ValueIsReadOnlyPropertyId);
			obj = currentPropertyValue is bool && (bool)currentPropertyValue;
		}
		bool? flag = obj;
		currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsKeyboardFocusablePropertyId);
		bool flag2 = currentPropertyValue is bool && (bool)currentPropertyValue;
		switch (uIA_CONTROLTYPE_ID)
		{
		case UIA_CONTROLTYPE_ID.UIA_ComboBoxControlTypeId:
		case UIA_CONTROLTYPE_ID.UIA_EditControlTypeId:
			return flag != true;
		case UIA_CONTROLTYPE_ID.UIA_DocumentControlTypeId:
			if (flag.HasValue)
			{
				bool valueOrDefault = flag == true;
				return !valueOrDefault && flag2;
			}
			if (flag2)
			{
				currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId);
				if (currentPropertyValue is bool && (bool)currentPropertyValue)
				{
					return !TextIsReadOnly(element);
				}
			}
			return false;
		default:
			return flag == false && flag2;
		}
	}

	private static bool TextIsReadOnly(IUIAutomationElement element)
	{
		try
		{
			if (!(element.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId) is IUIAutomationTextPattern iUIAutomationTextPattern))
			{
				return true;
			}
			object attributeValue = iUIAutomationTextPattern.DocumentRange.GetAttributeValue(UIA_TEXTATTRIBUTE_ID.UIA_IsReadOnlyAttributeId);
			return attributeValue is bool && (bool)attributeValue;
		}
		catch (COMException)
		{
			return true;
		}
	}

	private static (string Before, string Selected, string After) ReadSurroundingText(IUIAutomationElement element)
	{
		try
		{
			object currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId);
			if (currentPropertyValue is bool && (bool)currentPropertyValue && element.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId) is IUIAutomationTextPattern iUIAutomationTextPattern)
			{
				IUIAutomationTextRangeArray selection = iUIAutomationTextPattern.GetSelection();
				if (selection != null && selection.Length > 0)
				{
					IUIAutomationTextRange element2 = selection.GetElement(0);
					string item = Text(element2.GetText(2000));
					IUIAutomationTextRange iUIAutomationTextRange = element2.Clone();
					iUIAutomationTextRange.MoveEndpointByRange(TextPatternRangeEndpoint.TextPatternRangeEndpoint_End, element2, TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start);
					iUIAutomationTextRange.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start, TextUnit.TextUnit_Character, -2000);
					IUIAutomationTextRange iUIAutomationTextRange2 = element2.Clone();
					iUIAutomationTextRange2.MoveEndpointByRange(TextPatternRangeEndpoint.TextPatternRangeEndpoint_Start, element2, TextPatternRangeEndpoint.TextPatternRangeEndpoint_End);
					iUIAutomationTextRange2.MoveEndpointByUnit(TextPatternRangeEndpoint.TextPatternRangeEndpoint_End, TextUnit.TextUnit_Character, 2000);
					return (Before: Clean(Text(iUIAutomationTextRange.GetText(4000))), Selected: item, After: Clean(Text(iUIAutomationTextRange2.GetText(2000))));
				}
			}
		}
		catch (COMException)
		{
		}
		string text = ReadValue(element, 200000) ?? "";
		string item2;
		if (text.Length <= 2000)
		{
			item2 = text;
		}
		else
		{
			int length = text.Length;
			int num = length - 2000;
			item2 = text.Substring(num, length - num);
		}
		return (Before: item2, Selected: "", After: "");
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

	private static string Clean(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "";
		}
		text = text.Replace("\r\n", "\n").Replace('\r', '\n');
		if (text.Length <= 2000)
		{
			return text;
		}
		string? text2 = text;
		int length = text2.Length;
		int num = length - 2000;
		return text2.Substring(num, length - num);
	}

	private static string? ReadValue(IUIAutomationElement element, int cap)
	{
		try
		{
			object currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsValuePatternAvailablePropertyId);
			if (currentPropertyValue is bool && (bool)currentPropertyValue && element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_ValueValuePropertyId) is string text)
			{
				return (text.Length >= cap) ? null : text;
			}
			currentPropertyValue = element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsTextPatternAvailablePropertyId);
			if (currentPropertyValue is bool && (bool)currentPropertyValue && element.GetCurrentPattern(UIA_PATTERN_ID.UIA_TextPatternId) is IUIAutomationTextPattern iUIAutomationTextPattern)
			{
				string text2 = Text(iUIAutomationTextPattern.DocumentRange.GetText(cap));
				return (text2.Length >= cap) ? null : text2;
			}
		}
		catch (COMException)
		{
		}
		return null;
	}

	private unsafe static bool IsSingleLine(IUIAutomationElement element, int controlType, RectD? frame)
	{
		switch ((UIA_CONTROLTYPE_ID)controlType)
		{
		case UIA_CONTROLTYPE_ID.UIA_ComboBoxControlTypeId:
			return true;
		case UIA_CONTROLTYPE_ID.UIA_EditControlTypeId:
		{
			nint num = Int(element, UIA_PROPERTY_ID.UIA_NativeWindowHandlePropertyId);
			if (num != 0)
			{
				Span<char> span = stackalloc char[64];
				int className;
				fixed (char* ptr = span)
				{
					className = PInvoke.GetClassName(new HWND(num), ptr, span.Length);
				}
				string text = new string(span.Slice(0, Math.Max(0, className)));
				if (text.Equals("Edit", StringComparison.OrdinalIgnoreCase) || text.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase))
				{
					return (GetWindowStyle(num, -16) & 4) == 0;
				}
			}
			if (element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_AriaPropertiesPropertyId) is string text2 && text2.Contains("multiline=true", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (frame.HasValue)
			{
				RectD valueOrDefault = frame.GetValueOrDefault();
				double num2 = Math.Max(1.0, (double)PInvoke.GetDpiForWindow(PInvoke.GetForegroundWindow()) / 96.0);
				return valueOrDefault.Height / num2 < 44.0;
			}
			return false;
		}
		default:
			return false;
		}
	}

	private static string RoleName(int controlType)
	{
		UIA_CONTROLTYPE_ID uIA_CONTROLTYPE_ID = (UIA_CONTROLTYPE_ID)controlType;
		string text = uIA_CONTROLTYPE_ID.ToString();
		if (text.StartsWith("UIA_", StringComparison.Ordinal))
		{
			string text2 = text;
			text = text2.Substring(4, text2.Length - 4);
		}
		if (text.EndsWith("ControlTypeId", StringComparison.Ordinal))
		{
			string text2 = text;
			int length = "ControlTypeId".Length;
			text = text2.Substring(0, text2.Length - length);
		}
		return text;
	}

	private static nint TopLevelWindow(IUIAutomationElement element, int processId)
	{
		nint num = Int(element, UIA_PROPERTY_ID.UIA_NativeWindowHandlePropertyId);
		if (num != 0)
		{
			return (nint)PInvoke.GetAncestor(new HWND(num), GET_ANCESTOR_FLAGS.GA_ROOT);
		}
		ForegroundApp foregroundApp = ForegroundAppInfo.Capture();
		if ((object)foregroundApp == null || foregroundApp.ProcessId != processId)
		{
			return 0;
		}
		return foregroundApp.WindowHandle;
	}

	private static string? HostOf(string? address)
	{
		if (string.IsNullOrWhiteSpace(address))
		{
			return null;
		}
		string text = address.Trim();
		if (!text.Contains("://", StringComparison.Ordinal))
		{
			text = "https://" + text;
		}
		if (!Uri.TryCreate(text, UriKind.Absolute, out Uri result) || !result.Host.Contains('.'))
		{
			return null;
		}
		return result.Host;
	}

	private static int Int(IUIAutomationElement element, UIA_PROPERTY_ID property)
	{
		object currentPropertyValue = element.GetCurrentPropertyValue(property);
		if (currentPropertyValue is int)
		{
			return (int)currentPropertyValue;
		}
		return 0;
	}

	private static RectD? Rect(IUIAutomationElement element)
	{
		if (!(element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId) is double[] array) || array.Length != 4 || !(array[2] > 0.0) || !(array[3] > 0.0))
		{
			return null;
		}
		return new RectD(array[0], array[1], array[2], array[3]);
	}

	public DictationWorkApp? ForegroundWorkApp()
	{
		ForegroundApp foregroundApp = ForegroundAppInfo.Capture();
		if ((object)foregroundApp == null || foregroundApp.ProcessId == Environment.ProcessId)
		{
			return null;
		}
		return new DictationWorkApp(foregroundApp.ProcessId, foregroundApp.WindowHandle, foregroundApp.AppName, null, BlindPasteProcesses.Contains(foregroundApp.ProcessName));
	}

	public Task<DictationInsertOutcome> InsertAsync(string text, DictationFocus focus, Action<string, int>? onPasteVerification = null)
	{
		return InsertAsync(text, focus, appendTrailingSpace: true, onPasteVerification);
	}

	public async Task<DictationInsertOutcome> InsertAsync(string text, DictationFocus focus, bool appendTrailingSpace, Action<string, int>? onPasteVerification)
	{
		if (focus.IsSecure)
		{
			return DictationInsertOutcome.RefusedUnsafeTarget;
		}
		object anchor = focus.Anchor;
		Anchor anchor2 = anchor as Anchor;
		if ((object)anchor2 == null)
		{
			return DictationInsertOutcome.FocusLost;
		}
		string text2 = text.Trim();
		if (text2.Length == 0)
		{
			return DictationInsertOutcome.EmptyTranscript;
		}
		string normalized = ((focus.IsSingleLine || (text2.Contains('\n') && (TerminalProcesses.Contains(anchor2.ProcessName) || anchor2.Name.Contains("terminal", StringComparison.OrdinalIgnoreCase)))) ? text2.Replace("\r\n", " ").Replace('\n', ' ').Replace("  ", " ") : text2.Replace("\r\n", "\n").Replace("\n", "\r\n"));
		string toInsert = ((!appendTrailingSpace || EndsInScriptWrittenWithoutSpaces(normalized)) ? normalized : (normalized + " "));
		if (!(await WaitForShortcutModifiersReleasedAsync().ConfigureAwait(continueOnCapturedContext: false)))
		{
			return DictationInsertOutcome.ModifiersHeld;
		}
		BringToFront(focus.WindowHandle);
		bool flag = await UIAutomationHelpers.RunAsync((IUIAutomation automation) => IsStillFocused(automation, anchor2)).ConfigureAwait(continueOnCapturedContext: false);
		if (!flag)
		{
			await UIAutomationHelpers.RunAsync(delegate
			{
				try
				{
					anchor2.Element.SetFocus();
				}
				catch (COMException)
				{
				}
				return true;
			}).ConfigureAwait(continueOnCapturedContext: false);
			await Task.Delay(50).ConfigureAwait(continueOnCapturedContext: false);
			flag = await UIAutomationHelpers.RunAsync((IUIAutomation automation) => IsStillFocused(automation, anchor2)).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!flag)
		{
			Log.Info("DictationTarget", "insert refused: the captured field is no longer focused");
			return DictationInsertOutcome.FocusLost;
		}
		string text3 = Normalize(normalized);
		string text4;
		if (text3.Length <= 24)
		{
			text4 = text3;
		}
		else
		{
			int length = text3.Length;
			int num = length - 24;
			text4 = text3.Substring(num, length - num);
		}
		string marker = text4;
		int? num2 = await UIAutomationHelpers.RunAsync(delegate
		{
			string text5 = ReadValue(anchor2.Element, 200000);
			return (text5 == null) ? ((int?)null) : new int?(Occurrences(text5, marker));
		}).ConfigureAwait(continueOnCapturedContext: false);
		object obj;
		if (num2.HasValue)
		{
			int before = num2.GetValueOrDefault();
			obj = (Func<Task<bool?>>)(async () => await UIAutomationHelpers.RunAsync(delegate
			{
				string text6 = ReadValue(anchor2.Element, 200000);
				return (text6 == null) ? ((bool?)null) : new bool?(Occurrences(text6, marker) > before);
			}).ConfigureAwait(continueOnCapturedContext: false));
		}
		else
		{
			obj = null;
		}
		Func<Task<bool?>> landed = (Func<Task<bool?>>)obj;
		if (!(await TextInsertion.PasteTextVerifyingLandingAsync(toInsert, DictationRestoreDelay, landed, onPasteVerification).ConfigureAwait(continueOnCapturedContext: false)))
		{
			return DictationInsertOutcome.ClipboardWriteFailed;
		}
		Log.Info("DictationTarget", $"pasted {toInsert.Length} chars into {focus.AppName} ({focus.Role})");
		return DictationInsertOutcome.Inserted;
	}

	private static bool IsStillFocused(IUIAutomation automation, Anchor anchor)
	{
		try
		{
			object currentPropertyValue = anchor.Element.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_HasKeyboardFocusPropertyId);
			if (currentPropertyValue is bool && (bool)currentPropertyValue)
			{
				return true;
			}
		}
		catch (COMException)
		{
		}
		IUIAutomationElement focusedElement = automation.GetFocusedElement();
		if (focusedElement == null)
		{
			return false;
		}
		if ((bool)automation.CompareElements(focusedElement, anchor.Element))
		{
			return true;
		}
		IUIAutomationElement iUIAutomationElement = FindTextInput(automation, focusedElement);
		if (iUIAutomationElement != null)
		{
			RectD? frame = anchor.Frame;
			if (frame.HasValue)
			{
				RectD valueOrDefault = frame.GetValueOrDefault();
				object currentPropertyValue = iUIAutomationElement.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
				if (currentPropertyValue is bool && (bool)currentPropertyValue)
				{
					return false;
				}
				if (Int(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ControlTypePropertyId) != anchor.ControlType)
				{
					return false;
				}
				frame = Rect(iUIAutomationElement);
				if (frame.HasValue)
				{
					RectD valueOrDefault2 = frame.GetValueOrDefault();
					int num;
					if (Math.Abs(valueOrDefault2.X - valueOrDefault.X) <= 8.0 && Math.Abs(valueOrDefault2.Y - valueOrDefault.Y) <= 8.0 && Math.Abs(valueOrDefault2.Width - valueOrDefault.Width) <= 8.0)
					{
						num = ((Math.Abs(valueOrDefault2.Height - valueOrDefault.Height) <= 8.0) ? 1 : 0);
						if (num != 0)
						{
							Log.Info("DictationTarget", "captured element identity lost; accepting the same-type/same-frame focused field");
						}
					}
					else
					{
						num = 0;
					}
					return (byte)num != 0;
				}
				return false;
			}
		}
		return false;
	}

	public async Task<bool> BlindPasteAsync(string text, DictationWorkApp app)
	{
		if (!BlindPasteProcesses.Contains(ForegroundAppInfo.DescribeProcess(app.ProcessId).ProcessName))
		{
			return false;
		}
		string trimmed = text.Trim();
		if (trimmed.Length == 0)
		{
			return false;
		}
		if (!(await WaitForShortcutModifiersReleasedAsync().ConfigureAwait(continueOnCapturedContext: false)))
		{
			return false;
		}
		BringToFront(app.WindowHandle);
		await Task.Delay(50).ConfigureAwait(continueOnCapturedContext: false);
		if (await UIAutomationHelpers.RunAsync(delegate(IUIAutomation automation)
		{
			object obj = automation.GetFocusedElement()?.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
			return obj is bool && (bool)obj;
		}).ConfigureAwait(continueOnCapturedContext: false))
		{
			return false;
		}
		return await TextInsertion.PasteTextAsync(EndsInScriptWrittenWithoutSpaces(trimmed) ? trimmed : (trimmed + " "), null, restoreClipboard: true).ConfigureAwait(continueOnCapturedContext: false);
	}

	public Task<bool> CopyToClipboardAsync(string text)
	{
		return TextInsertion.CopyTextAsync(text);
	}

	public Task<string?> ReadFieldValueAsync(DictationFocus focus)
	{
		object anchor = focus.Anchor;
		Anchor anchor2 = anchor as Anchor;
		if ((object)anchor2 == null)
		{
			return Task.FromResult<string>(null);
		}
		return UIAutomationHelpers.RunAsync(delegate
		{
			string text = ReadValue(anchor2.Element, 20001);
			return (text == null) ? null : Normalize(text);
		});
	}

	private static async Task<bool> WaitForShortcutModifiersReleasedAsync()
	{
		long deadline = Environment.TickCount64 + 3000;
		while (true)
		{
			if (!Down(91) && !Down(92) && !Down(18) && !Down(16))
			{
				return true;
			}
			if (Environment.TickCount64 > deadline)
			{
				break;
			}
			await Task.Delay(20).ConfigureAwait(continueOnCapturedContext: false);
		}
		Log.Warn("DictationTarget", "Win/Alt/Shift still held after 3 s; not pasting");
		return false;
		static bool Down(int vk)
		{
			return (PInvoke.GetAsyncKeyState(vk) & 0x8000) != 0;
		}
	}

	private static void BringToFront(nint window)
	{
		if (window != 0 && (bool)PInvoke.IsWindow(new HWND(window)) && !(PInvoke.GetForegroundWindow() == new HWND(window)))
		{
			if (!PInvoke.SetForegroundWindow(new HWND(window)))
			{
				Log.Warn("DictationTarget", "SetForegroundWindow refused");
			}
			Thread.Sleep(50);
		}
	}

	private static string Normalize(string text)
	{
		return text.Replace("\r\n", "\n").Replace('\r', '\n');
	}

	private static int Occurrences(string text, string marker)
	{
		if (marker.Length == 0)
		{
			return 0;
		}
		text = Normalize(text);
		int num = 0;
		int startIndex = 0;
		while ((startIndex = text.IndexOf(marker, startIndex, StringComparison.Ordinal)) >= 0)
		{
			num++;
			startIndex += marker.Length;
		}
		return num;
	}

	public static bool EndsInScriptWrittenWithoutSpaces(string text)
	{
		for (int num = text.Length - 1; num >= 0; num--)
		{
			char c = text[num];
			if ((!char.IsWhiteSpace(c) && !char.IsPunctuation(c)) || IsNoSpaceScript(c))
			{
				return IsNoSpaceScript(c);
			}
		}
		return false;
	}

	private static bool IsNoSpaceScript(char c)
	{
		if (c >= '㐀')
		{
			if (c >= '豈')
			{
				if (c >= '\uff00')
				{
					if (c <= '\uffef')
					{
						goto IL_0098;
					}
				}
				else if (c <= '\ufaff')
				{
					goto IL_0098;
				}
			}
			else if (c >= '一')
			{
				if (c <= '鿿')
				{
					goto IL_0098;
				}
			}
			else if (c <= '䶿')
			{
				goto IL_0098;
			}
		}
		else if (c <= '\u17ff')
		{
			if (c <= '႟')
			{
				if (c <= '\u0eff')
				{
					if (c >= '\u0e00')
					{
						goto IL_0098;
					}
				}
				else if (c >= 'က')
				{
					goto IL_0098;
				}
			}
			else if (c >= 'ក')
			{
				goto IL_0098;
			}
		}
		else if (c <= '〿')
		{
			if (c >= '\u3000')
			{
				goto IL_0098;
			}
		}
		else if (c <= 'ヿ')
		{
			goto IL_0098;
		}
		return false;
		IL_0098:
		return true;
	}
}
