using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using HeyClicky.App.Services;
using HeyClicky.App.Services.Updates;
using HeyClicky.App.Shell;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace HeyClicky.App;

public static class Program
{
	public static readonly Stopwatch SinceStart = Stopwatch.StartNew();

	private static bool ownsInstance;

	public static string[] Args { get; private set; } = Array.Empty<string>();

	public static event Action<string[]>? ArgumentsForwarded;

	[STAThread]
	private static int Main(string[] args)
	{
		try
		{
			UpdateService.ConfigureVelopack(args).Run();
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "Velopack startup: " + ex.Message);
		}
		Args = args;
		string environmentVariable = Environment.GetEnvironmentVariable("HEYCLICKY_TEST_CULTURE");
		if (environmentVariable != null && environmentVariable.Length > 0)
		{
			CultureInfo.CurrentCulture = (CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo(environmentVariable));
		}
		bool flag = Environment.GetEnvironmentVariable("HEYCLICKY_OFFSCREEN") == "1";
		if (!args.Any(delegate(string a)
		{
			switch (a)
			{
			case "--demo-states":
			case "--demo-only":
			case "--fakes":
				return true;
			default:
				return false;
			}
		}) && (!flag || (!args.Contains("--e2e") && !AppPaths.IsOverridden)) && (!args.Contains("--e2e") || !OverlayWindow.OnscreenBesideReal))
		{
			if (!SingleInstance.TryClaim(args))
			{
				return 0;
			}
			ownsInstance = true;
			SingleInstance.ArgumentsReceived += delegate(string[] forwarded)
			{
				Program.ArgumentsForwarded?.Invoke(forwarded);
			};
		}
		CrashReporting.Install();
		GpuScheduling.RaiseForProcess();
		try
		{
			Log.Info("startup", "Initializing ComWrappers...");
			ComWrappersSupport.InitializeComWrappers();
			Log.Info("startup", "Calling Application.Start...");
			Application.Start(delegate
			{
				try
				{
					Log.Info("startup", "Inside Application.Start callback...");
					SynchronizationContext.SetSynchronizationContext(new CrashReporting.ReportingSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
					new App();
				}
				catch (Exception ex2)
				{
					Log.Error("startup", "Exception in Application.Start callback", ex2);
					throw;
				}
			});
		}
		catch (Exception ex)
		{
			Log.Error("startup", "Exception in Main", ex);
		}
		if (ownsInstance)
		{
			SingleInstance.Release();
		}
		Log.Info("app", "exited");
		Exit();
		return 0;
	}

	internal static void Exit()
	{
		Thread thread = new Thread((ThreadStart)delegate
		{
			Thread.Sleep(TimeSpan.FromSeconds(5L));
			Log.Warn("shutdown", "still exiting 5 s later: terminating");
			Terminate();
		});
		thread.IsBackground = true;
		thread.Name = "ExitWatchdog";
		thread.Start();
		Environment.Exit(0);
	}

	internal static void Terminate()
	{
		TerminateProcess(GetCurrentProcess(), 0u);
	}

	[DllImport("kernel32.dll")]
	private static extern nint GetCurrentProcess();

	[DllImport("kernel32.dll")]
	private static extern bool TerminateProcess(nint process, uint exitCode);

	public static double MillisecondsSinceProcessStart()
	{
		try
		{
			return (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
		}
		catch
		{
			return SinceStart.Elapsed.TotalMilliseconds;
		}
	}
}
