using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading.Tasks;
using HeyClicky.App.Design;
using HeyClicky.Core.Auth;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace HeyClicky.App.UI;

public static class IntegrationLogos
{
	private static readonly (string Key, string Asset)[] Map = new(string, string)[36]
	{
		("gmail", "gmail-logo"),
		("googlemeet", "google-meet-logo"),
		("meet", "google-meet-logo"),
		("google", "google-logo"),
		("github", "github-logo"),
		("notion", "notion-logo"),
		("linear", "linear-logo"),
		("discord", "discord-logo"),
		("airtable", "airtable-logo"),
		("spotify", "spotify-logo"),
		("zoom", "zoom-logo"),
		("microsoftteams", "ms-teams-logo"),
		("msteams", "ms-teams-logo"),
		("teams", "ms-teams-logo"),
		("whatsapp", "whatsapp-logo"),
		("imessage", "imessage-logo"),
		("obsidian", "obsidian-logo"),
		("excalidraw", "excalidraw-logo"),
		("powerpoint", "powerpoint-logo"),
		("word", "word-logo"),
		("reddit", "reddit-logo"),
		("twitter", "x-logo"),
		("youtube", "youtube-logo"),
		("linkedin", "linkedin-logo"),
		("instagram", "instagram-logo"),
		("tiktok", "tiktok-logo"),
		("polymarket", "polymarket-logo"),
		("applenotes", "apple-notes-logo"),
		("applereminders", "apple-reminders-logo"),
		("reminders", "apple-reminders-logo"),
		("findmy", "findmy-logo"),
		("chatgpt", "chatgpt-logo"),
		("openai", "openai-logo"),
		("claudecode", "claude-code-logo"),
		("claude", "claude-logo"),
		("heyclicky", "heyclicky-logo")
	};

	private static readonly HashSet<string> TemplateAssets = new HashSet<string> { "chatgpt-logo", "openai-logo", "github-logo", "discord-logo" };

	private static readonly HashSet<string> PngAssets = new HashSet<string> { "gmail-logo", "google-meet-logo", "ms-teams-logo", "whatsapp-logo", "zoom-logo", "findmy-logo", "apple-reminders-logo", "heyclicky-logo" };

	private static readonly HttpClient RemoteHttp = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15L)
	};

	private static readonly ConcurrentDictionary<string, Task<string?>> remoteLoads = new ConcurrentDictionary<string, Task<string>>();

	private static string CacheDirectory => AppPaths.Combine("logo-cache");

	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex NonAlnum()
	{
		return _003CRegexGenerator_g_003EF36FB2D25FB3E06331DF866801390D20E97C6BDCC4C519A6278D40F829CA940A3__NonAlnum_0.Instance;
	}

	private static string Normalized(string? value)
	{
		return NonAlnum().Replace((value ?? "").Trim().ToLowerInvariant(), " ").Trim();
	}

	public static string? AssetFor(params string?[] identifiers)
	{
		for (int i = 0; i < identifiers.Length; i++)
		{
			string text = Normalized(identifiers[i]);
			if (text.Length == 0)
			{
				continue;
			}
			string text2 = text.Replace(" ", "");
			if (text2 == "x")
			{
				return "x-logo";
			}
			(string, string)[] map = Map;
			for (int j = 0; j < map.Length; j++)
			{
				var (text3, result) = map[j];
				if (text2 == text3 || (text3 == "google" && text2.StartsWith(text3, StringComparison.Ordinal)))
				{
					return result;
				}
			}
		}
		return null;
	}

	private static string PathFor(string asset)
	{
		return Path.Combine(AppContext.BaseDirectory, "Assets", "Logos", asset + (PngAssets.Contains(asset) ? ".png" : ".svg"));
	}

	public static FrameworkElement Image(string asset, double size, Color? templateTint = null, double scale = 2.0)
	{
		int num = (int)Math.Ceiling(size * scale);
		ImageSource source;
		if (PngAssets.Contains(asset))
		{
			source = new BitmapImage(new Uri(PathFor(asset)))
			{
				DecodePixelWidth = Math.Max(num, 16)
			};
		}
		else
		{
			if (templateTint.HasValue)
			{
				Color valueOrDefault = templateTint.GetValueOrDefault();
				if (TemplateAssets.Contains(asset))
				{
					SvgImageSource obj = new SvgImageSource
					{
						RasterizePixelWidth = num,
						RasterizePixelHeight = num
					};
					LoadTintedAsync(obj, asset, valueOrDefault);
					source = obj;
					goto IL_0098;
				}
			}
			source = new SvgImageSource(new Uri(PathFor(asset)))
			{
				RasterizePixelWidth = num,
				RasterizePixelHeight = num
			};
		}
		goto IL_0098;
		IL_0098:
		return new Image
		{
			Source = source,
			Width = size,
			Height = size,
			Stretch = Stretch.Uniform
		};
	}

	private static async Task LoadTintedAsync(SvgImageSource svg, string asset, Color tint)
	{
		_ = 1;
		try
		{
			string input = await File.ReadAllTextAsync(PathFor(asset));
			string text = $"#{tint.R:X2}{tint.G:X2}{tint.B:X2}";
			input = Regex.Replace(input, "fill=\"(?!none)[^\"]*\"", "fill=\"" + text + "\"");
			input = Regex.Replace(input, "<svg ", "<svg fill=\"" + text + "\" ", RegexOptions.None, TimeSpan.FromSeconds(1L));
			using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
			await svg.SetSourceAsync(stream.AsRandomAccessStream());
		}
		catch (Exception ex)
		{
			Log.Warn("logos", asset + ": " + ex.Message);
		}
	}

	public static FrameworkElement Artwork(string? toolkitSlug, string? displayName, double size, double fallbackIconSize, string? logoUrl = null)
	{
		string text = AssetFor(toolkitSlug, displayName);
		if (text != null)
		{
			return Image(text, size, null);
		}
		if (logoUrl != null && logoUrl.Length > 0)
		{
			return Remote(logoUrl, size, Artwork(null, null, size, fallbackIconSize));
		}
		TextBlock textBlock = Icons.Glyph("\uf0e2", fallbackIconSize, DS.Colors.TextSecondary);
		textBlock.FontWeight = FontWeights.SemiBold;
		return new Grid
		{
			Width = size,
			Height = size,
			Children = { (UIElement)textBlock }
		};
	}

	public static FrameworkElement Remote(string url, double size, FrameworkElement fallback)
	{
		Grid host = new Grid
		{
			Width = size,
			Height = size
		};
		host.Children.Add(fallback);
		Uri uri;
		bool flag = !Uri.TryCreate(url, UriKind.Absolute, out uri);
		if (!flag)
		{
			string scheme = uri.Scheme;
			bool flag2 = ((scheme == "https" || scheme == "http") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			return host;
		}
		DispatcherQueue dq = DispatcherQueue.GetForCurrentThread();
		Task<string> orAdd = remoteLoads.GetOrAdd(uri.AbsoluteUri, (string _) => Task.Run(() => CachedPathAsync(uri)));
		if (orAdd.IsCompletedSuccessfully)
		{
			Show(orAdd.Result);
		}
		else
		{
			orAdd.ContinueWith((Task<string?> t) => dq?.TryEnqueue(delegate
			{
				Show(t.IsCompletedSuccessfully ? t.Result : null);
			}), TaskScheduler.Default);
		}
		return host;
		void Show(string? path)
		{
			Image image;
			if (path != null)
			{
				int num = (int)Math.Ceiling(size * 2.0);
				ImageSource imageSource = (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ? ((ImageSource)new SvgImageSource(new Uri(path))
				{
					RasterizePixelWidth = num,
					RasterizePixelHeight = num
				}) : ((ImageSource)new BitmapImage(new Uri(path))
				{
					DecodePixelWidth = Math.Max(num, 16)
				}));
				image = new Image
				{
					Source = imageSource,
					Width = size,
					Height = size,
					Stretch = Stretch.Uniform,
					Opacity = 0.0
				};
				if (imageSource is SvgImageSource svgImageSource)
				{
					svgImageSource.Opened += delegate
					{
						Opened();
					};
					svgImageSource.OpenFailed += delegate
					{
						host.Children.Remove(image);
					};
				}
				else if (imageSource is BitmapImage bitmapImage)
				{
					bitmapImage.ImageOpened += delegate
					{
						Opened();
					};
					bitmapImage.ImageFailed += delegate
					{
						host.Children.Remove(image);
					};
				}
				host.Children.Add(image);
			}
			void Opened()
			{
				image.Opacity = 1.0;
				fallback.Visibility = Visibility.Collapsed;
			}
		}
	}

	private static async Task<string?> CachedPathAsync(Uri uri)
	{
		string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri))).Substring(0, 24).ToLowerInvariant();
		try
		{
			if (Directory.Exists(CacheDirectory))
			{
				string[] array = new string[6] { ".svg", ".png", ".jpg", ".gif", ".webp", ".ico" };
				foreach (string text in array)
				{
					string text2 = Path.Combine(CacheDirectory, key + text);
					if (File.Exists(text2) && new FileInfo(text2).Length > 0)
					{
						return text2;
					}
				}
			}
			using HttpResponseMessage response = await RemoteHttp.GetAsync(uri).ConfigureAwait(continueOnCapturedContext: false);
			if (!response.IsSuccessStatusCode)
			{
				Log.Warn("logos", $"{uri.Host}{uri.AbsolutePath}: HTTP {response.StatusCode}");
				return null;
			}
			byte[] array2 = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (array2.Length == 0 || array2.Length > 2000000)
			{
				return null;
			}
			string text3 = SniffExtension(array2, response.Content.Headers.ContentType?.MediaType);
			if (text3 == null)
			{
				Log.Warn("logos", uri.Host + uri.AbsolutePath + ": not an image");
				return null;
			}
			Directory.CreateDirectory(CacheDirectory);
			string text4 = Path.Combine(CacheDirectory, key + text3);
			AtomicFile.WriteAllBytes(text4, array2);
			Log.Info("logos", $"cached {uri.Host}{uri.AbsolutePath} ({array2.Length} bytes, {text3})");
			return text4;
		}
		catch (Exception ex)
		{
			Log.Warn("logos", uri.Host + uri.AbsolutePath + ": " + ex.Message);
			return null;
		}
	}

	internal static string? SniffExtension(byte[] b, string? mediaType)
	{
		if (b.Length >= 8 && b[0] == 137 && b[1] == 80 && b[2] == 78 && b[3] == 71)
		{
			return ".png";
		}
		if (b.Length >= 3 && b[0] == byte.MaxValue && b[1] == 216 && b[2] == byte.MaxValue)
		{
			return ".jpg";
		}
		if (b.Length >= 4 && b[0] == 71 && b[1] == 73 && b[2] == 70)
		{
			return ".gif";
		}
		if (b.Length >= 12 && b[0] == 82 && b[1] == 73 && b[2] == 70 && b[3] == 70 && b[8] == 87 && b[9] == 69 && b[10] == 66 && b[11] == 80)
		{
			return ".webp";
		}
		if (b.Length >= 4 && b[0] == 0 && b[1] == 0 && b[2] == 1 && b[3] == 0)
		{
			return ".ico";
		}
		string text = Encoding.UTF8.GetString(b, 0, Math.Min(b.Length, 512)).TrimStart('\ufeff', ' ', '\r', '\n', '\t');
		if (text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || (text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && text.Contains("<svg", StringComparison.OrdinalIgnoreCase)) || mediaType == "image/svg+xml")
		{
			return ".svg";
		}
		return null;
	}
}
