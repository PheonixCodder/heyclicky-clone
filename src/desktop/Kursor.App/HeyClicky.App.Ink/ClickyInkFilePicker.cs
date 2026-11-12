using System;
using System.Threading.Tasks;
using HeyClicky.App.Interop;
using Microsoft.UI;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;

namespace HeyClicky.App.Ink;

public static class ClickyInkFilePicker
{
	public static Task<string?> ChooseAsync(FrameworkElement anchor, string message, bool folder = false)
	{
		try
		{
			nint owner = 0;
			ContentIslandEnvironment contentIslandEnvironment = anchor.XamlRoot?.ContentIslandEnvironment;
			if ((object)contentIslandEnvironment != null)
			{
				owner = Win32Interop.GetWindowFromWindowId(contentIslandEnvironment.AppWindowId);
			}
			string title = ((!string.IsNullOrWhiteSpace(message)) ? message.Trim() : (folder ? "Choose a folder" : "Choose a file"));
			return Task.FromResult(FolderDialog.Pick(owner, title, "Choose", null, folder));
		}
		catch (Exception ex)
		{
			Log.Warn("ink", "file picker (" + message + "): " + ex.Message);
			return Task.FromResult<string>(null);
		}
	}
}
