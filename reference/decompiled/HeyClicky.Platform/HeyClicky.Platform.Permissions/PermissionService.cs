using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Platform.Audio;
using Microsoft.Win32;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Permissions;

public sealed class PermissionService : IPermissionService, IDisposable
{
	private const string ConsentStore = "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone";

	private static readonly TimeSpan FastPollInterval = TimeSpan.FromSeconds(1.5);

	private static readonly TimeSpan SettledPollInterval = TimeSpan.FromSeconds(30L);

	public static readonly TimeSpan OpenButtonRevealDelay = TimeSpan.FromSeconds(1.0);

	private readonly object gate = new object();

	private readonly Dictionary<PermissionKind, PermissionStatus> statuses = new Dictionary<PermissionKind, PermissionStatus>();

	private readonly Timer pollTimer;

	private readonly Func<PermissionStatus> readMicrophone;

	private readonly Func<bool?> probeMicrophone;

	private readonly Action<PermissionKind> openSettings;

	private readonly TimeSpan openButtonRevealDelay;

	private bool polling;

	private TimeSpan currentInterval;

	private bool waitingInSettingsForMicrophone;

	private volatile bool microphoneProbePending;

	private bool? lastProbeAllowed;

	public MicrophoneBlock MicrophoneBlockedBy
	{
		get
		{
			PermissionStatus status = GetStatus(PermissionKind.Microphone);
			if ((status != PermissionStatus.WaitingInSettings && status != PermissionStatus.Denied) || 1 == 0)
			{
				return MicrophoneBlock.None;
			}
			return ReadMicrophoneBlock();
		}
	}

	public event EventHandler? Changed;

	public PermissionService()
		: this(ReadMicrophoneStatus, ProbeMicrophoneAccess, null, OpenButtonRevealDelay)
	{
	}

	internal PermissionService(Func<PermissionStatus> readMicrophone, Func<bool?> probeMicrophone, Action<PermissionKind>? openSettings, TimeSpan openButtonRevealDelay)
	{
		this.readMicrophone = readMicrophone;
		this.probeMicrophone = probeMicrophone;
		this.openSettings = openSettings ?? new Action<PermissionKind>(OpenSettingsPage);
		this.openButtonRevealDelay = openButtonRevealDelay;
		pollTimer = new Timer(delegate
		{
			Poll();
		}, null, -1, -1);
		statuses[PermissionKind.Microphone] = readMicrophone();
		statuses[PermissionKind.ScreenCapture] = PermissionStatus.Granted;
	}

	public PermissionStatus GetStatus(PermissionKind kind)
	{
		lock (gate)
		{
			PermissionStatus value;
			return statuses.TryGetValue(kind, out value) ? value : PermissionStatus.NotDetermined;
		}
	}

	public async Task<PermissionStatus> RequestAsync(PermissionKind kind)
	{
		switch (kind)
		{
		case PermissionKind.Microphone:
			if (readMicrophone() == PermissionStatus.Granted)
			{
				microphoneProbePending = true;
				Update(kind, PermissionStatus.Prompting);
				Task<bool?> probeTask = Task.Run(probeMicrophone);
				if (await Task.WhenAny(probeTask, Task.Delay(openButtonRevealDelay)).ConfigureAwait(continueOnCapturedContext: false) != probeTask)
				{
					Log.Info("Permissions", $"microphone: no answer in {openButtonRevealDelay.TotalSeconds:0.#} s, revealing Open");
					waitingInSettingsForMicrophone = true;
					Update(kind, PermissionStatus.WaitingInSettings);
					probeTask.ContinueWith(delegate(Task<bool?> t)
					{
						FinishLateProbe(t.IsCompletedSuccessfully ? t.Result : ((bool?)null));
					}, TaskScheduler.Default);
					return PermissionStatus.Prompting;
				}
				microphoneProbePending = false;
				if ((lastProbeAllowed = (probeTask.IsCompletedSuccessfully ? probeTask.Result : ((bool?)null))) != false)
				{
					Update(kind, PermissionStatus.Granted);
					return PermissionStatus.Granted;
				}
			}
			OpenSystemSettings(kind);
			waitingInSettingsForMicrophone = true;
			Update(kind, PermissionStatus.WaitingInSettings);
			return PermissionStatus.WaitingInSettings;
		case PermissionKind.ScreenCapture:
			await RequestBorderlessCaptureAsync().ConfigureAwait(continueOnCapturedContext: false);
			Update(kind, PermissionStatus.Granted);
			return PermissionStatus.Granted;
		default:
			return PermissionStatus.NotDetermined;
		}
	}

	private void FinishLateProbe(bool? allowed)
	{
		microphoneProbePending = false;
		lastProbeAllowed = allowed;
		Log.Info("Permissions", "microphone probe answered late: " + ((!allowed.HasValue) ? "inconclusive" : (allowed.Value ? "allowed" : "refused")));
		if (allowed != false)
		{
			waitingInSettingsForMicrophone = false;
			Update(PermissionKind.Microphone, PermissionStatus.Granted);
		}
	}

	public void OpenSystemSettings(PermissionKind kind)
	{
		openSettings(kind);
	}

	private static void OpenSettingsPage(PermissionKind kind)
	{
		string text = ((kind == PermissionKind.Microphone) ? "ms-settings:privacy-microphone" : "ms-settings:privacy-graphicscapturewithoutborder");
		try
		{
			ShellOpen.Start(text);
		}
		catch (Exception ex)
		{
			Log.Warn("Permissions", "could not open " + text + ": " + ex.Message);
		}
	}

	public void StartPolling()
	{
		lock (gate)
		{
			polling = true;
			currentInterval = FastPollInterval;
			pollTimer.Change(TimeSpan.Zero, currentInterval);
		}
	}

	public void StopPolling()
	{
		lock (gate)
		{
			polling = false;
			pollTimer.Change(-1, -1);
		}
	}

	public PermissionStatus RefreshMicrophoneStatus()
	{
		Poll();
		return GetStatus(PermissionKind.Microphone);
	}

	private void Poll()
	{
		if (microphoneProbePending)
		{
			return;
		}
		PermissionStatus permissionStatus = readMicrophone();
		if (permissionStatus == PermissionStatus.Denied && waitingInSettingsForMicrophone)
		{
			permissionStatus = PermissionStatus.WaitingInSettings;
		}
		if (permissionStatus == PermissionStatus.Granted)
		{
			waitingInSettingsForMicrophone = false;
		}
		Update(PermissionKind.Microphone, permissionStatus);
		lock (gate)
		{
			if (polling)
			{
				TimeSpan timeSpan = (statuses.Values.All((PermissionStatus status) => status == PermissionStatus.Granted) ? SettledPollInterval : FastPollInterval);
				if (timeSpan != currentInterval)
				{
					currentInterval = timeSpan;
					pollTimer.Change(timeSpan, timeSpan);
				}
			}
		}
	}

	private void Update(PermissionKind kind, PermissionStatus status)
	{
		bool flag;
		lock (gate)
		{
			flag = !statuses.TryGetValue(kind, out var value) || value != status;
			statuses[kind] = status;
		}
		if (flag)
		{
			Log.Info("Permissions", $"{kind} -> {status}");
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	public static PermissionStatus ReadMicrophoneStatus()
	{
		if (ReadMicrophoneBlock() != 0)
		{
			return PermissionStatus.Denied;
		}
		return PermissionStatus.Granted;
	}

	public static MicrophoneBlock ReadMicrophoneBlock()
	{
		if (ReadPolicyForcesDeny())
		{
			return MicrophoneBlock.Policy;
		}
		if (IsDeny(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone"))
		{
			return MicrophoneBlock.Device;
		}
		if (IsDeny(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone"))
		{
			return MicrophoneBlock.AllApps;
		}
		if (IsDeny(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged"))
		{
			return MicrophoneBlock.DesktopApps;
		}
		return MicrophoneBlock.None;
	}

	public static (string? Machine, string? User, string? DesktopApps, int? Policy) ReadMicrophoneToggles()
	{
		return (Machine: ReadValue(Registry.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone"), User: ReadValue(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone"), DesktopApps: ReadValue(Registry.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone\\NonPackaged"), Policy: Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy")?.GetValue("LetAppsAccessMicrophone") as int?);
	}

	private static bool ReadPolicyForcesDeny()
	{
		using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy");
		return registryKey?.GetValue("LetAppsAccessMicrophone") is int num && num == 2;
	}

	private static bool IsDeny(RegistryKey root, string path)
	{
		return string.Equals(ReadValue(root, path), "Deny", StringComparison.OrdinalIgnoreCase);
	}

	private static string? ReadValue(RegistryKey root, string path)
	{
		try
		{
			using RegistryKey registryKey = root.OpenSubKey(path);
			return registryKey?.GetValue("Value") as string;
		}
		catch
		{
			return null;
		}
	}

	public unsafe static bool? ProbeMicrophoneAccess()
	{
		IMMDeviceEnumerator iMMDeviceEnumerator = null;
		IMMDevice iMMDevice = null;
		IAudioClient audioClient = null;
		try
		{
			iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
			iMMDevice = CoreAudio.TryGetDefaultDevice(iMMDeviceEnumerator, EDataFlow.eCapture, ERole.eConsole);
			if (iMMDevice == null)
			{
				return null;
			}
			audioClient = CoreAudio.ActivateAudioClient(iMMDevice);
			WAVEFORMATEX wAVEFORMATEX = CoreAudio.Pcm16(24000, 1);
			audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, 2147483648u, 200000L, 0L, &wAVEFORMATEX, null);
			return true;
		}
		catch (COMException ex) when (ex.HResult == -2147024891)
		{
			return false;
		}
		catch (Exception ex2)
		{
			Log.Warn("Permissions", "microphone probe inconclusive: " + ex2.Message);
			return null;
		}
		finally
		{
			CoreAudio.ReleaseComObject(audioClient);
			CoreAudio.ReleaseComObject(iMMDevice);
			CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
		}
	}

	private static async Task RequestBorderlessCaptureAsync()
	{
		try
		{
			if (ApiInformation.IsTypePresent("Windows.Graphics.Capture.GraphicsCaptureAccess"))
			{
				AppCapabilityAccessStatus value = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Borderless);
				Log.Info("Permissions", $"borderless capture access: {value}");
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Permissions", "borderless capture request failed: " + ex.Message);
		}
	}

	public void Dispose()
	{
		pollTimer.Dispose();
	}
}
