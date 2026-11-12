using System;
using System.IO;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.Ink;
using HeyClicky.App.UI;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HeyClicky.App.Dock;

public sealed class FileCard : Grid
{
	public const double CardWidth = 138.0;

	public const double CardHeight = 166.0;

	private const double PreviewHeight = 92.0;

	private readonly ShadowLayer restShadow;

	private readonly ShadowLayer liftShadow;

	public string Entry { get; }

	public FileCard(string entry, string? workspace, string readyButtonTitle, Action open)
	{
		Entry = entry;
		base.Width = 138.0;
		base.Height = 166.0;
		bool flag = CardReadyOutput.IsWebLink(entry);
		string text = DockFiles.Resolve(entry, workspace);
		DockFileStyle dockFileStyle = DockFileStyle.For(flag ? entry : text);
		restShadow = new ShadowLayer(10.0, DS.Black(0.16), 5.0, 3.0);
		liftShadow = new ShadowLayer(10.0, DS.Black(0.24), 10.0, 6.0)
		{
			Opacity = 0.0
		};
		base.Children.Add(restShadow);
		base.Children.Add(liftShadow);
		base.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(10.0),
			Background = Ui.Brush(Colors.White),
			BorderBrush = Ui.Brush(DS.Black(0.07)),
			BorderThickness = new Thickness(0.5)
		});
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 7.0,
			Padding = new Thickness(7.0)
		};
		Grid grid = new Grid
		{
			Width = 124.0,
			Height = 92.0
		};
		if (flag)
		{
			grid.Children.Add(ClickyInkLinkPreviewPlate.Make(entry, 40.0, null));
		}
		else
		{
			grid.Children.Add(new Border
			{
				Background = Ui.Brush(dockFileStyle.Tint.WithAlpha(0.14))
			});
			grid.Children.Add(Icons.Glyph(dockFileStyle.Glyph, 26.0, dockFileStyle.Tint));
		}
		Ui.ClipRounded(grid, 6.0);
		Grid item = new Grid
		{
			Children = 
			{
				(UIElement)grid,
				(UIElement)new Border
				{
					CornerRadius = new CornerRadius(6.0),
					BorderBrush = Ui.Brush(DS.Black(0.22)),
					BorderThickness = new Thickness(1.0)
				}
			}
		};
		stackPanel.Children.Add(item);
		if (!flag)
		{
			ImageSource imageSource = ArtifactThumbnails.Cached(text);
			if ((object)imageSource != null)
			{
				ShowPicture(grid, imageSource);
			}
			else
			{
				LoadAsync(grid, text);
			}
		}
		Uri result;
		TextBlock textBlock = Ui.Text((flag && Uri.TryCreate(entry, UriKind.Absolute, out result)) ? result.Host : Path.GetFileName(text.TrimEnd('\\', '/')), 12.0, FontWeights.Bold, ArcColors.Ink);
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		StackPanel stackPanel2 = new StackPanel
		{
			Spacing = 5.0,
			Padding = new Thickness(3.0, 4.0, 3.0, 6.0)
		};
		stackPanel2.Children.Add(textBlock);
		GelButton gelButton = ArcButtons.Gel(readyButtonTitle, FileCardStack.FileKind(flag ? entry : text).Symbol, GelAppearance.SecondaryWhite, null, null, GelButton.Sizing.Compact, open);
		gelButton.HorizontalAlignment = HorizontalAlignment.Left;
		stackPanel2.Children.Add(gelButton);
		stackPanel.Children.Add(stackPanel2);
		base.Children.Add(stackPanel);
	}

	private static void ShowPicture(Grid preview, ImageSource image)
	{
		preview.Children.Add(new Image
		{
			Source = image,
			Stretch = Stretch.UniformToFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});
	}

	private static async Task LoadAsync(Grid preview, string path)
	{
		if (!File.Exists(path))
		{
			return;
		}
		try
		{
			ImageSource imageSource = await ArtifactThumbnails.GetAsync(path, 260);
			if ((object)imageSource != null)
			{
				ShowPicture(preview, imageSource);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("dock", "file card picture: " + ex.Message);
		}
	}

	public void SetHovered(bool hovered)
	{
		Anim.Opacity(restShadow, (!hovered) ? 1 : 0, 0.2);
		Anim.Opacity(liftShadow, hovered ? 1 : 0, 0.2);
	}

	public static string AccessibilityText(string entry, string? workspace)
	{
		bool flag = CardReadyOutput.IsWebLink(entry);
		string text = DockFiles.Resolve(entry, workspace);
		Uri result;
		string text2 = ((!flag) ? Path.GetFileName(text) : (Uri.TryCreate(entry, UriKind.Absolute, out result) ? result.Host : entry));
		return "Open " + text2 + ", " + DockFileStyle.For(flag ? entry : text).Kind;
	}
}
