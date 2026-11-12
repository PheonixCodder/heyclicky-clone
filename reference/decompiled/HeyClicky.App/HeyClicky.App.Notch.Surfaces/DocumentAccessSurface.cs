using HeyClicky.App.Design;
using HeyClicky.App.Interop;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class DocumentAccessSurface : NotchSurface
{
	private readonly Grid view;

	public override FrameworkElement View => view;

	public DocumentAccessSurface(NotchLiveEvent.DocumentFolderAccessDenied e)
	{
		DocumentAccessSurface documentAccessSurface = this;
		TextBlock textBlock = Icons.Glyph("\ue8b7", 14.0, DS.Hex(2450411u));
		textBlock.FontWeight = FontWeights.SemiBold;
		TextBlock textBlock2 = Ui.Text("?", 7.0, FontWeights.Black, DS.Hex(2450411u), TextAlignment.Center);
		textBlock2.HorizontalAlignment = HorizontalAlignment.Center;
		textBlock2.VerticalAlignment = VerticalAlignment.Center;
		Grid item = new Grid
		{
			Width = 8.0,
			Height = 8.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, -3.0, -1.0),
			Children = 
			{
				(UIElement)new Ellipse
				{
					Fill = Ui.Brush(DS.White(0.94))
				},
				(UIElement)textBlock2
			}
		};
		Grid tile = CardParts.Tile(new Grid
		{
			Width = 16.0,
			Height = 14.0,
			Children = 
			{
				(UIElement)textBlock,
				(UIElement)item
			}
		});
		StackPanel lines = CardParts.TwoLines("HeyClicky can't read this document", e.Subtitle);
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Spacing = 11.0
		};
		if (e.ShowsDontShowAgain)
		{
			GelActionButton gelActionButton = new GelActionButton("Don't show again", null, GelAppearance.SecondaryWhite, 30.0, 14.0).Tip("Never show this reminder again");
			gelActionButton.Click += delegate
			{
				FlowHooks.SuppressDocumentAccessNotice?.Invoke();
				Dismiss();
			};
			stackPanel.Children.Add(gelActionButton);
		}
		GelActionButton gelActionButton2 = new GelActionButton("Give permission", null, GelAppearance.PrimaryBlue, 30.0, 14.0).Tip("Open this file's Security settings");
		gelActionButton2.Click += delegate
		{
			string path = e.Path;
			if (path == null || !FileSecurityProperties.Show(path))
			{
				documentAccessSurface.Host.Actions.OpenUrl("ms-settings:privacy-documents");
			}
			Dismiss();
		};
		stackPanel.Children.Add(gelActionButton2);
		Pressable pressable = new Pressable
		{
			Width = 24.0,
			Height = 24.0
		};
		pressable.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(DS.White(0.1)),
			Stroke = Ui.Brush(DS.White(0.14)),
			StrokeThickness = 0.5
		});
		TextBlock textBlock3 = Icons.Glyph("\ue711", 8.5, DS.Colors.TextSecondary);
		textBlock3.FontWeight = FontWeights.Bold;
		pressable.Children.Add(textBlock3);
		pressable.WithIconHover(24.0);
		pressable.Click += Dismiss;
		pressable.Tip("Dismiss for now");
		view = CardParts.HeadsUpRow(tile, lines, stackPanel, pressable);
		void Dismiss()
		{
			documentAccessSurface.Host.Services.Presenter.DismissLiveEvent(typeof(NotchLiveEvent.DocumentFolderAccessDenied));
		}
	}
}
