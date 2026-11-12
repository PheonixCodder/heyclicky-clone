using System;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.Core.Voice;

public sealed class PageReads
{
	private sealed record PendingRead(long Generation, Task<PageReference?> Reference, Task<PageContext?> Context, DateTimeOffset StartedAt, CancellationTokenSource Cancellation);

	public static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(120L);

	public const double WaitSecondsWhenStartedEarly = 0.8;

	public const double WaitSecondsAtTurn = 1.6;

	private static readonly TimeSpan ReferenceBudget = TimeSpan.FromMilliseconds(600L, 0L);

	private readonly IPageReader reader;

	private readonly Func<DateTimeOffset> clock;

	private readonly object gate = new object();

	private PendingRead? pending;

	private long generation;

	public bool StartedBeforeTurn { get; private set; }

	public PageReads(IPageReader reader, Func<DateTimeOffset>? clock = null)
	{
		this.reader = reader;
		this.clock = clock ?? ((Func<DateTimeOffset>)(() => DateTimeOffset.Now));
	}

	public void BeginRead(bool beforeTurn = true)
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		Task<PageReference> task = SafeReferenceAsync(cancellationTokenSource.Token);
		Task<PageContext> context = ExtractWhenReferencedAsync(task, cancellationTokenSource.Token);
		PendingRead pendingRead;
		lock (gate)
		{
			pendingRead = pending;
			pending = new PendingRead(++generation, task, context, clock(), cancellationTokenSource);
			StartedBeforeTurn = beforeTurn;
		}
		pendingRead?.Cancellation.Cancel();
	}

	public void BeginReadForTextInput(Func<bool> isStillOpen)
	{
		long at;
		lock (gate)
		{
			at = generation;
		}
		Task.Delay(300).ContinueWith(delegate
		{
			lock (gate)
			{
				if (generation != at)
				{
					return;
				}
			}
			if (isStillOpen())
			{
				BeginRead();
			}
		}, TaskScheduler.Default);
	}

	public void TextInputClosed()
	{
		long at;
		lock (gate)
		{
			at = generation;
		}
		Task.Delay(5000).ContinueWith(delegate
		{
			lock (gate)
			{
				if (generation != at || (object)pending == null)
				{
					return;
				}
			}
			Discard();
		}, TaskScheduler.Default);
	}

	public void Discard()
	{
		PendingRead pendingRead;
		lock (gate)
		{
			pendingRead = pending;
			pending = null;
			generation++;
			StartedBeforeTurn = false;
		}
		pendingRead?.Cancellation.Cancel();
	}

	public async Task<PageContext?> TakeForTurnAsync(double? waitSeconds = null, CancellationToken ct = default(CancellationToken))
	{
		double wait;
		lock (gate)
		{
			wait = waitSeconds ?? (StartedBeforeTurn ? 0.8 : 1.6);
		}
		Task<PageReference?> currentTask = SafeReferenceAsync(ct);
		bool flag = await Task.WhenAny(currentTask, Task.Delay(ReferenceBudget, CancellationToken.None)).ConfigureAwait(continueOnCapturedContext: false) == currentTask;
		PageReference current = (flag ? currentTask.Result : null);
		PendingRead read;
		lock (gate)
		{
			read = pending;
		}
		if ((object)read != null)
		{
			PageReference pageReference = (read.Reference.IsCompletedSuccessfully ? read.Reference.Result : null);
			if (flag && (object)current == null)
			{
				Discard();
				return null;
			}
			bool num = clock() - read.StartedAt > FreshFor;
			bool flag2 = (object)current != null && read.Reference.IsCompleted && pageReference?.Reference != current.Reference;
			if (num || flag2)
			{
				BeginRead(beforeTurn: false);
				lock (gate)
				{
					read = pending;
				}
			}
		}
		else
		{
			if (flag && (object)current == null)
			{
				return null;
			}
			BeginRead(beforeTurn: false);
			lock (gate)
			{
				read = pending;
			}
		}
		if ((object)read == null)
		{
			return null;
		}
		lock (gate)
		{
			if (pending == read)
			{
				pending = null;
				StartedBeforeTurn = false;
			}
		}
		PageContext pageContext = await WithBudget(read.Context, TimeSpan.FromSeconds(wait)).ConfigureAwait(continueOnCapturedContext: false);
		if ((object)pageContext != null)
		{
			return pageContext;
		}
		PageReference pageReference2 = (read.Reference.IsCompletedSuccessfully ? read.Reference.Result : current);
		return ((object)pageReference2 == null) ? null : PageContext.ReferenceOnly(pageReference2);
	}

	private async Task<PageReference?> SafeReferenceAsync(CancellationToken ct)
	{
		try
		{
			return await reader.CaptureReferenceAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("PageRead", "reference: " + ex.Message);
			return null;
		}
	}

	private async Task<PageContext?> ExtractWhenReferencedAsync(Task<PageReference?> referenceTask, CancellationToken ct)
	{
		PageReference reference = await referenceTask.ConfigureAwait(continueOnCapturedContext: false);
		if ((object)reference == null || ct.IsCancellationRequested)
		{
			return null;
		}
		try
		{
			long started = Environment.TickCount64;
			PageContext pageContext = await reader.ExtractAsync(reference, ct).ConfigureAwait(continueOnCapturedContext: false);
			Log.Info("PageRead", $"{pageContext.Kind} read in {Environment.TickCount64 - started} ms: {pageContext.Text?.Length ?? 0} chars{(pageContext.AccessDenied ? ", access denied" : "")}");
			return pageContext;
		}
		catch (Exception ex)
		{
			Log.Warn("PageRead", "extract: " + ex.Message);
			return PageContext.ReferenceOnly(reference);
		}
	}

	private static async Task<T?> WithBudget<T>(Task<T?> task, TimeSpan budget) where T : class
	{
		if (await Task.WhenAny(task, Task.Delay(budget)).ConfigureAwait(continueOnCapturedContext: false) != task)
		{
			return null;
		}
		return task.IsCompletedSuccessfully ? task.Result : null;
	}
}
