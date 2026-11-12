using System;
using System.Collections.Generic;
using System.Linq;

namespace HeyClicky.Core.Proactive;

public sealed class IntegrationSuggestionRules
{
	private sealed record Entry(string Slug, string Name, string[] Hosts, string[] TitleNeedles, string[] Processes, string[]? Paths = null)
	{
		public int? HostSpecificity(string host)
		{
			int? result = null;
			string[] hosts = Hosts;
			foreach (string text in hosts)
			{
				if (host == text || host.EndsWith("." + text, StringComparison.Ordinal))
				{
					result = Math.Max(result.GetValueOrDefault(), text.Length);
				}
			}
			return result;
		}

		public bool MatchesPath(string path)
		{
			string[] paths = Paths;
			if (paths != null && paths.Length > 0)
			{
				return Paths.Any((string p) => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
			}
			return false;
		}

		public bool MatchesTitle(string lowered)
		{
			return TitleNeedles.Any(lowered.Contains);
		}
	}

	public enum SampleOutcome
	{
		None,
		SpotifyTipReady,
		CandidateReady
	}

	public readonly record struct SuppressionInputs(bool SignedIn, bool PermissionsComplete, bool OnboardingComplete, string? QuietReason, bool NotchExpanded, bool SpotifyTipActive, bool SuggestionActive, bool VoiceBusy, bool IntroActive, bool TutorialEngaged, bool PostOnboardingQuietPeriod);

	public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2.5);

	public static readonly TimeSpan DwellThreshold = TimeSpan.FromSeconds(10L);

	public static readonly TimeSpan SpotifyDwellThreshold = TimeSpan.FromSeconds(7.5);

	public static readonly TimeSpan NotNowSnooze = TimeSpan.FromHours(48);

	public static readonly TimeSpan MaximumCreditedSample = TimeSpan.FromSeconds(30L);

	public const int MaximumPromptsPerDay = 3;

	public static readonly TimeSpan PromptsWindow = TimeSpan.FromHours(24);

	private static readonly Entry[] Popular = new Entry[20]
	{
		new Entry("notion", "Notion", new string[2] { "notion.so", "notion.site" }, new string[1] { "notion" }, new string[1] { "notion" }),
		new Entry("linear", "Linear", new string[1] { "linear.app" }, new string[1] { "linear" }, new string[1] { "linear" }),
		new Entry("github", "GitHub", new string[1] { "github.com" }, new string[1] { "github" }, new string[1] { "githubdesktop" }),
		new Entry("gmail", "Gmail", new string[1] { "mail.google.com" }, new string[1] { "gmail" }, Array.Empty<string>()),
		new Entry("googlecalendar", "Google Calendar", new string[1] { "calendar.google.com" }, new string[1] { "google calendar" }, Array.Empty<string>()),
		new Entry("googledrive", "Google Drive", new string[1] { "drive.google.com" }, new string[1] { "google drive" }, Array.Empty<string>()),
		new Entry("googledocs", "Google Docs", new string[1] { "docs.google.com" }, new string[1] { "google docs" }, Array.Empty<string>(), new string[1] { "/document" }),
		new Entry("googlesheets", "Google Sheets", new string[1] { "docs.google.com" }, new string[1] { "google sheets" }, Array.Empty<string>(), new string[1] { "/spreadsheets" }),
		new Entry("slack", "Slack", new string[2] { "app.slack.com", "slack.com" }, new string[1] { "slack" }, new string[1] { "slack" }),
		new Entry("jira", "Jira", new string[1] { "atlassian.net" }, new string[1] { "jira" }, Array.Empty<string>(), new string[5] { "/jira", "/browse", "/issues", "/secure", "/projects" }),
		new Entry("asana", "Asana", new string[2] { "app.asana.com", "asana.com" }, new string[1] { "asana" }, new string[1] { "asana" }),
		new Entry("trello", "Trello", new string[1] { "trello.com" }, new string[1] { "trello" }, Array.Empty<string>()),
		new Entry("clickup", "ClickUp", new string[2] { "clickup.com", "app.clickup.com" }, new string[1] { "clickup" }, new string[1] { "clickup" }),
		new Entry("airtable", "Airtable", new string[1] { "airtable.com" }, new string[1] { "airtable" }, Array.Empty<string>()),
		new Entry("hubspot", "HubSpot", new string[2] { "app.hubspot.com", "hubspot.com" }, new string[1] { "hubspot" }, Array.Empty<string>()),
		new Entry("salesforce", "Salesforce", new string[2] { "salesforce.com", "force.com" }, new string[1] { "salesforce" }, Array.Empty<string>()),
		new Entry("zendesk", "Zendesk", new string[1] { "zendesk.com" }, new string[1] { "zendesk" }, Array.Empty<string>()),
		new Entry("discord", "Discord", new string[2] { "discord.com", "discordapp.com" }, new string[1] { "discord" }, new string[3] { "discord", "discordcanary", "discordptb" }),
		new Entry("dropbox", "Dropbox", new string[1] { "dropbox.com" }, new string[1] { "dropbox" }, Array.Empty<string>()),
		new Entry("zoom", "Zoom", new string[1] { "zoom.us" }, new string[1] { "zoom" }, new string[1] { "zoom" })
	};

	private static readonly Entry[] Catalog = new Entry[89]
	{
		H("apaleo", "Apaleo", "apaleo.com"),
		H("attio", "Attio", "attio.com"),
		H("basecamp", "Basecamp", "basecamp.com"),
		H("bitbucket", "Bitbucket", "bitbucket.org"),
		H("blackbaud", "Blackbaud", "blackbaud.com"),
		H("boldsign", "Boldsign", "boldsign.com"),
		H("box", "Box", "box.com"),
		H("cal", "Cal", "cal.com"),
		H("calendly", "Calendly", "calendly.com"),
		H("canva", "Canva", "canva.com"),
		H("capsule_crm", "Capsule CRM", "capsulecrm.com"),
		new Entry("confluence", "Confluence", new string[1] { "atlassian.net" }, new string[1] { "confluence" }, Array.Empty<string>(), new string[1] { "/wiki" }),
		H("contentful", "Contentful", "contentful.com"),
		H("crowdin", "Crowdin", "crowdin.com"),
		H("dart", "Dart", "itsdart.com"),
		H("dialpad", "Dialpad", "dialpad.com"),
		H("digital_ocean", "DigitalOcean", "digitalocean.com"),
		H("dub", "Dub", "dub.co"),
		H("dynamics365", "Dynamics 365", "dynamics.com"),
		H("eventbrite", "Eventbrite", "eventbrite.com"),
		new Entry("excel", "Excel", new string[1] { "excel.cloud.microsoft" }, Array.Empty<string>(), new string[1] { "excel" }),
		H("exist", "Exist", "exist.io"),
		H("facebook", "Facebook", "facebook.com"),
		H("fathom", "Fathom", "fathom.video"),
		new Entry("figma", "Figma", new string[1] { "figma.com" }, Array.Empty<string>(), new string[1] { "figma" }),
		H("freeagent", "Freeagent", "freeagent.com"),
		H("freshbooks", "FreshBooks", "freshbooks.com"),
		H("gitlab", "GitLab", "gitlab.com"),
		H("gong", "Gong", "gong.io"),
		H("google_analytics", "Google Analytics", "analytics.google.com"),
		H("google_classroom", "Google Classroom", "classroom.google.com"),
		H("google_search_console", "Google Search Console", "search.google.com"),
		H("googleads", "Google Ads", "ads.google.com"),
		H("googlemeet", "Google Meet", "meet.google.com"),
		H("googlephotos", "Google Photos", "photos.google.com"),
		H("googletasks", "Google Tasks", "tasks.google.com"),
		H("gorgias", "Gorgias", "gorgias.com"),
		H("gumroad", "Gumroad", "gumroad.com"),
		H("harvest", "Harvest", "getharvest.com"),
		H("hugging_face", "Hugging Face", "huggingface.co"),
		H("instagram", "Instagram", "instagram.com"),
		H("intercom", "Intercom", "intercom.com"),
		H("kit", "Kit", "kit.com"),
		H("linkedin", "LinkedIn", "linkedin.com"),
		H("linkhut", "Linkhut", "linkhut.org"),
		H("mailchimp", "Mailchimp", "mailchimp.com"),
		new Entry("microsoft_teams", "Microsoft Teams", new string[1] { "teams.microsoft.com" }, Array.Empty<string>(), new string[2] { "ms-teams", "teams" }),
		H("miro", "Miro", "miro.com"),
		H("monday", "Monday", "monday.com"),
		H("moneybird", "Moneybird", "moneybird.com"),
		H("mural", "Mural", "mural.co"),
		H("omnisend", "Omnisend", "omnisend.com"),
		new Entry("one_drive", "OneDrive", new string[1] { "onedrive.live.com" }, Array.Empty<string>(), Array.Empty<string>()),
		new Entry("outlook", "Outlook", new string[3] { "outlook.office.com", "outlook.live.com", "outlook.office365.com" }, Array.Empty<string>(), new string[2] { "outlook", "olk" }),
		H("pagerduty", "PagerDuty", "pagerduty.com"),
		H("prisma", "Prisma", "console.prisma.io"),
		H("productboard", "Productboard", "productboard.com"),
		H("pushbullet", "Pushbullet", "pushbullet.com"),
		H("quickbooks", "QuickBooks", "quickbooks.intuit.com"),
		H("reddit", "Reddit", "reddit.com"),
		H("reddit_ads", "Reddit Ads", "ads.reddit.com"),
		H("roam", "Roam", "ro.am"),
		H("sentry", "Sentry", "sentry.io"),
		H("servicem8", "Servicem8", "servicem8.com"),
		H("share_point", "SharePoint", "sharepoint.com"),
		H("shippo", "Shippo", "goshippo.com"),
		H("splitwise", "Splitwise", "splitwise.com"),
		H("square", "Square", "squareup.com"),
		H("stack_exchange", "Stack Exchange", "stackexchange.com"),
		H("stripe", "Stripe", "stripe.com"),
		H("supabase", "Supabase", "supabase.com"),
		H("ticktick", "Ticktick", "ticktick.com"),
		H("timely", "Timely", "timelyapp.com"),
		new Entry("todoist", "Todoist", new string[1] { "todoist.com" }, Array.Empty<string>(), new string[1] { "todoist" }),
		H("toneden", "Toneden", "toneden.io"),
		H("typeform", "Typeform", "typeform.com"),
		H("wakatime", "WakaTime", "wakatime.com"),
		H("webex", "Webex", "webex.com"),
		new Entry("whatsapp", "WhatsApp Business", new string[1] { "web.whatsapp.com" }, Array.Empty<string>(), new string[1] { "whatsapp" }),
		H("wrike", "Wrike", "wrike.com"),
		H("ynab", "YNAB", "ynab.com"),
		H("youtube", "YouTube", "youtube.com"),
		H("zeplin", "Zeplin", "zeplin.io"),
		H("zoho_bigin", "Zoho Bigin", "bigin.com"),
		H("zoho_books", "Zoho Books", "books.zoho.com"),
		H("zoho_desk", "Zoho Desk", "desk.zoho.com"),
		H("zoho_inventory", "Zoho Inventory", "inventory.zoho.com"),
		H("zoho_invoice", "Zoho Invoice", "invoice.zoho.com"),
		H("zoho_mail", "Zoho Mail", "mail.zoho.com")
	};

	private HashSet<string> available = new HashSet<string>();

	private readonly Dictionary<string, TimeSpan> dwell = new Dictionary<string, TimeSpan>();

	private TimeSpan spotifyDwell;

	private DateTimeOffset? lastSample;

	public int AvailableToolkitCount => available.Count;

	private IEnumerable<Entry> Active => from e in Popular.Concat(Catalog)
		where available.Contains(e.Slug)
		select e;

	private static Entry H(string slug, string name, params string[] hosts)
	{
		return new Entry(slug, name, hosts, Array.Empty<string>(), Array.Empty<string>());
	}

	public void SetAvailableToolkits(IEnumerable<string> slugs)
	{
		available = slugs.Select((string s) => s.ToLowerInvariant()).ToHashSet();
		foreach (string item in dwell.Keys.Where((string k) => !available.Contains(k)).ToList())
		{
			dwell.Remove(item);
		}
	}

	public static bool IsSpotify(ForegroundActivity a)
	{
		if (!a.ProcessName.Equals("spotify", StringComparison.OrdinalIgnoreCase))
		{
			if (a.IsBrowser)
			{
				string host = a.Host;
				if (host != null)
				{
					if (!(host == "spotify.com"))
					{
						return host.EndsWith(".spotify.com", StringComparison.Ordinal);
					}
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public IntegrationCandidate? Detect(ForegroundActivity a)
	{
		List<Entry> list = Active.ToList();
		if (list.Count == 0)
		{
			return null;
		}
		Entry entry = list.FirstOrDefault((Entry e) => e.Processes.Any((string p) => p.Equals(a.ProcessName, StringComparison.OrdinalIgnoreCase)));
		if ((object)entry != null)
		{
			return new IntegrationCandidate(entry.Slug, entry.Name, a.AppName, null);
		}
		if (a.IsBrowser)
		{
			string host = a.Host;
			if (host != null)
			{
				List<(Entry, int?)> list2 = list.Select((Entry e) => (Entry: e, Score: e.HostSpecificity(host))).Where(delegate((Entry Entry, int? Score) x)
				{
					int? item = x.Score;
					return item.HasValue;
				}).ToList();
				if (list2.Count == 0)
				{
					return null;
				}
				int best = list2.Max<(Entry, int?)>(((Entry Entry, int? Score) x) => x.Score.Value);
				List<Entry> list3 = (from x in list2
					where x.Score == best
					select x.Entry).ToList();
				object obj;
				if (list3.Count != 1)
				{
					string path = a.Path;
					obj = ((path != null) ? list3.FirstOrDefault((Entry e) => e.MatchesPath(path)) : null);
					if (obj == null)
					{
						string title = a.WindowTitle?.ToLowerInvariant();
						obj = ((title != null) ? list3.FirstOrDefault((Entry e) => e.MatchesTitle(title)) : null);
					}
				}
				else
				{
					obj = list3[0];
				}
				Entry entry2 = (Entry)obj;
				if ((object)entry2 != null)
				{
					return new IntegrationCandidate(entry2.Slug, entry2.Name, a.AppName, host);
				}
				return null;
			}
		}
		return null;
	}

	public (SampleOutcome Outcome, IntegrationCandidate? Candidate) Sample(ForegroundActivity? a, DateTimeOffset now, bool suppressed, ProactiveStoreData state)
	{
		DateTimeOffset? dateTimeOffset = lastSample;
		lastSample = now;
		if (suppressed || (object)a == null)
		{
			return (Outcome: SampleOutcome.None, Candidate: null);
		}
		TimeSpan timeSpan;
		if (dateTimeOffset.HasValue)
		{
			DateTimeOffset valueOrDefault = dateTimeOffset.GetValueOrDefault();
			timeSpan = now - valueOrDefault;
		}
		else
		{
			timeSpan = TimeSpan.Zero;
		}
		TimeSpan timeSpan2 = timeSpan;
		if (timeSpan2 <= TimeSpan.Zero)
		{
			return (Outcome: SampleOutcome.None, Candidate: null);
		}
		if (timeSpan2 > MaximumCreditedSample)
		{
			timeSpan2 = MaximumCreditedSample;
		}
		if (!state.SpotifyVoiceTipShown && IsSpotify(a))
		{
			spotifyDwell += timeSpan2;
			if (!(spotifyDwell >= SpotifyDwellThreshold))
			{
				return (Outcome: SampleOutcome.None, Candidate: null);
			}
			return (Outcome: SampleOutcome.SpotifyTipReady, Candidate: null);
		}
		IntegrationCandidate integrationCandidate = Detect(a);
		if ((object)integrationCandidate == null)
		{
			return (Outcome: SampleOutcome.None, Candidate: null);
		}
		TimeSpan timeSpan3 = dwell.GetValueOrDefault(integrationCandidate.ToolkitSlug) + timeSpan2;
		dwell[integrationCandidate.ToolkitSlug] = timeSpan3;
		if (!(timeSpan3 >= DwellThreshold) || !ShouldPrompt(integrationCandidate.ToolkitSlug, now, state))
		{
			return (Outcome: SampleOutcome.None, Candidate: null);
		}
		return (Outcome: SampleOutcome.CandidateReady, Candidate: integrationCandidate);
	}

	public static bool ShouldPrompt(string slug, DateTimeOffset now, ProactiveStoreData state)
	{
		if (state.IntegrationNeverAsk.Contains(slug))
		{
			return false;
		}
		if (state.IntegrationSnoozedUntil.TryGetValue(slug, out var value) && value > now)
		{
			return false;
		}
		return state.IntegrationPromptTimestamps.Count((DateTimeOffset t) => t >= now - PromptsWindow) < 3;
	}

	public static string? SuppressionReason(in SuppressionInputs s)
	{
		if (!s.SignedIn)
		{
			return "signedOut";
		}
		if (!s.PermissionsComplete)
		{
			return "permissionsIncomplete";
		}
		if (!s.OnboardingComplete)
		{
			return "onboardingIncomplete";
		}
		if (s.QuietReason != null)
		{
			return "quietContext";
		}
		if (s.NotchExpanded)
		{
			return "notchExpanded";
		}
		if (s.SpotifyTipActive)
		{
			return "spotifyVoiceTipAlreadyActive";
		}
		if (s.SuggestionActive)
		{
			return "suggestionAlreadyActive";
		}
		if (s.VoiceBusy)
		{
			return "voiceBusy";
		}
		if (s.IntroActive)
		{
			return "onboardingIntroActive";
		}
		if (s.TutorialEngaged)
		{
			return "onboardingTutorialEngaged";
		}
		if (s.PostOnboardingQuietPeriod)
		{
			return "postOnboardingQuietPeriod";
		}
		return null;
	}

	public static string? AmbientQuietReason(bool anotherAppUsesMicrophone, bool doNotDisturb)
	{
		if (!anotherAppUsesMicrophone)
		{
			if (!doNotDisturb)
			{
				return null;
			}
			return "do_not_disturb";
		}
		return "call_microphone_in_use_by_another_app";
	}

	public void ResetDwell(string slug)
	{
		dwell[slug] = TimeSpan.Zero;
	}

	public void ResetSpotifyDwell()
	{
		spotifyDwell = TimeSpan.Zero;
	}

	public void ResetAll()
	{
		dwell.Clear();
		spotifyDwell = TimeSpan.Zero;
		lastSample = null;
	}
}
