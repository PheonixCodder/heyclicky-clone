using System;
using System.Threading.Tasks;
using HeyClicky.Core.Platform;
using HeyClicky.Platform.Win32;

namespace HeyClicky.App.Wiring;

public sealed class PlatformDesktopActions : IDesktopActions
{
	private readonly WindowsDesktopActions basic = new WindowsDesktopActions();

	private readonly DictationTextTarget fieldTarget = new DictationTextTarget();

	public bool OpenUrl(string url)
	{
		return basic.OpenUrl(url);
	}

	public bool CopyToClipboard(string text)
	{
		return basic.CopyToClipboard(text);
	}

	public async Task<string> TypeIntoFocusedFieldAsync(string text)
	{
		string status = (await InsertIntoFocusedFieldAsync(text).ConfigureAwait(continueOnCapturedContext: false)).Status;
		return (status == "inserted") ? "typed" : ((!(status == "copied_to_clipboard")) ? "refused" : "copied_to_clipboard");
	}

	public async Task<TextInsertOutcome> InsertIntoFocusedFieldAsync(string text)
	{
		UiaPageText.FocusTarget target = await UiaPageText.FocusedTargetAsync().ConfigureAwait(continueOnCapturedContext: false);
		if ((object)target == null || !target.HasFocus)
		{
			return new TextInsertOutcome("refused", "no_focused_text_field");
		}
		if (target.ProcessId == Environment.ProcessId)
		{
			return new TextInsertOutcome("refused", "own_window");
		}
		if (target.IsPassword)
		{
			return new TextInsertOutcome("refused", "secure_field");
		}
		if (target.IsAddressBar)
		{
			return new TextInsertOutcome("refused", "address_bar");
		}
		string appName = ForegroundAppInfo.CaptureUserApp()?.AppName ?? target.AppName;
		DictationFocus dictationFocus = await fieldTarget.CaptureFocusAsync().ConfigureAwait(continueOnCapturedContext: false);
		if ((object)dictationFocus != null && !dictationFocus.IsSecure)
		{
			TaskCompletionSource<string> verdict = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
			DictationInsertOutcome dictationInsertOutcome = await fieldTarget.InsertAsync(text, dictationFocus, appendTrailingSpace: false, delegate(string result, int _)
			{
				verdict.TrySetResult(result);
			}).ConfigureAwait(continueOnCapturedContext: false);
			switch (dictationInsertOutcome)
			{
			case DictationInsertOutcome.Inserted:
			{
				string text2 = ((await Task.WhenAny(verdict.Task, Task.Delay(TimeSpan.FromSeconds(3L))).ConfigureAwait(continueOnCapturedContext: false) == verdict.Task) ? verdict.Task.Result : "no_answer");
				Log.Info("Desktop", $"insert into {target.AppName ?? "?"} ({target.ControlType}): pasted, landing {text2}");
				return (text2 == "not_observed") ? new TextInsertOutcome("unconfirmed", "not_observed", appName) : new TextInsertOutcome("inserted", null, appName);
			}
			case DictationInsertOutcome.ClipboardWriteFailed:
				return new TextInsertOutcome("failed", "clipboard_unavailable");
			}
			Log.Info("Desktop", $"insert via the dictation path: {dictationInsertOutcome}; pasting directly");
		}
		bool flag = await TextInsertion.PasteTextAsync(text, null).ConfigureAwait(continueOnCapturedContext: false);
		Log.Info("Desktop", $"insert into {target.AppName ?? "?"} ({target.ControlType}): {(flag ? "pasted (unverified)" : "clipboard write failed")}");
		return flag ? new TextInsertOutcome("inserted", null, appName) : new TextInsertOutcome("failed", "clipboard_unavailable");
	}

	public (string? DisplayName, string? ProcessName) FrontApplication()
	{
		ForegroundApp foregroundApp = ForegroundAppInfo.CaptureUserApp();
		if ((object)foregroundApp != null)
		{
			return (DisplayName: foregroundApp.AppName, ProcessName: foregroundApp.ProcessName);
		}
		return (DisplayName: null, ProcessName: null);
	}
}
