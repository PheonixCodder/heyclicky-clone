using System;
using System.IO;
using System.Text.Json;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Configuration;

namespace HeyClicky.Core.Proactive;

public sealed class ProactiveStore
{
	private readonly string path;

	private readonly object gate = new object();

	private ProactiveStoreData data;

	public ProactiveStore(string? path = null)
	{
		this.path = path ?? Path.Combine(AppBundleConfiguration.AppDataDirectory, "proactive.json");
		data = Load(this.path);
	}

	private static ProactiveStoreData Load(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				return JsonSerializer.Deserialize(AtomicFile.ReadJson(path).Span, ProactiveJsonContext.Default.ProactiveStoreData) ?? new ProactiveStoreData();
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Proactive", "proactive.json unreadable (" + ex.Message + "); starting fresh");
			AtomicFile.KeepUnreadable(path);
		}
		return new ProactiveStoreData();
	}

	public T Read<T>(Func<ProactiveStoreData, T> read)
	{
		lock (gate)
		{
			return read(data);
		}
	}

	public T ReadAccount<T>(string accountKey, Func<ProactiveAccountState, T> read)
	{
		lock (gate)
		{
			ProactiveAccountState value;
			return read(data.Accounts.TryGetValue(accountKey, out value) ? value : new ProactiveAccountState());
		}
	}

	public void Update(Action<ProactiveStoreData> change)
	{
		byte[] array;
		lock (gate)
		{
			change(data);
			array = JsonSerializer.SerializeToUtf8Bytes(data, ProactiveJsonContext.Default.ProactiveStoreData);
		}
		try
		{
			AtomicFile.WriteAllBytes(path, array);
		}
		catch (Exception ex)
		{
			Log.Warn("Proactive", "proactive.json write failed: " + ex.Message);
		}
	}

	public void UpdateAccount(string accountKey, Action<ProactiveAccountState> change)
	{
		Update(delegate(ProactiveStoreData d)
		{
			if (!d.Accounts.TryGetValue(accountKey, out ProactiveAccountState value))
			{
				value = (d.Accounts[accountKey] = new ProactiveAccountState());
			}
			change(value);
		});
	}
}
