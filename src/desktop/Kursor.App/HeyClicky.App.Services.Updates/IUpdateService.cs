using System;
using System.Threading.Tasks;

namespace HeyClicky.App.Services.Updates;

public interface IUpdateService
{
	string CurrentVersion { get; }

	UpdateState State { get; }

	string? AvailableVersion { get; }

	int DownloadPercent { get; }

	string RowTitle { get; }

	string RowSubtitle { get; }

	string ButtonTitle { get; }

	bool ButtonIsPrimary { get; }

	event EventHandler? Changed;

	void PerformRowAction(string surface);

	Task CheckNowAsync(string surface);

	void InstallAndRelaunch(string surface);
}
