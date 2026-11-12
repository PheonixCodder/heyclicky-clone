using System;
using HeyClicky.App.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Home;

public static class HomeParts
{
	public const string GiftPath = "M2.5,8.2 h8.6 v3.8 h-8.6 z M12.9,8.2 h8.6 v3.8 h-8.6 z M4,13.4 h7.1 v8.1 h-5.6 a1.5,1.5 0 0 1 -1.5,-1.5 z M12.9,13.4 h7.1 v6.6 a1.5,1.5 0 0 1 -1.5,1.5 h-5.6 z M12,7.6 C10.6,4.6 7.8,2.8 6.6,4.2 C5.6,5.4 6.6,7.4 12,7.6 z M12,7.6 C13.4,4.6 16.2,2.8 17.4,4.2 C18.4,5.4 17.4,7.4 12,7.6 z";

	public static Grid AccountAvatar(double size, string initial, string? avatarUrl)
	{
		Grid grid = new Grid
		{
			Width = size,
			Height = size,
			Tag = "account-avatar"
		};
		grid.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(HomeTheme.AvatarPlate)
		});
		grid.Children.Add(Ui.Text(initial, Math.Round(size * 0.43), FontWeights.SemiBold, HomeTheme.PrimaryText).Center());
		Uri uri = AvatarUri(avatarUrl);
		if ((object)uri != null)
		{
			BitmapImage bitmapImage = new BitmapImage
			{
				DecodePixelWidth = (int)Math.Ceiling(size * 2.0),
				DecodePixelType = DecodePixelType.Logical
			};
			Ellipse photo = new Ellipse
			{
				Opacity = 0.0,
				Tag = "account-avatar-photo",
				Fill = new ImageBrush
				{
					ImageSource = bitmapImage,
					Stretch = Stretch.UniformToFill
				}
			};
			bitmapImage.ImageOpened += delegate
			{
				photo.Opacity = 1.0;
			};
			bitmapImage.ImageFailed += delegate(object _, ExceptionRoutedEventArgs e)
			{
				Log.Warn("home", "profile photo failed: " + e.ErrorMessage);
			};
			bitmapImage.UriSource = uri;
			grid.Children.Add(photo);
		}
		return grid;
	}

	public static Uri? AvatarUri(string? avatarUrl)
	{
		Uri result = default(Uri);
		bool flag = avatarUrl != null && avatarUrl.Length > 0 && Uri.TryCreate(avatarUrl, UriKind.Absolute, out result);
		if (flag)
		{
			string scheme = result.Scheme;
			bool flag2 = ((scheme == "https" || scheme == "http") ? true : false);
			flag = flag2 || result.IsFile;
		}
		if (!flag)
		{
			return null;
		}
		return result;
	}
}
