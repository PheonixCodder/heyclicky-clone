using HeyClicky.App.Interop;
using HeyClicky.App.Shell;
using HeyClicky.Core.Notch;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Notch.Peek;

internal sealed class NotchChatResizeGrip : Grid
{
	private readonly INotchHost host;

	private readonly bool left;

	private readonly bool right;

	private readonly bool bottom;

	private (int X, int Y)? dragStartPx;

	private double dragStartWidth;

	private double dragStartHeight;

	private double scale = 1.0;

	public NotchChatResizeGrip(INotchHost host, bool left, bool right, bool bottom)
	{
		this.host = host;
		this.left = left;
		this.right = right;
		this.bottom = bottom;
		base.Background = new SolidColorBrush(Colors.Transparent);
		base.ProtectedCursor = InputSystemCursor.Create((left && bottom) ? InputSystemCursorShape.SizeNortheastSouthwest : ((right && bottom) ? InputSystemCursorShape.SizeNorthwestSoutheast : (bottom ? InputSystemCursorShape.SizeNorthSouth : InputSystemCursorShape.SizeWestEast)));
		base.PointerPressed += OnPressed;
		base.PointerMoved += OnMoved;
		base.PointerReleased += delegate(object _, PointerRoutedEventArgs e)
		{
			End(e.Pointer);
		};
		base.PointerCaptureLost += delegate(object _, PointerRoutedEventArgs e)
		{
			End(e.Pointer);
		};
		base.DoubleTapped += delegate(object _, DoubleTappedRoutedEventArgs e)
		{
			e.Handled = true;
			NotchChatSize.ResetToDefault();
		};
	}

	private static (int X, int Y) Cursor()
	{
		Native.GetCursorPos(out var p);
		return (X: p.X, Y: p.Y);
	}

	private void OnPressed(object sender, PointerRoutedEventArgs e)
	{
		if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			e.Handled = true;
			CapturePointer(e.Pointer);
			dragStartPx = Cursor();
			scale = Displays.ForWindow(host.WindowHandle).Scale;
			dragStartWidth = NotchChatSize.ContentWidth;
			dragStartHeight = NotchChatSize.BodyHeight;
			(host.Services.Presenter as NotchPresenter)?.SetNotchChatBeingResized(resizing: true);
			NotchChatSize.BeginResizing();
		}
	}

	private void OnMoved(object sender, PointerRoutedEventArgs e)
	{
		(int, int)? tuple = dragStartPx;
		if (tuple.HasValue)
		{
			(int, int) valueOrDefault = tuple.GetValueOrDefault();
			e.Handled = true;
			(int X, int Y) tuple2 = Cursor();
			double num = (double)(tuple2.X - valueOrDefault.Item1) / scale;
			double num2 = (double)(tuple2.Y - valueOrDefault.Item2) / scale;
			double num3 = dragStartWidth;
			double num4 = dragStartHeight;
			if (right)
			{
				num3 += 2.0 * num;
			}
			if (left)
			{
				num3 -= 2.0 * num;
			}
			if (bottom)
			{
				num4 += num2;
			}
			NotchChatSize.Resize(num3, num4);
		}
	}

	private void End(Pointer pointer)
	{
		(int, int)? tuple = dragStartPx;
		if (tuple.HasValue)
		{
			dragStartPx = null;
			ReleasePointerCapture(pointer);
			NotchChatSize.EndResizing();
			(host.Services.Presenter as NotchPresenter)?.SetNotchChatBeingResized(resizing: false);
		}
	}
}
