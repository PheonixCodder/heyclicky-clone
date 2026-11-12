using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core.Billing;

public sealed class BillingStateService : IDisposable
{
	private const string Area = "Billing";

	private readonly WorkerClient worker;

	private readonly SupabaseAuthService auth;

	private readonly string cacheDirectory;

	private readonly object gate = new object();

	private PlanSnapshot? plan;

	private bool isFetching;

	private BillingFetchError? lastFetchError;

	private int? localMessagesUsed;

	private bool gateSuspendedAfterFailOpen;

	private Task? activeRefresh;

	private int requestedRefreshGeneration;

	private string? pendingActivationSessionId;

	private readonly HashSet<string> handledActivationSessionIds = new HashSet<string>();

	private string? currentUserId;

	private ReferralPaywallDiscount? referralDiscount;

	private DateTimeOffset? lastPlanAnswerAt;

	private CancellationTokenSource? browserWatch;

	private PlanBillingFingerprint? browserWatchStart;

	private static readonly TimeSpan BrowserWatchDuration = TimeSpan.FromMinutes(15L);

	private static readonly TimeSpan BrowserWatchPollInterval = TimeSpan.FromSeconds(20L);

	private static readonly TimeSpan[] BrowserReturnFollowUps = new TimeSpan[4]
	{
		TimeSpan.FromSeconds(2L),
		TimeSpan.FromSeconds(4L),
		TimeSpan.FromSeconds(8L),
		TimeSpan.FromSeconds(15L)
	};

	public const string SignedOutMessage = "it seems like you got logged out. can you log in again for me and try again?";

	public PlanSnapshot? Plan
	{
		get
		{
			lock (gate)
			{
				return plan;
			}
		}
	}

	public bool IsFetching
	{
		get
		{
			lock (gate)
			{
				return isFetching;
			}
		}
	}

	public BillingFetchError? LastFetchError
	{
		get
		{
			lock (gate)
			{
				return lastFetchError;
			}
		}
	}

	public DateTimeOffset? LastPlanAnswerAt
	{
		get
		{
			lock (gate)
			{
				return lastPlanAnswerAt;
			}
		}
	}

	public string? AuthenticatedUserId
	{
		get
		{
			if (!auth.IsSignedIn)
			{
				return null;
			}
			return auth.UserId;
		}
	}

	public ReferralPaywallDiscount? ReferralDiscount
	{
		get
		{
			lock (gate)
			{
				return referralDiscount;
			}
		}
	}

	public bool IsUnlimited => (Plan?.Plan ?? "unlimited") == "unlimited";

	public string CurrentTier => Plan?.EffectiveTier ?? "max";

	public bool IsMaxTier => CurrentTier == "max";

	public string? PaidPlanName
	{
		get
		{
			if (IsUnlimited)
			{
				if (!IsMaxTier)
				{
					return "Kursor Pro";
				}
				return "Kursor Max";
			}
			return null;
		}
	}

	public string ReportPlanLabel
	{
		get
		{
			if ((object)Plan != null)
			{
				if (IsUnlimited)
				{
					if (!IsMaxTier)
					{
						return "pro";
					}
					return "max";
				}
				return "free";
			}
			return "unknown";
		}
	}

	public bool IsTeamManagedPlan
	{
		get
		{
			if (IsUnlimited)
			{
				return Plan?.TierSource == "team";
			}
			return false;
		}
	}

	public string? TeamPlanTeamName
	{
		get
		{
			if (!IsTeamManagedPlan)
			{
				return null;
			}
			return Plan?.TeamName;
		}
	}

	public string? CurrentBillingInterval => Plan?.Subscription?.BillingInterval;

	public bool RequiresPaymentResolution
	{
		get
		{
			PlanSubscription planSubscription = Plan?.Subscription;
			if ((object)planSubscription != null)
			{
				return RequiresPayment(planSubscription);
			}
			return false;
		}
	}

	public bool IsBillingEnforcementOn => false;

	public PaywallPayload? ManualUpgradePayload
	{
		get
		{
			PlanSnapshot planSnapshot = Plan;
			return PaywallPayloads.ManualUpgrade(CurrentTier, planSnapshot?.ProAgentsCap, planSnapshot?.MaxAgentsCap, planSnapshot?.ProYearlyAvailable == true, planSnapshot?.MaxYearlyAvailable == true);
		}
	}

	public string? AnnualBillingSavingsCtaTitle
	{
		get
		{
			PlanSnapshot planSnapshot = Plan;
			if (!IsUnlimited || CurrentBillingInterval != "month")
			{
				return null;
			}
			string currentTier = CurrentTier;
			if (!(currentTier == "pro"))
			{
				if (currentTier == "max" && (object)planSnapshot != null && planSnapshot.MaxYearlyAvailable == true)
				{
					return "Save $240 on annual billing of HeyClicky Max plan";
				}
			}
			else if ((object)planSnapshot != null && planSnapshot.ProYearlyAvailable == true)
			{
				return "Save $48 on annual billing of HeyClicky Pro plan";
			}
			return null;
		}
	}

	public bool ShouldOfferManualUpgrade
	{
		get
		{
			bool isBillingEnforcementOn = IsBillingEnforcementOn;
			if ((object)Plan != null && CurrentTier == "free" && !IsTeamManagedPlan && isBillingEnforcementOn)
			{
				return (object)ManualUpgradePayload != null;
			}
			return false;
		}
	}

	public DateTimeOffset? UsageResetsDate
	{
		get
		{
			PlanSnapshot planSnapshot = Plan;
			if ((object)planSnapshot == null)
			{
				return null;
			}
			DateTimeOffset? dateTimeOffset = ParseDate(planSnapshot.Usage.WindowResetsAt);
			if (dateTimeOffset.HasValue)
			{
				return dateTimeOffset.GetValueOrDefault();
			}
			if (!(planSnapshot.Plan == "unlimited"))
			{
				return null;
			}
			return ParseDate(planSnapshot.Subscription?.CurrentPeriodEnd);
		}
	}

	public string? UsageResetsPhrase
	{
		get
		{
			DateTimeOffset? usageResetsDate = UsageResetsDate;
			if (usageResetsDate.HasValue)
			{
				DateTimeOffset valueOrDefault = usageResetsDate.GetValueOrDefault();
				return RelativePhrase(valueOrDefault, null);
			}
			return null;
		}
	}

	public string? UsageResetsLabel
	{
		get
		{
			string usageResetsPhrase = UsageResetsPhrase;
			if (usageResetsPhrase == null)
			{
				return null;
			}
			return "Your current usage resets " + usageResetsPhrase + ".";
		}
	}

	public string? SubscriptionCancellationLabel
	{
		get
		{
			PlanSnapshot planSnapshot = Plan;
			if (!(planSnapshot?.Plan != "unlimited"))
			{
				PlanSubscription subscription = planSnapshot.Subscription;
				if ((object)subscription != null)
				{
					if (!subscription.CancelAtPeriodEnd && subscription.CancelAt == null)
					{
						return null;
					}
					DateTimeOffset? dateTimeOffset = ((!subscription.CancelAtPeriodEnd) ? ParseDate(subscription.CancelAt) : (ParseDate(subscription.CurrentPeriodEnd) ?? ParseDate(subscription.CancelAt)));
					if (dateTimeOffset.HasValue)
					{
						DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
						return "Cancels " + RelativePhrase(valueOrDefault, null) + ".";
					}
					return null;
				}
			}
			return null;
		}
	}

	public PlanUsageBucket? DisplayedMessagesUsage
	{
		get
		{
			lock (gate)
			{
				if ((object)plan == null)
				{
					return null;
				}
				object result;
				if (plan.Plan == "free")
				{
					int? num = localMessagesUsed;
					if (num.HasValue)
					{
						int valueOrDefault = num.GetValueOrDefault();
						result = plan.Usage.Messages with
						{
							Used = valueOrDefault
						};
						goto IL_007d;
					}
				}
				result = plan.Usage.Messages;
				goto IL_007d;
				IL_007d:
				return (PlanUsageBucket?)result;
			}
		}
	}

	public event EventHandler? Changed;

	public event EventHandler<string>? PaidPlanStarted;

	public BillingStateService(WorkerClient worker, SupabaseAuthService auth, string? cacheDirectory = null)
	{
		this.worker = worker;
		this.auth = auth;
		this.cacheDirectory = cacheDirectory ?? Path.Combine(AppBundleConfiguration.AppDataDirectory, "billing");
		currentUserId = auth.UserId;
		plan = RestoreLastKnown(currentUserId);
		auth.Changed += OnAuthChanged;
	}

	public void Dispose()
	{
		auth.Changed -= OnAuthChanged;
		lock (gate)
		{
			EndBrowserWatchLocked();
		}
	}

	public static bool RequiresPayment(PlanSubscription subscription)
	{
		switch (subscription.Status.Trim().ToLowerInvariant())
		{
		case "past_due":
		case "unpaid":
		case "payment_due":
			return true;
		default:
			return false;
		}
	}

	public static string RelativePhrase(DateTimeOffset target, DateTimeOffset? now = null)
	{
		int num = (int)(target - (now ?? DateTimeOffset.Now)).TotalSeconds;
		if (num <= 0)
		{
			return "soon";
		}
		int num2 = num / 3600;
		int num3 = num2 / 24;
		if (num3 <= 0)
		{
			if (num2 <= 0)
			{
				return $"in {Math.Max(1, num / 60)}m";
			}
			return $"in {num2}h";
		}
		return $"in {num3}d {num2 % 24}h";
	}

	private static DateTimeOffset? ParseDate(string? raw)
	{
		if (raw == null || !DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result))
		{
			return null;
		}
		return result;
	}

	public void RegisterLocalMessageTurnConsumed()
	{
		lock (gate)
		{
			int? num = localMessagesUsed;
			if (!num.HasValue)
			{
				return;
			}
			int valueOrDefault = num.GetValueOrDefault();
			localMessagesUsed = valueOrDefault + 1;
		}
		RaiseChanged();
	}

	public void RollbackLocalMessageTurnConsumed()
	{
		lock (gate)
		{
			int? num = localMessagesUsed;
			if (!num.HasValue)
			{
				return;
			}
			int valueOrDefault = num.GetValueOrDefault();
			int num2 = Math.Max(0, valueOrDefault - 1);
			PlanSnapshot planSnapshot = plan;
			localMessagesUsed = (((object)planSnapshot != null && planSnapshot.Plan == "free") ? Math.Max(num2, planSnapshot.Usage.Messages.Used) : num2);
		}
		RaiseChanged();
	}

	public bool ShouldBlockFreeUserMessageTurnLocally()
	{
		PlanSnapshot planSnapshot;
		int? num;
		bool flag;
		lock (gate)
		{
			planSnapshot = plan;
			num = localMessagesUsed;
			flag = gateSuspendedAfterFailOpen;
		}
		if ((planSnapshot?.IsFailOpenFallback ?? true) || flag)
		{
			return false;
		}
		if (!(planSnapshot.Plan != "free") && !(planSnapshot.BillingEnforcement != "on") && num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			DateTimeOffset? dateTimeOffset = ParseDate(planSnapshot.Usage.WindowResetsAt);
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault2 = dateTimeOffset.GetValueOrDefault();
				if (valueOrDefault2 <= DateTimeOffset.Now)
				{
					RefreshAsync();
					return false;
				}
			}
			return valueOrDefault >= planSnapshot.Usage.Messages.Cap;
		}
		return false;
	}

	public PaywallPayload? MakeLocalMessagesLimitPaywallPayload()
	{
		PlanSnapshot planSnapshot;
		int? num;
		lock (gate)
		{
			planSnapshot = plan;
			num = localMessagesUsed;
		}
		if ((object)planSnapshot == null || !(planSnapshot.Plan == "free"))
		{
			return null;
		}
		return PaywallPayloads.LocalMessagesLimit(num ?? planSnapshot.Usage.Messages.Used, planSnapshot.Usage.Messages.Cap, planSnapshot.Usage.WindowResetsAt, planSnapshot.ProAgentsCap, planSnapshot.MaxAgentsCap, planSnapshot.ProYearlyAvailable == true, planSnapshot.MaxYearlyAvailable == true);
	}

	public Task RefreshAsync()
	{
		lock (gate)
		{
			requestedRefreshGeneration++;
			Task task = activeRefresh;
			if (task != null)
			{
				return task;
			}
			if (!auth.IsSignedIn)
			{
				return Task.CompletedTask;
			}
			isFetching = true;
			activeRefresh = Task.Run((Func<Task?>)RefreshLoopAsync);
			return activeRefresh;
		}
	}

	private async Task RefreshLoopAsync()
	{
		RaiseChanged();
		try
		{
			while (true)
			{
				int served;
				lock (gate)
				{
					served = requestedRefreshGeneration;
				}
				await PerformSingleRefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
				lock (gate)
				{
					if (requestedRefreshGeneration == served)
					{
						activeRefresh = null;
						isFetching = false;
						break;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("Billing", "plan refresh loop: " + ex.Message);
			lock (gate)
			{
				activeRefresh = null;
				isFetching = false;
			}
		}
		RaiseChanged();
	}

	private async Task PerformSingleRefreshAsync()
	{
		string requestUserId = auth.UserId;
		PlanSnapshot planSnapshot;
		try
		{
			planSnapshot = await worker.GetPlanAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (WorkerHttpException ex)
		{
			bool flag = IsAuthFailure(ex.Status, ex.Body);
			lock (gate)
			{
				lastFetchError = new BillingFetchError((!flag) ? BillingFetchErrorKind.Server : BillingFetchErrorKind.SignedOut, ex.Status, ex.Message);
			}
			Log.Warn("Billing", "plan refresh failed: " + ex.Message);
			return;
		}
		catch (Exception ex2) when (((ex2 is HttpRequestException || ex2 is TaskCanceledException || ex2 is IOException || ex2 is JsonException) ? 1 : 0) != 0)
		{
			lock (gate)
			{
				lastFetchError = new BillingFetchError((ex2 is JsonException) ? BillingFetchErrorKind.Decode : BillingFetchErrorKind.Transport, 0, ex2.Message);
			}
			Log.Warn("Billing", "plan refresh failed: " + ex2.Message);
			return;
		}
		if ((object)planSnapshot == null)
		{
			if (auth.IsSignedIn)
			{
				lock (gate)
				{
					lastFetchError = new BillingFetchError(BillingFetchErrorKind.SignedOut, 401, "no session");
					return;
				}
			}
			return;
		}
		if (requestUserId == null || requestUserId != auth.UserId)
		{
			Log.Warn("Billing", "discarding a plan fetched for a different account than the one signed in now");
			return;
		}
		string text = null;
		(string, string)? tuple = null;
		lock (gate)
		{
			lastPlanAnswerAt = DateTimeOffset.UtcNow;
			if (planSnapshot.IsFailOpenFallback == true)
			{
				lastFetchError = null;
				gateSuspendedAfterFailOpen = true;
				PlanSnapshot planSnapshot2 = plan;
				if (((object)planSnapshot2 != null && !(planSnapshot2.IsFailOpenFallback ?? false)) ? true : false)
				{
					Log.Warn("Billing", "plan refresh returned the fail-open placeholder; keeping the known " + plan.Plan + " plan");
				}
				else
				{
					plan = null;
					Log.Warn("Billing", "plan refresh returned the fail-open placeholder with no known plan; leaving it unknown");
				}
			}
			else
			{
				string text2 = plan?.Plan;
				string text3 = plan?.EffectiveTier;
				bool flag2 = pendingActivationSessionId != null;
				plan = planSnapshot;
				lastFetchError = null;
				gateSuspendedAfterFailOpen = false;
				localMessagesUsed = ((planSnapshot.Plan == "free") ? new int?(planSnapshot.Usage.Messages.Used) : ((int?)null));
				if (planSnapshot.Plan == "unlimited" && (text2 == "free" || (text2 == null && flag2)))
				{
					string text4 = pendingActivationSessionId;
					if (text4 != null)
					{
						handledActivationSessionIds.Add(text4);
					}
					pendingActivationSessionId = null;
					string item = text4;
					string effectiveTier = planSnapshot.EffectiveTier;
					string item2 = ((effectiveTier == "max") ? "HeyClicky Max" : ((!(effectiveTier == "pro")) ? "Free" : "HeyClicky Pro"));
					tuple = (item, item2);
				}
				string effectiveTier2 = planSnapshot.EffectiveTier;
				bool flag3 = text2 == "free" || (text2 == null && flag2);
				bool flag4 = text3 == "pro" && effectiveTier2 == "max" && flag2;
				if (planSnapshot.Plan == "unlimited" && effectiveTier2 != "free" && planSnapshot.TierSource != "team" && (flag3 || flag4))
				{
					text = effectiveTier2;
				}
			}
		}
		if (planSnapshot.IsFailOpenFallback != true)
		{
			PersistLastKnown(planSnapshot, requestUserId);
			Log.Info("Billing", $"plan refreshed: {planSnapshot.Plan}/{planSnapshot.EffectiveTier}, messages={planSnapshot.Usage.Messages.Used}/{planSnapshot.Usage.Messages.Cap}, agents={planSnapshot.Usage.Agents.Used}/{planSnapshot.Usage.Agents.Cap}, enforcement={planSnapshot.BillingEnforcement}");
		}
		if (tuple.HasValue)
		{
			(string, string) valueOrDefault = tuple.GetValueOrDefault();
			ClickyAnalytics.TrackSubscriptionActivated(valueOrDefault.Item1, valueOrDefault.Item2);
		}
		if (text == null)
		{
			return;
		}
		Log.Info("Billing", "paid plan started: " + text);
		try
		{
			this.PaidPlanStarted?.Invoke(this, text);
		}
		catch (Exception ex3)
		{
			Log.Error("Billing", "PaidPlanStarted handler threw", ex3);
		}
	}

	public async Task HandleCheckoutCompleteAsync(CheckoutCompleteLink link)
	{
		Log.Info("Billing", $"checkout-complete URL received (canceled={link.IsCanceled}, portal={link.IsPortal})");
		PlanBillingFingerprint beforeReturn = PlanBillingFingerprint.From(Plan);
		bool flag = true;
		if (!link.IsCanceled && !link.IsPortal)
		{
			string checkoutSessionId = link.CheckoutSessionId;
			if (checkoutSessionId != null && checkoutSessionId.Length > 0)
			{
				lock (gate)
				{
					if (handledActivationSessionIds.Contains(checkoutSessionId))
					{
						Log.Info("Billing", "ignoring a duplicate checkout-complete callback for a handled session");
						flag = false;
					}
					else
					{
						pendingActivationSessionId = checkoutSessionId;
					}
				}
			}
		}
		if (link.IsCanceled)
		{
			lock (gate)
			{
				EndBrowserWatchLocked();
			}
		}
		if (flag)
		{
			await RefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		if (!link.IsCanceled)
		{
			PlanBillingFingerprint planBillingFingerprint;
			lock (gate)
			{
				planBillingFingerprint = browserWatchStart;
			}
			WatchForBrowserPlanChange(planBillingFingerprint ?? beforeReturn, BrowserReturnFollowUps);
		}
	}

	public Task RefreshIfStaleAsync(TimeSpan? maximumAge = null)
	{
		lock (gate)
		{
			if ((object)plan != null)
			{
				DateTimeOffset? dateTimeOffset = lastPlanAnswerAt;
				if (dateTimeOffset.HasValue)
				{
					DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
					if (DateTimeOffset.UtcNow - valueOrDefault < (maximumAge ?? TimeSpan.FromMinutes(1L)))
					{
						return Task.CompletedTask;
					}
				}
			}
		}
		return RefreshAsync();
	}

	public void BeginWatchingForBrowserPlanChange()
	{
		WatchForBrowserPlanChange(PlanBillingFingerprint.From(Plan), Array.Empty<TimeSpan>());
	}

	private void WatchForBrowserPlanChange(PlanBillingFingerprint? start, TimeSpan[] followUps)
	{
		CancellationTokenSource watch;
		lock (gate)
		{
			EndBrowserWatchLocked();
			if (HasPlanChanged(start, PlanBillingFingerprint.From(plan)))
			{
				return;
			}
			browserWatchStart = start;
			watch = (browserWatch = new CancellationTokenSource());
		}
		CancellationToken token = watch.Token;
		DateTimeOffset deadline = DateTimeOffset.UtcNow + BrowserWatchDuration;
		Task.Run(async delegate
		{
			Queue<TimeSpan> pending = new Queue<TimeSpan>(followUps);
			try
			{
				while (DateTimeOffset.UtcNow < deadline)
				{
					await Task.Delay((pending.Count > 0) ? pending.Dequeue() : BrowserWatchPollInterval, token).ConfigureAwait(continueOnCapturedContext: false);
					await RefreshAsync().ConfigureAwait(continueOnCapturedContext: false);
					if (token.IsCancellationRequested)
					{
						return;
					}
					if (HasPlanChanged(start, PlanBillingFingerprint.From(Plan)))
					{
						Log.Info("Billing", "the plan change from the browser is in; ending the watch");
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				return;
			}
			lock (gate)
			{
				if (browserWatch == watch)
				{
					EndBrowserWatchLocked();
				}
			}
		});
	}

	internal static bool HasPlanChanged(PlanBillingFingerprint? start, PlanBillingFingerprint? now)
	{
		if ((object)now != null)
		{
			if ((object)start != null)
			{
				return now != start;
			}
			return now.Plan == "unlimited";
		}
		return false;
	}

	private void EndBrowserWatchLocked()
	{
		browserWatch?.Cancel();
		browserWatch = null;
		browserWatchStart = null;
	}

	public async Task RefreshReferralAsync()
	{
		if (!auth.IsSignedIn)
		{
			return;
		}
		try
		{
			ReferralPaywallDiscount referralPaywallDiscount = ReferralPaywallDiscount.FromSnapshot(await worker.GetReferralAsync().ConfigureAwait(continueOnCapturedContext: false));
			bool flag;
			lock (gate)
			{
				flag = referralPaywallDiscount != referralDiscount;
				referralDiscount = referralPaywallDiscount;
			}
			if (flag)
			{
				RaiseChanged();
			}
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			Log.Warn("Billing", "/me/referral failed: " + ex.Message);
		}
	}

	private void OnAuthChanged(object? sender, EventArgs e)
	{
		string text = (auth.IsSignedIn ? auth.UserId : null);
		lock (gate)
		{
			if (text == currentUserId)
			{
				return;
			}
			currentUserId = text;
			plan = RestoreLastKnown(text);
			localMessagesUsed = null;
			gateSuspendedAfterFailOpen = false;
			lastFetchError = null;
			referralDiscount = null;
			lastPlanAnswerAt = null;
			EndBrowserWatchLocked();
		}
		RaiseChanged();
		if (text != null)
		{
			RefreshAsync();
		}
	}

	private string? CachePath(string? userId)
	{
		if (!string.IsNullOrEmpty(userId) && userId.IndexOfAny(Path.GetInvalidFileNameChars()) < 0)
		{
			return Path.Combine(cacheDirectory, "last-plan-" + userId + ".json");
		}
		return null;
	}

	private PlanSnapshot? RestoreLastKnown(string? userId)
	{
		string text = CachePath(userId);
		if (text == null || !File.Exists(text))
		{
			return null;
		}
		try
		{
			PlanSnapshot planSnapshot = JsonSerializer.Deserialize(File.ReadAllBytes(text), CoreJsonContext.Default.PlanSnapshot);
			if ((object)planSnapshot != null)
			{
				Log.Info("Billing", "restored last-known plan: " + planSnapshot.Plan);
			}
			return planSnapshot;
		}
		catch (Exception ex) when (((ex is JsonException || ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			Log.Warn("Billing", "could not read the last-known plan; ignoring it");
			return null;
		}
	}

	private void PersistLastKnown(PlanSnapshot snapshot, string? userId)
	{
		string text = CachePath(userId);
		if (text == null)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(cacheDirectory);
			string text2 = text + ".tmp";
			File.WriteAllBytes(text2, JsonSerializer.SerializeToUtf8Bytes(snapshot, CoreJsonContext.Default.PlanSnapshot));
			File.Move(text2, text, overwrite: true);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			Log.Warn("Billing", "could not save the last-known plan: " + ex.Message);
		}
	}

	private void RaiseChanged()
	{
		try
		{
			this.Changed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			Log.Error("Billing", "Changed handler threw", ex);
		}
	}

	public static bool IsAuthFailure(int status, string body)
	{
		if ((status != 401 && status != 403) || 1 == 0)
		{
			return false;
		}
		string text = body.ToLowerInvariant();
		if (!text.Contains("authentication required") && !text.Contains("bad_jwt") && !text.Contains("invalid jwt") && !text.Contains("unable to parse or verify signature"))
		{
			return text.Contains("unrecognized jwt kid");
		}
		return true;
	}

	internal void SetPlanForTesting(PlanSnapshot? snapshot)
	{
		lock (gate)
		{
			plan = snapshot;
			localMessagesUsed = (((object)snapshot != null && snapshot.Plan == "free") ? new int?(snapshot.Usage.Messages.Used) : ((int?)null));
		}
		RaiseChanged();
	}
}
