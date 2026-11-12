using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Tools;

public interface IAgentToolHost
{
	IReadOnlyList<AgentClickyInfo> Clickys { get; }

	IReadOnlyList<AgentTaskInfo> Tasks { get; }

	string ProjectsRoot { get; }

	AgentClickyInfo? FindClicky(string slug);

	string? WorkspacePath(string clickySlug);

	Task<string> LaunchAsync(AgentToolLaunch launch, CancellationToken ct);

	Task SteerAsync(string taskId, string text, CancellationToken ct);

	Task SteerAsync(string taskId, string text, IReadOnlyList<byte[]>? imagesJpeg, CancellationToken ct)
	{
		return SteerAsync(taskId, text, ct);
	}

	string? BuildsOnContext(IReadOnlyList<string> jobIds, string? receivingThreadId)
	{
		return null;
	}

	string OpenThread(string threadId);

	Task StopAsync(string taskId);

	Task ApproveExtraUsageAsync(string taskId, bool asManyAsNeeded, CancellationToken ct);

	AgentClickyInfo CreateClicky(string name, string roleText, string oneLiner);

	Task<IReadOnlyList<(string Role, string Text)>> RecentMessagesAsync(string threadId, int limit, CancellationToken ct);

	Task<IReadOnlyList<AgentThreadInfo>> HistoryAsync(int limit, CancellationToken ct);

	bool OpenHome(string? threadId);

	bool CloseHome();

	bool OpenPath(string pathOrUrl);

	bool RevealPath(string path);

	bool SkipOnboardingInterview();

	AgentJobInfo? FindJob(string threadId)
	{
		return null;
	}

	string? ClickySlugForThread(string threadId)
	{
		return Clickys.FirstOrDefault((AgentClickyInfo c) => c.ThreadId == threadId)?.Slug;
	}

	Task<string?> HandleSuggestionActionAsync(string task)
	{
		return Task.FromResult<string>(null);
	}

	void ApproveComputerUse(string taskId)
	{
	}

	void DeclineComputerUse(string taskId)
	{
	}

	Task<string> FindJobsAsync(string query, int limit)
	{
		return Task.FromResult("{\"status\":\"none\",\"jobs\":[],\"message\":\"No earlier work on this PC yet.\"}");
	}

	(bool IsJob, string? ThreadId) ThreadIdForJobId(string jobId)
	{
		return (IsJob: false, ThreadId: null);
	}

	string JobIdForThread(string threadId)
	{
		return threadId;
	}

	void RetitleJob(string threadId, string title)
	{
	}
}
