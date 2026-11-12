using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Worker;
using HeyClicky.Platform.Win32;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Billing;

public sealed class BillingUi : IPaywallCardHost
{
	private sealed class BillingSignedOutException : Exception
	{
		public BillingSignedOutException()
			: base("not signed in")
		{
		}
	}

	private const string Area = "Billing";

	private readonly WorkerClient worker;

	private readonly DispatcherQueue ui;

	private readonly Action<string> openUrl;

	private readonly Action tuckAway;

	private readonly bool excludeFromCapture;

	private PaywallWindow? window;

	private PaywallCard? card;

	private PaywallSource source;

	private PaywallPayload? payload;

	private int presentation;

	private ReferralPaywallDiscount? previewReferral;

	private DispatcherQueueTimer? paidReturnTimer;

	private DateTimeOffset? lastPaidPlanCelebratedAt;

	private (PaywallPayload Payload, PaywallSource Source, DateTimeOffset At)? heldBack;

	private DispatcherQueueTimer? quietWatch;

	private static readonly TimeSpan HeldBackLifetime = TimeSpan.FromMinutes(15L);

	public BillingStateService State { get; }

	public BillingPlanCard PlanCard => BillingPlanCard.From(State);

	public bool IsPaywallVisible => window?.IsShowing ?? false;

	public static Func<string?> QuietReasonNow { get; set; } = () => (!QuietContext.IgnoredForTests) ? ((QuietContext.QueryNotificationState() != QuietContext.NotificationState.PresentationMode) ? ((!QuietContext.IsAnotherAppUsingMicrophone()) ? null : "on a call") : "presenting") : null;

	public PaywallCard? Card
	{
		get
		{
			PaywallWindow? paywallWindow = window;
			if ((object)paywallWindow == null || !paywallWindow.IsShowing)
			{
				return null;
			}
			return card;
		}
	}

	public PaywallWindow? Window => window;

	public Func<bool> CanSayLine { get; set; } = () => true;

	public Func<string> HoldShortcutDescription { get; set; } = () => "ctrl + alt";

	private string HoldToTalkHint => "hold " + HoldShortcutDescription() + " anytime to talk to me.";

	public event Action? Changed;

	public event Action<PaywallSource, PaywallPayload>? PaywallShown;

	public event Action<PaywallSource, PaywallCloseKind>? PaywallClosed;

	public event Action<string>? CursorLine;

	public BillingUi(BillingStateService state, WorkerClient worker, DispatcherQueue ui, Action<string> openUrl, Action tuckAway, bool excludeFromCapture)
	{
		BillingUi billingUi = this;
		State = state;
		this.worker = worker;
		this.ui = ui;
		this.openUrl = openUrl;
		this.tuckAway = tuckAway;
		this.excludeFromCapture = excludeFromCapture;
		state.Changed += delegate
		{
			ui.TryEnqueue(billingUi.OnStateChanged);
		};
		state.PaidPlanStarted += delegate(object? _, string tier)
		{
			ui.TryEnqueue(delegate
			{
				billingUi.OnPaidPlanStarted(tier);
			});
		};
		BillingPaywallDetector.PaywallDetected += delegate(PaywallPayload p, JsonElement raw)
		{
			ui.TryEnqueue(delegate
			{
				billingUi.ShowPaywall(p, PaywallSource.LimitHit);
				billingUi.State.RefreshAsync();
			});
		};
	}

	public Task RefreshAsync()
	{
		return State.RefreshAsync();
	}

	public async Task<string?> RunPlanActionAsync(BillingPlanActionKind kind)
	{
		switch (kind)
		{
		case BillingPlanActionKind.TryAgain:
			await State.RefreshAsync();
			return null;
		case BillingPlanActionKind.Upgrade:
			return ShowPaywall(PaywallSource.SettingsUpgradeButton) ? null : BillingPlanCard.ActionErrorText(signedOut: false);
		case BillingPlanActionKind.FixPayment:
		case BillingPlanActionKind.ManageSubscription:
			return await ManageBillingAsync((kind == BillingPlanActionKind.FixPayment) ? "settings_fix_payment" : "settings_manage_subscription");
		case BillingPlanActionKind.SwitchToYearly:
			return await SwitchToYearlyAsync();
		case BillingPlanActionKind.ViewTeam:
			openUrl("https://team.heyclicky.com");
			tuckAway();
			return null;
		default:
			return null;
		}
	}

	public async Task<string?> ManageBillingAsync(string analyticsSource = "settings_manage_subscription")
	{
		try
		{
			await OpenPortalOrThrowAsync(analyticsSource);
			return null;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			Log.Warn("Billing", "portal launch failed: " + ex.Message);
			return BillingPlanCard.ActionErrorText(IsSignedOut(ex));
		}
	}

	public async Task<string?> SwitchToYearlyAsync()
	{
		string currentTier = State.CurrentTier;
		if ((!(currentTier == "pro") && !(currentTier == "max")) || 1 == 0)
		{
			return null;
		}
		ClickyAnalytics.TrackAnnualBillingSwitchClicked(currentTier);
		try
		{
			await OpenCheckoutOrThrowAsync(currentTier, "year", "settings_plan_section", null, null);
			return null;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			Log.Warn("Billing", "yearly checkout failed: " + ex.Message);
			return BillingPlanCard.ActionErrorText(IsSignedOut(ex));
		}
	}

	public bool ShowPaywall(PaywallSource source)
	{
		PaywallPayload manualUpgradePayload = State.ManualUpgradePayload;
		if ((object)manualUpgradePayload == null)
		{
			Log.Info("Billing", "no upgrade to offer (" + State.CurrentTier + "); paywall not shown");
			return false;
		}
		return ShowPaywall(manualUpgradePayload, source);
	}

	public async Task<bool> ShowOnboardingPaywallAsync()
	{
		string userId = State.AuthenticatedUserId;
		if (userId == null)
		{
			return NotShown("signed out");
		}
		if (IsPaywallVisible)
		{
			return NotShown("a " + source.WireName() + " card is up");
		}
		int generation = presentation;
		await State.RefreshAsync();
		if (State.AuthenticatedUserId != userId)
		{
			return NotShown("the account changed");
		}
		if ((object)State.Plan == null)
		{
			return NotShown("the plan is unknown");
		}
		if (State.CurrentTier != "free" || State.IsTeamManagedPlan)
		{
			return NotShown("a " + (State.IsTeamManagedPlan ? "team" : State.CurrentTier) + " plan");
		}
		if (generation != presentation)
		{
			return NotShown("another card came up");
		}
		PaywallPayload manualUpgradePayload = State.ManualUpgradePayload;
		if ((object)manualUpgradePayload == null)
		{
			return NotShown("no upgrade to offer");
		}
		return ShowPaywall(manualUpgradePayload, PaywallSource.OnboardingCompleted);
		static bool NotShown(string why)
		{
			Log.Info("Billing", "onboarding paywall not shown: " + why);
			return false;
		}
	}

	public bool ShowPaywall(PaywallPayload payload, PaywallSource source)
	{
		if (WaitsOutQuiet(source))
		{
			string text = QuietReasonNow();
			if (text != null)
			{
				heldBack = (payload, source, DateTimeOffset.Now);
				StartQuietWatch();
				Log.Info("Billing", $"paywall held back: {text} (source={source.WireName()})");
				return false;
			}
		}
		heldBack = null;
		try
		{
			tuckAway();
		}
		catch (Exception ex)
		{
			Log.Warn("Billing", "tuck away: " + ex.Message);
		}
		presentation++;
		previewReferral = null;
		this.source = source;
		this.payload = payload;
		PaywallPalette palette = PaywallPalette.Current();
		card = new PaywallCard(payload, source, palette, State.CurrentTier, State.ReferralDiscount, this);
		if ((object)window == null)
		{
			window = new PaywallWindow(excludeFromCapture);
		}
		double width = (source.OffersFreeOption() ? 800 : ((payload.UpgradeOptions.Count >= 2) ? 560 : 400));
		window.Present(card, width, palette);
		Log.Info("Billing", $"paywall shown: source={source.WireName()} surface={payload.Surface} reason={payload.Reason ?? "-"} plans={payload.UpgradeOptions.Count}");
		ClickyAnalytics.TrackPaywallShown(payload.Surface, payload.Reason, payload.CurrentUsage, payload.Limit, payload.PriceDisplay, source.WireName(), source.OffersFreeOption(), payload.UpgradeOptions.Select((PaywallPlanOption o) => o.Key), payload.SupportsYearly, payload.DefaultInterval);
		if (WaitsOutQuiet(source))
		{
			StartQuietWatch();
		}
		if (State.AuthenticatedUserId != null)
		{
			State.RefreshReferralAsync();
		}
		this.PaywallShown?.Invoke(source, payload);
		return true;
	}

	public void DismissPaywall()
	{
		Close(PaywallCloseKind.Dismissed);
	}

	private static bool WaitsOutQuiet(PaywallSource source)
	{
		return source == PaywallSource.LimitHit;
	}

	private void StartQuietWatch()
	{
		if ((object)quietWatch == null)
		{
			quietWatch = ui.CreateTimer();
			quietWatch.Interval = TimeSpan.FromSeconds(2L);
			quietWatch.Tick += delegate
			{
				CheckQuiet();
			};
		}
		if (!quietWatch.IsRunning)
		{
			quietWatch.Start();
		}
	}

	private void CheckQuiet()
	{
		string text = QuietReasonNow();
		if (IsPaywallVisible && WaitsOutQuiet(source))
		{
			if (text == null)
			{
				return;
			}
			Log.Info("Billing", $"paywall dismissed: {text} (source={source.WireName()})");
			Close(PaywallCloseKind.Dismissed);
		}
		(PaywallPayload, PaywallSource, DateTimeOffset)? tuple = heldBack;
		if (tuple.HasValue)
		{
			(PaywallPayload, PaywallSource, DateTimeOffset) valueOrDefault = tuple.GetValueOrDefault();
			if (DateTimeOffset.Now - valueOrDefault.Item3 > HeldBackLifetime)
			{
				heldBack = null;
				Log.Info("Billing", "held-back paywall dropped (too old)");
			}
			else if (text == null && !IsPaywallVisible)
			{
				heldBack = null;
				Log.Info("Billing", "the call is over: showing the held-back paywall");
				ShowPaywall(valueOrDefault.Item1, valueOrDefault.Item2);
				return;
			}
		}
		tuple = heldBack;
		if (!tuple.HasValue && (!IsPaywallVisible || !WaitsOutQuiet(source)))
		{
			quietWatch?.Stop();
		}
	}

	void IPaywallCardHost.Close(PaywallCloseKind kind)
	{
		Close(kind);
	}

	private void Close(PaywallCloseKind kind)
	{
		if ((object)window != null && window.IsShowing)
		{
			window.Dismiss();
			card = null;
			Log.Info("Billing", $"paywall closed: {kind} (source={source.WireName()})");
			this.PaywallClosed?.Invoke(source, kind);
		}
	}

	void IPaywallCardHost.ContentResized()
	{
		PaywallWindow? paywallWindow = window;
		if ((object)paywallWindow != null && paywallWindow.IsShowing)
		{
			ui.TryEnqueue(DispatcherQueuePriority.Low, delegate
			{
				window?.Fit();
			});
		}
	}

	async Task<string?> IPaywallCardHost.StartCheckoutAsync(string plan, string interval, string surface, PaywallSource source)
	{
		try
		{
			string analyticsSource = ((source == PaywallSource.DeveloperOnboardingPreview) ? "developer_paywall_preview" : (source.OffersFreeOption() ? "onboarding_paywall" : "paywall"));
			await OpenCheckoutOrThrowAsync(plan, interval, analyticsSource, surface, source.WireName());
			return null;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			Log.Warn("Billing", $"checkout launch failed (plan={plan} interval={interval}): {ex.Message}");
			return CheckoutFailureMessage(ex);
		}
	}

	async Task<string?> IPaywallCardHost.OpenPortalAsync()
	{
		try
		{
			await OpenPortalOrThrowAsync("paywall_payment_problem");
			return null;
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			Log.Warn("Billing", "portal launch failed: " + ex.Message);
			return CheckoutFailureMessage(ex);
		}
	}

	private void OnStateChanged()
	{
		card?.SetReferral(previewReferral ?? State.ReferralDiscount);
		this.Changed?.Invoke();
	}

	private async Task OpenCheckoutOrThrowAsync(string plan, string interval, string analyticsSource, string? surface, string? presentationSource)
	{
		Uri result;
		bool flag = !Uri.TryCreate(((await worker.CreateCheckoutAsync(plan, interval)) ?? throw new BillingSignedOutException()).Url, UriKind.Absolute, out result);
		if (!flag)
		{
			string scheme = result.Scheme;
			bool flag2 = ((scheme == "https" || scheme == "http") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			throw new JsonException("checkout URL missing");
		}
		tuckAway();
		openUrl(result.AbsoluteUri);
		State.BeginWatchingForBrowserPlanChange();
		Log.Info("Billing", $"checkout opened in the browser: plan={plan} interval={interval} source={analyticsSource} host={result.Host}");
		ClickyAnalytics.TrackCheckoutStarted(analyticsSource, surface, plan, interval, presentationSource);
		SayCheckoutOpened(plan);
	}

	private async Task OpenPortalOrThrowAsync(string analyticsSource)
	{
		Uri result;
		bool flag = !Uri.TryCreate(((await worker.CreatePortalSessionAsync()) ?? throw new BillingSignedOutException()).Url, UriKind.Absolute, out result);
		if (!flag)
		{
			string scheme = result.Scheme;
			bool flag2 = ((scheme == "https" || scheme == "http") ? true : false);
			flag = !flag2;
		}
		if (flag)
		{
			throw new JsonException("portal URL missing");
		}
		tuckAway();
		openUrl(result.AbsoluteUri);
		State.BeginWatchingForBrowserPlanChange();
		Log.Info("Billing", "customer portal opened in the browser: source=" + analyticsSource + " host=" + result.Host);
		ClickyAnalytics.TrackCustomerPortalOpened(analyticsSource);
	}

	private static bool IsSignedOut(Exception ex)
	{
		if (!(ex is BillingSignedOutException))
		{
			if (ex is WorkerHttpException ex2)
			{
				return BillingStateService.IsAuthFailure(ex2.Status, ex2.Body);
			}
			return false;
		}
		return true;
	}

	public static string CheckoutFailureMessage(Exception ex)
	{
		if (ex is WorkerHttpException ex2)
		{
			string body = ex2.Body;
			if (ServerField(body, "code") == "invalid_discount_code")
			{
				string text = ServerField(body, "error");
				if (text != null)
				{
					return text;
				}
			}
		}
		string text2;
		if (!IsSignedOut(ex))
		{
			if (!(ex is WorkerHttpException ex3))
			{
				text2 = ((ex is JsonException) ? "we couldn't read the server's response" : ((!(ex is HttpRequestException) && !(ex is TaskCanceledException) && !(ex is IOException)) ? ex.Message : "a network problem reaching our servers"));
			}
			else
			{
				string text3 = ServerField(ex3.Body, "error");
				text2 = ((text3 != null) ? $"{text3} (HTTP {ex3.Status})" : $"the server returned HTTP {ex3.Status}");
			}
		}
		else
		{
			text2 = "you appear to be signed out, try signing in again";
		}
		string text4 = text2;
		return "Couldn't start checkout — " + text4 + ". Please email hi@heyclicky.com and we'll sort it out fast.";
	}

	private static string? ServerField(string body, string name)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(body);
			object result;
			if (jsonDocument.RootElement.ValueKind == JsonValueKind.Object && jsonDocument.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
			{
				string text = value.GetString()?.Trim();
				if (text != null && text.Length > 0)
				{
					result = text;
					goto IL_0064;
				}
			}
			result = null;
			goto IL_0064;
			IL_0064:
			return (string?)result;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	public void HandleCheckoutComplete(CheckoutCompleteLink link)
	{
		State.HandleCheckoutCompleteAsync(link);
		if (link.IsCanceled)
		{
			SayLine("( \u02d8▽\u02d8) no worries, nothing was charged. i'm here whenever you're ready.");
			return;
		}
		if (link.IsPortal)
		{
			SayLine("(^-^)ノ welcome back!");
			return;
		}
		paidReturnTimer?.Stop();
		paidReturnTimer = ui.CreateTimer();
		paidReturnTimer.Interval = TimeSpan.FromSeconds(6L);
		paidReturnTimer.IsRepeating = false;
		paidReturnTimer.Tick += delegate
		{
			paidReturnTimer = null;
			DateTimeOffset? dateTimeOffset = lastPaidPlanCelebratedAt;
			if (dateTimeOffset.HasValue)
			{
				DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
				if (DateTimeOffset.Now - valueOrDefault < TimeSpan.FromMinutes(10L))
				{
					SayLine("(・ω・)ノ hi, i'm here!");
					return;
				}
			}
			if (State.CurrentTier != "free")
			{
				SayLine("(^_^)v you're all set! thanks for making me more powerful. " + HoldToTalkHint);
			}
			else
			{
				SayLine("(^_^)v payment's through! your plan will switch on in a moment.");
			}
		};
		paidReturnTimer.Start();
	}

	private void SayCheckoutOpened(string plan)
	{
		SayLine((plan == "max") ? "＼(^o^)／ let's go max! i'll be right here when you're done." : "(\uff3e▽\uff3e) let's go pro! i'll be right here when you're done.");
	}

	private void OnPaidPlanStarted(string tier)
	{
		paidReturnTimer?.Stop();
		paidReturnTimer = null;
		lastPaidPlanCelebratedAt = DateTimeOffset.Now;
		string text = ((tier == "max") ? "＼(^o^)／ welcome to max! thank you for making me extra powerful." : "(\uff3e▽\uff3e) welcome to pro! thank you for making me more powerful.");
		SayLine(text + " " + HoldToTalkHint);
	}

	private void SayLine(string line)
	{
		if (!CanSayLine())
		{
			Log.Info("Billing", "cursor line skipped (busy or onboarding): " + line);
			return;
		}
		Log.Info("Billing", "cursor line: " + line);
		this.CursorLine?.Invoke(line);
	}
}
