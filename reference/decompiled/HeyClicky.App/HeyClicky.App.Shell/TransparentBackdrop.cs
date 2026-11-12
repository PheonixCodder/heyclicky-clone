using System;
using System.Runtime.InteropServices;
using HeyClicky.App.Interop;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI;
using Windows.UI.Composition;

namespace HeyClicky.App.Shell;

public sealed class TransparentBackdrop : SystemBackdrop
{
	private static Windows.UI.Composition.Compositor? systemCompositor;

	private static nint dispatcherQueueController;

	internal static Windows.UI.Composition.Compositor SystemCompositor
	{
		get
		{
			if ((object)systemCompositor == null)
			{
				if ((object)Windows.System.DispatcherQueue.GetForCurrentThread() == null)
				{
					Native.DispatcherQueueOptions options = default(Native.DispatcherQueueOptions);
					options.dwSize = Marshal.SizeOf<Native.DispatcherQueueOptions>();
					options.threadType = 2;
					options.apartmentType = 0;
					Native.CreateDispatcherQueueController(options, out dispatcherQueueController);
				}
				systemCompositor = new Windows.UI.Composition.Compositor();
			}
			return systemCompositor;
		}
	}

	protected override void OnTargetConnected(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
	{
		base.OnTargetConnected(connectedTarget, xamlRoot);
		try
		{
			connectedTarget.SystemBackdrop = SystemCompositor.CreateColorBrush(Color.FromArgb(0, 0, 0, 0));
			Log.Info("backdrop", "transparent backdrop connected");
		}
		catch (Exception ex)
		{
			Log.Error("backdrop", "connect failed", ex);
		}
	}

	protected override void OnDefaultSystemBackdropConfigurationChanged(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop target, XamlRoot xamlRoot)
	{
	}

	protected override void OnTargetDisconnected(Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop disconnectedTarget)
	{
		base.OnTargetDisconnected(disconnectedTarget);
		disconnectedTarget.SystemBackdrop = null;
	}
}
