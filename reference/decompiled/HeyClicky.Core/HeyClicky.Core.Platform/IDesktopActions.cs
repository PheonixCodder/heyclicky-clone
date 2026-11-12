using System.Threading.Tasks;

namespace HeyClicky.Core.Platform;

public interface IDesktopActions
{
	bool OpenUrl(string url);

	bool CopyToClipboard(string text);

	Task<string> TypeIntoFocusedFieldAsync(string text);

	(string? DisplayName, string? ProcessName) FrontApplication();

	async Task<TextInsertOutcome> InsertIntoFocusedFieldAsync(string text)
	{
		string text2 = await TypeIntoFocusedFieldAsync(text).ConfigureAwait(continueOnCapturedContext: false);
		return (text2 == "typed") ? new TextInsertOutcome("inserted") : ((!(text2 == "copied_to_clipboard")) ? new TextInsertOutcome("refused", "unsafe_target") : new TextInsertOutcome("copied_to_clipboard"));
	}
}
