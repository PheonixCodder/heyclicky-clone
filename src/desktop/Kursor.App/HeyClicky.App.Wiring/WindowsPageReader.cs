using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Documents;
using HeyClicky.Core.Voice;
using HeyClicky.Platform.Win32;

namespace HeyClicky.App.Wiring;

public sealed class WindowsPageReader : IPageReader
{
	public async Task<PageReference?> CaptureReferenceAsync(CancellationToken ct = default(CancellationToken))
	{
		ForegroundApp foregroundApp = await ForegroundAppInfo.CaptureUserAppAsync(includeBrowserUrl: true, ct).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)foregroundApp == null || foregroundApp.ProcessId == Environment.ProcessId)
		{
			return null;
		}
		if (ForegroundAppInfo.IsBrowser(foregroundApp.ProcessName))
		{
			string text = WebAddress(foregroundApp.BrowserUrl);
			if (text == null)
			{
				return null;
			}
			if (text.StartsWith("file:", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(text, UriKind.Absolute, out Uri result) && File.Exists(result.LocalPath))
			{
				return new PageReference("file", result.LocalPath, foregroundApp.ProcessId, foregroundApp.WindowHandle);
			}
			return text.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? new PageReference("web_page", text, foregroundApp.ProcessId, foregroundApp.WindowHandle) : null;
		}
		string text2 = RecentFiles.FileNameFromTitle(foregroundApp.WindowTitle);
		if (text2 == null)
		{
			return null;
		}
		string text3 = RecentFiles.Resolve(text2);
		return new PageReference("file", text3 ?? text2, foregroundApp.ProcessId, foregroundApp.WindowHandle);
	}

	internal static string? WebAddress(string? shown)
	{
		string text = shown?.Trim();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		bool flag = text.Length > 3 && char.IsAsciiLetter(text[0]) && text[1] == ':';
		if (flag)
		{
			char c = text[2];
			bool flag2 = ((c == '/' || c == '\\') ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return new Uri(text.Replace('/', '\\')).AbsoluteUri;
		}
		if (text.Contains(' '))
		{
			return null;
		}
		if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		if (text.Contains("://", StringComparison.Ordinal) || text.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		if (!text.Contains('.') && !text.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		return "https://" + text;
	}

	public async Task<PageContext> ExtractAsync(PageReference reference, CancellationToken ct = default(CancellationToken))
	{
		if (reference.IsWebPage)
		{
			UiaPageText.PageText pageText = await UiaPageText.ReadAsync(reference.WindowHandle, 60000, ct).ConfigureAwait(continueOnCapturedContext: false);
			return new PageContext("web_page", reference.Reference, pageText?.Text, pageText?.IsTruncated ?? false, null, AccessDenied: false);
		}
		PageContext fromFile = null;
		if (Path.IsPathFullyQualified(reference.Reference) && File.Exists(reference.Reference))
		{
			fromFile = await Task.Run(() => DocumentText.Extract(reference.Reference), ct).ConfigureAwait(continueOnCapturedContext: false);
			if (fromFile.AccessDenied || !string.IsNullOrEmpty(fromFile.Text))
			{
				return fromFile;
			}
		}
		bool flag = reference.WindowHandle != 0;
		UiaPageText.PageText shown = default(UiaPageText.PageText);
		if (flag)
		{
			shown = await UiaPageText.ReadAsync(reference.WindowHandle, 60000, ct).ConfigureAwait(continueOnCapturedContext: false);
			flag = (object)shown != null;
		}
		if (flag)
		{
			return new PageContext((fromFile?.Kind == "pdf") ? "pdf" : "text_file", reference.Reference, shown.Text, shown.IsTruncated, fromFile?.PageCount, AccessDenied: false);
		}
		return fromFile ?? PageContext.ReferenceOnly(reference);
	}
}
