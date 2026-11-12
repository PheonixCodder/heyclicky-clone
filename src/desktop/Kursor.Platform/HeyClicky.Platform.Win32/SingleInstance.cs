using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Platform.Win32;

public static class SingleInstance
{
	internal readonly record struct PrimaryRecord(int ProcessId, bool OwnPipe, nint Window);

	private static Mutex? ownedMutex;

	private static MemoryMappedFile? record;

	private static NamedPipeServerStream? waitingServer;

	private static CancellationTokenSource? listenerCancellation;

	private static readonly object Gate = new object();

	private static readonly Queue<string[]> Pending = new Queue<string[]>();

	private static Action<string[]>? argumentsReceived;

	internal const string StillClosingMessage = "HeyClicky is still closing from last time. Restart your PC if this keeps happening.";

	private const int RecordSize = 16;

	private const string Exited = "has exited";

	private const uint WM_NULL = 0u;

	private const uint SMTO_ABORTIFHUNG = 2u;

	private const uint MB_ICONWARNING = 48u;

	private const uint MB_SETFOREGROUND = 65536u;

	private const uint MB_TOPMOST = 262144u;

	public static event Action<string[]>? ArgumentsReceived
	{
		add
		{
			lock (Gate)
			{
				argumentsReceived = (Action<string[]>)Delegate.Combine(argumentsReceived, value);
				while (Pending.Count > 0)
				{
					string[] args = Pending.Dequeue();
					ThreadPool.QueueUserWorkItem(delegate
					{
						value?.Invoke(args);
					});
				}
			}
		}
		remove
		{
			lock (Gate)
			{
				argumentsReceived = (Action<string[]>)Delegate.Remove(argumentsReceived, value);
			}
		}
	}

	internal static string Identity(string appId)
	{
		string text = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
		return appId + "-" + text;
	}

	public static bool TryClaim(string[] args, string appId = "HeyClicky", Func<int, bool>? endProcess = null, Action<string>? showMessage = null)
	{
		string text = Identity(appId);
		bool createdNew;
		Mutex mutex = new Mutex(initiallyOwned: true, "Local\\" + text, out createdNew);
		if (createdNew || Take(mutex, 0))
		{
			return BecomePrimary(text, mutex);
		}
		if (Forward(PipeName(text, ReadRecord(text)), args, out var serverProcessId))
		{
			mutex.Dispose();
			return false;
		}
		if (Take(mutex, 0))
		{
			return BecomePrimary(text, mutex);
		}
		PrimaryRecord? primaryRecord = ReadRecord(text);
		if (serverProcessId == 0)
		{
			serverProcessId = primaryRecord?.ProcessId ?? 0;
		}
		string text2 = ((serverProcessId == 0) ? "the running instance" : $"the running instance (pid {serverProcessId})");
		string text3 = ((serverProcessId == 0) ? "can't be found" : Trouble(serverProcessId, (primaryRecord.HasValue && primaryRecord.GetValueOrDefault().ProcessId == serverProcessId) ? primaryRecord.Value.Window : 0));
		if (text3 == null)
		{
			Log.Warn("SingleInstance", text2 + " answers but didn't take the arguments");
			mutex.Dispose();
			return false;
		}
		if (serverProcessId != 0 && (text3 == "has exited" || (endProcess ?? new Func<int, bool>(EndProcess))(serverProcessId)) && Take(mutex, 3000))
		{
			Log.Error("SingleInstance", text2 + " " + text3 + ": starting in its place");
			return BecomePrimary(text, mutex);
		}
		Log.Error("SingleInstance", text2 + " " + text3 + " and still holds the lock: telling the user");
		mutex.Dispose();
		(showMessage ?? new Action<string>(ShowMessage))("HeyClicky is still closing from last time. Restart your PC if this keeps happening.");
		return false;
	}

	public static void Release()
	{
		listenerCancellation?.Cancel();
		listenerCancellation = null;
		Interlocked.Exchange(ref waitingServer, null)?.Dispose();
		if (ownedMutex != null)
		{
			try
			{
				ownedMutex.ReleaseMutex();
			}
			catch
			{
			}
			ownedMutex.Dispose();
			ownedMutex = null;
		}
		record?.Dispose();
		record = null;
	}

	public static void NoteMainWindow(nint window)
	{
		try
		{
			using MemoryMappedViewAccessor memoryMappedViewAccessor = record?.CreateViewAccessor(0L, 16L);
			memoryMappedViewAccessor?.Write(8L, window);
		}
		catch (Exception ex)
		{
			Log.Warn("SingleInstance", "instance record: " + ex.Message);
		}
	}

	private static bool BecomePrimary(string identity, Mutex mutex)
	{
		ownedMutex = mutex;
		NamedPipeServerStream namedPipeServerStream = null;
		try
		{
			namedPipeServerStream = NewServer(PipeName(identity, null));
		}
		catch (Exception ex)
		{
			Log.Warn("SingleInstance", "the arguments pipe is still held (" + ex.Message + "): listening on this process's own");
		}
		PrimaryRecord primaryRecord = new PrimaryRecord(Environment.ProcessId, namedPipeServerStream == null, 0);
		try
		{
			record = WriteRecord(identity, primaryRecord);
		}
		catch (Exception ex2)
		{
			Log.Warn("SingleInstance", "instance record: " + ex2.Message);
		}
		listenerCancellation = new CancellationTokenSource();
		ListenAsync(PipeName(identity, primaryRecord), namedPipeServerStream, listenerCancellation.Token);
		return true;
	}

	private static bool Take(Mutex mutex, int milliseconds)
	{
		try
		{
			return mutex.WaitOne(milliseconds);
		}
		catch (AbandonedMutexException)
		{
			Log.Error("SingleInstance", "the last instance ended without letting go of the lock: taking it over");
			return true;
		}
	}

	private static string RecordName(string identity)
	{
		return "Local\\" + identity + "-primary";
	}

	private static string PipeName(string identity, PrimaryRecord? primary)
	{
		if (primary.HasValue)
		{
			PrimaryRecord valueOrDefault = primary.GetValueOrDefault();
			if (valueOrDefault.OwnPipe)
			{
				return $"{identity}-args-{valueOrDefault.ProcessId}";
			}
		}
		return identity + "-args";
	}

	internal static MemoryMappedFile WriteRecord(string identity, PrimaryRecord primary)
	{
		MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateOrOpen(RecordName(identity), 16L);
		try
		{
			using MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0L, 16L);
			memoryMappedViewAccessor.Write(0L, primary.ProcessId);
			memoryMappedViewAccessor.Write(4L, primary.OwnPipe ? 1 : 0);
			memoryMappedViewAccessor.Write(8L, primary.Window);
			return memoryMappedFile;
		}
		catch
		{
			memoryMappedFile.Dispose();
			throw;
		}
	}

	internal static PrimaryRecord? ReadRecord(string identity)
	{
		try
		{
			using MemoryMappedFile memoryMappedFile = MemoryMappedFile.OpenExisting(RecordName(identity), MemoryMappedFileRights.Read);
			using MemoryMappedViewAccessor memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0L, 16L, MemoryMappedFileAccess.Read);
			int num = memoryMappedViewAccessor.ReadInt32(0L);
			return (num == 0) ? ((PrimaryRecord?)null) : new PrimaryRecord?(new PrimaryRecord(num, memoryMappedViewAccessor.ReadInt32(4L) == 1, (nint)memoryMappedViewAccessor.ReadInt64(8L)));
		}
		catch
		{
			return null;
		}
	}

	private static bool Forward(string pipeName, string[] args, out int serverProcessId)
	{
		serverProcessId = 0;
		try
		{
			using NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
			namedPipeClientStream.Connect(3000);
			if (GetNamedPipeServerProcessId(namedPipeClientStream.SafePipeHandle.DangerousGetHandle(), out var serverProcessId2))
			{
				serverProcessId = (int)serverProcessId2;
				AllowSetForegroundWindow(serverProcessId);
			}
			byte[] bytes = Encoding.UTF8.GetBytes(string.Join('\0', args));
			byte[] bytes2 = BitConverter.GetBytes(bytes.Length);
			byte[] array = bytes;
			int num = 0;
			byte[] array2 = new byte[bytes2.Length + array.Length];
			ReadOnlySpan<byte> readOnlySpan = new ReadOnlySpan<byte>(bytes2);
			readOnlySpan.CopyTo(new Span<byte>(array2).Slice(num, readOnlySpan.Length));
			num += readOnlySpan.Length;
			ReadOnlySpan<byte> readOnlySpan2 = new ReadOnlySpan<byte>(array);
			readOnlySpan2.CopyTo(new Span<byte>(array2).Slice(num, readOnlySpan2.Length));
			num += readOnlySpan2.Length;
			byte[] array3 = array2;
			using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3L));
			namedPipeClientStream.WriteAsync(array3, cancellationTokenSource.Token).AsTask().GetAwaiter()
				.GetResult();
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("SingleInstance", "could not forward arguments to the running instance: " + ((ex is OperationCanceledException) ? "it didn't read them within 3 s" : ex.Message));
			return false;
		}
	}

	private static string? Trouble(int processId, nint window)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			if (process.HasExited)
			{
				return "has exited";
			}
			if (window == 0 || GetWindowThreadProcessId(window, out var processId2) == 0 || processId2 != (uint)processId)
			{
				window = process.MainWindowHandle;
			}
			if (window == 0)
			{
				return (DateTime.Now - process.StartTime < TimeSpan.FromSeconds(30L)) ? null : "has no window";
			}
			nint result;
			return (SendMessageTimeoutW(window, 0u, 0, 0, 2u, 2000u, out result) == 0) ? "doesn't answer" : null;
		}
		catch (ArgumentException)
		{
			return "has exited";
		}
		catch (Exception ex2)
		{
			return "can't be checked (" + ex2.Message + ")";
		}
	}

	private static bool EndProcess(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			using Process process2 = Process.GetCurrentProcess();
			if (process.Id == process2.Id)
			{
				return false;
			}
			if (!string.Equals(process.ProcessName, process2.ProcessName, StringComparison.OrdinalIgnoreCase))
			{
				Log.Warn("SingleInstance", $"pid {processId} is {process.ProcessName}, not a HeyClicky: leaving it");
				return false;
			}
			if (CheckRemoteDebuggerPresent(process.Handle, out var debuggerPresent) && debuggerPresent)
			{
				Log.Warn("SingleInstance", $"pid {processId} is under a debugger: leaving it");
				return false;
			}
			process.Kill();
			process.WaitForExit(2000);
			return true;
		}
		catch (Exception ex)
		{
			Log.Warn("SingleInstance", $"could not end pid {processId}: {ex.Message}");
			return false;
		}
	}

	private static void ShowMessage(string text)
	{
		MessageBoxW(0, text, "HeyClicky", 327728u);
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool GetNamedPipeServerProcessId(nint pipe, out uint serverProcessId);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CheckRemoteDebuggerPresent(nint process, out bool debuggerPresent);

	[DllImport("user32.dll")]
	private static extern bool AllowSetForegroundWindow(int processId);

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

	[DllImport("user32.dll")]
	private static extern nint SendMessageTimeoutW(nint window, uint message, nint wParam, nint lParam, uint flags, uint timeoutMilliseconds, out nint result);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int MessageBoxW(nint owner, string text, string caption, uint type);

	private static NamedPipeServerStream NewServer(string pipeName)
	{
		return new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
	}

	private static async Task ListenAsync(string pipeName, NamedPipeServerStream? server, CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				if (server == null)
				{
					server = NewServer(pipeName);
				}
				Volatile.Write(ref waitingServer, server);
				if (ct.IsCancellationRequested)
				{
					break;
				}
				await server.WaitForConnectionAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
				byte[] lengthBytes = new byte[4];
				await server.ReadExactlyAsync(lengthBytes, ct).ConfigureAwait(continueOnCapturedContext: false);
				int num = BitConverter.ToInt32(lengthBytes);
				if (num < 0 || num > 1048576)
				{
					continue;
				}
				byte[] payload = new byte[num];
				await server.ReadExactlyAsync(payload, ct).ConfigureAwait(continueOnCapturedContext: false);
				string @string = Encoding.UTF8.GetString(payload);
				string[] array = ((@string.Length == 0) ? Array.Empty<string>() : @string.Split('\0'));
				Action<string[]> action;
				lock (Gate)
				{
					action = argumentsReceived;
					if (action == null)
					{
						Pending.Enqueue(array);
						continue;
					}
				}
				action(array);
			}
			catch (Exception) when (ct.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex2)
			{
				Log.Warn("SingleInstance", "pipe listener: " + ex2.Message);
				await Task.Delay(200, CancellationToken.None).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				server?.Dispose();
				server = null;
			}
		}
	}
}
