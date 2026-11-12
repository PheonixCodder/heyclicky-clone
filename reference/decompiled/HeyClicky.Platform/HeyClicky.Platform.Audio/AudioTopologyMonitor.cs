using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;

namespace HeyClicky.Platform.Audio;

public sealed class AudioTopologyMonitor : IDisposable
{
	[ComVisible(true)]
	private sealed class TopologyListener(AutoResetEvent signal) : IMMNotificationClient
	{
		private void Signal()
		{
			try
			{
				signal.Set();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		public void OnDeviceStateChanged(PCWSTR pwstrDeviceId, DEVICE_STATE dwNewState)
		{
			Signal();
		}

		public void OnDeviceAdded(PCWSTR pwstrDeviceId)
		{
			Signal();
		}

		public void OnDeviceRemoved(PCWSTR pwstrDeviceId)
		{
			Signal();
		}

		public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, PCWSTR pwstrDefaultDeviceId)
		{
			if (role == ERole.eConsole)
			{
				Signal();
			}
		}

		public void OnPropertyValueChanged(PCWSTR pwstrDeviceId, PROPERTYKEY key)
		{
		}
	}

	private const int DebounceMilliseconds = 400;

	private static readonly Lazy<AudioTopologyMonitor> shared = new Lazy<AudioTopologyMonitor>(() => new AudioTopologyMonitor());

	private readonly object gate = new object();

	private readonly AutoResetEvent signal = new AutoResetEvent(initialState: false);

	private readonly Thread thread;

	private volatile bool disposed;

	private AudioTopologySnapshot snapshot = AudioTopologySnapshot.Unknown;

	public static AudioTopologyMonitor Shared => shared.Value;

	public AudioTopologySnapshot Snapshot
	{
		get
		{
			lock (gate)
			{
				return snapshot;
			}
		}
	}

	public event Action<AudioTopologySnapshot>? Changed;

	private AudioTopologyMonitor()
	{
		thread = new Thread(Run)
		{
			IsBackground = true,
			Name = "HeyClicky audio topology"
		};
		thread.SetApartmentState(ApartmentState.MTA);
		thread.Start();
	}

	private void Run()
	{
		IMMDeviceEnumerator iMMDeviceEnumerator = null;
		TopologyListener topologyListener = null;
		try
		{
			iMMDeviceEnumerator = CoreAudio.CreateDeviceEnumerator();
			topologyListener = new TopologyListener(signal);
			try
			{
				iMMDeviceEnumerator.RegisterEndpointNotificationCallback(topologyListener);
			}
			catch (COMException ex)
			{
				topologyListener = null;
				Log.Warn("AudioTopology", $"notifications unavailable (0x{ex.HResult:X8})");
			}
			Recompute(notify: false);
			Log.Info("AudioTopology", $"initial: {Snapshot}");
			while (!disposed)
			{
				signal.WaitOne();
				if (!disposed)
				{
					while (!disposed && signal.WaitOne(400))
					{
					}
					if (!disposed)
					{
						Recompute(notify: true);
						continue;
					}
					break;
				}
				break;
			}
		}
		catch (Exception ex2)
		{
			Log.Error("AudioTopology", "monitor stopped", ex2);
		}
		finally
		{
			if (topologyListener != null && iMMDeviceEnumerator != null)
			{
				try
				{
					iMMDeviceEnumerator.UnregisterEndpointNotificationCallback(topologyListener);
				}
				catch
				{
				}
			}
			CoreAudio.ReleaseComObject(iMMDeviceEnumerator);
		}
	}

	private void Recompute(bool notify)
	{
		AudioEndpointInfo audioEndpointInfo = AudioEndpoints.DefaultEndpoint(capture: false);
		AudioEndpointInfo audioEndpointInfo2 = AudioEndpoints.DefaultEndpoint(capture: true);
		AudioTopologySnapshot audioTopologySnapshot = new AudioTopologySnapshot(audioEndpointInfo?.Name ?? "none", audioEndpointInfo?.TransportCategory ?? "none", audioEndpointInfo2?.Name ?? "none", audioEndpointInfo2?.TransportCategory ?? "none", audioEndpointInfo?.IsHeadphones ?? false);
		AudioTopologySnapshot audioTopologySnapshot2;
		lock (gate)
		{
			audioTopologySnapshot2 = snapshot;
			snapshot = audioTopologySnapshot;
		}
		if (!notify)
		{
			return;
		}
		if (audioTopologySnapshot != audioTopologySnapshot2)
		{
			Log.Info("AudioTopology", $"changed: {audioTopologySnapshot}");
		}
		try
		{
			this.Changed?.Invoke(audioTopologySnapshot);
		}
		catch (Exception ex)
		{
			Log.Warn("AudioTopology", "observer threw: " + ex.Message);
		}
	}

	public void Dispose()
	{
		disposed = true;
		try
		{
			signal.Set();
		}
		catch (ObjectDisposedException)
		{
		}
	}
}
