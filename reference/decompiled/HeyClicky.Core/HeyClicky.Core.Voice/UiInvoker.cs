using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Voice;

public sealed class UiInvoker(SynchronizationContext? context)
{
	public bool IsInline
	{
		get
		{
			if (context != null)
			{
				return SynchronizationContext.Current == context;
			}
			return true;
		}
	}

	public Task<T> InvokeAsync<T>(Func<T> work)
	{
		if (IsInline)
		{
			try
			{
				return Task.FromResult(work());
			}
			catch (Exception exception)
			{
				return Task.FromException<T>(exception);
			}
		}
		TaskCompletionSource<T> completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
		context.Post(delegate
		{
			try
			{
				completion.SetResult(work());
			}
			catch (Exception exception2)
			{
				completion.SetException(exception2);
			}
		}, null);
		return completion.Task;
	}

	public Task InvokeAsync(Action work)
	{
		return InvokeAsync(delegate
		{
			work();
			return true;
		});
	}

	public void Post(Action work)
	{
		if (IsInline)
		{
			try
			{
				work();
				return;
			}
			catch (Exception ex)
			{
				Log.Error("Ui", "UI call failed", ex);
				return;
			}
		}
		context.Post(delegate
		{
			try
			{
				work();
			}
			catch (Exception ex2)
			{
				Log.Error("Ui", "UI call failed", ex2);
			}
		}, null);
	}
}
