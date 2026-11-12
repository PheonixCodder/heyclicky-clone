using System;
using System.Numerics;
using System.Runtime.InteropServices;
using HeyClicky.App.Shell;
using WinRT;
using Windows.UI.Composition;
using Windows.UI.Composition.Desktop;

namespace HeyClicky.App.Notch;

public sealed class NotchSystemLayer
{
	private readonly DesktopWindowTarget target;

	private readonly ContainerVisual root;

	public Compositor Compositor { get; }

	private NotchSystemLayer(Compositor compositor, DesktopWindowTarget target)
	{
		Compositor = compositor;
		this.target = target;
		root = compositor.CreateContainerVisual();
		target.Root = root;
	}

	public unsafe static NotchSystemLayer? Create(nint window)
	{
		try
		{
			Compositor systemCompositor = TransparentBackdrop.SystemCompositor;
			Marshal.ThrowExceptionForHR(Marshal.QueryInterface(((IWinRTObject)systemCompositor).NativeObject.ThisPtr, new Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"), out var ppv));
			int num2;
			nint num = default(nint);
			try
			{
				num2 = ((delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)ppv) + (nint)3 * (nint)sizeof(nint))))(ppv, window, 0, &num);
			}
			finally
			{
				Marshal.Release(ppv);
			}
			if (num2 < 0)
			{
				Log.Warn("notch", $"no system layer under the content (0x{num2:X8}): the shape draws on WinUI's compositor");
				return null;
			}
			try
			{
				return new NotchSystemLayer(systemCompositor, DesktopWindowTarget.FromAbi(num));
			}
			finally
			{
				Marshal.Release(num);
			}
		}
		catch (Exception ex)
		{
			Log.Error("notch", "no system layer under the content: the shape draws on WinUI's compositor", ex);
			return null;
		}
	}

	public void Add(Visual visual)
	{
		root.Children.InsertAtTop(visual);
	}

	public void SetScale(double scale, double width, double height)
	{
		root.Scale = new Vector3((float)scale, (float)scale, 1f);
		root.Size = new Vector2((float)width, (float)height);
	}
}
