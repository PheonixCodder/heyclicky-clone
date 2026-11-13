using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using HeyClicky.Core.Browser;
using HeyClicky.Core.Telemetry;

namespace HeyClicky.App.Browser;

public static class ChromeAutoInstall
{
	public static bool IsRunning { get; private set; }

	public static void InstallByDriving(ChromeSetupFlow flow, string? threadId, bool evenIfConnected, Action<bool, bool>? finished = null)
	{
		if (finished == null)
		{
			finished = delegate(bool connected, bool stoppedByUser)
			{
				if (!connected && !stoppedByUser)
				{
					flow.ShowHow(afterDrive: true);
				}
			};
		}
		ClickyBrowserBridge bridge = ClickyBrowserBridge.Shared;
		IChromeClickyDrive drive = flow.Drive;
		(string Name, string Executable) browser;
		bool swapsOldCopy;
		string process;
		if (drive != null && drive.IsAvailable && !drive.IsDrivingNow && !IsRunning)
		{
			(string, string)? tuple = ChromiumBrowsers.Preferred();
			if (tuple.HasValue)
			{
				browser = tuple.GetValueOrDefault();
				IsRunning = true;
				swapsOldCopy = bridge.HasOldCopyToRemove;
				ClickyAnalytics.TrackChromeJobAction("chrome_install_drive_started", threadId, new JsonObject((JsonNodeOptions?)null)
				{
					["browser"] = browser.Name,
					["removes_old_copy"] = swapsOldCopy
				});
				process = Path.GetFileNameWithoutExtension(browser.Executable);
				RunAsync();
				return;
			}
		}
		finished(arg1: false, arg2: false);
		async Task AfterDriveAsync(ClickyDriveEnding ending)
		{
			if (swapsOldCopy)
			{
				if (ending == ClickyDriveEnding.Finished)
				{
					bridge.SetHasOldCopyToRemove(value: false);
				}
				ClickyAnalytics.TrackChromeJobAction("chrome_old_copy_drive_ended", threadId, new JsonObject((JsonNodeOptions?)null)
				{
					["stopped_by_user"] = ending == ClickyDriveEnding.UserTookOver,
					["ending"] = EndingName(ending)
				});
			}
			if (ending == ClickyDriveEnding.UserTookOver)
			{
				await EndedAsync(threadId, stoppedByUser: true, finished);
			}
			else
			{
				for (int i = 0; i < 24; i++)
				{
					if (bridge.IsConnected)
					{
						break;
					}
					await Task.Delay(250);
				}
				await EndedAsync(threadId, stoppedByUser: false, finished);
			}
		}
		async Task RunAsync()
		{
			if (!(await bridge.ConnectAsync(!swapsOldCopy)) && !bridge.IsConnected)
			{
				IsRunning = false;
				finished(arg1: false, arg2: true);
			}
			else if (bridge.IsConnected && !evenIfConnected)
			{
				await EndedAsync(threadId, stoppedByUser: false, finished);
			}
			else
			{
				if (swapsOldCopy)
				{
					await bridge.OpenExtensionsPageAsync();
				}
				else if (evenIfConnected)
				{
					bridge.OpenStoreListing();
				}
				await Task.Delay(1500);
				string storeUrl = Environment.GetEnvironmentVariable("KURSOR_CHROME_STORE_URL") ?? "https://chromewebstore.google.com/detail/kursor/bphdnlcghkpiaoiplkhfofpfboddahce";
				string goal = (swapsOldCopy ? ChromeSetupBriefs.SwapGoal(browser.Name, storeUrl) : ChromeSetupBriefs.StoreGoal(browser.Name, storeUrl));
				if (!drive.DriveForApp(goal, process, delegate(ClickyDriveEnding ending)
				{
					AfterDriveAsync(ending);
				}))
				{
					IsRunning = false;
					finished(arg1: false, arg2: false);
				}
			}
		}
	}

	private static Task EndedAsync(string? threadId, bool stoppedByUser, Action<bool, bool> finished)
	{
		ClickyBrowserBridge shared = ClickyBrowserBridge.Shared;
		IsRunning = false;
		ClickyAnalytics.TrackChromeJobAction(shared.IsConnected ? "chrome_install_drive_connected" : "chrome_install_drive_fell_back", threadId);
		finished(shared.IsConnected, stoppedByUser && !shared.IsConnected);
		return Task.CompletedTask;
	}

	private static string EndingName(ClickyDriveEnding ending)
	{
		return ending switch
		{
			ClickyDriveEnding.Finished => "finished", 
			ClickyDriveEnding.UserTookOver => "userTookOver", 
			ClickyDriveEnding.SafetyStop => "safetyStop", 
			ClickyDriveEnding.CouldNotSee => "couldNotSee", 
			ClickyDriveEnding.OutOfAgentMessages => "outOfAgentMessages", 
			_ => "failed", 
		};
	}
}
