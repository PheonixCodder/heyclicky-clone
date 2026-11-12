using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HeyClicky.Core.Browser;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace HeyClicky.App.Dock;

internal static class ChromeIcon
{
	private static readonly Dictionary<string, ImageSource> icons = new Dictionary<string, ImageSource>();

	private static readonly Dictionary<string, string?> executables = new Dictionary<string, string>();

	private static readonly HashSet<string> lookedUp = new HashSet<string>();

	public static FrameworkElement? Make(double size)
	{
		string setupBrowserName = ClickyBrowserBridge.Shared.SetupBrowserName;
		if (!executables.TryGetValue(setupBrowserName, out string value))
		{
			value = (executables[setupBrowserName] = ChromiumBrowsers.ExecutablePathFor(setupBrowserName));
		}
		if (value == null || value.Length <= 0)
		{
			return null;
		}
		Image image = new Image
		{
			Width = size,
			Height = size,
			Stretch = Stretch.Uniform
		};
		if (icons.TryGetValue(setupBrowserName, out ImageSource value2))
		{
			image.Source = value2;
		}
		else if (lookedUp.Add(setupBrowserName))
		{
			LoadAsync(setupBrowserName, value, image);
		}
		return image;
	}

	private static async Task LoadAsync(string name, string executable, Image image)
	{
		_ = 2;
		try
		{
			using StorageItemThumbnail thumbnail = await (await StorageFile.GetFileFromPathAsync(executable)).GetThumbnailAsync(ThumbnailMode.SingleItem, 64u);
			BitmapImage bitmap = new BitmapImage();
			await bitmap.SetSourceAsync(thumbnail);
			icons[name] = bitmap;
			image.Source = bitmap;
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "browser icon: " + ex.Message);
		}
	}
}
