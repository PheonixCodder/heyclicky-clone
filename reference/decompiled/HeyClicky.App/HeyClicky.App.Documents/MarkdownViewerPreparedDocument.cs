using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Documents.Markdown;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace HeyClicky.App.Documents;

public sealed class MarkdownViewerPreparedDocument
{
	[CompilerGenerated]
	private sealed class _003CImageBlocks_003Ed__18 : IEnumerable<MarkdownViewerBlock.Image>, IEnumerable, IEnumerator<MarkdownViewerBlock.Image>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private MarkdownViewerBlock.Image _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private IEnumerable<MarkdownViewerBlock> blocks;

		public IEnumerable<MarkdownViewerBlock> _003C_003E3__blocks;

		private IEnumerator<MarkdownViewerBlock> _003C_003E7__wrap1;

		private IEnumerator<MarkdownViewerBlock.Image> _003C_003E7__wrap2;

		MarkdownViewerBlock.Image IEnumerator<MarkdownViewerBlock.Image>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CImageBlocks_003Ed__18(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if ((uint)(num - -4) <= 1u || (uint)(num - 1) <= 1u)
			{
				try
				{
					if (num == -4 || num == 2)
					{
						try
						{
						}
						finally
						{
							_003C_003Em__Finally2();
						}
					}
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003C_003E7__wrap1 = null;
			_003C_003E7__wrap2 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				MarkdownViewerBlock.Blockquote blockquote;
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003C_003E7__wrap1 = blocks.GetEnumerator();
					_003C_003E1__state = -3;
					goto IL_00e9;
				case 1:
					_003C_003E1__state = -3;
					goto IL_00e9;
				case 2:
					{
						_003C_003E1__state = -4;
						goto IL_00cf;
					}
					IL_00e9:
					do
					{
						if (_003C_003E7__wrap1.MoveNext())
						{
							MarkdownViewerBlock current = _003C_003E7__wrap1.Current;
							if (current is MarkdownViewerBlock.Image image)
							{
								_003C_003E2__current = image;
								_003C_003E1__state = 1;
								return true;
							}
							blockquote = current as MarkdownViewerBlock.Blockquote;
							continue;
						}
						_003C_003Em__Finally1();
						_003C_003E7__wrap1 = null;
						return false;
					}
					while ((object)blockquote == null);
					_003C_003E7__wrap2 = ImageBlocks(blockquote.ChildBlocks).GetEnumerator();
					_003C_003E1__state = -4;
					goto IL_00cf;
					IL_00cf:
					if (_003C_003E7__wrap2.MoveNext())
					{
						MarkdownViewerBlock.Image current2 = _003C_003E7__wrap2.Current;
						_003C_003E2__current = current2;
						_003C_003E1__state = 2;
						return true;
					}
					_003C_003Em__Finally2();
					_003C_003E7__wrap2 = null;
					goto IL_00e9;
				}
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			if (_003C_003E7__wrap1 != null)
			{
				_003C_003E7__wrap1.Dispose();
			}
		}

		private void _003C_003Em__Finally2()
		{
			_003C_003E1__state = -3;
			if (_003C_003E7__wrap2 != null)
			{
				_003C_003E7__wrap2.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<MarkdownViewerBlock.Image> IEnumerable<MarkdownViewerBlock.Image>.GetEnumerator()
		{
			_003CImageBlocks_003Ed__18 _003CImageBlocks_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CImageBlocks_003Ed__ = this;
			}
			else
			{
				_003CImageBlocks_003Ed__ = new _003CImageBlocks_003Ed__18(0);
			}
			_003CImageBlocks_003Ed__.blocks = _003C_003E3__blocks;
			return _003CImageBlocks_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MarkdownViewerBlock.Image>)this).GetEnumerator();
		}
	}

	private static readonly HttpClient Http = CreateHttp();

	public IReadOnlyList<MarkdownViewerBlock> Blocks { get; }

	public string? BaseDirectory { get; }

	public IReadOnlyDictionary<string, MarkdownViewerImage> Images { get; }

	public IReadOnlyList<string> RemoteImageSourcesToLoad { get; }

	private MarkdownViewerPreparedDocument(IReadOnlyList<MarkdownViewerBlock> blocks, string? baseDirectory, IReadOnlyDictionary<string, MarkdownViewerImage> images, IReadOnlyList<string> remote)
	{
		Blocks = blocks;
		BaseDirectory = baseDirectory;
		Images = images;
		RemoteImageSourcesToLoad = remote;
	}

	private static HttpClient CreateHttp()
	{
		HttpClient httpClient = new HttpClient();
		httpClient.Timeout = TimeSpan.FromSeconds(30L);
		httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HeyClicky/1.0 (Windows)");
		return httpClient;
	}

	public static bool IsRemote(string source)
	{
		if (!source.StartsWith("http://", StringComparison.Ordinal))
		{
			return source.StartsWith("https://", StringComparison.Ordinal);
		}
		return true;
	}

	public static async Task<MarkdownViewerPreparedDocument> PrepareAsync(IReadOnlyList<MarkdownViewerBlock> blocks, string? baseDirectory, IReadOnlyDictionary<string, byte[]>? remoteImagesBySource = null, CancellationToken ct = default(CancellationToken))
	{
		Dictionary<string, MarkdownViewerImage> images = new Dictionary<string, MarkdownViewerImage>();
		List<string> remote = new List<string>();
		byte[] bytes = default(byte[]);
		(double Width, double Height) size = default((double Width, double Height));
		foreach (MarkdownViewerBlock.Image image in ImageBlocks(blocks))
		{
			ct.ThrowIfCancellationRequested();
			if (images.ContainsKey(image.Source))
			{
				continue;
			}
			if (IsRemote(image.Source))
			{
				bool flag = remoteImagesBySource?.TryGetValue(image.Source, out bytes) ?? false;
				if (flag)
				{
					(double, double)? tuple = await MeasureAsync(bytes);
					int num;
					if (tuple.HasValue)
					{
						size = tuple.GetValueOrDefault();
						num = 1;
					}
					else
					{
						num = 0;
					}
					flag = (byte)num != 0;
				}
				if (flag)
				{
					images[image.Source] = new MarkdownViewerImage(null, bytes, size.Width, size.Height);
				}
				else if (remoteImagesBySource == null || !remoteImagesBySource.ContainsKey(image.Source))
				{
					remote.Add(image.Source);
				}
				bytes = null;
			}
			else
			{
				MarkdownViewerImage markdownViewerImage = await LoadLocalImageAsync(image.Source, baseDirectory);
				if ((object)markdownViewerImage != null)
				{
					images[image.Source] = markdownViewerImage;
				}
			}
		}
		return new MarkdownViewerPreparedDocument(blocks, baseDirectory, images, remote);
	}

	public static async Task<Dictionary<string, byte[]>> DownloadAsync(IEnumerable<string> sources, CancellationToken ct)
	{
		Dictionary<string, byte[]> loaded = new Dictionary<string, byte[]>();
		foreach (string source in sources.Distinct())
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				if (Uri.TryCreate(source, UriKind.Absolute, out Uri result))
				{
					byte[] bytes = await Http.GetByteArrayAsync(result, ct);
					if ((await MeasureAsync(bytes)).HasValue)
					{
						loaded[source] = bytes;
					}
				}
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				Log.Info("markdown", "image " + source + " not loaded: " + ex2.Message);
			}
		}
		return loaded;
	}

	[IteratorStateMachine(typeof(_003CImageBlocks_003Ed__18))]
	private static IEnumerable<MarkdownViewerBlock.Image> ImageBlocks(IEnumerable<MarkdownViewerBlock> blocks)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CImageBlocks_003Ed__18(-2)
		{
			_003C_003E3__blocks = blocks
		};
	}

	private static async Task<MarkdownViewerImage?> LoadLocalImageAsync(string source, string? baseDirectory)
	{
		string text = source;
		if (text.StartsWith("file://", StringComparison.OrdinalIgnoreCase) && Uri.TryCreate(text, UriKind.Absolute, out Uri result))
		{
			text = result.LocalPath;
		}
		List<string> list = new List<string> { text };
		string text2 = Uri.UnescapeDataString(text);
		if (text2 != text)
		{
			list.Add(text2);
		}
		foreach (string item in list)
		{
			string text3;
			if (!item.StartsWith('~'))
			{
				text3 = item;
			}
			else
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string text4 = item;
				text3 = Path.Combine(folderPath, text4.Substring(1, text4.Length - 1).TrimStart('/', '\\'));
			}
			string text5 = text3;
			string resolved;
			if ((Path.IsPathRooted(text5) && !text5.StartsWith('\\')) || text5.StartsWith("\\\\", StringComparison.Ordinal))
			{
				resolved = text5;
			}
			else
			{
				if (baseDirectory == null)
				{
					continue;
				}
				resolved = Path.Combine(baseDirectory, item.TrimStart('/', '\\'));
			}
			try
			{
				if (File.Exists(resolved))
				{
					(double, double)? tuple = await MeasureAsync(await File.ReadAllBytesAsync(resolved));
					if (tuple.HasValue)
					{
						(double, double) valueOrDefault = tuple.GetValueOrDefault();
						return new MarkdownViewerImage(resolved, null, valueOrDefault.Item1, valueOrDefault.Item2);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Info("markdown", "image " + resolved + " not loaded: " + ex.Message);
			}
		}
		return null;
	}

	private static async Task<(double Width, double Height)?> MeasureAsync(byte[] bytes)
	{
		_ = 1;
		try
		{
			using InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
			await stream.WriteAsync(bytes.AsBuffer());
			stream.Seek(0uL);
			BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(stream);
			double num = bitmapDecoder.PixelWidth;
			double num2 = bitmapDecoder.PixelHeight;
			double dpiX = bitmapDecoder.DpiX;
			if (dpiX > 1.0 && Math.Abs(dpiX - 72.0) > 1.0 && Math.Abs(dpiX - 96.0) > 1.0)
			{
				num = num * 72.0 / dpiX;
				num2 = num2 * 72.0 / bitmapDecoder.DpiY;
			}
			return (num > 0.0 && num2 > 0.0) ? new(double, double)?((num, num2)) : (((double, double)?)null);
		}
		catch
		{
			return null;
		}
	}
}
