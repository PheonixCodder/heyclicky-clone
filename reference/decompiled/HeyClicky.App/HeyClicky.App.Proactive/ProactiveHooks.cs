using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HeyClicky.Core.Proactive;

namespace HeyClicky.App.Proactive;

public static class ProactiveHooks
{
	public static volatile Func<string?>? LiveStateNote;

	public static volatile Func<ProactiveJobLaunch, bool>? LaunchApprovedCardAsJob;

	public static ISet<Guid>? LiveCardIds()
	{
		try
		{
			return ProactiveCoordinator.Current?.VisibleCards.Select((ProactiveSuggestionCard c) => c.Id).ToHashSet();
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public static Task<T?> RunOnUiAsync<T>(Func<ProactiveCoordinator, T> work) where T : class
	{
		ProactiveCoordinator c = ProactiveCoordinator.Current;
		if (c == null)
		{
			return Task.FromResult<T>(null);
		}
		TaskCompletionSource<T?> done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!c.Dispatcher.TryEnqueue(delegate
		{
			try
			{
				done.TrySetResult(work(c));
			}
			catch (Exception exception)
			{
				done.TrySetException(exception);
			}
		}))
		{
			done.TrySetResult(null);
		}
		return done.Task;
	}
}
