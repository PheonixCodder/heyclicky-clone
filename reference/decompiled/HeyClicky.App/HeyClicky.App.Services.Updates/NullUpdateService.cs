using System;
using System.Threading.Tasks;
using HeyClicky.Core.Identity;

namespace HeyClicky.App.Services.Updates;

public sealed class NullUpdateService : IUpdateService
{
	public static readonly NullUpdateService Instance = new NullUpdateService();

	public string CurrentVersion => ClickyClientIdentity.Current.AppVersion;

	public UpdateState State => UpdateState.Unavailable;

	public string? AvailableVersion => null;

	public int DownloadPercent => 0;

	public string RowTitle => "App updates";

	public string RowSubtitle => "You're on version " + CurrentVersion + ".";

	public string ButtonTitle => "Check for updates";

	public bool ButtonIsPrimary => false;

	public event EventHandler? Changed
	{
		add
		{
		}
		remove
		{
		}
	}

	public void PerformRowAction(string surface)
	{
	}

	public Task CheckNowAsync(string surface)
	{
		return Task.CompletedTask;
	}

	public void InstallAndRelaunch(string surface)
	{
	}
}
