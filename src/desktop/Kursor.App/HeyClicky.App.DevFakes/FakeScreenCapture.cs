using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Interop;
using HeyClicky.App.Shell;

namespace HeyClicky.App.DevFakes;

public sealed class FakeScreenCapture : IScreenCaptureService
{
	public IReadOnlyList<DisplayInfo> GetDisplays()
	{
		List<DisplayInfo> list = new List<DisplayInfo>();
		Native.EnumDisplayMonitors(0, 0, delegate(nint mon, nint hdc, ref Native.RECT rc, nint data)
		{
			Displays.Monitor monitor = Displays.From(mon);
			list.Add(new DisplayInfo(list.Count, $"DISPLAY{list.Count + 1}", new RectD(monitor.Bounds.Left, monitor.Bounds.Top, monitor.Bounds.Width, monitor.Bounds.Height), new RectD(monitor.Work.Left, monitor.Work.Top, monitor.Work.Width, monitor.Work.Height), monitor.Scale, monitor.Bounds.Left == 0 && monitor.Bounds.Top == 0));
			return true;
		}, 0);
		return list;
	}

	public DisplayInfo GetDisplayUnderCursor()
	{
		return GetDisplays().FirstOrDefault((DisplayInfo d) => d.IsPrimary) ?? GetDisplays()[0];
	}

	public Task<IReadOnlyList<ScreenCaptureFrame>> CaptureAllAsync(int maxDimension = 1280, int jpegQuality = 80, CancellationToken ct = default(CancellationToken))
	{
		return Task.FromResult((IReadOnlyList<ScreenCaptureFrame>)Array.Empty<ScreenCaptureFrame>());
	}
}
