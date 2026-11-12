using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky;

public interface IDictationTextTarget
{
	Task<DictationFocus?> CaptureFocusAsync(CancellationToken ct = default(CancellationToken));

	DictationWorkApp? ForegroundWorkApp();

	Task<DictationInsertOutcome> InsertAsync(string text, DictationFocus focus, Action<string, int>? onPasteVerification = null);

	Task<bool> BlindPasteAsync(string text, DictationWorkApp app);

	Task<bool> CopyToClipboardAsync(string text);

	Task<string?> ReadFieldValueAsync(DictationFocus focus);
}
