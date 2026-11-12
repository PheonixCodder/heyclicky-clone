using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.UI;
using HeyClicky.Core.Notch;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using WinRT.Interop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace HeyClicky.App.Notch.Surfaces;

public sealed class ComposerBox : Grid
{
	public const int MaxVisibleLines = 8;

	private const double LeadingPadding = 12.0;

	private const double TrailingPadding = 8.0;

	private const double Spacing = 10.0;

	private const double VerticalPadding = 8.0;

	private const double CornerRadiusValue = 14.0;

	private readonly TextBlock placeholder;

	private readonly TextBlock measurer;

	private readonly Grid fieldHost;

	private readonly Pressable send;

	private readonly GelSurface sendGel;

	private readonly TextBlock sendGlyph;

	private readonly Pressable? attach;

	private readonly StackPanel chips = new StackPanel
	{
		Orientation = Orientation.Horizontal,
		Spacing = 4.0
	};

	private readonly ScrollViewer chipRow;

	private readonly Grid row;

	private readonly List<string> attachments = new List<string>();

	private int lines = 1;

	private Grid? capNotice;

	private Microsoft.UI.Dispatching.DispatcherQueueTimer? capNoticeTimer;

	private bool capNoticeShowing;

	private readonly StackPanel column;

	private readonly Grid dropZone;

	private TextBlock dropGlyph;

	private TextBlock dropBadgePlus;

	private bool dropTargeting;

	private string placeholderText;

	public TextBox Input { get; }

	public IReadOnlyList<string> Attachments => attachments;

	public Func<nint>? WindowHandle { get; set; }

	public static double LineHeight { get; private set; } = 16.0;

	private bool HasContent
	{
		get
		{
			if (Input.Text.Trim().Length <= 0)
			{
				return attachments.Count > 0;
			}
			return true;
		}
	}

	internal bool IsCapNoticeShowing => capNoticeShowing;

	public bool HasDraft => HasContent;

	public event Action<string, IReadOnlyList<string>>? Submitted;

	public event Action? Cancelled;

	public event Action<int, int>? MetricsChanged;

	public event Action<bool>? FocusChanged;

	public event Action? AttachmentsChanged;

	public void RemoveAttachButton()
	{
		if ((object)attach != null && row.Children.Contains(attach))
		{
			int num = Grid.GetColumn(attach);
			row.Children.Remove(attach);
			row.ColumnDefinitions.RemoveAt(num);
			Grid.SetColumn(send, num);
		}
	}

	public ComposerBox(string placeholderText, bool showAttach)
	{
		base.Margin = new Thickness(8.0, 0.0, 8.0, 0.0);
		Border item = new Border
		{
			CornerRadius = new CornerRadius(14.0),
			Background = Ui.Brush(DS.Colors.NotchCardSurface),
			BorderBrush = Ui.Brush(DS.White(0.14)),
			BorderThickness = new Thickness(1.0)
		};
		base.Children.Add(item);
		Input = new TextBox
		{
			FontSize = 13.0,
			FontFamily = DS.Fonts.Text,
			Foreground = Ui.Brush(Colors.White),
			Background = Ui.Transparent,
			BorderThickness = new Thickness(0.0),
			Padding = new Thickness(0.0),
			MinHeight = 0.0,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			IsSpellCheckEnabled = false,
			IsTextScaleFactorEnabled = false,
			SelectionHighlightColor = Ui.Brush(DS.Colors.CursorBlue),
			VerticalAlignment = VerticalAlignment.Center
		};
		StripChrome(Input);
		AutomationProperties.SetName(Input, "Message HeyClicky");
		ScrollViewer.SetVerticalScrollBarVisibility(Input, ScrollBarVisibility.Auto);
		this.placeholderText = placeholderText;
		placeholder = Ui.Text(placeholderText, 13.0, FontWeights.Normal, DS.White(0.45), TextAlignment.Left, 1);
		placeholder.IsHitTestVisible = false;
		placeholder.VerticalAlignment = VerticalAlignment.Center;
		measurer = new TextBlock
		{
			FontSize = 13.0,
			FontFamily = DS.Fonts.Text,
			TextWrapping = TextWrapping.Wrap,
			IsTextScaleFactorEnabled = false
		};
		fieldHost = new Grid
		{
			VerticalAlignment = VerticalAlignment.Center
		};
		fieldHost.Children.Add(Input);
		fieldHost.Children.Add(placeholder);
		send = new Pressable
		{
			Width = 24.0,
			Height = 24.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		sendGel = new GelSurface(GelAppearance.PrimaryBlue, 12.0);
		sendGlyph = Icons.Glyph("\ue74a", 10.5, Colors.Black);
		sendGlyph.FontWeight = FontWeights.Bold;
		send.Children.Add(sendGel);
		send.Children.Add(sendGlyph);
		send.Click += Submit;
		send.Tip("Send");
		row = new Grid
		{
			ColumnSpacing = 10.0,
			Margin = new Thickness(12.0, 0.0, 8.0, 0.0)
		};
		row.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		row.Children.Add(fieldHost);
		int value = 1;
		if (showAttach)
		{
			attach = new Pressable
			{
				Width = 24.0,
				Height = 24.0,
				VerticalAlignment = VerticalAlignment.Center
			};
			TextBlock clip = Icons.Glyph("\ue723", 11.5, DS.White(0.65));
			clip.FontWeight = FontWeights.Medium;
			attach.Children.Add(clip);
			attach.WithPressScale(0.94f, 0.12, easeOut: true);
			attach.PressedChanged += delegate(bool p)
			{
				Anim.Opacity(clip, p ? 0.85f : 1f, 0.12);
			};
			attach.Click += PickFiles;
			attach.Tip("Attach files");
			row.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			Grid.SetColumn(attach, value++);
			row.Children.Add(attach);
		}
		row.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(send, value);
		row.Children.Add(send);
		chipRow = new ScrollViewer
		{
			Content = chips,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Enabled,
			VerticalScrollMode = ScrollMode.Disabled,
			Height = 46.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0),
			Visibility = Visibility.Collapsed
		};
		chips.Margin = new Thickness(12.0, 0.0, 12.0, 0.0);
		column = new StackPanel
		{
			Spacing = 4.0,
			Margin = new Thickness(0.0, 8.0, 0.0, 8.0)
		};
		column.Children.Add(chipRow);
		column.Children.Add(row);
		base.Children.Add(column);
		dropZone = BuildDropZone();
		base.Children.Add(dropZone);
		Input.TextChanged += delegate
		{
			placeholder.Visibility = ((Input.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			Remeasure();
			UpdateSend();
		};
		Input.PreviewKeyDown += OnKey;
		Input.GotFocus += delegate
		{
			this.FocusChanged?.Invoke(obj: true);
		};
		Input.LostFocus += delegate
		{
			this.FocusChanged?.Invoke(obj: false);
		};
		Input.Paste += OnPaste;
		base.SizeChanged += delegate
		{
			Remeasure();
		};
		UpdateSend();
		Input.Loaded += delegate
		{
			placeholder.Visibility = ((Input.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			UpdateSend();
		};
		ApplyFieldHeight();
	}

	public static void StripChrome(TextBox t)
	{
		ResourceDictionary resources = t.Resources;
		string[] array = new string[8] { "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused", "TextControlBackgroundDisabled", "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused", "TextControlBorderBrushDisabled" };
		foreach (string key in array)
		{
			resources[key] = Ui.Transparent;
		}
		array = new string[3] { "TextControlForeground", "TextControlForegroundPointerOver", "TextControlForegroundFocused" };
		foreach (string key2 in array)
		{
			resources[key2] = Ui.Brush(Colors.White);
		}
		resources["TextControlPlaceholderForeground"] = Ui.Brush(DS.White(0.45));
		resources["TextControlBorderThemeThickness"] = new Thickness(0.0);
		resources["TextControlBorderThemeThicknessFocused"] = new Thickness(0.0);
		resources["TextControlThemePadding"] = new Thickness(0.0);
		resources["TextControlThemeMinHeight"] = 0.0;
	}

	public static void CalibrateLineHeight()
	{
		TextBlock textBlock = new TextBlock();
		textBlock.FontSize = 13.0;
		textBlock.FontFamily = DS.Fonts.Text;
		textBlock.Text = "Ag";
		textBlock.IsTextScaleFactorEnabled = false;
		textBlock.Measure(new Size(1000f, 1000f));
		LineHeight = Math.Ceiling(textBlock.DesiredSize.Height);
		NotchGeometry.InputLineHeight = LineHeight;
	}

	private void Remeasure()
	{
		double actualWidth = fieldHost.ActualWidth;
		if (!(actualWidth <= 0.0))
		{
			string text = Input.Text;
			if (text.Length == 0)
			{
				text = " ";
			}
			if (text.EndsWith('\r') || text.EndsWith('\n'))
			{
				text += " ";
			}
			measurer.Width = actualWidth;
			measurer.Text = text;
			measurer.Measure(new Size(actualWidth, double.PositiveInfinity));
			int num = Math.Max(1, (int)Math.Round(measurer.DesiredSize.Height / LineHeight));
			if (num != lines)
			{
				lines = num;
				ApplyFieldHeight();
				this.MetricsChanged?.Invoke(Math.Max(0, lines - 1), attachments.Count);
			}
		}
	}

	private void ApplyFieldHeight()
	{
		double num = (double)Math.Clamp(lines, 1, 8) * LineHeight + 6.0;
		Input.Height = num;
		fieldHost.Height = num;
		double height = Math.Max(24.0, num);
		row.Height = height;
		VerticalAlignment verticalAlignment = ((lines <= 1) ? VerticalAlignment.Center : VerticalAlignment.Bottom);
		send.VerticalAlignment = verticalAlignment;
		if ((object)attach != null)
		{
			attach.VerticalAlignment = verticalAlignment;
		}
		placeholder.VerticalAlignment = VerticalAlignment.Center;
	}

	private void OnKey(object sender, KeyRoutedEventArgs e)
	{
		bool flag = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
		if (e.Key == VirtualKey.Enter && !flag)
		{
			e.Handled = true;
			Submit();
		}
		else if (e.Key == VirtualKey.Escape)
		{
			e.Handled = true;
			this.Cancelled?.Invoke();
		}
	}

	private void UpdateSend()
	{
		bool hasContent = HasContent;
		sendGel.Enabled = hasContent;
		sendGlyph.Foreground = Ui.Brush(hasContent ? Colors.Black : DS.Colors.GelDisabledContent);
		send.Enabled = hasContent;
	}

	private void Submit()
	{
		if (HasContent)
		{
			string arg = Input.Text.Trim();
			List<string> arg2 = attachments.ToList();
			Input.Text = "";
			attachments.Clear();
			chips.Children.Clear();
			chipRow.Visibility = Visibility.Collapsed;
			UpdateSend();
			UpdateAttachButton();
			this.Submitted?.Invoke(arg, arg2);
		}
	}

	public void FocusInput()
	{
		Input.Focus(FocusState.Programmatic);
	}

	private async void PickFiles()
	{
		try
		{
			FileOpenPicker fileOpenPicker = new FileOpenPicker();
			fileOpenPicker.FileTypeFilter.Add("*");
			nint? num = WindowHandle?.Invoke();
			if (num.HasValue)
			{
				nint valueOrDefault = num.GetValueOrDefault();
				if (valueOrDefault != 0)
				{
					InitializeWithWindow.Initialize(fileOpenPicker, valueOrDefault);
				}
			}
			AddAttachments((await fileOpenPicker.PickMultipleFilesAsync()).Select((StorageFile f) => f.Path));
		}
		catch (Exception ex)
		{
			Log.Warn("composer", "file picker failed: " + ex.Message);
		}
	}

	private async void OnPaste(object sender, TextControlPasteEventArgs e)
	{
		if ((object)attach == null)
		{
			return;
		}
		DataPackageView content = Clipboard.GetContent();
		if (content.Contains(StandardDataFormats.StorageItems))
		{
			e.Handled = true;
			try
			{
				IReadOnlyList<IStorageItem> readOnlyList = await content.GetStorageItemsAsync();
				IReadOnlyList<string> readOnlyList2 = ComposerAttachmentPolicy.PastableImageFiles(readOnlyList.Select((IStorageItem i) => i.Path));
				if (readOnlyList2.Count > 0)
				{
					AddAttachments(readOnlyList2);
				}
				Log.Info("composer", $"pasted {readOnlyList.Count} copied file(s): {readOnlyList2.Count} image(s) attached");
				return;
			}
			catch (Exception ex)
			{
				Log.Warn("composer", "paste files failed: " + ex.Message);
				return;
			}
		}
		if (!content.Contains(StandardDataFormats.Bitmap))
		{
			return;
		}
		e.Handled = true;
		try
		{
			RandomAccessStreamReference obj = await content.GetBitmapAsync();
			string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"clicky-paste-{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.png");
			using (IRandomAccessStreamWithContentType src = await obj.OpenReadAsync())
			{
				using FileStream dst = File.Create(path);
				await src.AsStreamForRead().CopyToAsync(dst);
			}
			AddAttachment(path);
		}
		catch (Exception ex2)
		{
			Log.Warn("composer", "paste image failed: " + ex2.Message);
		}
	}

	public void AddAttachment(string path)
	{
		AddAttachments(new global::_003C_003Ez__ReadOnlySingleElementList<string>(path));
	}

	public int AddAttachments(IEnumerable<string> paths)
	{
		var (readOnlyList, flag) = ComposerAttachmentPolicy.Admit(attachments.Count, paths);
		foreach (string item in readOnlyList)
		{
			AddChip(item);
		}
		if (flag)
		{
			ShowCapNotice();
		}
		if (readOnlyList.Count == 0)
		{
			return 0;
		}
		chipRow.Visibility = Visibility.Visible;
		AttachmentsDidChange();
		return readOnlyList.Count;
	}

	private void AddChip(string path)
	{
		attachments.Add(path);
		Grid tile = new Grid
		{
			Width = 46.0,
			Height = 46.0
		};
		tile.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(11.0),
			Background = Ui.Brush(DS.White(0.3))
		});
		Border border = new Border
		{
			Margin = new Thickness(3.0),
			CornerRadius = new CornerRadius(8.0),
			Background = Ui.Brush(DS.Colors.Surface2)
		};
		string text = System.IO.Path.GetExtension(path).ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = text[1];
					if (c != 'j')
					{
						if (c != 't')
						{
							if (c == 'w' && text == ".webp")
							{
								goto IL_01b1;
							}
						}
						else if (text == ".tiff")
						{
							goto IL_01b1;
						}
					}
					else if (text == ".jpeg")
					{
						goto IL_01b1;
					}
				}
			}
			else
			{
				char c = text[1];
				if ((uint)c <= 103u)
				{
					if (c != 'b')
					{
						if (c == 'g' && text == ".gif")
						{
							goto IL_01b1;
						}
					}
					else if (text == ".bmp")
					{
						goto IL_01b1;
					}
				}
				else if (c != 'j')
				{
					if (c == 'p' && text == ".png")
					{
						goto IL_01b1;
					}
				}
				else if (text == ".jpg")
				{
					goto IL_01b1;
				}
			}
		}
		bool flag = false;
		goto IL_01b9;
		IL_01b1:
		flag = true;
		goto IL_01b9;
		IL_01b9:
		if (flag)
		{
			border.Background = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri(path))
				{
					DecodePixelWidth = 120
				},
				Stretch = Stretch.UniformToFill
			};
		}
		else
		{
			border.Background = Ui.Transparent;
			LoadShellIconAsync(border, path);
		}
		tile.Children.Add(border);
		Pressable remove = new Pressable
		{
			Width = 13.0,
			Height = 13.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 3.0, 3.0, 0.0),
			Opacity = 0.0,
			IsHitTestVisible = false
		}.Named("Remove attachment");
		remove.Children.Add(new Ellipse
		{
			Fill = Ui.Brush(DS.Black(0.6))
		});
		TextBlock textBlock = Icons.Glyph("\ue711", 7.0, Colors.White);
		textBlock.FontWeight = FontWeights.Bold;
		textBlock.HorizontalAlignment = HorizontalAlignment.Center;
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		remove.Children.Add(textBlock);
		remove.Click += delegate
		{
			RemoveAttachment(path, tile);
		};
		tile.Children.Add(remove);
		tile.PointerEntered += delegate
		{
			remove.IsHitTestVisible = true;
			Anim.Opacity(remove, 1f, 0.12);
		};
		tile.PointerExited += delegate(object _, PointerRoutedEventArgs e)
		{
			if (!PointerFrames.IsStale(e))
			{
				remove.IsHitTestVisible = false;
				Anim.Opacity(remove, 0f, 0.12);
			}
		};
		ToolTipService.SetToolTip(tile, System.IO.Path.GetFileName(path));
		chips.Children.Add(tile);
	}

	public static async Task LoadShellIconAsync(Border inner, string path)
	{
		_ = 2;
		try
		{
			using StorageItemThumbnail thumb = await (await StorageFile.GetFileFromPathAsync(path)).GetThumbnailAsync(ThumbnailMode.SingleItem, 80u);
			if ((object)thumb == null)
			{
				throw new InvalidOperationException("no icon");
			}
			BitmapImage bmp = new BitmapImage();
			await bmp.SetSourceAsync(thumb);
			inner.Child = new Image
			{
				Source = bmp,
				Width = 40.0,
				Height = 40.0,
				Stretch = Stretch.Uniform
			};
		}
		catch
		{
			inner.Background = Ui.Brush(DS.Colors.Surface2);
			inner.Child = Icons.Glyph("\ue8a5", 16.0, DS.Colors.TextSecondary);
		}
	}

	private void RemoveAttachment(string path, FrameworkElement tile)
	{
		attachments.Remove(path);
		chips.Children.Remove(tile);
		if (attachments.Count == 0)
		{
			chipRow.Visibility = Visibility.Collapsed;
		}
		AttachmentsDidChange();
	}

	internal bool RemoveAttachmentAt(int index)
	{
		if (index < 0 || index >= chips.Children.Count || index >= attachments.Count)
		{
			return false;
		}
		RemoveAttachment(attachments[index], (FrameworkElement)chips.Children[index]);
		return true;
	}

	private void AttachmentsDidChange()
	{
		UpdateSend();
		UpdateAttachButton();
		this.MetricsChanged?.Invoke(Math.Max(0, lines - 1), attachments.Count);
		this.AttachmentsChanged?.Invoke();
	}

	private void UpdateAttachButton()
	{
		if ((object)attach != null)
		{
			bool flag = attachments.Count >= 10;
			attach.Enabled = !flag;
			attach.Opacity = (flag ? 0.4615384615384615 : 1.0);
			attach.Tip(flag ? $"Up to {10} files" : "Attach files");
		}
	}

	private static Grid WarningTriangleFill(double size, Color knockout)
	{
		TextBlock item = Icons.Glyph("\uf139", size, Colors.White);
		TextBlock textBlock = Ui.Text("!", size * 0.72, FontWeights.Black, knockout, TextAlignment.Center);
		textBlock.Margin = new Thickness(0.0, size * 0.18, 0.0, 0.0);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		return new Grid
		{
			VerticalAlignment = VerticalAlignment.Center,
			Children = 
			{
				(UIElement)item,
				(UIElement)textBlock
			}
		};
	}

	private void ShowCapNotice()
	{
		if ((object)capNotice == null)
		{
			Grid grid = WarningTriangleFill(9.0, DS.Colors.Warning);
			TextBlock textBlock = Ui.Text(ComposerAttachmentPolicy.CapNotice, 10.5, FontWeights.SemiBold, Colors.White);
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			Border border = new Border();
			border.CornerRadius = new CornerRadius(100.0);
			border.Background = Ui.Brush(DS.Colors.Warning);
			border.Padding = new Thickness(9.0, 3.0, 9.0, 3.0);
			border.Child = Ui.H(4.0, grid, textBlock);
			Border item = border.Capsule();
			ShadowLayer item2 = new ShadowLayer(100.0, DS.Black(0.3), 4.0, 1.0);
			capNotice = new Grid
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 5.0, 0.0, 0.0),
				IsHitTestVisible = false,
				Opacity = 0.0,
				Tag = "attachment-cap-notice",
				Children = 
				{
					(UIElement)item2,
					(UIElement)item
				}
			};
			base.Children.Add(capNotice);
			capNoticeTimer = base.DispatcherQueue.CreateTimer();
			capNoticeTimer.IsRepeating = false;
			capNoticeTimer.Interval = ComposerAttachmentPolicy.CapNoticeDuration;
			capNoticeTimer.Tick += delegate
			{
				capNoticeShowing = false;
				if ((object)capNotice != null)
				{
					Anim.Opacity(capNotice, 0f, 0.18);
				}
			};
		}
		Log.Info("composer", "attachment cap reached: notice shown");
		capNoticeShowing = true;
		Anim.Opacity(capNotice, 1f, 0.18);
		capNoticeTimer.Stop();
		capNoticeTimer.Start();
	}

	public void SetDropTargeting(bool on, Color tint)
	{
		if (base.Children[0] is Border border)
		{
			border.BorderBrush = Ui.Brush(on ? tint : DS.White(0.14));
			border.BorderThickness = new Thickness(on ? 1.5 : 1.0);
		}
		dropTargeting = on;
		dropGlyph.Foreground = Ui.Brush(tint);
		dropBadgePlus.Foreground = Ui.Brush(tint);
		Anim.Opacity(column, (!on) ? 1 : 0, 0.15);
		Anim.Opacity(dropZone, on ? 1 : 0, 0.15);
		column.IsHitTestVisible = !on;
	}

	private Grid BuildDropZone()
	{
		dropGlyph = Icons.Glyph("\ue7c3", 16.0, DS.Colors.CursorBlue);
		dropGlyph.FontWeight = FontWeights.SemiBold;
		dropBadgePlus = Icons.Glyph("\ue710", 6.5, DS.Colors.CursorBlue);
		dropBadgePlus.FontWeight = FontWeights.Bold;
		Grid item = new Grid
		{
			Width = 10.0,
			Height = 10.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, -3.0, -2.0),
			Children = 
			{
				(UIElement)new Ellipse
				{
					Fill = Ui.Brush(DS.Colors.NotchCardSurface)
				},
				(UIElement)dropBadgePlus
			}
		};
		Grid grid = new Grid
		{
			Width = 16.0,
			Height = 16.0,
			Children = 
			{
				(UIElement)dropGlyph,
				(UIElement)item
			}
		};
		TextBlock textBlock = Ui.Text("Drop files here to attach", 13.0, FontWeights.SemiBold, DS.White(0.92));
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel = Ui.H(8.0, grid, textBlock);
		stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		return new Grid
		{
			Opacity = 0.0,
			IsHitTestVisible = false,
			MinHeight = 24.0,
			Children = { (UIElement)stackPanel }
		};
	}

	public void SetPlaceholder(string text)
	{
		placeholderText = text;
		if (!dropTargeting)
		{
			placeholder.Text = text;
		}
	}
}
