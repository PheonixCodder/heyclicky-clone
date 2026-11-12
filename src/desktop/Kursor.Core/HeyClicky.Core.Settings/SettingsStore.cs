using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Configuration;

namespace HeyClicky.Core.Settings;

public sealed class SettingsStore : IDisposable
{
	private readonly string filePath;

	private readonly object gate = new object();

	private readonly Timer saveTimer;

	private AppSettings current;

	private bool dirty;

	public string FilePath => filePath;

	public AppSettings Current
	{
		get
		{
			lock (gate)
			{
				return Clone(current);
			}
		}
	}

	public event EventHandler? Changed;

	public SettingsStore(string? filePath = null)
	{
		this.filePath = filePath ?? Path.Combine(AppBundleConfiguration.AppDataDirectory, "settings.json");
		current = Load(this.filePath);
		saveTimer = new Timer(delegate
		{
			Flush();
		}, null, -1, -1);
	}

	public T Read<T>(Func<AppSettings, T> read)
	{
		lock (gate)
		{
			return read(current);
		}
	}

	public void Update(Action<AppSettings> change)
	{
		lock (gate)
		{
			change(current);
			dirty = true;
			saveTimer.Change(250, -1);
		}
		try
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Error("Settings", "Changed handler threw", ex);
		}
	}

	public void Flush()
	{
		byte[] array;
		lock (gate)
		{
			if (!dirty)
			{
				return;
			}
			dirty = false;
			array = JsonSerializer.SerializeToUtf8Bytes(current, CoreJsonContext.Default.AppSettings);
		}
		try
		{
			AtomicFile.WriteAllBytes(filePath, array);
		}
		catch (Exception ex)
		{
			Log.Error("Settings", "settings.json write failed", ex);
			lock (gate)
			{
				dirty = true;
			}
		}
	}

	private static AppSettings Load(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				return JsonSerializer.Deserialize(AtomicFile.ReadJson(path).Span, CoreJsonContext.Default.AppSettings) ?? new AppSettings();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Settings", "settings.json unreadable (" + ex.Message + "); using defaults (the old file is kept as settings.json.unreadable)");
			AtomicFile.KeepUnreadable(path);
		}
		return new AppSettings();
	}

	private static AppSettings Clone(AppSettings settings)
	{
		return JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(settings, CoreJsonContext.Default.AppSettings), CoreJsonContext.Default.AppSettings);
	}

	public void Dispose()
	{
		saveTimer.Dispose();
		Flush();
	}
}
