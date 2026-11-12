using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Voice;

public interface IPageReader
{
	Task<PageReference?> CaptureReferenceAsync(CancellationToken ct = default(CancellationToken));

	Task<PageContext> ExtractAsync(PageReference reference, CancellationToken ct = default(CancellationToken));
}
