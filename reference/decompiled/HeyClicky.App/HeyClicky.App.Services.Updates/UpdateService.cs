using System;
using System.Globalization;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Identity;
using HeyClicky.Core.Telemetry;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;
using Velopack;
using Velopack.Logging;

namespace HeyClicky.App.Services.Updates;

public sealed class UpdateService : IUpdateService, IDisposable
{
	private sealed class VelopackLog : IVelopackLogger
	{
		public void Log(VelopackLogLevel level, string? message, Exception? exception)
		{
			if (level >= VelopackLogLevel.Information)
			{
				string message2 = ((exception == null) ? (message ?? "") : (message + ": " + exception.Message));
				if (level >= VelopackLogLevel.Error)
				{
					HeyClicky.Log.Warn("velopack", message2);
				}
				else
				{
					HeyClicky.Log.Info("velopack", message2);
				}
			}
		}
	}

	private const string Area = "updates";

	private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

	private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromSeconds(20L);

	private static readonly TimeSpan UpToDateDisplay = TimeSpan.FromSeconds(10L);

	private static readonly TimeSpan IdleInstallAfter = (double.TryParse(Environment.GetEnvironmentVariable("HEYCLICKY_IDLE_INSTALL_AFTER_SECONDS"), NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? TimeSpan.FromSeconds(result) : IdleInstallPolicy.IdleFor);

	private static readonly TimeSpan IdleInstallCheckInterval = ((IdleInstallAfter < TimeSpan.FromMinutes(1L)) ? TimeSpan.FromSeconds(5L) : TimeSpan.FromSeconds(15L));

	private readonly DispatcherQueue ui;

	private readonly UpdateManager? manager;

	private readonly string feed;

	private readonly bool feedIsPlaceholder;

	private readonly SemaphoreSlim busy = new SemaphoreSlim(1, 1);

	private Timer? timer;

	private Timer? idleInstallTimer;

	private string? idleInstallWait;

	private long lastBusyTicks = Environment.TickCount64;

	private UpdateInfo? pending;

	private CancellationTokenSource? upToDateReset;

	private bool installOnQuitArmed;

	public string CurrentVersion { get; }

	public UpdateState State { get; private set; }

	public string? AvailableVersion { get; private set; }

	public int DownloadPercent { get; private set; }

	public Action? QuitApp { get; set; }

	public Func<bool, IdleInstallContext>? ReadIdleContext { get; set; }

	public static string PendingInstallFile => Path.Combine(AppBundleConfiguration.AppDataDirectory, "pending-update.json");

	public string RowTitle => State switch
	{
		UpdateState.ReadyToInstall => "Update ready to install", 
		UpdateState.Downloading => "Update available", 
		_ => "App updates", 
	};

	public string RowSubtitle => State switch
	{
		UpdateState.ReadyToInstall => "Version " + AvailableVersion + " is ready. Installing relaunches HeyClicky.", 
		UpdateState.Downloading => $"Version {AvailableVersion} is downloading ({DownloadPercent}%).", 
		UpdateState.Checking => "Checking for updates…", 
		UpdateState.UpToDate => "You're up to date. You're on version " + CurrentVersion + ".", 
		UpdateState.Failed => "Couldn't check for updates just now. Try again in a bit.", 
		UpdateState.Unavailable => "You're on version " + CurrentVersion + ". This copy wasn't installed with the HeyClicky installer, so it can't update itself.", 
		_ => "You're on version " + CurrentVersion + ".", 
	};

	public string ButtonTitle => State switch
	{
		UpdateState.ReadyToInstall => "Install & Relaunch", 
		UpdateState.Downloading => "Downloading…", 
		UpdateState.Checking => "Checking…", 
		_ => "Check for updates", 
	};

	public bool ButtonIsPrimary
	{
		get
		{
			UpdateState state = State;
			if ((uint)(state - 4) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public event EventHandler? Changed;

	public UpdateService(DispatcherQueue ui, TelemetryConfiguration? config = null)
	{
		this.ui = ui;
		if ((object)config == null)
		{
			config = TelemetryConfiguration.Current;
		}
		feed = config.UpdateFeedUrl;
		feedIsPlaceholder = config.IsPlaceholderFeed;
		try
		{
			manager = new UpdateManager(feed, new UpdateOptions
			{
				AllowVersionDowngrade = false
			});
			if (!manager.IsInstalled)
			{
				manager = null;
			}
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "updater unavailable: " + ex.Message);
			manager = null;
		}
		CurrentVersion = manager?.CurrentVersion?.ToString() ?? ClickyClientIdentity.Current.AppVersion;
		State = ((manager != null) ? UpdateState.Idle : UpdateState.Unavailable);
		if (manager != null)
		{
			VelopackAsset updatePendingRestart = manager.UpdatePendingRestart;
			if ((object)updatePendingRestart != null)
			{
				AvailableVersion = updatePendingRestart.Version.ToString();
				State = UpdateState.ReadyToInstall;
			}
		}
		Log.Info("updates", (manager == null) ? ("v" + CurrentVersion + ": not an installed copy, updates off") : $"v{CurrentVersion} (installed), feed {feed}{(feedIsPlaceholder ? " (placeholder: no releases published yet)" : "")}");
	}

	public static bool IsVelopackInstall()
	{
		try
		{
			return new UpdateManager("https://localhost.invalid/").IsInstalled;
		}
		catch
		{
			return false;
		}
	}

	public void Start()
	{
		if (manager == null)
		{
			return;
		}
		if (feedIsPlaceholder)
		{
			Log.Info("updates", "scheduled checks off: the update feed is still the placeholder (set updateFeedUrl, RELEASING.md)");
			return;
		}
		timer = new Timer(delegate(object? _)
		{
			_ = CheckAsync(manual: false, null);
		}, null, FirstCheckDelay, CheckInterval);
		idleInstallTimer = new Timer(delegate
		{
			ui.TryEnqueue(InstallIfIdle);
		}, null, IdleInstallCheckInterval, IdleInstallCheckInterval);
	}

	public void NoteHeyClickyBusy()
	{
		Interlocked.Exchange(ref lastBusyTicks, Environment.TickCount64);
	}

	public void InstallIfIdle()
	{
		if (installOnQuitArmed)
		{
			return;
		}
		Func<bool, IdleInstallContext> readIdleContext = ReadIdleContext;
		if (readIdleContext == null)
		{
			return;
		}
		string text;
		try
		{
			bool flag = State == UpdateState.ReadyToInstall;
			IdleInstallContext now = readIdleContext(flag);
			if (IdleInstallPolicy.BusyReason(now) != null)
			{
				NoteHeyClickyBusy();
			}
			if (!flag)
			{
				return;
			}
			TimeSpan heyClickyIdle = TimeSpan.FromMilliseconds(Environment.TickCount64 - Interlocked.Read(in lastBusyTicks), 0L);
			text = IdleInstallPolicy.WhyWait(now, heyClickyIdle, IdleInstallAfter);
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "idle install check failed: " + ex.Message);
			return;
		}
		if (text != null)
		{
			if (text != idleInstallWait)
			{
				Log.Info("updates", "v" + AvailableVersion + " waits to install: " + text);
			}
			idleInstallWait = text;
		}
		else
		{
			Log.Info("updates", $"HeyClicky idle for {IdleInstallAfter.TotalMinutes:0.#}+ min: installing v{AvailableVersion} by itself");
			InstallAndRelaunch("idle_auto");
		}
	}

	public Task CheckAfterWakeAsync()
	{
		if (!feedIsPlaceholder)
		{
			return CheckAsync(manual: false, null);
		}
		return Task.CompletedTask;
	}

	public Task CheckNowAsync(string surface)
	{
		ClickyAnalytics.TrackCheckForUpdatesClicked(surface);
		return CheckAsync(manual: true, surface);
	}

	private async Task CheckAsync(bool manual, string? surface)
	{
		if (manager == null)
		{
			if (manual)
			{
				Log.Info("updates", "check skipped: not an installed copy");
			}
		}
		else
		{
			if (State == UpdateState.ReadyToInstall || !(await busy.WaitAsync(0).ConfigureAwait(continueOnCapturedContext: false)))
			{
				return;
			}
			try
			{
				if (manual)
				{
					Set(UpdateState.Checking);
				}
				Log.Info("updates", "checking (" + (manual ? ("manual, " + surface) : "scheduled") + ")");
				UpdateInfo info = await manager.CheckForUpdatesAsync().ConfigureAwait(continueOnCapturedContext: false);
				if (info == null)
				{
					Log.Info("updates", "up to date (v" + CurrentVersion + ")");
					if (manual)
					{
						Set(UpdateState.UpToDate);
						ScheduleUpToDateReset();
						return;
					}
					UpdateState state = State;
					if ((state == UpdateState.Checking || state == UpdateState.Failed) ? true : false)
					{
						Set(UpdateState.Idle);
					}
					return;
				}
				string target = info.TargetFullRelease.Version.ToString();
				Log.Info("updates", $"found v{target} ({info.DeltasToTarget.Length} delta(s) from v{CurrentVersion})");
				ClickyAnalytics.TrackUpdateFound(CurrentVersion, target);
				AvailableVersion = target;
				DownloadPercent = 0;
				Set(UpdateState.Downloading);
				int lastLogged = -10;
				await manager.DownloadUpdatesAsync(info, delegate(int percent)
				{
					DownloadPercent = percent;
					if (percent - lastLogged >= 10)
					{
						lastLogged = percent;
						Log.Info("updates", $"downloading v{target}: {percent}%");
					}
					Post();
				}).ConfigureAwait(continueOnCapturedContext: false);
				pending = info;
				RecordPendingInstall(target);
				Log.Info("updates", "v" + target + " downloaded; installs on quit or with Install & Relaunch");
				Set(UpdateState.ReadyToInstall);
				if (Environment.GetEnvironmentVariable("HEYCLICKY_TEST_INSTALL_AND_RELAUNCH") == "1")
				{
					ui.TryEnqueue(delegate
					{
						InstallAndRelaunch("update_flow_test");
					});
				}
			}
			catch (Exception ex)
			{
				Log.Warn("updates", "check failed: " + ex.Message);
				if (manual || State == UpdateState.Downloading)
				{
					Set(UpdateState.Failed);
				}
				if (!manual && !feedIsPlaceholder)
				{
					ClickyAnalytics.ReportInternalError("updates", "scheduled_check", ex, null, CrashReportLevel.Warning);
				}
			}
			finally
			{
				busy.Release();
			}
		}
	}

	private void ScheduleUpToDateReset()
	{
		upToDateReset?.Cancel();
		CancellationTokenSource cancellationTokenSource = (upToDateReset = new CancellationTokenSource());
		Task.Delay(UpToDateDisplay, cancellationTokenSource.Token).ContinueWith(delegate(Task t)
		{
			if (!t.IsCanceled && State == UpdateState.UpToDate)
			{
				Set(UpdateState.Idle);
			}
		}, TaskScheduler.Default);
	}

	public void PerformRowAction(string surface)
	{
		if (State == UpdateState.ReadyToInstall)
		{
			InstallAndRelaunch(surface);
			return;
		}
		UpdateState state = State;
		if ((state != UpdateState.Checking && state != UpdateState.Downloading) || 1 == 0)
		{
			CheckNowAsync(surface);
		}
	}

	public void InstallAndRelaunch(string surface)
	{
		if (manager == null || State != UpdateState.ReadyToInstall)
		{
			return;
		}
		string text = AvailableVersion ?? "unknown";
		ClickyAnalytics.TrackUpdateInstallClicked(CurrentVersion, text, surface);
		RecordPendingInstall(text);
		ClickyAnalytics.FlushAsync();
		try
		{
			Log.Info("updates", "installing v" + text + " and relaunching");
			VelopackAsset velopackAsset = pending?.TargetFullRelease ?? manager.UpdatePendingRestart;
			manager.WaitExitThenApplyUpdates(velopackAsset, silent: true, restart: true, Array.Empty<string>());
			installOnQuitArmed = true;
			if (surface != "idle_auto")
			{
				UpdateWindow.TryShow(velopackAsset?.FileName);
			}
			QuitApp?.Invoke();
		}
		catch (Exception ex)
		{
			Log.Error("updates", "install failed", ex);
			ClickyAnalytics.ReportInternalError("updates", "install", ex);
			Set(UpdateState.Failed);
		}
	}

	public void ApplyOnQuitIfReady()
	{
		if (manager == null || installOnQuitArmed)
		{
			return;
		}
		VelopackAsset velopackAsset = pending?.TargetFullRelease ?? manager.UpdatePendingRestart;
		if ((object)velopackAsset == null)
		{
			return;
		}
		try
		{
			Log.Info("updates", $"quitting with v{velopackAsset.Version} downloaded: installing it as the app exits");
			manager.WaitExitThenApplyUpdates(velopackAsset, silent: true, restart: false, Array.Empty<string>());
			installOnQuitArmed = true;
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "install on quit failed: " + ex.Message);
		}
	}

	private static void RecordPendingInstall(string toVersion)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(PendingInstallFile));
			JsonObject jsonObject = new JsonObject((JsonNodeOptions?)null)
			{
				["from_version"] = CurrentVersionStatic(),
				["to_version"] = toVersion,
				["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};
			File.WriteAllText(PendingInstallFile, jsonObject.ToJsonString());
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "pending-install record not saved: " + ex.Message);
		}
	}

	private static string CurrentVersionStatic()
	{
		try
		{
			return new UpdateManager("https://localhost.invalid/").CurrentVersion?.ToString() ?? ClickyClientIdentity.Current.AppVersion;
		}
		catch
		{
			return ClickyClientIdentity.Current.AppVersion;
		}
	}

	public string? ConsumePendingInstallCompletion()
	{
		if (manager == null)
		{
			return null;
		}
		try
		{
			if (!File.Exists(PendingInstallFile))
			{
				return null;
			}
			JsonObject obj = JsonNode.Parse(File.ReadAllText(PendingInstallFile), null) as JsonObject;
			File.Delete(PendingInstallFile);
			string text = obj?["from_version"]?.GetValue<string>();
			string text2 = obj?["to_version"]?.GetValue<string>() ?? "unknown";
			if (text == null || text == CurrentVersion)
			{
				Log.Info("updates", $"pending install of v{text2} didn't land (still v{CurrentVersion})");
				return null;
			}
			Log.Info("updates", "updated: v" + text + " → v" + CurrentVersion);
			ClickyAnalytics.TrackUpdateInstallCompleted(text, text2);
			return CurrentVersion;
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "pending-install record unreadable: " + ex.Message);
			try
			{
				File.Delete(PendingInstallFile);
			}
			catch
			{
			}
			return null;
		}
	}

	private void Set(UpdateState state)
	{
		State = state;
		Post();
	}

	private void Post()
	{
		if (!ui.TryEnqueue(delegate
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}))
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public void Dispose()
	{
		timer?.Dispose();
		idleInstallTimer?.Dispose();
		upToDateReset?.Cancel();
	}

	public static VelopackApp ConfigureVelopack(string[] args)
	{
		return VelopackApp.Build().SetArgs(args).SetLogger(new VelopackLog())
			.OnAfterInstallFastCallback(delegate
			{
				RegisterShellIntegration();
			})
			.OnAfterUpdateFastCallback(delegate
			{
				RegisterShellIntegration();
			})
			.OnBeforeUninstallFastCallback(delegate
			{
				RemoveShellIntegration();
			});
	}

	private static void RegisterShellIntegration()
	{
		try
		{
			ProtocolRegistration.Register();
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "protocol registration: " + ex.Message);
		}
	}

	private static void RemoveShellIntegration()
	{
		try
		{
			if (ProtocolRegistration.IsRegistered())
			{
				ProtocolRegistration.Unregister();
			}
			FileOpenRegistration.Unregister();
			string command = LaunchAtLogin.GetCommand();
			if (command != null)
			{
				string processPath = Environment.ProcessPath;
				if (processPath != null && command.Contains(processPath, StringComparison.OrdinalIgnoreCase))
				{
					LaunchAtLogin.SetEnabled(enabled: false);
				}
			}
			Log.Info("updates", "uninstall: deep links and launch at login removed");
		}
		catch (Exception ex)
		{
			Log.Warn("updates", "uninstall cleanup: " + ex.Message);
		}
	}
}
