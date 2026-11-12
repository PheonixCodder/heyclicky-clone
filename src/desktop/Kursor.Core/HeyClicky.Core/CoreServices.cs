using System;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.Core.Auth;
using HeyClicky.Core.Billing;
using HeyClicky.Core.Brain;
using HeyClicky.Core.Configuration;
using HeyClicky.Core.Dictation;
using HeyClicky.Core.Identity;
using HeyClicky.Core.Net;
using HeyClicky.Core.Notch;
using HeyClicky.Core.Onboarding;
using HeyClicky.Core.Settings;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;

namespace HeyClicky.Core;

public sealed class CoreServices : IAsyncDisposable
{
	public ClickyEndpoints Endpoints { get; }

	public ClientIdentity Identity { get; }

	public ClickyHttp Http { get; }

	public SettingsStore Settings { get; }

	public SupabaseAuthService Auth { get; }

	public WorkerClient Worker { get; }

	public BrainClient BrainClient { get; }

	public BrainSession BrainSession { get; }

	public NotchPresenter Notch { get; }

	public BillingStateService Billing { get; }

	private CoreServices(ClickyEndpoints endpoints, ClientIdentity identity, SettingsStore? settings, Func<Uri, bool>? openBrowser)
	{
		Endpoints = endpoints;
		Identity = identity;
		Http = new ClickyHttp(endpoints, identity);
		Settings = settings ?? new SettingsStore();
		ClickyAnalytics.Configure(TelemetryConfiguration.Current, endpoints, identity, Http.Client);
		Auth = new SupabaseAuthService(Http, null, openBrowser);
		Worker = new WorkerClient(Http, Auth);
		ClickyAnalytics.ErrorRelay = Worker.ReportError;
		ClickyAnalytics.CurrentAccount = () => (UserId: Auth.UserId, Email: Auth.User?.Email);
		BrainClient = new BrainClient(Http);
		BrainSession = new BrainSession(BrainClient, (CancellationToken ct) => Auth.GetValidAccessTokenAsync(ct));
		Notch = new NotchPresenter();
		Billing = new BillingStateService(Worker, Auth);
		Notch.SetSignedIn(Auth.IsSignedIn);
		Auth.Changed += delegate
		{
			Notch.SetSignedIn(Auth.IsSignedIn);
			if (Auth.IsSignedIn)
			{
				BrainSession.AdoptAccount(Auth.UserId);
			}
			else
			{
				BrainSession.ResetForSignOut();
			}
			if (Auth.IsSignedIn)
			{
				DictationVocabulary.AddBuiltInTermsIfNeeded(Settings);
			}
			if (Auth.IsSignedIn)
			{
				string text = Auth.User?.DisplayName;
				if (text != null)
				{
					DictationVocabulary.AddAccountNameIfNeeded(Settings, text);
				}
			}
		};
	}

	public static CoreServices Create(ClickyEndpoints? endpoints = null, ClientIdentity? identity = null, SettingsStore? settings = null, Func<Uri, bool>? openBrowser = null)
	{
		return new CoreServices(endpoints ?? AppBundleConfiguration.Load(), identity ?? ClickyClientIdentity.Current, settings, openBrowser);
	}

	public CompanionSession CreateCompanionSession(IMicrophoneCapture microphone, IAudioOutput audio, IScreenCaptureService screens, IScreenDrawingCanvas canvas, ICursorOverlay cursor, IHighlightOverlay? highlight, CompanionSessionOptions? options = null)
	{
		return new CompanionSession(Http, Auth, BrainClient, BrainSession, Settings, microphone, audio, screens, canvas, cursor, highlight, Notch, options);
	}

	public async Task StartAsync(CancellationToken ct = default(CancellationToken))
	{
		Http.PrewarmAll();
		if (!Auth.IsSignedIn)
		{
			return;
		}
		BrainSession.AdoptAccount(Auth.UserId);
		try
		{
			await Worker.RegisterThisDeviceIfNeededAsync(ct).ConfigureAwait(continueOnCapturedContext: false);
			await PreSignInIntroViewModel.FlushPendingDiscoveryChannelAsync(Settings, (string channel, string? other) => Worker.SetDiscoveryChannelAsync(channel, other, ct)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex)
		{
			Log.Warn("Core", "launch work: " + ex.Message);
		}
	}

	public ValueTask DisposeAsync()
	{
		Billing.Dispose();
		Notch.Dispose();
		BrainSession.Dispose();
		Settings.Dispose();
		Http.Dispose();
		return ValueTask.CompletedTask;
	}
}
