using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky;

public interface IScreenCaptureService
{
	IReadOnlyList<DisplayInfo> GetDisplays();

	DisplayInfo GetDisplayUnderCursor();

	Task<IReadOnlyList<ScreenCaptureFrame>> CaptureAllAsync(int maxDimension = 1280, int jpegQuality = 80, CancellationToken ct = default(CancellationToken));
}
