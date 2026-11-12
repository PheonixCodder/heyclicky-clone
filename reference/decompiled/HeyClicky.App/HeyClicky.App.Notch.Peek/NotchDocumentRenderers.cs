using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.App.Home;
using HeyClicky.App.UI;
using HeyClicky.Core.Documents;
using HeyClicky.Core.Voice;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace HeyClicky.App.Notch.Peek;

public static class NotchDocumentRenderers
{
	private sealed class Renderer(Func<string, Action, FrameworkElement> render) : INotchDocumentRenderer
	{
		public bool CanRender(string path)
		{
			return true;
		}

		public FrameworkElement Render(string path, Action reload)
		{
			return render(path, reload);
		}
	}

	private static readonly Markdown.Options DarkReaderStyle = ReaderStyleIn(MessagesPalette.Dark);

	private static readonly Markdown.Options LightReaderStyle = ReaderStyleIn(MessagesPalette.Light);

	private static Markdown.Options ReaderStyle
	{
		get
		{
			if (!MessagesPalette.Current.IsDark)
			{
				return LightReaderStyle;
			}
			return DarkReaderStyle;
		}
	}

	private static Markdown.Options ReaderStyleIn(MessagesPalette p)
	{
		Markdown.Options options = new Markdown.Options(14.0, p.PrimaryText, p.LinkBlue, p.SecondaryText);
		options.BlockSpacing = 12.0;
		options.HeadingSizes = new double[4] { 20.0, 17.0, 15.0, 14.0 };
		options.LineSpacing = 6.0;
		options.ListRowSpacing = 4.0;
		options.BulletIndent = 20.0;
		options.NumberIndent = 28.0;
		options.CodeFill = p.ComposerFieldFill;
		options.CodeStroke = p.ComposerFieldStroke;
		options.CodePadding = new Thickness(12.0, 10.0, 12.0, 10.0);
		options.CodeCopyButton = true;
		return options;
	}

	public static INotchDocumentRenderer For(string path)
	{
		switch (Path.GetExtension(path).ToLowerInvariant())
		{
		case ".mdx":
		case ".md":
		case ".markdown":
			return new Renderer((string p, Action _) => Async(p, delegate(string text)
			{
				List<(string, string)> images;
				StackPanel stackPanel = Markdown.Render(text, ReaderStyle, out images);
				stackPanel.Margin = new Thickness(22.0, 18.0, 22.0, 28.0);
				stackPanel.MaxWidth = 760.0;
				return Scroll(stackPanel);
			}));
		case ".png":
		case ".jpg":
		case ".gif":
		case ".bmp":
		case ".tif":
		case ".ico":
		case ".jpeg":
		case ".webp":
		case ".tiff":
			return new Renderer((string p, Action _) => new Border
			{
				CornerRadius = new CornerRadius(8.0),
				VerticalAlignment = VerticalAlignment.Center,
				Child = new Image
				{
					Source = new BitmapImage(new Uri(p)),
					Stretch = Stretch.Uniform,
					Margin = new Thickness(16.0)
				}
			});
		case ".pdf":
			return new Renderer((string p, Action _) => Pdf(p));
		case ".odt":
		case ".rtf":
		case ".docx":
		case ".docm":
		case ".dotx":
		case ".pptx":
		case ".xlsx":
			return new Renderer((string p, Action _) => DocumentText(p));
		default:
			if (HeyClicky.Core.Documents.DocumentText.LikelyYieldsText(path) || IsProbablyText(path))
			{
				return new Renderer((string p, Action _) => Async(p, (string text) => Scroll(new TextBlock
				{
					Text = text,
					FontFamily = DS.Fonts.Mono,
					FontSize = 12.0,
					Foreground = Ui.Brush(MessagesPalette.Current.PrimaryText),
					TextWrapping = TextWrapping.Wrap,
					IsTextSelectionEnabled = true,
					IsTextScaleFactorEnabled = false,
					Margin = new Thickness(18.0, 14.0, 18.0, 24.0)
				})));
			}
			return new Renderer((string p, Action _) => NoPreview(p));
		}
	}

	private static FrameworkElement Async(string path, Func<string, UIElement> build)
	{
		Grid host = new Grid
		{
			Children = { (UIElement)Message("Loading document…") }
		};
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		MessagesPalette look = MessagesPalette.Current;
		Task.Run(delegate
		{
			try
			{
				using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
				using StreamReader streamReader = new StreamReader(stream);
				char[] array = new char[200000];
				int length = streamReader.ReadBlock(array, 0, array.Length);
				return new string(array, 0, length);
			}
			catch (Exception ex)
			{
				Log.Warn("notch-doc", "read: " + ex.Message);
				return (string)null;
			}
		}).ContinueWith((Task<string> t) => dq.TryEnqueue(delegate
		{
			using (MessagesPalette.Use(look))
			{
				host.Children.Clear();
				UIElementCollection children = host.Children;
				string result = t.Result;
				children.Add((result != null) ? build(result) : Message("This document couldn’t be loaded."));
			}
		}), TaskScheduler.Default);
		return host;
	}

	private static FrameworkElement DocumentText(string path)
	{
		Grid host = new Grid
		{
			Children = { (UIElement)Message("Loading document…") }
		};
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		MessagesPalette look = MessagesPalette.Current;
		Task.Run(() => HeyClicky.Core.Documents.DocumentText.Extract(path)).ContinueWith(delegate(Task<PageContext> t)
		{
			string text = (t.IsCompletedSuccessfully ? t.Result.Text : null);
			dq.TryEnqueue(delegate
			{
				using (MessagesPalette.Use(look))
				{
					host.Children.Clear();
					host.Children.Add(string.IsNullOrWhiteSpace(text) ? Message("This document has no text to show.") : Scroll(Paragraphs(text)));
				}
			});
		}, TaskScheduler.Default);
		return host;
	}

	private static FrameworkElement Pdf(string path)
	{
		StackPanel pages = new StackPanel
		{
			Spacing = 12.0,
			Padding = new Thickness(16.0, 16.0, 16.0, 24.0)
		};
		MessagesPalette look = MessagesPalette.Current;
		ScrollViewer result = Scroll(pages);
		Load();
		return result;
		async Task Load()
		{
			_ = 3;
			try
			{
				PdfDocument doc = await PdfDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(path));
				double width = Math.Max(320.0, 742.0);
				double scale = pages.XamlRoot?.RasterizationScale ?? 1.5;
				uint count = Math.Min(doc.PageCount, 60u);
				for (uint i = 0u; i < count; i++)
				{
					using PdfPage page = doc.GetPage(i);
					using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
					await page.RenderToStreamAsync(stream, new PdfPageRenderOptions
					{
						DestinationWidth = (uint)Math.Round(width * scale)
					});
					BitmapImage bmp = new BitmapImage();
					stream.Seek(0uL);
					await bmp.SetSourceAsync(stream);
					double height = ((page.Size.Width > 0.0) ? (width * page.Size.Height / page.Size.Width) : (width * 1.294));
					pages.Children.Add(new Border
					{
						Background = Ui.Brush(Colors.White),
						CornerRadius = new CornerRadius(4.0),
						Width = width,
						Height = height,
						Child = new Image
						{
							Source = bmp,
							Stretch = Stretch.Uniform
						}
					});
				}
				if (doc.PageCount > count)
				{
					pages.Children.Add(Ui.Text($"{doc.PageCount - count} more pages · Open to read the rest", 12.0, FontWeights.Medium, look.SecondaryText, TextAlignment.Center));
				}
			}
			catch (Exception ex)
			{
				Log.Warn("notch-doc", "pdf: " + ex.Message);
				using (MessagesPalette.Use(look))
				{
					pages.Children.Clear();
					pages.Children.Add(Message("This document couldn’t be loaded."));
				}
			}
		}
	}

	private static bool IsProbablyText(string path)
	{
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			byte[] array = new byte[Math.Min(4096, (int)Math.Min(fileStream.Length, 4096L))];
			int num = fileStream.Read(array, 0, array.Length);
			for (int i = 0; i < num; i++)
			{
				if (array[i] == 0)
				{
					return false;
				}
			}
			return num > 0;
		}
		catch
		{
			return false;
		}
	}

	private static ScrollViewer Scroll(UIElement content)
	{
		return new ScrollViewer
		{
			Content = content,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
	}

	private static UIElement Paragraphs(string text)
	{
		StackPanel stackPanel = new StackPanel
		{
			Spacing = 10.0,
			Margin = new Thickness(22.0, 18.0, 22.0, 28.0),
			MaxWidth = 760.0
		};
		foreach (string item in (from p in text.Replace("\r\n", "\n").Split('\n')
			select p.Trim() into p
			where p.Length > 0
			select p).Take(1500))
		{
			TextBlock textBlock = Ui.Text(item, 13.5, FontWeights.Normal, MessagesPalette.Current.PrimaryText, TextAlignment.Left, 0, wrap: true);
			textBlock.IsTextSelectionEnabled = true;
			stackPanel.Children.Add(textBlock);
		}
		return stackPanel;
	}

	public static FrameworkElement Message(string text)
	{
		return Ui.Text(text, 13.0, FontWeights.Normal, MessagesPalette.Current.SecondaryText, TextAlignment.Center).Center();
	}

	private static FrameworkElement NoPreview(string path)
	{
		GelButton gelButton = new GelButton("Open", GelAppearance.SecondaryOutline, GelButton.Sizing.Compact, 100.0, "\ue8a7", null, null)
		{
			HorizontalAlignment = HorizontalAlignment.Center
		};
		gelButton.Click += delegate
		{
			ChatBlockViews.OpenPath(path);
		};
		StackPanel obj = new StackPanel
		{
			Spacing = 12.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		Color secondaryText = MessagesPalette.Current.SecondaryText;
		obj.Children.Add(Icons.Glyph("\ue8e5", 30.0, secondaryText));
		obj.Children.Add(Ui.Text("No preview for this " + ChatBlockViews.Kind(path).ToLowerInvariant() + " yet.", 13.0, FontWeights.Normal, secondaryText, TextAlignment.Center));
		obj.Children.Add(gelButton);
		return obj;
	}
}
