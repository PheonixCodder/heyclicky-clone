using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Security;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;
using Windows.Win32.System.Memory;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.HiDpi;
using Windows.Win32.UI.Input;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Windows.Win32;

[GeneratedCode("Microsoft.Windows.CsWin32", "0.3.363+6f436ab751.RR")]
internal static class PInvoke
{
	internal const uint WM_KEYDOWN = 256u;

	internal const uint WM_KEYUP = 257u;

	internal const uint WM_SYSKEYDOWN = 260u;

	internal const uint WM_SYSKEYUP = 261u;

	internal const uint WM_LBUTTONDOWN = 513u;

	internal const uint WM_RBUTTONDOWN = 516u;

	internal const uint WM_MBUTTONDOWN = 519u;

	internal const uint WM_XBUTTONDOWN = 523u;

	internal const uint WM_MOUSEWHEEL = 522u;

	internal const uint WM_MOUSEHWHEEL = 526u;

	internal const uint WM_MOUSEMOVE = 512u;

	internal const uint WM_QUIT = 18u;

	internal const uint WM_USER = 1024u;

	internal const uint WM_APP = 32768u;

	internal const uint WM_INPUT = 255u;

	internal const uint WM_TIMER = 275u;

	internal const uint WM_DESTROY = 2u;

	internal const uint WM_DISPLAYCHANGE = 126u;

	internal const uint WM_DPICHANGED = 736u;

	internal const uint WM_NCHITTEST = 132u;

	internal const uint WM_MOUSEACTIVATE = 33u;

	internal const uint WM_CLOSE = 16u;

	internal const uint AUDCLNT_STREAMFLAGS_EVENTCALLBACK = 262144u;

	internal const uint AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM = 2147483648u;

	internal const uint AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY = 134217728u;

	internal const uint MONITORINFOF_PRIMARY = 1u;

	internal const uint EVENT_SYSTEM_FOREGROUND = 3u;

	internal const uint WINEVENT_OUTOFCONTEXT = 0u;

	internal const uint WINEVENT_SKIPOWNPROCESS = 2u;

	[SupportedOSPlatform("windows8.1")]
	[OverloadResolutionPriority(1)]
	internal unsafe static HRESULT GetDpiForMonitor(HMONITOR hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY)
	{
		fixed (uint* dpiY2 = &dpiY)
		{
			fixed (uint* dpiX2 = &dpiX)
			{
				return GetDpiForMonitor(hmonitor, dpiType, dpiX2, dpiY2);
			}
		}
	}

	[DllImport("api-ms-win-shcore-scaling-l1-1-1.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows8.1")]
	internal unsafe static extern HRESULT GetDpiForMonitor(HMONITOR hmonitor, MONITOR_DPI_TYPE dpiType, uint* dpiX, uint* dpiY);

	[DllImport("AVRT.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows6.0.6000")]
	internal static extern BOOL AvRevertMmThreadCharacteristics(AVRT_TASK_HANDLE AvrtHandle);

	[SupportedOSPlatform("windows6.0.6000")]
	[OverloadResolutionPriority(1)]
	internal unsafe static AvRevertMmThreadCharacteristicsSafeHandle AvSetMmThreadCharacteristics(string TaskName, ref uint TaskIndex)
	{
		fixed (uint* taskIndex = &TaskIndex)
		{
			fixed (char* ptr = TaskName)
			{
				AvRevertMmThreadCharacteristicsSafeHandle avRevertMmThreadCharacteristicsSafeHandle = new AvRevertMmThreadCharacteristicsSafeHandle(0);
				AVRT_TASK_HANDLE aVRT_TASK_HANDLE = AvSetMmThreadCharacteristics(ptr, taskIndex);
				Marshal.InitHandle(avRevertMmThreadCharacteristicsSafeHandle, (nint)aVRT_TASK_HANDLE);
				return avRevertMmThreadCharacteristicsSafeHandle;
			}
		}
	}

	[DllImport("AVRT.dll", EntryPoint = "AvSetMmThreadCharacteristicsW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows6.0.6000")]
	internal unsafe static extern AVRT_TASK_HANDLE AvSetMmThreadCharacteristics(PCWSTR TaskName, uint* TaskIndex);

	[DllImport("dwmapi.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows6.0.6000")]
	internal static extern HRESULT DwmFlush();

	[SupportedOSPlatform("windows6.0.6000")]
	internal unsafe static HRESULT DwmGetWindowAttribute(HWND hwnd, DWMWINDOWATTRIBUTE dwAttribute, Span<byte> pvAttribute)
	{
		fixed (byte* pvAttribute2 = pvAttribute)
		{
			return DwmGetWindowAttribute(hwnd, dwAttribute, pvAttribute2, (uint)pvAttribute.Length);
		}
	}

	[SupportedOSPlatform("windows6.0.6000")]
	internal unsafe static HRESULT DwmGetWindowAttribute(HWND hwnd, [MarshalAs(UnmanagedType.U4)] DWMWINDOWATTRIBUTE dwAttribute, void* pvAttribute, uint cbAttribute)
	{
		return LocalExternFunction(hwnd, dwAttribute, pvAttribute, cbAttribute);
		[DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		static extern unsafe HRESULT LocalExternFunction(HWND hwnd, [MarshalAs(UnmanagedType.U4)] DWMWINDOWATTRIBUTE dwAttribute, void* pvAttribute, uint cbAttribute);
	}

	[DllImport("GDI32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL DeleteObject(HGDIOBJ ho);

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern BOOL FreeLibrary(HMODULE hLibModule);

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL CloseHandle(HANDLE hObject);

	[DllImport("KERNEL32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern uint GetCurrentThreadId();

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static FreeLibrarySafeHandle GetModuleHandle([Optional] string lpModuleName)
	{
		fixed (char* ptr = lpModuleName)
		{
			FreeLibrarySafeHandle freeLibrarySafeHandle = new FreeLibrarySafeHandle(0, ownsHandle: false);
			HMODULE moduleHandle = GetModuleHandle(ptr);
			Marshal.InitHandle(freeLibrarySafeHandle, (nint)moduleHandle);
			return freeLibrarySafeHandle;
		}
	}

	[DllImport("KERNEL32.dll", EntryPoint = "GetModuleHandleW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HMODULE GetModuleHandle([Optional] PCWSTR lpModuleName);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static SafeFileHandle OpenProcess_SafeHandle(PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwProcessId)
	{
		SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
		HANDLE hANDLE = OpenProcess(dwDesiredAccess, bInheritHandle, dwProcessId);
		Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
		return safeFileHandle;
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HANDLE OpenProcess(PROCESS_ACCESS_RIGHTS dwDesiredAccess, BOOL bInheritHandle, uint dwProcessId);

	[SupportedOSPlatform("windows6.0.6000")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL QueryFullProcessImageName(SafeHandle hProcess, PROCESS_NAME_FORMAT dwFlags, Span<char> lpExeName, ref uint lpdwSize)
	{
		bool success = false;
		try
		{
			fixed (uint* lpdwSize2 = &lpdwSize)
			{
				fixed (char* ptr = lpExeName)
				{
					if (hProcess != null)
					{
						hProcess.DangerousAddRef(ref success);
						HANDLE hProcess2 = (HANDLE)hProcess.DangerousGetHandle();
						return QueryFullProcessImageName(hProcess2, dwFlags, ptr, lpdwSize2);
					}
					throw new ArgumentNullException("hProcess");
				}
			}
		}
		finally
		{
			if (success)
			{
				hProcess.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", EntryPoint = "QueryFullProcessImageNameW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows6.0.6000")]
	internal unsafe static extern BOOL QueryFullProcessImageName(HANDLE hProcess, PROCESS_NAME_FORMAT dwFlags, PWSTR lpExeName, uint* lpdwSize);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static GlobalFreeSafeHandle GlobalFree_SafeHandle([Optional] HGLOBAL hMem)
	{
		GlobalFreeSafeHandle globalFreeSafeHandle = new GlobalFreeSafeHandle(0);
		HGLOBAL hGLOBAL = GlobalFree(hMem);
		Marshal.InitHandle(globalFreeSafeHandle, (nint)hGLOBAL);
		return globalFreeSafeHandle;
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HGLOBAL GlobalFree([Optional] HGLOBAL hMem);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static GlobalFreeSafeHandle GlobalAlloc_SafeHandle(GLOBAL_ALLOC_FLAGS uFlags, nuint dwBytes)
	{
		GlobalFreeSafeHandle globalFreeSafeHandle = new GlobalFreeSafeHandle(0);
		HGLOBAL hGLOBAL = GlobalAlloc(uFlags, dwBytes);
		Marshal.InitHandle(globalFreeSafeHandle, (nint)hGLOBAL);
		return globalFreeSafeHandle;
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern HGLOBAL GlobalAlloc(GLOBAL_ALLOC_FLAGS uFlags, nuint dwBytes);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static void* GlobalLock(SafeHandle hMem)
	{
		bool success = false;
		try
		{
			if (hMem != null)
			{
				hMem.DangerousAddRef(ref success);
				HGLOBAL hMem2 = (HGLOBAL)hMem.DangerousGetHandle();
				return GlobalLock(hMem2);
			}
			throw new ArgumentNullException("hMem");
		}
		finally
		{
			if (success)
			{
				hMem.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern void* GlobalLock(HGLOBAL hMem);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static BOOL GlobalUnlock(SafeHandle hMem)
	{
		bool success = false;
		try
		{
			if (hMem != null)
			{
				hMem.DangerousAddRef(ref success);
				HGLOBAL hMem2 = (HGLOBAL)hMem.DangerousGetHandle();
				return GlobalUnlock(hMem2);
			}
			throw new ArgumentNullException("hMem");
		}
		finally
		{
			if (success)
			{
				hMem.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern BOOL GlobalUnlock(HGLOBAL hMem);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static nuint GlobalSize(SafeHandle hMem)
	{
		bool success = false;
		try
		{
			if (hMem != null)
			{
				hMem.DangerousAddRef(ref success);
				HGLOBAL hMem2 = (HGLOBAL)hMem.DangerousGetHandle();
				return GlobalSize(hMem2);
			}
			throw new ArgumentNullException("hMem");
		}
		finally
		{
			if (success)
			{
				hMem.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern nuint GlobalSize(HGLOBAL hMem);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal unsafe static SafeFileHandle CreateEvent([Optional] SECURITY_ATTRIBUTES? lpEventAttributes, BOOL bManualReset, BOOL bInitialState, [Optional] string lpName)
	{
		fixed (char* ptr = lpName)
		{
			SECURITY_ATTRIBUTES valueOrDefault = lpEventAttributes.GetValueOrDefault();
			SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
			HANDLE hANDLE = CreateEvent(lpEventAttributes.HasValue ? (&valueOrDefault) : null, bManualReset, bInitialState, ptr);
			Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
			return safeFileHandle;
		}
	}

	[DllImport("KERNEL32.dll", EntryPoint = "CreateEventW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern HANDLE CreateEvent([Optional] SECURITY_ATTRIBUTES* lpEventAttributes, BOOL bManualReset, BOOL bInitialState, [Optional] PCWSTR lpName);

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static WAIT_EVENT WaitForMultipleObjects(ReadOnlySpan<HANDLE> lpHandles, BOOL bWaitAll, uint dwMilliseconds)
	{
		fixed (HANDLE* lpHandles2 = lpHandles)
		{
			return WaitForMultipleObjects((uint)lpHandles.Length, lpHandles2, bWaitAll, dwMilliseconds);
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern WAIT_EVENT WaitForMultipleObjects(uint nCount, HANDLE* lpHandles, BOOL bWaitAll, uint dwMilliseconds);

	[SupportedOSPlatform("windows5.1.2600")]
	[OverloadResolutionPriority(1)]
	internal static WAIT_EVENT WaitForSingleObject(SafeHandle hHandle, uint dwMilliseconds)
	{
		bool success = false;
		try
		{
			if (hHandle != null)
			{
				hHandle.DangerousAddRef(ref success);
				HANDLE hHandle2 = (HANDLE)hHandle.DangerousGetHandle();
				return WaitForSingleObject(hHandle2, dwMilliseconds);
			}
			throw new ArgumentNullException("hHandle");
		}
		finally
		{
			if (success)
			{
				hHandle.DangerousRelease();
			}
		}
	}

	[DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal static extern WAIT_EVENT WaitForSingleObject(HANDLE hHandle, uint dwMilliseconds);

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static HRESULT CoCreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(in Guid rclsid, [Optional] object pUnkOuter, CLSCTX dwClsContext, out T ppv) where T : class
	{
		fixed (Guid* rclsid2 = &rclsid)
		{
			Guid iID = ComOrWinRTObjectMarshaller.GetIID<T>();
			object ppv2 = null;
			HRESULT result = CoCreateInstance(rclsid2, pUnkOuter, dwClsContext, &iID, out ppv2);
			ppv = (T)ComOrWinRTObjectMarshaller.ConvertToManaged(ppv2);
			return result;
		}
	}

	[DllImport("OLE32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern HRESULT CoCreateInstance(Guid* rclsid, [Optional][MarshalAs(UnmanagedType.IUnknown)] object pUnkOuter, CLSCTX dwClsContext, Guid* riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static HRESULT CoInitializeEx(COINIT dwCoInit)
	{
		return CoInitializeEx(null, dwCoInit);
	}

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static HRESULT CoInitializeEx([Optional] void* pvReserved, [MarshalAs(UnmanagedType.U4)] COINIT dwCoInit)
	{
		return LocalExternFunction(pvReserved, dwCoInit);
		[DllImport("OLE32.dll", EntryPoint = "CoInitializeEx", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		static extern unsafe HRESULT LocalExternFunction([Optional] void* pvReserved, [MarshalAs(UnmanagedType.U4)] COINIT dwCoInit);
	}

	[DllImport("OLE32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HRESULT PropVariantClear(ref PROPVARIANT pvar);

	[DllImport("OLEAUT32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	internal static extern void SysFreeString([Optional] BSTR bstrString);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL UnhookWindowsHookEx(HHOOK hhk);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static UnhookWindowsHookExSafeHandle SetWindowsHookEx(WINDOWS_HOOK_ID idHook, HOOKPROC lpfn, [Optional] SafeHandle hmod, uint dwThreadId)
	{
		bool success = false;
		try
		{
			HINSTANCE hmod2;
			if (hmod != null)
			{
				hmod.DangerousAddRef(ref success);
				hmod2 = (HINSTANCE)hmod.DangerousGetHandle();
			}
			else
			{
				hmod2 = (HINSTANCE)new IntPtr(0L);
			}
			UnhookWindowsHookExSafeHandle unhookWindowsHookExSafeHandle = new UnhookWindowsHookExSafeHandle(0);
			HHOOK hHOOK = SetWindowsHookEx(idHook, lpfn, hmod2, dwThreadId);
			Marshal.InitHandle(unhookWindowsHookExSafeHandle, (nint)hHOOK);
			return unhookWindowsHookExSafeHandle;
		}
		finally
		{
			if (success)
			{
				hmod.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", EntryPoint = "SetWindowsHookExW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HHOOK SetWindowsHookEx(WINDOWS_HOOK_ID idHook, [MarshalAs(UnmanagedType.FunctionPtr)] HOOKPROC lpfn, [Optional] HINSTANCE hmod, uint dwThreadId);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static LRESULT CallNextHookEx([Optional] SafeHandle hhk, int nCode, WPARAM wParam, LPARAM lParam)
	{
		bool success = false;
		try
		{
			HHOOK hhk2;
			if (hhk != null)
			{
				hhk.DangerousAddRef(ref success);
				hhk2 = (HHOOK)hhk.DangerousGetHandle();
			}
			else
			{
				hhk2 = (HHOOK)new IntPtr(0L);
			}
			return CallNextHookEx(hhk2, nCode, wParam, lParam);
		}
		finally
		{
			if (success)
			{
				hhk.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern LRESULT CallNextHookEx([Optional] HHOOK hhk, int nCode, WPARAM wParam, LPARAM lParam);

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static uint SendInput(ReadOnlySpan<INPUT> pInputs, int cbSize)
	{
		fixed (INPUT* pInputs2 = pInputs)
		{
			return SendInput((uint)pInputs.Length, pInputs2, cbSize);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern uint SendInput(uint cInputs, INPUT* pInputs, int cbSize);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern short GetAsyncKeyState(int vKey);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern short GetKeyState(int nVirtKey);

	[DllImport("USER32.dll", EntryPoint = "MapVirtualKeyW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern uint MapVirtualKey(uint uCode, MAP_VIRTUAL_KEY_TYPE uMapType);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL UnloadKeyboardLayout(HKL hkl);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static short VkKeyScanEx(char ch, SafeHandle dwhkl)
	{
		bool success = false;
		try
		{
			if (dwhkl != null)
			{
				dwhkl.DangerousAddRef(ref success);
				HKL dwhkl2 = (HKL)dwhkl.DangerousGetHandle();
				return VkKeyScanEx(ch, dwhkl2);
			}
			throw new ArgumentNullException("dwhkl");
		}
		finally
		{
			if (success)
			{
				dwhkl.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", CharSet = CharSet.Unicode, EntryPoint = "VkKeyScanExW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern short VkKeyScanEx(char ch, HKL dwhkl);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static UnloadKeyboardLayoutSafeHandle GetKeyboardLayout_SafeHandle(uint idThread)
	{
		UnloadKeyboardLayoutSafeHandle unloadKeyboardLayoutSafeHandle = new UnloadKeyboardLayoutSafeHandle(0);
		HKL keyboardLayout = GetKeyboardLayout(idThread);
		Marshal.InitHandle(unloadKeyboardLayoutSafeHandle, (nint)keyboardLayout);
		return unloadKeyboardLayoutSafeHandle;
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HKL GetKeyboardLayout(uint idThread);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetMessage(out MSG lpMsg, [Optional] HWND hWnd, uint wMsgFilterMin, uint wMsgFilterMax)
	{
		fixed (MSG* lpMsg2 = &lpMsg)
		{
			return GetMessage(lpMsg2, hWnd, wMsgFilterMin, wMsgFilterMax);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "GetMessageW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL GetMessage(MSG* lpMsg, [Optional] HWND hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL PeekMessage(out MSG lpMsg, [Optional] HWND hWnd, uint wMsgFilterMin, uint wMsgFilterMax, PEEK_MESSAGE_REMOVE_TYPE wRemoveMsg)
	{
		fixed (MSG* lpMsg2 = &lpMsg)
		{
			return PeekMessage(lpMsg2, hWnd, wMsgFilterMin, wMsgFilterMax, wRemoveMsg);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "PeekMessageW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL PeekMessage(MSG* lpMsg, [Optional] HWND hWnd, uint wMsgFilterMin, uint wMsgFilterMax, PEEK_MESSAGE_REMOVE_TYPE wRemoveMsg);

	[DllImport("USER32.dll", EntryPoint = "PostThreadMessageW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL PostThreadMessage(uint idThread, uint Msg, WPARAM wParam, LPARAM lParam);

	[DllImport("USER32.dll", EntryPoint = "PostMessageW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL PostMessage([Optional] HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL TranslateMessage(in MSG lpMsg)
	{
		fixed (MSG* lpMsg2 = &lpMsg)
		{
			return TranslateMessage(lpMsg2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL TranslateMessage(MSG* lpMsg);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static LRESULT DispatchMessage(in MSG lpMsg)
	{
		fixed (MSG* lpMsg2 = &lpMsg)
		{
			return DispatchMessage(lpMsg2);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "DispatchMessageW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern LRESULT DispatchMessage(MSG* lpMsg);

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static WAIT_EVENT MsgWaitForMultipleObjectsEx([Optional] ReadOnlySpan<HANDLE> pHandles, uint dwMilliseconds, QUEUE_STATUS_FLAGS dwWakeMask, MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS dwFlags)
	{
		fixed (HANDLE* pHandles2 = pHandles)
		{
			return MsgWaitForMultipleObjectsEx((uint)pHandles.Length, pHandles2, dwMilliseconds, dwWakeMask, dwFlags);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern WAIT_EVENT MsgWaitForMultipleObjectsEx(uint nCount, [Optional] HANDLE* pHandles, uint dwMilliseconds, QUEUE_STATUS_FLAGS dwWakeMask, MSG_WAIT_FOR_MULTIPLE_OBJECTS_EX_FLAGS dwFlags);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL DestroyIcon(HICON hIcon);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL DestroyCursor(HCURSOR hCursor);

	[DllImport("USER32.dll", EntryPoint = "RegisterClassExW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern ushort RegisterClassEx(in WNDCLASSEXW param0);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL UnregisterClass(string lpClassName, [Optional] SafeHandle hInstance)
	{
		bool success = false;
		try
		{
			fixed (char* ptr = lpClassName)
			{
				HINSTANCE hInstance2;
				if (hInstance != null)
				{
					hInstance.DangerousAddRef(ref success);
					hInstance2 = (HINSTANCE)hInstance.DangerousGetHandle();
				}
				else
				{
					hInstance2 = (HINSTANCE)new IntPtr(0L);
				}
				return UnregisterClass(ptr, hInstance2);
			}
		}
		finally
		{
			if (success)
			{
				hInstance.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", EntryPoint = "UnregisterClassW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL UnregisterClass(PCWSTR lpClassName, [Optional] HINSTANCE hInstance);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL DestroyMenu(HMENU hMenu);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static HWND CreateWindowEx(WINDOW_EX_STYLE dwExStyle, [Optional] string lpClassName, [Optional] string lpWindowName, WINDOW_STYLE dwStyle, int X, int Y, int nWidth, int nHeight, [Optional] HWND hWndParent, [Optional] SafeHandle hMenu, [Optional] SafeHandle hInstance, [Optional] void* lpParam)
	{
		bool success = false;
		bool success2 = false;
		try
		{
			fixed (char* ptr2 = lpWindowName)
			{
				fixed (char* ptr = lpClassName)
				{
					HMENU hMenu2;
					if (hMenu != null)
					{
						hMenu.DangerousAddRef(ref success);
						hMenu2 = (HMENU)hMenu.DangerousGetHandle();
					}
					else
					{
						hMenu2 = (HMENU)new IntPtr(0L);
					}
					HINSTANCE hInstance2;
					if (hInstance != null)
					{
						hInstance.DangerousAddRef(ref success2);
						hInstance2 = (HINSTANCE)hInstance.DangerousGetHandle();
					}
					else
					{
						hInstance2 = (HINSTANCE)new IntPtr(0L);
					}
					return CreateWindowEx(dwExStyle, ptr, ptr2, dwStyle, X, Y, nWidth, nHeight, hWndParent, hMenu2, hInstance2, lpParam);
				}
			}
		}
		finally
		{
			if (success)
			{
				hMenu.DangerousRelease();
			}
			if (success2)
			{
				hInstance.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", EntryPoint = "CreateWindowExW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern HWND CreateWindowEx(WINDOW_EX_STYLE dwExStyle, [Optional] PCWSTR lpClassName, [Optional] PCWSTR lpWindowName, WINDOW_STYLE dwStyle, int X, int Y, int nWidth, int nHeight, [Optional] HWND hWndParent, [Optional] HMENU hMenu, [Optional] HINSTANCE hInstance, [Optional] void* lpParam);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL DestroyWindow(HWND hWnd);

	[DllImport("USER32.dll", EntryPoint = "DefWindowProcW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern LRESULT DefWindowProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL ShowWindow(HWND hWnd, SHOW_WINDOW_CMD nCmdShow);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL SetWindowPos(HWND hWnd, [Optional] HWND hWndInsertAfter, int X, int Y, int cx, int cy, SET_WINDOW_POS_FLAGS uFlags);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetWindowRect(HWND hWnd, out RECT lpRect)
	{
		fixed (RECT* lpRect2 = &lpRect)
		{
			return GetWindowRect(hWnd, lpRect2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL GetWindowRect(HWND hWnd, RECT* lpRect);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows6.1")]
	internal static extern BOOL SetWindowDisplayAffinity(HWND hWnd, WINDOW_DISPLAY_AFFINITY dwAffinity);

	[SupportedOSPlatform("windows6.1")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetWindowDisplayAffinity(HWND hWnd, out uint pdwAffinity)
	{
		fixed (uint* pdwAffinity2 = &pdwAffinity)
		{
			return GetWindowDisplayAffinity(hWnd, pdwAffinity2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows6.1")]
	internal unsafe static extern BOOL GetWindowDisplayAffinity(HWND hWnd, uint* pdwAffinity);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL SetLayeredWindowAttributes(HWND hwnd, COLORREF crKey, byte bAlpha, LAYERED_WINDOW_ATTRIBUTES_FLAGS dwFlags);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetCursorPos(out Point lpPoint)
	{
		fixed (Point* lpPoint2 = &lpPoint)
		{
			return GetCursorPos(lpPoint2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL GetCursorPos(Point* lpPoint);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetCursorInfo(ref CURSORINFO pci)
	{
		fixed (CURSORINFO* pci2 = &pci)
		{
			return GetCursorInfo(pci2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL GetCursorInfo(CURSORINFO* pci);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL SetCursorPos(int X, int Y);

	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static BOOL RegisterRawInputDevices(ReadOnlySpan<RAWINPUTDEVICE> pRawInputDevices, uint cbSize)
	{
		fixed (RAWINPUTDEVICE* pRawInputDevices2 = pRawInputDevices)
		{
			return RegisterRawInputDevices(pRawInputDevices2, (uint)pRawInputDevices.Length, cbSize);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.1.2600")]
	internal unsafe static extern BOOL RegisterRawInputDevices(RAWINPUTDEVICE* pRawInputDevices, uint uiNumDevices, uint cbSize);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows10.0.14393")]
	internal static extern DPI_AWARENESS_CONTEXT SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT dpiContext);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows10.0.14393")]
	internal static extern uint GetDpiForWindow(HWND hwnd);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL EnumDisplayMonitors([Optional] HDC hdc, [Optional] RECT? lprcClip, MONITORENUMPROC lpfnEnum, LPARAM dwData)
	{
		RECT valueOrDefault = lprcClip.GetValueOrDefault();
		return EnumDisplayMonitors(hdc, lprcClip.HasValue ? (&valueOrDefault) : null, lpfnEnum, dwData);
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL EnumDisplayMonitors([Optional] HDC hdc, [Optional] RECT* lprcClip, [MarshalAs(UnmanagedType.FunctionPtr)] MONITORENUMPROC lpfnEnum, LPARAM dwData);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetMonitorInfo(HMONITOR hMonitor, ref MONITORINFO lpmi)
	{
		fixed (MONITORINFO* lpmi2 = &lpmi)
		{
			return GetMonitorInfo(hMonitor, lpmi2);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL GetMonitorInfo(HMONITOR hMonitor, MONITORINFO* lpmi);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HMONITOR MonitorFromPoint(Point pt, MONITOR_FROM_FLAGS dwFlags);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HMONITOR MonitorFromWindow(HWND hwnd, MONITOR_FROM_FLAGS dwFlags);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern int GetSystemMetrics(SYSTEM_METRICS_INDEX nIndex);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern nuint SetTimer([Optional] HWND hWnd, nuint nIDEvent, uint uElapse, [Optional][MarshalAs(UnmanagedType.FunctionPtr)] TIMERPROC lpTimerFunc);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL KillTimer([Optional] HWND hWnd, nuint uIDEvent);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL IsWindow([Optional] HWND hWnd);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HWND WindowFromPoint(Point Point);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HWND GetAncestor(HWND hwnd, GET_ANCESTOR_FLAGS gaFlags);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL SetForegroundWindow(HWND hWnd);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HWND GetForegroundWindow();

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static uint GetWindowThreadProcessId(HWND hWnd, out uint lpdwProcessId)
	{
		fixed (uint* lpdwProcessId2 = &lpdwProcessId)
		{
			return GetWindowThreadProcessId(hWnd, lpdwProcessId2);
		}
	}

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static uint GetWindowThreadProcessId(HWND hWnd)
	{
		return GetWindowThreadProcessId(hWnd, null);
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern uint GetWindowThreadProcessId(HWND hWnd, [Optional] uint* lpdwProcessId);

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static int GetWindowText(HWND hWnd, Span<char> lpString)
	{
		fixed (char* ptr = lpString)
		{
			return GetWindowText(hWnd, ptr, lpString.Length);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "GetWindowTextW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern int GetWindowText(HWND hWnd, PWSTR lpString, int nMaxCount);

	[DllImport("USER32.dll", EntryPoint = "GetWindowTextLengthW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern int GetWindowTextLength(HWND hWnd);

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static int GetClassName(HWND hWnd, Span<char> lpClassName)
	{
		fixed (char* ptr = lpClassName)
		{
			return GetClassName(hWnd, ptr, lpClassName.Length);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "GetClassNameW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern int GetClassName(HWND hWnd, PWSTR lpClassName, int nMaxCount);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL OpenClipboard([Optional] HWND hWndNewOwner);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL CloseClipboard();

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL EmptyClipboard();

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static SafeFileHandle GetClipboardData_SafeHandle(uint uFormat)
	{
		SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: false);
		HANDLE clipboardData = GetClipboardData(uFormat);
		Marshal.InitHandle(safeFileHandle, (nint)clipboardData);
		return safeFileHandle;
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HANDLE GetClipboardData(uint uFormat);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static SafeFileHandle SetClipboardData(uint uFormat, [Optional] SafeHandle hMem)
	{
		bool success = false;
		try
		{
			HANDLE hMem2;
			if (hMem != null)
			{
				hMem.DangerousAddRef(ref success);
				hMem2 = (HANDLE)hMem.DangerousGetHandle();
			}
			else
			{
				hMem2 = (HANDLE)new IntPtr(0L);
			}
			SafeFileHandle safeFileHandle = new SafeFileHandle(0, ownsHandle: true);
			HANDLE hANDLE = SetClipboardData(uFormat, hMem2);
			Marshal.InitHandle(safeFileHandle, (nint)hANDLE);
			return safeFileHandle;
		}
		finally
		{
			if (success)
			{
				hMem.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HANDLE SetClipboardData(uint uFormat, [Optional] HANDLE hMem);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern uint EnumClipboardFormats(uint format);

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL IsClipboardFormatAvailable(uint format);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern uint GetClipboardSequenceNumber();

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HWND GetClipboardOwner();

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static uint RegisterClipboardFormat(string lpszFormat)
	{
		fixed (char* ptr = lpszFormat)
		{
			return RegisterClipboardFormat(ptr);
		}
	}

	[DllImport("USER32.dll", EntryPoint = "RegisterClipboardFormatW", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern uint RegisterClipboardFormat(PCWSTR lpszFormat);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal unsafe static BOOL GetLastInputInfo(ref LASTINPUTINFO plii)
	{
		fixed (LASTINPUTINFO* plii2 = &plii)
		{
			return GetLastInputInfo(plii2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern BOOL GetLastInputInfo(LASTINPUTINFO* plii);

	[SupportedOSPlatform("windows5.0")]
	internal unsafe static int GetKeyboardLayoutList([Optional] Span<HKL> lpList)
	{
		fixed (HKL* lpList2 = lpList)
		{
			return GetKeyboardLayoutList(lpList.Length, lpList2);
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true, SetLastError = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal unsafe static extern int GetKeyboardLayoutList(int nBuff, [Optional] HKL* lpList);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HWND GetShellWindow();

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern BOOL UnhookWinEvent(HWINEVENTHOOK hWinEventHook);

	[SupportedOSPlatform("windows5.0")]
	[OverloadResolutionPriority(1)]
	internal static UnhookWinEventSafeHandle SetWinEventHook(uint eventMin, uint eventMax, [Optional] SafeHandle hmodWinEventProc, WINEVENTPROC pfnWinEventProc, uint idProcess, uint idThread, uint dwFlags)
	{
		bool success = false;
		try
		{
			HMODULE hmodWinEventProc2;
			if (hmodWinEventProc != null)
			{
				hmodWinEventProc.DangerousAddRef(ref success);
				hmodWinEventProc2 = (HMODULE)hmodWinEventProc.DangerousGetHandle();
			}
			else
			{
				hmodWinEventProc2 = (HMODULE)new IntPtr(0L);
			}
			UnhookWinEventSafeHandle unhookWinEventSafeHandle = new UnhookWinEventSafeHandle(0);
			HWINEVENTHOOK hWINEVENTHOOK = SetWinEventHook(eventMin, eventMax, hmodWinEventProc2, pfnWinEventProc, idProcess, idThread, dwFlags);
			Marshal.InitHandle(unhookWinEventSafeHandle, (nint)hWINEVENTHOOK);
			return unhookWinEventSafeHandle;
		}
		finally
		{
			if (success)
			{
				hmodWinEventProc.DangerousRelease();
			}
		}
	}

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern HWINEVENTHOOK SetWinEventHook(uint eventMin, uint eventMax, [Optional] HMODULE hmodWinEventProc, [MarshalAs(UnmanagedType.FunctionPtr)] WINEVENTPROC pfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

	[DllImport("USER32.dll", ExactSpelling = true)]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[SupportedOSPlatform("windows5.0")]
	internal static extern int GetMessageTime();
}
