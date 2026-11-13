using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;

namespace HeyClicky.Platform.Win32;

public static class UIAutomationHelpers
{
	private static readonly BlockingCollection<Action> Work = new BlockingCollection<Action>();

	private static readonly Lazy<Thread> WorkerThread = new Lazy<Thread>(delegate
	{
		Thread thread = new Thread(WorkerMain);
		thread.IsBackground = true;
		thread.Name = "HeyClicky UI Automation";
		thread.SetApartmentState(ApartmentState.MTA);
		thread.Start();
		return thread;
	});

	private static IUIAutomation? automation;

	private static IUIAutomationCacheRequest? elementCache;

	private static IUIAutomationCondition? editCondition;

	private static readonly int OwnProcessId = Environment.ProcessId;

	private const int TransactionTimeoutMs = 300;

	private static readonly UIA_PROPERTY_ID[] CachedProperties = new UIA_PROPERTY_ID[8]
	{
		UIA_PROPERTY_ID.UIA_NamePropertyId,
		UIA_PROPERTY_ID.UIA_ControlTypePropertyId,
		UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId,
		UIA_PROPERTY_ID.UIA_ProcessIdPropertyId,
		UIA_PROPERTY_ID.UIA_AutomationIdPropertyId,
		UIA_PROPERTY_ID.UIA_ClassNamePropertyId,
		UIA_PROPERTY_ID.UIA_IsPasswordPropertyId,
		UIA_PROPERTY_ID.UIA_IsInvokePatternAvailablePropertyId
	};

	private static readonly HashSet<UIA_CONTROLTYPE_ID> ControlTypes = new HashSet<UIA_CONTROLTYPE_ID>
	{
		UIA_CONTROLTYPE_ID.UIA_ButtonControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_HyperlinkControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_MenuItemControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_SplitButtonControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_CheckBoxControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_RadioButtonControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_EditControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_ComboBoxControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_SliderControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_SpinnerControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_TabItemControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_ListItemControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_TreeItemControlTypeId
	};

	private static readonly HashSet<UIA_CONTROLTYPE_ID> ContainerTypes = new HashSet<UIA_CONTROLTYPE_ID>
	{
		UIA_CONTROLTYPE_ID.UIA_WindowControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_DocumentControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_PaneControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_ScrollBarControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_ListControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_TableControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_TreeControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_DataGridControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_TitleBarControlTypeId,
		UIA_CONTROLTYPE_ID.UIA_AppBarControlTypeId
	};

	internal static readonly ConcurrentDictionary<nint, IUIAutomationElement> AddressBarByWindow = new ConcurrentDictionary<nint, IUIAutomationElement>();

	private static void WorkerMain()
	{
		foreach (Action item in Work.GetConsumingEnumerable())
		{
			try
			{
				item();
			}
			catch (Exception ex)
			{
				Log.Warn("UIA", "work item failed: " + ex.Message);
			}
		}
	}

	internal static Task<T?> RunAsync<T>(Func<IUIAutomation, T?> query, CancellationToken ct = default(CancellationToken))
	{
		_ = WorkerThread.Value;
		TaskCompletionSource<T?> completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		Work.Add(delegate
		{
			if (ct.IsCancellationRequested)
			{
				completion.TrySetCanceled(ct);
				return;
			}
			try
			{
				completion.TrySetResult(query(EnsureAutomation()));
			}
			catch (COMException ex)
			{
				Log.Warn("UIA", $"query failed (0x{ex.HResult:X8})");
				completion.TrySetResult(default(T));
			}
			catch (Exception ex2)
			{
				Log.Warn("UIA", "query failed: " + ex2.Message);
				completion.TrySetResult(default(T));
			}
		});
		return completion.Task;
	}

	private static IUIAutomation EnsureAutomation()
	{
		if (automation != null)
		{
			return automation;
		}
		using (DpiAwarenessScope.PerMonitorV2())
		{
			automation = (IUIAutomation)new CUIAutomation8();
			if (automation is IUIAutomation2 iUIAutomation)
			{
				iUIAutomation.ConnectionTimeout = 1000u;
				iUIAutomation.TransactionTimeout = 300u;
			}
			elementCache = automation.CreateCacheRequest();
			UIA_PROPERTY_ID[] cachedProperties = CachedProperties;
			foreach (UIA_PROPERTY_ID propertyId in cachedProperties)
			{
				elementCache.AddProperty(propertyId);
			}
			editCondition = automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ControlTypePropertyId, 50004);
			return automation;
		}
	}

	public static Task<UiElementInfo?> ElementAtPointAsync(PointD pointPx, CancellationToken ct = default(CancellationToken))
	{
		return RunAsync((IUIAutomation automation) => Describe(automation.ElementFromPointBuildCache(new Point((int)pointPx.X, (int)pointPx.Y), elementCache)), ct);
	}

	public static Task<UiElementInfo?> FocusedElementAsync(CancellationToken ct = default(CancellationToken))
	{
		return RunAsync((IUIAutomation automation) => Describe(automation.GetFocusedElementBuildCache(elementCache)), ct);
	}

	public static async Task<RectD?> BoundingRectAtPointAsync(PointD pointPx, CancellationToken ct = default(CancellationToken))
	{
		return (await ElementAtPointAsync(pointPx, ct).ConfigureAwait(continueOnCapturedContext: false))?.BoundsPx;
	}

	public static Task<RectD?> ControlFrameNearAsync(RectD modelFramePx, RectD displayBoundsPx, double displayScale, CancellationToken ct = default(CancellationToken))
	{
		return RunAsync(delegate(IUIAutomation automation)
		{
			long num = Environment.TickCount64 + 300;
			Point pt = new Point((int)(modelFramePx.X + modelFramePx.Width / 2.0), (int)(modelFramePx.Y + modelFramePx.Height / 2.0));
			IUIAutomationElement iUIAutomationElement = automation.ElementFromPointBuildCache(pt, elementCache);
			if (iUIAutomationElement == null || CachedInt(iUIAutomationElement, UIA_PROPERTY_ID.UIA_ProcessIdPropertyId) == OwnProcessId)
			{
				return (RectD?)null;
			}
			IUIAutomationTreeWalker controlViewWalker = automation.ControlViewWalker;
			for (int i = 0; i < 8; i++)
			{
				if (iUIAutomationElement == null)
				{
					break;
				}
				if (Environment.TickCount64 > num)
				{
					return (RectD?)null;
				}
				if (IsControl(iUIAutomationElement))
				{
					RectD? rectD = CachedRect(iUIAutomationElement);
					if (rectD.HasValue)
					{
						RectD valueOrDefault = rectD.GetValueOrDefault();
						if (Agrees(valueOrDefault, modelFramePx, displayBoundsPx, displayScale))
						{
							return valueOrDefault;
						}
					}
					return (RectD?)null;
				}
				iUIAutomationElement = controlViewWalker.GetParentElementBuildCache(iUIAutomationElement, elementCache);
			}
			return (RectD?)null;
		}, ct);
	}

	private static bool IsControl(IUIAutomationElement element)
	{
		UIA_CONTROLTYPE_ID item = (UIA_CONTROLTYPE_ID)CachedInt(element, UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
		if (ControlTypes.Contains(item))
		{
			return true;
		}
		if (ContainerTypes.Contains(item))
		{
			return false;
		}
		object cachedPropertyValue = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_IsInvokePatternAvailablePropertyId);
		if (cachedPropertyValue is bool)
		{
			return (bool)cachedPropertyValue;
		}
		return false;
	}

	private static bool Agrees(RectD frame, RectD modelFrame, RectD displayFrame, double scale)
	{
		if (frame.Width < 8.0 * scale || frame.Height < 8.0 * scale)
		{
			return false;
		}
		if (!(frame.X < displayFrame.Right) || !(frame.Right > displayFrame.X) || !(frame.Y < displayFrame.Bottom) || !(frame.Bottom > displayFrame.Y))
		{
			return false;
		}
		double num = frame.Width * frame.Height;
		if (num > displayFrame.Width * displayFrame.Height * 0.25)
		{
			return false;
		}
		double num2 = num / Math.Max(1.0, modelFrame.Width * modelFrame.Height);
		double num3 = 12.0 * scale;
		RectD rectD = new RectD(frame.X - num3, frame.Y - num3, frame.Width + 2.0 * num3, frame.Height + 2.0 * num3);
		if (num2 >= 0.1 && num2 <= 8.0)
		{
			return rectD.Contains(new PointD(modelFrame.X + modelFrame.Width / 2.0, modelFrame.Y + modelFrame.Height / 2.0));
		}
		return false;
	}

	internal static string? ReadBrowserAddress(IUIAutomation automation, nint hwnd, bool isFirefox)
	{
		if (UiaPageText.IsSensitiveWindow(hwnd))
		{
			return null;
		}
		if (!AddressBarByWindow.TryGetValue(hwnd, out IUIAutomationElement value))
		{
			IUIAutomationElement iUIAutomationElement = automation.ElementFromHandle(new HWND(hwnd));
			if (iUIAutomationElement == null)
			{
				return null;
			}
			IUIAutomationCondition condition = (isFirefox ? automation.CreateAndCondition(editCondition, automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId, "urlbar-input")) : automation.CreateAndCondition(editCondition, automation.CreatePropertyCondition(UIA_PROPERTY_ID.UIA_ClassNamePropertyId, "OmniboxViewViews")));
			IUIAutomation2 iUIAutomation = automation as IUIAutomation2;
			uint transactionTimeout = iUIAutomation?.TransactionTimeout ?? 0;
			if (iUIAutomation != null)
			{
				iUIAutomation.TransactionTimeout = 2000u;
			}
			try
			{
				value = iUIAutomationElement.FindFirst(TreeScope.TreeScope_Descendants, condition) ?? (isFirefox ? null : iUIAutomationElement.FindFirst(TreeScope.TreeScope_Descendants, editCondition));
			}
			finally
			{
				if (iUIAutomation != null)
				{
					iUIAutomation.TransactionTimeout = transactionTimeout;
				}
			}
			if (value == null)
			{
				return null;
			}
			if (AddressBarByWindow.Count > 32)
			{
				AddressBarByWindow.Clear();
			}
			AddressBarByWindow[hwnd] = value;
		}
		try
		{
			object isPasswordObj = value.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
			if ((isPasswordObj is bool isPassword && isPassword) || (bool)value.CurrentIsPassword)
			{
				return null;
			}
			return value.GetCurrentPropertyValue(UIA_PROPERTY_ID.UIA_ValueValuePropertyId) as string;
		}
		catch (COMException)
		{
			AddressBarByWindow.TryRemove(hwnd, out IUIAutomationElement _);
			return null;
		}
	}

	private static UiElementInfo? Describe(IUIAutomationElement? element)
	{
		if (element == null)
		{
			return null;
		}
		UIA_CONTROLTYPE_ID controlType = (UIA_CONTROLTYPE_ID)CachedInt(element, UIA_PROPERTY_ID.UIA_ControlTypePropertyId);
		string name = (element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_NamePropertyId) as string) ?? "";
		string controlType2 = ControlTypeName(controlType);
		string automationId = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_AutomationIdPropertyId) as string;
		string className = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_ClassNamePropertyId) as string;
		RectD valueOrDefault = CachedRect(element).GetValueOrDefault();
		int processId = CachedInt(element, UIA_PROPERTY_ID.UIA_ProcessIdPropertyId);
		object cachedPropertyValue = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_IsPasswordPropertyId);
		bool isPassword = cachedPropertyValue is bool && (bool)cachedPropertyValue;
		cachedPropertyValue = element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_IsInvokePatternAvailablePropertyId);
		return new UiElementInfo(name, controlType2, automationId, className, valueOrDefault, processId, isPassword, cachedPropertyValue is bool && (bool)cachedPropertyValue);
	}

	private static string ControlTypeName(UIA_CONTROLTYPE_ID controlType)
	{
		string text = controlType.ToString();
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

	private static int CachedInt(IUIAutomationElement element, UIA_PROPERTY_ID property)
	{
		object cachedPropertyValue = element.GetCachedPropertyValue(property);
		if (cachedPropertyValue is int)
		{
			return (int)cachedPropertyValue;
		}
		return 0;
	}

	private static RectD? CachedRect(IUIAutomationElement element)
	{
		if (!(element.GetCachedPropertyValue(UIA_PROPERTY_ID.UIA_BoundingRectanglePropertyId) is double[] array) || array.Length != 4 || !(array[2] > 0.0) || !(array[3] > 0.0))
		{
			return null;
		}
		return new RectD(array[0], array[1], array[2], array[3]);
	}
}
