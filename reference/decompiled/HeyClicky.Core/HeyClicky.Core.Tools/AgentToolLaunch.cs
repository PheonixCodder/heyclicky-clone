using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HeyClicky.Core.Tools;

public sealed record AgentToolLaunch(string Prompt, string? ClickySlug = null, string? NewClickyName = null, string NewClickyRole = "Helper", string? NewClickyOneLiner = null, string? LaunchContext = null, string LaunchSource = "voice", IReadOnlyList<byte[]>? ImagesJpeg = null, IReadOnlyList<string>? AttachedFilePaths = null, string UsageContext = "companion", bool AsJob = false, string? ChromeChoice = null, IReadOnlyList<string>? BuildsOnJobIds = null)
{
	public bool IsGeneratedTaskBrief => !RelaysUsersOwnRequest;

	public bool RelaysUsersOwnRequest
	{
		get
		{
			string launchSource = LaunchSource;
			if ((launchSource == "text" || launchSource == "voice") ? true : false)
			{
				string launchContext = LaunchContext;
				if (launchContext != null)
				{
					if (!launchContext.StartsWith("HeyClicky passed the user's message to you exactly as they", StringComparison.Ordinal))
					{
						if (AgentHandoffBlock.IsPresent(launchContext) && launchContext.Contains("Original user request (typed/STT text):", StringComparison.Ordinal))
						{
							return !launchContext.Contains("(not available at tool dispatch time)", StringComparison.Ordinal);
						}
						return false;
					}
					return true;
				}
			}
			return false;
		}
	}

	public const string UsersOwnWordsContextPrefix = "HeyClicky passed the user's message to you exactly as they";

	[CompilerGenerated]
	private AgentToolLaunch(AgentToolLaunch original)
	{
		Prompt = original.Prompt;
		ClickySlug = original.ClickySlug;
		NewClickyName = original.NewClickyName;
		NewClickyRole = original.NewClickyRole;
		NewClickyOneLiner = original.NewClickyOneLiner;
		LaunchContext = original.LaunchContext;
		LaunchSource = original.LaunchSource;
		ImagesJpeg = original.ImagesJpeg;
		AttachedFilePaths = original.AttachedFilePaths;
		UsageContext = original.UsageContext;
		AsJob = original.AsJob;
		ChromeChoice = original.ChromeChoice;
		BuildsOnJobIds = original.BuildsOnJobIds;
	}
}
