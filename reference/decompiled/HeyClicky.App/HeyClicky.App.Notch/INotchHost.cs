using System;
using System.Collections.Generic;
using HeyClicky.App.Services;
using HeyClicky.Core.Notch;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Notch;

public interface INotchHost
{
	AppServices Services { get; }

	IAppActions Actions { get; }

	NotchGeometry.SizeContext SizeContext { get; }

	bool IsHovering { get; }

	nint WindowHandle { get; }

	bool HasKeyboard { get; }

	MultiAgentRotationCursor MultiAgentRotation { get; }

	event Action<bool>? KeyboardChanged;

	event Action<bool>? HoverChanged;

	event Action? MultiAgentRotated;

	void Relayout();

	void BindToEdges(UIElement element, double ax, double ay);

	void RequestKeyboard();

	void ReleaseKeyboard();

	void ReleaseKeyboardIfUnheld();

	IReadOnlyList<string> TakePendingFiles();
}
