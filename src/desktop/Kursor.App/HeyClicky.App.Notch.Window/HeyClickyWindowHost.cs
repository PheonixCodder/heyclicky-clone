using System;
using System.Collections.Generic;
using HeyClicky.App.Interop;
using HeyClicky.App.Notch.Peek;
using HeyClicky.App.Services;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch.Window;

internal sealed class HeyClickyWindowHost(INotchHost notch, HeyClickyWindow window, nint hwnd) : INotchHost, IHeyClickyWindowHost
{
	private bool active;

	public AppServices Services => notch.Services;

	public IAppActions Actions => notch.Actions;

	public NotchGeometry.SizeContext SizeContext => notch.SizeContext;

	public bool IsHovering => false;

	public nint WindowHandle => hwnd;

	public bool HasKeyboard => active;

	public MultiAgentRotationCursor MultiAgentRotation => notch.MultiAgentRotation;

	public event Action<bool>? KeyboardChanged;

	public event Action<bool>? HoverChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	public event Action? MultiAgentRotated
	{
		add
		{
			notch.MultiAgentRotated += value;
		}
		remove
		{
			notch.MultiAgentRotated -= value;
		}
	}

	public void Relayout()
	{
	}

	public void BindToEdges(UIElement element, double ax, double ay)
	{
	}

	public void RequestKeyboard()
	{
		Native.SetForegroundWindow(hwnd);
	}

	public void ReleaseKeyboard()
	{
	}

	public void ReleaseKeyboardIfUnheld()
	{
	}

	public IReadOnlyList<string> TakePendingFiles()
	{
		return Array.Empty<string>();
	}

	public void NoteActive(bool isActive)
	{
		active = isActive;
		this.KeyboardChanged?.Invoke(isActive);
	}

	public void OpenAgentForEntry(string entryId)
	{
		window.OpenAgentForEntry(entryId);
	}
}
