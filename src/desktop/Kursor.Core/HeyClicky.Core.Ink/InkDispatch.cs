using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Ink;

public static class InkDispatch
{
	public sealed class RepeatingTimer : IDisposable
	{
		private readonly Timer timer;

		private readonly SynchronizationContext? context;

		private readonly Action tick;

		private int queued;

		public RepeatingTimer(double intervalSeconds, Action tick)
		{
			context = SynchronizationContext.Current;
			this.tick = tick;
			TimeSpan timeSpan = TimeSpan.FromSeconds(intervalSeconds);
			timer = new Timer(delegate
			{
				Fire();
			}, null, timeSpan, timeSpan);
		}

		private void Fire()
		{
			if (Interlocked.Exchange(ref queued, 1) == 1)
			{
				return;
			}
			Post(context, delegate
			{
				try
				{
					tick();
				}
				finally
				{
					Interlocked.Exchange(ref queued, 0);
				}
			});
		}

		public void Dispose()
		{
			timer.Dispose();
		}
	}

	public sealed class SerialQueue
	{
		private readonly object gate = new object();

		private Task last = Task.CompletedTask;

		public void Async(Action work)
		{
			lock (gate)
			{
				last = last.ContinueWith(delegate
				{
					try
					{
						work();
					}
					catch (Exception ex)
					{
						Log.Warn("ink", "a background ink write failed: " + ex.Message);
					}
				}, TaskScheduler.Default);
			}
		}

		public void Sync()
		{
			Task task;
			lock (gate)
			{
				task = last;
			}
			task.Wait();
		}
	}

	public static void After(double seconds, Action action)
	{
		SynchronizationContext context = SynchronizationContext.Current;
		Task.Delay(TimeSpan.FromSeconds(Math.Max(0.0, seconds))).ContinueWith(delegate
		{
			Post(context, action);
		}, TaskScheduler.Default);
	}

	private static void Post(SynchronizationContext? context, Action action)
	{
		if (context == null)
		{
			Run();
			return;
		}
		context.Post(delegate
		{
			Run();
		}, null);
		void Run()
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Log.Error("ink", "a timed ink update failed", ex);
			}
		}
	}
}
