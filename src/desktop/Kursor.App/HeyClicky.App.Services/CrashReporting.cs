using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Services.Updates;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Identity;
using HeyClicky.Core.Telemetry;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32.SafeHandles;
using Sentry;

namespace HeyClicky.App.Services;

public static class CrashReporting
{
	private sealed class SentryCrashReporter : ICrashReporter
	{
		public bool IsEnabled => SentrySdk.IsEnabled;

		private static SentryLevel Level(CrashReportLevel level)
		{
			return level switch
			{
				CrashReportLevel.Debug => SentryLevel.Debug, 
				CrashReportLevel.Info => SentryLevel.Info, 
				CrashReportLevel.Warning => SentryLevel.Warning, 
				CrashReportLevel.Fatal => SentryLevel.Fatal, 
				_ => SentryLevel.Error, 
			};
		}

		private static BreadcrumbLevel Crumb(CrashReportLevel level)
		{
			return level switch
			{
				CrashReportLevel.Debug => BreadcrumbLevel.Debug, 
				CrashReportLevel.Info => BreadcrumbLevel.Info, 
				CrashReportLevel.Warning => BreadcrumbLevel.Warning, 
				CrashReportLevel.Fatal => BreadcrumbLevel.Fatal, 
				_ => BreadcrumbLevel.Error, 
			};
		}

		public void AddBreadcrumb(string category, string? message, CrashReportLevel level, JsonObject? data)
		{
			Dictionary<string, string> data2 = data?.ToDictionary((KeyValuePair<string, JsonNode> p) => p.Key, (KeyValuePair<string, JsonNode> p) => p.Value?.ToString() ?? "");
			SentrySdk.AddBreadcrumb(message ?? "", category, null, data2, Crumb(level));
		}

		public void CaptureMessage(string message, CrashReportLevel level, IReadOnlyList<string> fingerprint, IReadOnlyDictionary<string, string> tags, string contextName, JsonObject context, IReadOnlyList<string>? attachmentPaths = null)
		{
			SentrySdk.CaptureMessage(message, delegate(Scope scope)
			{
				scope.Level = Level(level);
				foreach (var (key, value) in tags)
				{
					scope.SetTag(key, value);
				}
				scope.SetFingerprint(fingerprint);
				scope.Contexts[contextName] = context.ToDictionary<KeyValuePair<string, JsonNode>, string, object>((KeyValuePair<string, JsonNode> p) => p.Key, (KeyValuePair<string, JsonNode> p) => p.Value?.ToString());
				foreach (string item in attachmentPaths ?? Array.Empty<string>())
				{
					if (File.Exists(item))
					{
						scope.AddAttachment(item);
					}
				}
			}, Level(level));
		}

		public void CaptureException(Exception exception, string mechanism, bool handled)
		{
			exception.SetSentryMechanism(mechanism, null, handled, null);
			SentrySdk.CaptureException(exception);
		}

		public void SetUser(string? id, string? email, string? name)
		{
			SentrySdk.ConfigureScope(delegate(Scope scope)
			{
				scope.User = ((id == null) ? new SentryUser() : new SentryUser
				{
					Id = id,
					Email = email,
					Username = name
				});
			});
		}

		public void ClearBreadcrumbs()
		{
			SentrySdk.ConfigureScope(delegate(Scope scope)
			{
				scope.ClearBreadcrumbs();
			});
		}

		public Task FlushAsync(TimeSpan timeout)
		{
			return SentrySdk.FlushAsync(timeout);
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate int TopLevelExceptionFilter(nint exceptionPointers);

	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct MinidumpExceptionInformation
	{
		public uint ThreadId;

		public nint ExceptionPointers;

		[MarshalAs(UnmanagedType.Bool)]
		public bool ClientPointers;
	}

	public sealed class ReportingSynchronizationContext(DispatcherQueue queue) : SynchronizationContext()
	{
		private readonly DispatcherQueueSynchronizationContext inner = new DispatcherQueueSynchronizationContext(queue);

		public override void Post(SendOrPostCallback d, object? state)
		{
			inner.Post(delegate(object? s)
			{
				(SendOrPostCallback, object) obj = ((SendOrPostCallback, object))s;
				SendOrPostCallback item = obj.Item1;
				object item2 = obj.Item2;
				(object, long) previous = UiWork.Enter(UiWork.Pick(item, item2));
				try
				{
					item(item2);
				}
				catch (Exception exception)
				{
					ReportUiException(exception);
				}
				finally
				{
					UiWork.Exit(previous);
				}
			}, (d, state));
		}

		public override void Send(SendOrPostCallback d, object? state)
		{
			inner.Send(d, state);
		}

		public override SynchronizationContext CreateCopy()
		{
			return new ReportingSynchronizationContext(queue);
		}
	}

	private static class NativeCrash
	{
		[DllImport("kernel32.dll")]
		private static extern nint CreateThread(nint attributes, nuint stackSize, nint startAddress, nint parameter, uint flags, out uint threadId);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern nint LoadLibraryW(string name);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern nint GetProcAddress(nint module, string name);

		public static void CrashOnNativeThread()
		{
			nint procAddress = GetProcAddress(LoadLibraryW("ucrtbase.dll"), "strlen");
			CreateThread(IntPtr.Zero, UIntPtr.Zero, procAddress, new IntPtr(16), 0u, out var _);
		}
	}

	private const string Area = "crash";

	private static int dumpWritten;

	private static TopLevelExceptionFilter? nativeFilter;

	private static nint previousNativeFilter;

	private static bool installed;

	private const uint DumpType = 4452u;

	public static string LogsDirectory => Log.Directory;

	public static bool SentryEnabled { get; private set; }

	public static void Install()
	{
		if (!installed)
		{
			installed = true;
			AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
			InstallNativeFilter();
			StartSentry();
			PruneOldDumps();
		}
	}

	private static void StartSentry()
	{
		TelemetryConfiguration config = TelemetryConfiguration.Current;
		ClickyEndpoints endpoints = AppBundleConfiguration.Load();
		string text = null;
		if (text == null && AppBundleConfiguration.IsLocalDevelopmentConfiguration(endpoints))
		{
			text = "local endpoints";
		}
		if (text == null && (!config.HasSentryDsn || string.IsNullOrWhiteSpace(config.SentryDsn)))
		{
			text = "no DSN configured";
		}
		if (Environment.GetEnvironmentVariable("HEYCLICKY_SENTRY_OFF") == "1")
		{
			text = "HEYCLICKY_SENTRY_OFF=1";
		}
		if (text != null || string.IsNullOrWhiteSpace(config.SentryDsn))
		{
			Log.Info("crash", "Sentry off (" + (text ?? "empty DSN") + "): crash and error reports stay in the logs folder");
			return;
		}
		try
		{
			ClientIdentity identity = ClickyClientIdentity.Current;
			SentrySdk.Init(delegate(SentryOptions options)
			{
				options.Dsn = config.SentryDsn;
				options.Release = "kursor-windows@" + identity.AppVersion + "+" + identity.AppBuild;
				options.Environment = "production";
				options.IsGlobalModeEnabled = true;
				options.AutoSessionTracking = true;
				options.SendDefaultPii = false;
				options.AttachStacktrace = true;
				options.CacheDirectoryPath = Path.Combine(AppBundleConfiguration.AppDataDirectory, "sentry");
				options.TracesSampleRate = 0.0;
				options.DisableAppDomainUnhandledExceptionCapture();
				options.DisableUnobservedTaskExceptionCapture();
				options.DisableWinUiUnhandledExceptionIntegration();
			});
			SentrySdk.ConfigureScope(delegate(Scope scope)
			{
				bool flag = UpdateService.IsVelopackInstall();
				scope.SetTag("install_location", flag ? "installed" : "unpackaged");
				scope.SetTag("clicky_platform", identity.Platform);
				scope.SetTag("os_arch", RuntimeInformation.OSArchitecture.ToString());
				scope.SetTag("process_arch", RuntimeInformation.ProcessArchitecture.ToString());
				scope.Contexts["install_location"] = new Dictionary<string, object>
				{
					["path"] = AppContext.BaseDirectory,
					["category"] = (flag ? "installed" : "unpackaged")
				};
			});
			CrashReporter.Install(new SentryCrashReporter());
			SentryEnabled = true;
			Log.Info("crash", "Sentry on");
		}
		catch (Exception ex)
		{
			Log.Error("crash", "Sentry failed to start", ex);
		}
	}

	public static void ReportUiException(Exception exception)
	{
		Log.Error("crash", "unhandled UI exception", exception);
		Capture(exception, "Microsoft.UI.Xaml.UnhandledException", handled: false);
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		Log.Error("crash", "unobserved task exception", e.Exception);
		Capture(e.Exception, "UnobservedTaskException", handled: false);
		e.SetObserved();
	}

	private static void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
	{
		Exception ex = e.ExceptionObject as Exception;
		Log.Error("crash", $"unhandled exception on thread {Environment.CurrentManagedThreadId} (terminating: {e.IsTerminating})", ex);
		if (e.IsTerminating)
		{
			string text = WriteMinidump(Marshal.GetExceptionPointers(), (uint)(ex?.HResult ?? 0), ex?.GetType().FullName);
			if (text != null)
			{
				Log.Error("crash", "minidump written: " + text);
			}
		}
		if (ex != null)
		{
			Capture(ex, "AppDomain.UnhandledException", handled: false);
		}
		try
		{
			if (SentryEnabled)
			{
				SentrySdk.Flush(TimeSpan.FromSeconds(2L));
			}
		}
		catch
		{
		}
	}

	private static void Capture(Exception exception, string mechanism, bool handled)
	{
		try
		{
			if (CrashReporter.Current.IsEnabled)
			{
				CrashReporter.Current.CaptureException(exception, mechanism, handled);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("crash", "report failed: " + ex.Message);
		}
	}

	[DllImport("kernel32.dll")]
	private static extern nint SetUnhandledExceptionFilter(nint filter);

	[DllImport("kernel32.dll")]
	private static extern nint GetCurrentProcess();

	[DllImport("kernel32.dll")]
	private static extern uint GetCurrentThreadId();

	[DllImport("dbghelp.dll", SetLastError = true)]
	private static extern bool MiniDumpWriteDump(nint process, uint processId, SafeFileHandle file, uint dumpType, ref MinidumpExceptionInformation exceptionParam, nint userStreamParam, nint callbackParam);

	[DllImport("dbghelp.dll", EntryPoint = "MiniDumpWriteDump", SetLastError = true)]
	private static extern bool MiniDumpWriteDumpNoException(nint process, uint processId, SafeFileHandle file, uint dumpType, nint exceptionParam, nint userStreamParam, nint callbackParam);

	private static void InstallNativeFilter()
	{
		try
		{
			nativeFilter = OnNativeUnhandledException;
			previousNativeFilter = SetUnhandledExceptionFilter(Marshal.GetFunctionPointerForDelegate(nativeFilter));
		}
		catch (Exception ex)
		{
			Log.Warn("crash", "native crash filter not installed: " + ex.Message);
		}
	}

	private static int OnNativeUnhandledException(nint exceptionPointers)
	{
		try
		{
			uint num = 0u;
			if (exceptionPointers != IntPtr.Zero)
			{
				nint num2 = Marshal.ReadIntPtr(exceptionPointers);
				if (num2 != IntPtr.Zero)
				{
					num = (uint)Marshal.ReadInt32(num2);
				}
			}
			Log.Error("crash", $"native crash: unhandled exception 0x{num:X8} on thread {GetCurrentThreadId()}");
			string text = WriteMinidump(exceptionPointers, num, "native");
			if (text != null)
			{
				Log.Error("crash", "minidump written: " + text);
			}
		}
		catch
		{
		}
		if (previousNativeFilter != IntPtr.Zero)
		{
			try
			{
				return Marshal.GetDelegateForFunctionPointer<TopLevelExceptionFilter>(previousNativeFilter)(exceptionPointers);
			}
			catch
			{
			}
		}
		return 0;
	}

	public static string? WriteMinidump(nint exceptionPointers, uint exceptionCode, string? kind)
	{
		if (Interlocked.Exchange(ref dumpWritten, 1) == 1)
		{
			return null;
		}
		try
		{
			Directory.CreateDirectory(LogsDirectory);
			string text = Path.Combine(LogsDirectory, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}");
			string text2 = text + ".dmp";
			bool flag;
			using (FileStream fileStream = File.Create(text2))
			{
				SafeFileHandle safeFileHandle = fileStream.SafeFileHandle;
				if (exceptionPointers != IntPtr.Zero)
				{
					MinidumpExceptionInformation minidumpExceptionInformation = default(MinidumpExceptionInformation);
					minidumpExceptionInformation.ThreadId = GetCurrentThreadId();
					minidumpExceptionInformation.ExceptionPointers = exceptionPointers;
					minidumpExceptionInformation.ClientPointers = false;
					MinidumpExceptionInformation exceptionParam = minidumpExceptionInformation;
					flag = MiniDumpWriteDump(GetCurrentProcess(), (uint)Environment.ProcessId, safeFileHandle, 4452u, ref exceptionParam, IntPtr.Zero, IntPtr.Zero);
				}
				else
				{
					flag = MiniDumpWriteDumpNoException(GetCurrentProcess(), (uint)Environment.ProcessId, safeFileHandle, 4452u, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}
			}
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
			{
				["time"] = DateTimeOffset.Now.ToString("O"),
				["pid"] = Environment.ProcessId,
				["kind"] = kind,
				["exception_code"] = $"0x{exceptionCode:X8}",
				["dump"] = (flag ? Path.GetFileName(text2) : null),
				["app_version"] = ClickyClientIdentity.Current.AppVersion,
				["process"] = Path.GetFileName(Environment.ProcessPath)
			};
			File.WriteAllText(text + ".json", jsonObject.ToJsonString());
			if (!flag)
			{
				try
				{
					File.Delete(text2);
				}
				catch
				{
				}
				return null;
			}
			return text2;
		}
		catch
		{
			return null;
		}
	}

	public static void ReportPreviousCrashes()
	{
		try
		{
			if (!Directory.Exists(LogsDirectory))
			{
				return;
			}
			string[] files = Directory.GetFiles(LogsDirectory, "crash-*.json");
			foreach (string text in files)
			{
				if (text.Contains(".reported", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				JsonObject jsonObject = null;
				try
				{
					jsonObject = JsonNode.Parse(File.ReadAllText(text), null) as JsonObject;
				}
				catch
				{
				}
				if (jsonObject?["pid"]?.GetValue<int>() == Environment.ProcessId)
				{
					continue;
				}
				string text2 = jsonObject?["dump"]?.GetValue<string>();
				string dumpPath = ((text2 == null) ? null : Path.Combine(LogsDirectory, text2));
				string code = jsonObject?["exception_code"]?.GetValue<string>() ?? "unknown";
				string kind = jsonObject?["kind"]?.GetValue<string>() ?? "unknown";
				Log.Warn("crash", $"the previous run crashed ({kind}, {code}) at {jsonObject?["time"]}; minidump: {dumpPath ?? "none"}");
				if (SentryEnabled)
				{
					SentrySdk.CaptureMessage("Crash in a previous run: " + kind + " " + code, delegate(Scope scope)
					{
						scope.Level = SentryLevel.Fatal;
						scope.SetTag("crash_kind", kind);
						scope.SetTag("exception_code", code);
						scope.SetFingerprint("previous_run_crash", kind, code);
						if (dumpPath != null && File.Exists(dumpPath) && new FileInfo(dumpPath).Length < 20000000)
						{
							scope.AddAttachment(dumpPath, AttachmentType.Minidump);
						}
					}, SentryLevel.Fatal);
				}
				File.Move(text, Path.ChangeExtension(text, ".reported"), overwrite: true);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("crash", "previous crash reports: " + ex.Message);
		}
	}

	private static void PruneOldDumps()
	{
		try
		{
			if (!Directory.Exists(LogsDirectory))
			{
				return;
			}
			foreach (FileInfo item in (from f in new DirectoryInfo(LogsDirectory).GetFiles("crash-*.dmp")
				orderby f.LastWriteTimeUtc descending
				select f).Skip(5))
			{
				try
				{
					item.Delete();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	[Conditional("DEBUG")]
	public static void RunCrashTestIfRequested(DispatcherQueue ui)
	{
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_CRASH_TEST");
		if (string.IsNullOrEmpty(environmentVariable))
		{
			return;
		}
		Log.Warn("crash", "crash test: " + environmentVariable);
		switch (environmentVariable)
		{
		case "ui":
		{
			DispatcherTimer timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(200L, 0L)
			};
			timer.Tick += delegate
			{
				timer.Stop();
				throw new InvalidOperationException("HEYCLICKY_CRASH_TEST=ui: a forced UI-thread exception");
			};
			timer.Start();
			break;
		}
		case "async":
			ui.TryEnqueue(async delegate
			{
				await Task.Delay(100);
				throw new InvalidOperationException("HEYCLICKY_CRASH_TEST=async: a forced exception after an await");
			});
			break;
		case "task":
			Task.Run(delegate
			{
				throw new InvalidOperationException("HEYCLICKY_CRASH_TEST=task: a forced unobserved task exception");
			});
			Task.Run(async delegate
			{
				await Task.Delay(1000);
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			});
			break;
		case "thread":
		{
			Thread thread = new Thread((ThreadStart)delegate
			{
				throw new InvalidOperationException("HEYCLICKY_CRASH_TEST=thread: a forced unhandled exception on a background thread");
			});
			thread.IsBackground = true;
			thread.Start();
			break;
		}
		case "native":
			NativeCrash.CrashOnNativeThread();
			break;
		case "report-error":
			ClickyAnalytics.TrackAppError("crash_test", new InvalidOperationException("HEYCLICKY_CRASH_TEST=report-error: a forced user-visible error"), "error, plz restart me. if still broken, send email to hi@heyclicky.com very srry!");
			break;
		}
	}
}
