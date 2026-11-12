using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Memory;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HeyClicky.Platform.Win32;

public static class TextInsertion
{
	public static readonly TimeSpan DefaultRestoreDelay = TimeSpan.FromSeconds(0.8);

	private static readonly BlockingCollection<Action> Work = new BlockingCollection<Action>();

	private static readonly Lazy<Thread> ClipboardThread = new Lazy<Thread>(delegate
	{
		Thread thread = new Thread(ClipboardThreadMain);
		thread.IsBackground = true;
		thread.Name = "HeyClicky clipboard";
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		return thread;
	});

	private static HWND clipboardOwner;

	private static List<(uint Format, byte[] Data)>? pendingRestoreSnapshot;

	private static uint pendingRestoreSequence;

	private static readonly int[] LandingPollDelaysMs = new int[5] { 150, 150, 300, 600, 1200 };

	public static Task<bool> PasteTextAsync(string text, TimeSpan? restoreAfter = null, bool restoreClipboard = true)
	{
		TimeSpan? restoreDelay = (restoreClipboard ? new TimeSpan?(restoreAfter ?? DefaultRestoreDelay) : ((TimeSpan?)null));
		return RunOnClipboardThreadAsync(delegate
		{
			List<(uint, byte[])> snapshot = null;
			if (restoreDelay.HasValue)
			{
				snapshot = ((pendingRestoreSnapshot != null && pendingRestoreSequence == PInvoke.GetClipboardSequenceNumber()) ? pendingRestoreSnapshot : SnapshotClipboard());
			}
			if (!WriteText(text, markTransient: true))
			{
				return false;
			}
			uint injectedSequence = PInvoke.GetClipboardSequenceNumber();
			if (snapshot != null)
			{
				pendingRestoreSnapshot = snapshot;
				pendingRestoreSequence = injectedSequence;
			}
			SendControlV();
			if (restoreDelay.HasValue)
			{
				TimeSpan valueOrDefault = restoreDelay.GetValueOrDefault();
				if (snapshot != null)
				{
					Task.Delay(valueOrDefault).ContinueWith((Task _) => RunOnClipboardThreadAsync(delegate
					{
						if (PInvoke.GetClipboardSequenceNumber() == injectedSequence)
						{
							RestoreClipboard(snapshot);
						}
						if (pendingRestoreSequence == injectedSequence)
						{
							pendingRestoreSnapshot = null;
						}
						return true;
					}), TaskScheduler.Default);
				}
			}
			return true;
		});
	}

	public static async Task<bool> PasteTextVerifyingLandingAsync(string text, TimeSpan minimumRestoreDelay, Func<Task<bool?>>? landed, Action<string, int>? onVerification = null)
	{
		uint injectedSequence = 0u;
		List<(uint, byte[])> snapshot = null;
		if (!(await RunOnClipboardThreadAsync(delegate
		{
			snapshot = ((pendingRestoreSnapshot != null && pendingRestoreSequence == PInvoke.GetClipboardSequenceNumber()) ? pendingRestoreSnapshot : SnapshotClipboard());
			if (!WriteText(text, markTransient: true))
			{
				RestoreClipboard(snapshot);
				return false;
			}
			injectedSequence = PInvoke.GetClipboardSequenceNumber();
			pendingRestoreSnapshot = snapshot;
			pendingRestoreSequence = injectedSequence;
			SendControlV();
			return true;
		}).ConfigureAwait(continueOnCapturedContext: false)))
		{
			return false;
		}
		Task.Run(async delegate
		{
			long started = Environment.TickCount64;
			string verification = "not_observed";
			if (landed == null)
			{
				verification = "unverifiable";
			}
			else
			{
				int[] landingPollDelaysMs = LandingPollDelaysMs;
				for (int i = 0; i < landingPollDelaysMs.Length; i++)
				{
					await Task.Delay(landingPollDelaysMs[i]).ConfigureAwait(continueOnCapturedContext: false);
					bool? flag;
					try
					{
						flag = await landed().ConfigureAwait(continueOnCapturedContext: false);
					}
					catch
					{
						flag = null;
					}
					if (!flag.HasValue)
					{
						verification = "unverifiable";
						break;
					}
					if (flag.Value)
					{
						verification = "landed";
						break;
					}
				}
			}
			if (verification != "landed")
			{
				long num = (long)minimumRestoreDelay.TotalMilliseconds - (Environment.TickCount64 - started);
				if (num > 0)
				{
					await Task.Delay((int)num).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			await RunOnClipboardThreadAsync(delegate
			{
				if (PInvoke.GetClipboardSequenceNumber() == injectedSequence)
				{
					RestoreClipboard(snapshot);
				}
				if (pendingRestoreSequence == injectedSequence)
				{
					pendingRestoreSnapshot = null;
				}
				return true;
			}).ConfigureAwait(continueOnCapturedContext: false);
			int num2 = (int)(Environment.TickCount64 - started);
			Log.Info("TextInsertion", $"paste verification: {verification} after {num2} ms; clipboard restored");
			try
			{
				onVerification?.Invoke(verification, num2);
			}
			catch (Exception ex)
			{
				Log.Warn("TextInsertion", "paste verification handler threw: " + ex.Message);
			}
		});
		return true;
	}

	public static Task<bool> CopyTextAsync(string text)
	{
		return RunOnClipboardThreadAsync(() => WriteText(text, markTransient: false));
	}

	public static Task<string?> ReadTextAsync()
	{
		return RunOnClipboardThreadAsync(ReadText);
	}

	public static string ReplacingEmDashesForInsertion(string text)
	{
		if (!text.Contains('—'))
		{
			return text;
		}
		string input = EmDashPattern().Replace(text, ", ");
		return LeadingCommaPattern().Replace(input, "");
	}

	[GeneratedRegex("[ \\t]*—[ \\t]*")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex EmDashPattern()
	{
		return _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__EmDashPattern_0.Instance;
	}

	[GeneratedRegex("(?m)^, ")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.15.2218")]
	private static Regex LeadingCommaPattern()
	{
		return _003CRegexGenerator_g_003EFD27443086CA09DA37C0E5A8B6DAA42AD77D15FFFA56E38F8C84164A75A302D1A__LeadingCommaPattern_1.Instance;
	}

	private static Task<T> RunOnClipboardThreadAsync<T>(Func<T> work)
	{
		_ = ClipboardThread.Value;
		TaskCompletionSource<T> completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		Work.Add(delegate
		{
			try
			{
				completion.TrySetResult(work());
			}
			catch (Exception ex)
			{
				Log.Warn("TextInsertion", ex.Message);
				completion.TrySetResult(default(T));
			}
		});
		return completion.Task;
	}

	private unsafe static void ClipboardThreadMain()
	{
		clipboardOwner = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_LEFT, "STATIC", null, WINDOW_STYLE.WS_OVERLAPPED, 0, 0, 0, 0, new HWND(-3), null, null, null);
		foreach (Action item in Work.GetConsumingEnumerable())
		{
			item();
		}
	}

	private static bool OpenClipboardWithRetry()
	{
		for (int i = 0; i < 20; i++)
		{
			if ((bool)PInvoke.OpenClipboard(clipboardOwner))
			{
				return true;
			}
			Thread.Sleep(10);
		}
		Log.Warn("TextInsertion", "clipboard busy");
		return false;
	}

	private static bool WriteText(string text, bool markTransient)
	{
		if (!OpenClipboardWithRetry())
		{
			return false;
		}
		try
		{
			if (!PInvoke.EmptyClipboard())
			{
				return false;
			}
			byte[] array = new byte[(text.Length + 1) * 2];
			MemoryMarshal.AsBytes(text.AsSpan()).CopyTo(array);
			if (!SetClipboardBytes(13u, array))
			{
				return false;
			}
			if (markTransient)
			{
				SetClipboardBytes(PInvoke.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing"), new byte[1]);
				SetClipboardBytes(PInvoke.RegisterClipboardFormat("CanIncludeInClipboardHistory"), BitConverter.GetBytes(0));
				SetClipboardBytes(PInvoke.RegisterClipboardFormat("CanUploadToCloudClipboard"), BitConverter.GetBytes(0));
			}
			return true;
		}
		finally
		{
			PInvoke.CloseClipboard();
		}
	}

	private unsafe static bool SetClipboardBytes(uint format, byte[] data)
	{
		HGLOBAL hGLOBAL = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)data.Length);
		if (hGLOBAL == default(HGLOBAL))
		{
			return false;
		}
		void* ptr = PInvoke.GlobalLock(hGLOBAL);
		if (ptr == null)
		{
			PInvoke.GlobalFree(hGLOBAL);
			return false;
		}
		data.CopyTo(new Span<byte>(ptr, data.Length));
		PInvoke.GlobalUnlock(hGLOBAL);
		if (PInvoke.SetClipboardData(format, new HANDLE((nint)hGLOBAL.Value)).IsNull)
		{
			PInvoke.GlobalFree(hGLOBAL);
			return false;
		}
		return true;
	}

	private unsafe static string? ReadText()
	{
		if (!OpenClipboardWithRetry())
		{
			return null;
		}
		try
		{
			HANDLE clipboardData = PInvoke.GetClipboardData(13u);
			if (clipboardData.IsNull)
			{
				return null;
			}
			HGLOBAL hMem = new HGLOBAL(clipboardData.Value);
			void* ptr = PInvoke.GlobalLock(hMem);
			if (ptr == null)
			{
				return null;
			}
			try
			{
				return new string((char*)ptr);
			}
			finally
			{
				PInvoke.GlobalUnlock(hMem);
			}
		}
		finally
		{
			PInvoke.CloseClipboard();
		}
	}

	private static bool IsHandleFormat(uint format)
	{
		bool flag;
		switch (format)
		{
		case 2u:
		case 3u:
		case 9u:
		case 14u:
		case 128u:
		case 130u:
		case 131u:
		case 142u:
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			if (format >= 768)
			{
				return format <= 1023;
			}
			return false;
		}
		return true;
	}

	private unsafe static List<(uint Format, byte[] Data)> SnapshotClipboard()
	{
		List<(uint, byte[])> list = new List<(uint, byte[])>();
		if (!OpenClipboardWithRetry())
		{
			return list;
		}
		try
		{
			uint num = 0u;
			while ((num = PInvoke.EnumClipboardFormats(num)) != 0)
			{
				if (IsHandleFormat(num))
				{
					continue;
				}
				HANDLE clipboardData = PInvoke.GetClipboardData(num);
				if (clipboardData.IsNull)
				{
					continue;
				}
				HGLOBAL hMem = new HGLOBAL(clipboardData.Value);
				nuint num2 = PInvoke.GlobalSize(hMem);
				if (num2 == 0 || num2 > 67108864)
				{
					continue;
				}
				void* ptr = PInvoke.GlobalLock(hMem);
				if (ptr != null)
				{
					try
					{
						list.Add((num, new ReadOnlySpan<byte>(ptr, (int)num2).ToArray()));
					}
					finally
					{
						PInvoke.GlobalUnlock(hMem);
					}
				}
			}
			return list;
		}
		finally
		{
			PInvoke.CloseClipboard();
		}
	}

	private static void RestoreClipboard(List<(uint Format, byte[] Data)> snapshot)
	{
		if (!OpenClipboardWithRetry())
		{
			return;
		}
		try
		{
			PInvoke.EmptyClipboard();
			foreach (var (format, data) in snapshot)
			{
				SetClipboardBytes(format, data);
			}
		}
		finally
		{
			PInvoke.CloseClipboard();
		}
	}

	private unsafe static void SendControlV()
	{
		Span<INPUT> span = stackalloc INPUT[4];
		span[0] = KeyInput(17, keyUp: false);
		span[1] = KeyInput(86, keyUp: false);
		span[2] = KeyInput(86, keyUp: true);
		span[3] = KeyInput(17, keyUp: true);
		PInvoke.SendInput(span, sizeof(INPUT));
	}

	private static INPUT KeyInput(ushort virtualKey, bool keyUp)
	{
		INPUT result = default(INPUT);
		result.type = INPUT_TYPE.INPUT_KEYBOARD;
		result.Anonymous = new INPUT._Anonymous_e__Union
		{
			ki = new KEYBDINPUT
			{
				wVk = (VIRTUAL_KEY)virtualKey,
				dwFlags = (keyUp ? KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP : ((KEYBD_EVENT_FLAGS)0u)),
				dwExtraInfo = 1212369995u
			}
		};
		return result;
	}
}
