using System;
using System.Runtime.InteropServices;

namespace HeyClicky.App.Interop;

internal sealed class WindowSubclass : IDisposable
{
	public delegate nint? Handler(nint hwnd, uint msg, nint wParam, nint lParam);

	private delegate nint SubclassProc(nint hwnd, uint msg, nint wParam, nint lParam, nuint id, nuint refData);

	private static nuint nextId = 1u;

	private readonly nint hwnd;

	private readonly nuint id;

	private readonly SubclassProc proc;

	private readonly Handler handler;

	[DllImport("comctl32.dll")]
	private static extern bool SetWindowSubclass(nint hwnd, SubclassProc proc, nuint id, nuint refData);

	[DllImport("comctl32.dll")]
	private static extern bool RemoveWindowSubclass(nint hwnd, SubclassProc proc, nuint id);

	[DllImport("comctl32.dll")]
	private static extern nint DefSubclassProc(nint hwnd, uint msg, nint wParam, nint lParam);

	public WindowSubclass(nint hwnd, Handler handler)
	{
		this.hwnd = hwnd;
		this.handler = handler;
		id = nextId++;
		proc = Proc;
		SetWindowSubclass(hwnd, proc, id, 0u);
	}

	private nint Proc(nint h, uint msg, nint w, nint l, nuint _, nuint __)
	{
		try
		{
			nint? num = handler(h, msg, w, l);
			if (num.HasValue)
			{
				return num.Value;
			}
		}
		catch (Exception ex)
		{
			Log.Error("subclass", $"msg 0x{msg:X}", ex);
		}
		return DefSubclassProc(h, msg, w, l);
	}

	public void Dispose()
	{
		RemoveWindowSubclass(hwnd, proc, id);
	}
}
