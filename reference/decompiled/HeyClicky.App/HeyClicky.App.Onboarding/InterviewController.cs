using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeyClicky.App.Portraits;
using HeyClicky.App.Services;
using HeyClicky.App.UI;
using HeyClicky.App.Wiring;
using HeyClicky.Core.Telemetry;
using HeyClicky.Core.Tools;
using HeyClicky.Core.Voice;
using HeyClicky.Core.Worker;
using Microsoft.UI.Dispatching;

namespace HeyClicky.App.Onboarding;

public sealed class InterviewController
{
	public const int QuestionBudget = 4;

	public static readonly string[] OpeningParts = new string[2] { "So, I'm HeyClicky! I'm here to help you make things, learn things, start a business, whatever you're going for.", "I'm totally just curious, I have four quick questions. What are you working on these days? What's your story?" };

	private const string SpokenConversationRule = "[App] This turn is a short spoken conversation inside HeyClicky itself, not a drawing. Answer with say() lines only: no marks, no cards, no photos, no buttons, nothing drawn. Short sentences, one per say().";

	private readonly AppServices s;

	private readonly RealStack? real;

	private readonly DispatcherQueue dq;

	private readonly object gate = new object();

	private readonly List<InterviewLine> transcript = new List<InterviewLine>();

	private int answerCount;

	private bool pendingTypedAnswer;

	private string lastHeldWords = "";

	private VoiceState lastState;

	private DispatcherQueueTimer? wrapWatch;

	private DispatcherQueueTimer? openerWatch;

	private IReadOnlyList<string>? availableIntegrations;

	private long startedAt;

	private readonly bool hearsTurnEnds;

	private const int OpenerFirstLine = -100;

	private static readonly TimeSpan PreparationLifetime = TimeSpan.FromMinutes(5L);

	private Task<byte[][]?>? preparedOpening;

	private CancellationTokenSource? preparationCancellation;

	private long preparedAt;

	private int preparationAttempts;

	private int openerPartsShown;

	private long openerStartedAt;

	private readonly HashSet<DispatcherQueueTimer> pendingTimers = new HashSet<DispatcherQueueTimer>();

	public bool IsActive { get; private set; }

	public bool UsesNewAgentsUiDemoPlan { get; set; }

	public bool IsWrappingUp { get; private set; }

	public bool IsPlanInFlight { get; private set; }

	public bool HasBuiltPlan { get; private set; }

	public bool HasHeardFirstQuestion { get; private set; }

	public bool IsOpenerSpeaking { get; private set; }

	public bool WasCompleted { get; private set; }

	public string SpokenText { get; private set; } = "";

	public PortraitAppearance Appearance { get; private set; }

	public IReadOnlyList<InterviewLine> Transcript
	{
		get
		{
			lock (gate)
			{
				return transcript.ToList();
			}
		}
	}

	public int AnswerCount
	{
		get
		{
			lock (gate)
			{
				return answerCount;
			}
		}
	}

	public string? PlanError { get; private set; }

	public StarterPlanResponse? LastPlan { get; private set; }

	public IReadOnlyList<string> FoundedSlugs { get; private set; } = Array.Empty<string>();

	public IReadOnlyDictionary<string, string> RevealLines { get; private set; } = new Dictionary<string, string>();

	public string InterviewId { get; private set; } = Guid.NewGuid().ToString().ToUpperInvariant();

	public Func<bool>? IsOnInterviewStep { get; set; }

	private VoiceState Voice
	{
		get
		{
			if (!IsOpenerSpeaking)
			{
				return s.Companion.State;
			}
			return VoiceState.Responding;
		}
	}

	public VoiceState VoiceStateForStage => Voice;

	public bool WantsPreparedOpening { get; set; }

	public bool IsUsersTurn
	{
		get
		{
			bool flag = IsActive && !IsWrappingUp && !IsPlanInFlight && HasHeardFirstQuestion;
			if (flag)
			{
				bool flag2 = s.Companion.State == VoiceState.Listening;
				if (!flag2)
				{
					VoiceState voice = Voice;
					bool flag3 = (uint)(voice - 2) <= 1u;
					flag2 = !flag3;
				}
				flag = flag2;
			}
			return flag;
		}
	}

	public event Action? Changed;

	public event Action? Completed;

	public event Action<bool>? PlanLanded;

	public InterviewController(AppServices services, RealStack? real)
	{
		s = services;
		this.real = real;
		dq = DispatcherQueue.GetForCurrentThread();
		Appearance = PortraitAppearance.HomeMascot(s.Settings.CursorColorHex);
		s.Companion.StateChanged += delegate
		{
			dq.TryEnqueue(OnVoiceState);
		};
		s.Companion.ResponseTextUpdated += delegate(object? _, string text)
		{
			dq.TryEnqueue(delegate
			{
				OnResponseText(text);
			});
		};
		s.Companion.UserTranscriptUpdated += delegate(object? _, string words)
		{
			lock (gate)
			{
				lastHeldWords = words;
			}
		};
		AgentToolHooks.IsTourInterviewFoundingOnly = () => IsActive || (IsOnInterviewStep?.Invoke() ?? false);
		FlowHooks.MutedSpeakerReplyExempt = () => IsActive;
		CompanionSession companionSession = real?.Companion;
		if (companionSession != null)
		{
			hearsTurnEnds = true;
			companionSession.TurnFinished += delegate(object? _, CompanionTurnFinish finish)
			{
				dq.TryEnqueue(delegate
				{
					if ((IsActive || IsWrappingUp) && finish.SpokenText.Length > 0)
					{
						AppendLine("assistant", finish.SpokenText);
					}
				});
			};
		}
		if (real == null)
		{
			return;
		}
		real.Companion.Mouth.LineAudioQueued += delegate(int line, double offset)
		{
			if (line <= -100 && line > -100 - OpeningParts.Length)
			{
				dq.TryEnqueue(delegate
				{
					OnOpenerAudioQueued(line, offset);
				});
			}
		};
	}

	public void Start()
	{
		if (HasBuiltPlan || IsPlanInFlight || WasCompleted)
		{
			this.Changed?.Invoke();
		}
		else
		{
			if (IsActive)
			{
				return;
			}
			lock (gate)
			{
				transcript.Clear();
				answerCount = 0;
				pendingTypedAnswer = false;
				lastHeldWords = "";
			}
			IsWrappingUp = false;
			HasHeardFirstQuestion = false;
			SpokenText = "";
			Appearance = PortraitAppearance.HomeMascot(s.Settings.CursorColorHex);
			IsActive = true;
			InterviewId = Guid.NewGuid().ToString().ToUpperInvariant();
			startedAt = Environment.TickCount64;
			ClickyAnalytics.TrackHomeSpaceNewClickyInterviewStarted("onboarding", InterviewId);
			FlowHooks.LiveStateNote = FlowNote;
			FlowHooks.TurnFlow = () => (!IsActive) ? null : "onboarding_interview";
			FlowHooks.SkipInterview = delegate
			{
				if (!IsActive)
				{
					return false;
				}
				ClickyAnalytics.TrackProductEvent("onboarding_interview_skipped_by_voice", null, null, new JsonObject((JsonNodeOptions?)null)
				{
					["interview_id"] = InterviewId,
					["answer_count"] = AnswerCount
				});
				dq.TryEnqueue(delegate
				{
					Log.Info("interview", "skipped by voice");
					BeginWrapUp();
				});
				return true;
			};
			PrefetchIntegrationsAsync();
			this.Changed?.Invoke();
			real?.Audio.PrepareForUpcomingAudio();
			After(1.0, SpeakOpener);
		}
	}

	public void PrepareOpening()
	{
		RealStack r = real;
		if (r == null || !r.Core.Auth.IsSignedIn || IsActive || WasCompleted || HasBuiltPlan || IsPlanInFlight)
		{
			return;
		}
		Task<byte[][]> task = preparedOpening;
		if (task != null && task.IsCompleted && (!task.IsCompletedSuccessfully || task.Result == null))
		{
			preparedOpening = null;
		}
		if (preparedOpening == null && preparationAttempts < 3)
		{
			preparationAttempts++;
			CancellationTokenSource cancellation = new CancellationTokenSource();
			preparationCancellation = cancellation;
			preparedAt = Environment.TickCount64;
			Log.Info("interview", $"preparing the opener on a silent mouth (attempt {preparationAttempts})");
			preparedOpening = Task.Run(() => RenderOpeningAsync(r, cancellation.Token));
		}
	}

	private static async Task<byte[][]?> RenderOpeningAsync(RealStack r, CancellationToken ct)
	{
		RealtimeMouth silent = r.Companion.CreateSilentMouth();
		Stopwatch started = Stopwatch.StartNew();
		byte[][] result;
		try
		{
			_ = 1;
			try
			{
				if (!(await silent.EnsureOpenAsync(ct)))
				{
					Log.Warn("interview", "opener preparation: the silent mouth did not open");
					result = null;
				}
				else
				{
					byte[][] parts = new byte[OpeningParts.Length][];
					int i = 0;
					while (true)
					{
						if (i < OpeningParts.Length)
						{
							byte[] array = await silent.RenderLineAsync(OpeningParts[i], -100 - i, ct);
							if (array == null)
							{
								result = null;
								break;
							}
							parts[i] = array;
							i++;
							continue;
						}
						double value = (double)parts.Sum((byte[] p) => p.Length) / 2.0 / (double)r.Audio.SampleRate;
						Log.Info("interview", $"opener prepared in {started.ElapsedMilliseconds} ms ({value:0.0} s of audio, held for the interview)");
						result = parts;
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				result = null;
			}
			catch (Exception ex2)
			{
				Log.Warn("interview", "opener preparation: " + ex2.Message);
				result = null;
			}
		}
		finally
		{
			await silent.DisposeAsync();
		}
		return result;
	}

	public void CancelPreparedOpening()
	{
		WantsPreparedOpening = false;
		if (preparedOpening != null)
		{
			preparationCancellation?.Cancel();
			preparationCancellation = null;
			preparedOpening = null;
			Log.Info("interview", "prepared opener dropped");
		}
	}

	private byte[][]? TakePreparedOpening()
	{
		Task<byte[][]> task = preparedOpening;
		bool flag = (double)(Environment.TickCount64 - preparedAt) <= PreparationLifetime.TotalMilliseconds;
		byte[][] result = default(byte[][]);
		int num;
		if (task != null && task.IsCompletedSuccessfully)
		{
			result = task.Result;
			num = ((result != null) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			preparedOpening = null;
			preparationCancellation = null;
			return result;
		}
		if (task != null)
		{
			Log.Info("interview", (!task.IsCompleted) ? "prepared opener not ready yet" : (flag ? "prepared opener unavailable" : "prepared opener expired"));
		}
		CancelPreparedOpening();
		return null;
	}

	private void SpeakOpener()
	{
		Log.Info("interview", $"opener (active={IsActive})");
		if (!IsActive)
		{
			return;
		}
		string text = string.Join(" ", OpeningParts);
		AppendLine("assistant", text);
		real?.Core.BrainSession.NoteForNextTurn("[App] The interview has started: you opened it by saying \"" + text + "\"");
		HasHeardFirstQuestion = true;
		IsOpenerSpeaking = true;
		openerPartsShown = 0;
		openerStartedAt = Environment.TickCount64;
		RealStack r = real;
		if (r != null)
		{
			SpokenText = "";
			this.Changed?.Invoke();
			byte[][] prepared = TakePreparedOpening();
			SpeakOpenerOnMouthAsync(r, prepared);
			long started = Environment.TickCount64;
			bool sawBusy = false;
			openerWatch = dq.CreateTimer();
			openerWatch.Interval = TimeSpan.FromMilliseconds(250L, 0L);
			openerWatch.Tick += delegate
			{
				bool flag = r.Companion.Mouth.IsResponseInFlight || r.Audio.Buffered > TimeSpan.FromMilliseconds(60L, 0L);
				long num = Environment.TickCount64 - started;
				sawBusy |= flag;
				if (!sawBusy && num >= 8000)
				{
					ShowOpenerParts(OpeningParts.Length, "no audio");
				}
				if ((!flag && (sawBusy || num >= 8000)) || num >= 40000)
				{
					openerWatch.Stop();
					ShowOpenerParts(OpeningParts.Length, "played out");
					IsOpenerSpeaking = false;
					this.Changed?.Invoke();
				}
			};
			openerWatch.Start();
		}
		else
		{
			SpokenText = string.Join("\n\n", OpeningParts);
			this.Changed?.Invoke();
			After(Math.Max(3.0, (double)text.Length / 16.0), delegate
			{
				IsOpenerSpeaking = false;
				this.Changed?.Invoke();
			});
		}
	}

	private static async Task SpeakOpenerOnMouthAsync(RealStack r, byte[][]? prepared)
	{
		try
		{
			RealtimeMouth mouth = r.Companion.Mouth;
			if (prepared != null && mouth.PlayRenderedLine(prepared[0], -100))
			{
				for (int i = 1; i < prepared.Length; i++)
				{
					mouth.PlayRenderedLine(prepared[i], -100 - i);
				}
				Log.Info("interview", "opener: prepared audio, playing at once");
				return;
			}
			Log.Info("interview", "opener: spoken live");
			if (await mouth.EnsureOpenAsync())
			{
				for (int j = 0; j < OpeningParts.Length; j++)
				{
					mouth.SpeakLine(OpeningParts[j], -100 - j);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("interview", "opener: " + ex.Message);
		}
	}

	private void OnOpenerAudioQueued(int line, double offset)
	{
		if (!IsActive || !IsOpenerSpeaking)
		{
			return;
		}
		RealStack r = real;
		if (r == null)
		{
			return;
		}
		int part = -100 - line;
		long queuedAt = Environment.TickCount64;
		DispatcherQueueTimer wait = dq.CreateTimer().Rooted();
		wait.Interval = TimeSpan.FromMilliseconds(30L, 0L);
		pendingTimers.Add(wait);
		wait.Tick += delegate
		{
			if (!(r.Audio.PlayedSeconds + 0.02 < offset) || Environment.TickCount64 - queuedAt >= 10000 || !IsOpenerSpeaking)
			{
				wait.Stop();
				pendingTimers.Remove(wait);
				ShowOpenerParts(part + 1, "its audio started");
			}
		};
		wait.Start();
	}

	private void ShowOpenerParts(int count, string why)
	{
		if (IsActive && count > openerPartsShown)
		{
			openerPartsShown = Math.Min(count, OpeningParts.Length);
			SpokenText = string.Join("\n\n", OpeningParts.Take(openerPartsShown));
			Log.Info("interview", $"opener bubble {openerPartsShown} entered ({why}, {Environment.TickCount64 - openerStartedAt} ms after the opener began)");
			this.Changed?.Invoke();
		}
	}

	public void Stop()
	{
		if (IsActive)
		{
			if (Transcript.Any((InterviewLine l) => l.Role == "user") || UsesNewAgentsUiDemoPlan)
			{
				FinishAndBuildPlan(revealAfter: false);
			}
			else
			{
				End(founded: false);
			}
		}
	}

	private void End(bool founded)
	{
		if (IsActive)
		{
			IsActive = false;
			IsWrappingUp = false;
			ClickyAnalytics.TrackHomeSpaceNewClickyInterviewEnded(founded, "onboarding", AnswerCount, routedToExisting: false, InterviewId, (double)(Environment.TickCount64 - startedAt) / 1000.0);
			wrapWatch?.Stop();
			FlowHooks.LiveStateNote = null;
			FlowHooks.TurnFlow = null;
			FlowHooks.SkipInterview = null;
			if (!founded)
			{
				SpokenText = "";
				s.Companion.Cancel();
				real?.Core.BrainSession.NoteForNextTurn("[App] [New Clicky interview] The user left the interview. Drop it; do not found a Clicky.");
			}
			else if (string.IsNullOrWhiteSpace(SpokenText))
			{
				SpokenText = "Thanks for sharing. I'll get things ready while you finish the tour.";
			}
			this.Changed?.Invoke();
		}
	}

	public void SubmitTyped(string text)
	{
		string text2 = text.Trim();
		if (IsActive && !IsWrappingUp && text2.Length != 0)
		{
			AppendLine("user", text2);
			int answerNumber;
			bool flag;
			lock (gate)
			{
				answerCount++;
				answerNumber = answerCount;
				flag = answerCount >= 4;
				pendingTypedAnswer = true;
			}
			TrackAnswer("text", answerNumber);
			if (flag)
			{
				BeginWrapUp();
			}
			SpokenText = "";
			this.Changed?.Invoke();
			Log.Info("interview", $"typed answer {AnswerCount}/{4}");
			RealStack realStack = real;
			if (realStack != null)
			{
				realStack.Companion.SubmitTextAsync(text2, true);
			}
			else
			{
				s.Companion.SubmitTextAsync(text2);
			}
		}
	}

	private string? FlowNote()
	{
		if (!IsActive)
		{
			return null;
		}
		string text = null;
		bool final;
		int answerNumber;
		lock (gate)
		{
			if (pendingTypedAnswer)
			{
				pendingTypedAnswer = false;
			}
			else
			{
				answerCount++;
				text = lastHeldWords.Trim();
				if (text.Length > 0)
				{
					AppendLineLocked("user", text);
				}
			}
			final = answerCount >= 4;
			answerNumber = answerCount;
		}
		if (text != null)
		{
			TrackAnswer("voice", answerNumber);
			dq.TryEnqueue(delegate
			{
				if (final)
				{
					BeginWrapUp();
				}
				this.Changed?.Invoke();
			});
		}
		global::_003C_003Ey__InlineArray3<string> buffer = default(global::_003C_003Ey__InlineArray3<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 0) = InterviewNote();
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 1) = AnswerNote(final);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<global::_003C_003Ey__InlineArray3<string>, string>(ref buffer, 2) = "[App] This turn is a short spoken conversation inside HeyClicky itself, not a drawing. Answer with say() lines only: no marks, no cards, no photos, no buttons, nothing drawn. Short sentences, one per say().";
		return string.Join("\n", global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<global::_003C_003Ey__InlineArray3<string>, string>(in buffer, 3));
	}

	private void TrackAnswer(string source, int answerNumber)
	{
		ClickyAnalytics.TrackProductEvent("clicky_interview_answer_submitted", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["mode"] = "onboarding",
			["source"] = source,
			["answer_number"] = answerNumber,
			["interview_id"] = InterviewId
		});
	}

	private void AppendLine(string role, string text)
	{
		lock (gate)
		{
			AppendLineLocked(role, text);
		}
	}

	private void AppendLineLocked(string role, string text)
	{
		string text2 = text.Trim();
		if (text2.Length == 0)
		{
			return;
		}
		if (transcript.Count > 0)
		{
			List<InterviewLine> list = transcript;
			if (list[list.Count - 1].Role == role)
			{
				List<InterviewLine> list2 = transcript;
				if (list2[list2.Count - 1].Text == text2)
				{
					return;
				}
			}
		}
		transcript.Add(new InterviewLine(role, text2));
	}

	private void OnResponseText(string text)
	{
		if ((IsActive || IsWrappingUp) && !IsOpenerSpeaking)
		{
			SpokenText = Regex.Replace(text, "\\s*[—–]\\s*", ", ").Trim();
			if (SpokenText.Length > 0)
			{
				HasHeardFirstQuestion = true;
			}
			this.Changed?.Invoke();
		}
	}

	private void OnVoiceState()
	{
		VoiceState state = s.Companion.State;
		if (state == VoiceState.Idle && WantsPreparedOpening)
		{
			PrepareOpening();
		}
		if (IsActive || IsWrappingUp)
		{
			if (state == VoiceState.Processing && lastState != VoiceState.Processing && !IsWrappingUp)
			{
				SpokenText = "";
			}
			bool flag = !hearsTurnEnds && state == VoiceState.Idle;
			if (flag)
			{
				VoiceState voiceState = lastState;
				bool flag2 = (uint)(voiceState - 2) <= 1u;
				flag = flag2;
			}
			if (flag)
			{
				string text = s.Companion.ResponseText.Trim();
				if (text.Length > 0)
				{
					AppendLine("assistant", text);
				}
			}
		}
		lastState = state;
		this.Changed?.Invoke();
	}

	private string AnswerNote(bool isFinalAnswer)
	{
		if (isFinalAnswer)
		{
			return "[Onboarding interview] That was their LAST answer. Do not ask anything more. Wrap up warmly, the way a friend ends a good first chat, never abruptly (Farza 2026-10-21: the wrap felt abrupt): first a few words reacting to what they just said, then thank them for telling you about themselves and say you really appreciate it (with their first name if you know it), then one short line naming the thing they talked about most, in THEIR words, as a great starting point. The feeling: \"Claude Code, nice, so you know the drill. Thanks for telling me about yourself, <their name>, I really appreciate it. <Their thing> gives me a great starting point.\" Your own words, three short sentences, contractions, no dashes. Never offer help or say what you can or will do for them (no \"I can help you…\", no plan, no promise), no observation or advice, no recap, no question. Then stop. Never call sessions_spawn.";
		}
		List<string> list;
		int num;
		lock (gate)
		{
			list = (from l in transcript
				where l.Role == "assistant"
				select l.Text.Trim() into t
				where t.Length > 0
				select t).ToList();
			num = answerCount;
		}
		string value = ((list.Count == 0) ? "You have asked nothing but the opener so far." : ("You have already asked, in order: " + string.Join(" ", list.Select((string t, int i) => $"({i + 1}) \"{t}\"")) + "."));
		int num2 = Math.Max(0, 4 - num);
		string value2 = ((num2 == 1) ? "You have ONE question left after this reaction, and it is the fixed last one: frame it as the last question (\"okay, last one\" vibes) and ask whether they have used an AI agent before and what kind (Claude Code, Codex, a computer-use agent, or mostly ChatGPT). Light, quick, no explaining what an agent is." : $"You have {num2} questions left, not counting the fixed last one about AI agents.");
		return $"[Onboarding interview] The user just said this (this message). {value} {value2} React to what they ACTUALLY said first, in a few specific words that prove you heard it, then ask ONE easy, friendly question that grows out of that answer and covers ground none of your earlier questions touched, in words you haven't used yet. Not the next item on a list: the question they made you curious about. If their answer surprised you, follow it (never dig into their personal life on your own); if it was small or practical, take it as the real answer and move to something you haven't asked about, whichever their answer points at; never nudge for a bigger dream. Never a diagnostic, self-diagnosis, or how-will-you-get-there question, never about tools, never two questions, never something they've already answered. If you still don't have their concrete goal, one of your remaining free questions gets it, in your own words; the agent question waits for the last slot. Then guide them into answering: after the question, two or three concrete ideas of what they could talk about, taken from what they've told you (\"could be X, Y, or something else entirely\"), so it's easy to just talk; a nudge, not a menu, never a second question. Three short sentences at most, never a bare one-liner. Never call sessions_spawn.";
	}

	private string InterviewNote()
	{
		IReadOnlyList<string> readOnlyList = availableIntegrations;
		string value = ((readOnlyList == null || readOnlyList.Count == 0) ? "Gmail, Google Calendar, Google Drive, Google Docs, Google Sheets, Notion, Linear, GitHub" : string.Join(", ", readOnlyList));
		return $"[Onboarding interview] The user just installed HeyClicky and said hi a moment ago. This is a short, relaxed get-to-know-you chat on the welcome tour. Picture it: there's a person in front of you, and you have no idea yet who they are: a student, a maker, someone with a job, a founder, a hobbyist, anyone. You're simply curious about THEM: what they do, what they're going for, what else is going on in their life, what they care about. Never assume they're building a business (Farza 2026-10-20: the chat leaned too hard on the first-business vibe); let their answers tell you who they are. You want the general ARC of their life as a person, not the deep end of one idea (Farza 2026-09-08: \"you're going too deep into their idea; you want more of a general arc of their life\"). WHAT YOU ARE REALLY AFTER: what they want to succeed at, at whatever size it comes in (Farza 2026-09-10: \"it focuses HARD on the dream, but it's okay if they just want help with their work\"). For some people that is a big dream; for most it is doing well at the job in front of them: shipping the thing, landing the client, getting through the launch, being good at the work. Both are the real answer. You are a dream assistant, but at the core you exist to help them succeed at whatever THEY want, so a practical, work-sized answer is a full answer, never a small one to push past. You may ask about the bigger picture ONCE, lightly and like YOU: a playful little AI who knows it's a big question coming from a computer and can say so with a wink, in whatever words come to you that time. If they answer small, practical, or \"I just want to do my job well\", take it with a grin as the real answer and build from THERE; never nudge for a bigger version. You also work out what they're going for from what they say: the number they let slip, the thing they light up about, what they keep coming back to. The whole point is to help them find the helpers they don't even know they want yet, and those come from what you figured out they're trying to win at, not from a dream they recited. Behind the scenes HeyClicky uses this conversation afterwards to set up their space (their Clickys, the persistent agents that will work for them, and a few first things to try). Never mention Clickys, agents, setup, or what happens after; never call sessions_spawn; never describe the page or the process. Assume they have never used an agent; at best they have used ChatGPT.\nVIBE: a curious friend who happens to be an AI and finds that a little funny, not an interviewer and not a coach (Farza 2026-09-08: \"you're interviewing too hard\"; Farza 2026-09-09: \"he knows he's an AI and he's messing around a little\"). You're allowed to be self-aware and cheeky about the whole setup: a computer asking a human about their dreams is a bit absurd, and you can say so, lightly, once or twice, never as a bit you keep running. Easy questions, the kind a friend asks over coffee, in your own words each time; no two runs of this chat should sound alike. Take what they say and be genuinely interested in it: a small reaction, then ONE easy question with a few ideas of where they could take it. BREADTH OVER DEPTH: at most one follow-up on any one thing, then move to ground you haven't touched. Their work is the center of this chat; their personal life comes in only when THEY bring it there, and you never dig for it. Never spend three turns on the same project. Their goals and ideas come out because you're interested, never because you're extracting them. Never grill, never probe for gaps, never ask diagnostic or self-diagnosis questions (no \"where do your customers come from\", \"what's blocking you\", \"how are you going to get there\", \"what would move it most\"), never quiz them about tools or apps (a program they live in that comes up naturally is great to follow for one beat), never stack two questions, never summarize them back at length. If they go short or quiet, offer an easier angle, not pressure. When they say something big, meet it as big in a few words (\"A million a year off one product. Okay, I like that.\"). Never flatter, never gush, never lecture. You still quietly notice, like a friendly Steve Jobs, the bigger version of what they're doing and the thing on the way there they aren't looking at; that thinking stays in your head: it shapes your next question, and you never say it out loud.\nGUIDE THEM, DON'T JUST ASK (Farza 2026-10-21: bare one-liner questions are hard to answer; the chat should make it easy to talk): every question turn is a quick reaction, the ONE question, then two or three concrete ideas of what they could talk about, drawn from what they've told you so far (\"could be the app itself, the first people using it, or what you'd do once it works\"), so they never face a blank and can just ramble. The ideas are a nudge, never a multiple-choice menu or a second question, and different every time. LENGTH CAP: a question turn is at most three short sentences, about 35 words (the opener and the wrap have their own); never a bare question on its own. The page shows your words in small bubbles. Because your words are SHOWN as text, never use dashes of any kind (no em dashes, no en dashes, no spaced hyphens); use a comma or a new sentence instead. One question at a time, then wait.\nYou get exactly {4} turns. Notes that say \"[Onboarding interview]\" will tell you after each answer whether to keep going or wrap up.\n(1) The opener is FIXED and the page-open note gives you its exact words: \"{OpeningParts[0] + "\n\n" + OpeningParts[1]}\" Say it word for word (Farza 2026-09-10). This is the FIRST time you introduce yourself (the hello a step ago was just banter), and the only fixed line in the chat; everything after it is yours.\nEvery turn after the opener is NOT a script (Farza 2026-09-09: \"it always asks me a hardcoded list of questions no matter what I answer\"; Farza 2026-09-10: \"I get the same q's all the time\"). Nothing is fixed but the opener. Each question comes FROM what they just said: react to their actual words first, specifically, then ask the one thing that answer makes you curious about, in words you haven't used yet. Three things MUST come out of this chat, in your own words and whenever they fit, never as a checklist: (a) ONE real goal (Farza 2026-09-10: \"be more pushy to find a real goal; it doesn't have to be a number, it could be a thing, an event, anything\"): the concrete thing they're going for, in their words: a launch, a number, a role, a date, a thing they want to exist. If it hasn't surfaced by your second question, ask for it straight; if the answer is vague, ask once more for the concrete version, warmly (\"what's the actual thing, though?\"), and then take whatever they give. Work-sized is a full answer; never inflate it. (b) A real picture of them, built from their answers rather than a list of questions (Farza 2026-10-20: the rest of the tour draws for them from this chat, so the better you know them, the more magical it gets): what they actually do day to day, what they're learning or making right now, and the specific things in their world by name (the class, the project, the program they live in, the product, the team they follow). Ask the question their answer makes you most curious about, the one that tells you the most about who they are. (c) The LAST question is fixed and is always this one, framed as the last question (\"okay, last one\" vibes): have they used an AI agent before, and what kind (Claude Code, Codex, a computer-use agent, or has it mostly been ChatGPT)? Light and quick, so the tour can skip explaining what an agent is if they already know. Never explain what an agent is here; take the answer in a phrase and move on (Farza 2026-09-10: the agent question ends the interview, never sits in the middle). The rest of the ground, if a turn is free: what winning at the work looks like right now; what else is on their plate; what they're figuring out for the first time. The per-answer notes tell you what you've already asked and what's still open; pick from what's open, or leave the list when their answer hands you something better. Work first; if they hand you something surprising, follow THAT, but never dig into their personal life on your own; if they say something huge, sit with it for a beat before asking anything. Never ask a question they've already answered, and never ask something their last answer made irrelevant. Keep widening rather than drilling into one thing; if an app comes up naturally, remember it. (For your own context only, never to recite: the apps HeyClicky can connect are {value}; the connected_integrations catalog in your instructions says what each one lets a Clicky do.)\nThe last question is the fixed agent question above, framed as the last one; everything before it grows out of their answers.\nSKIPPING: if at ANY point they ask to skip, stop, or get out of this interview (any words, any language, however blunt), let them. Do not argue, do not ask why, do not offer to keep going, do not tell them what to do next: ONE short friendly line with no question in it, then call skip_onboarding_interview. The tour moves on by itself and whatever they already said still counts.\nREQUESTS: if instead of answering they ask you to DO something for them (open something, look something up, write or send something, build something, any task, in any words), don't do it now and don't start anything for it: no tool call, never sessions_spawn. In one short friendly line say you'll do it right after onboarding, then come straight back to the question you just asked, in the same reply, so the chat carries on (2026-10-23: a request in the middle of the interview waits for the end of the tour instead of derailing it).\nAfter the last answer, the note will say so: reply with ONE warm wrap, the way a friend ends a good first chat, never abruptly (Farza 2026-10-21: the wrap felt abrupt). First a few words reacting to what they just said; then thank them for telling you about themselves and say you really appreciate it (with their first name if you know it); then one short line naming the thing they talked about most, in THEIR words (the symptom tracker, the thesis, the trip), never \"everything\" or \"your work\", as a great starting point. The feeling: \"Claude Code, nice, so you know the drill. Thanks for telling me about yourself, <their name>, I really appreciate it. <Their thing> gives me a great starting point.\" Your own words each time, three short sentences, contractions, no dashes. Never offer help or say what you can or will do for them (no \"I can help you…\", no plan, no promise), no observation or advice, no recap, no \"based on what you told me\", no question. Then stop. Never ask about their name or email.";
	}

	private void BeginWrapUp()
	{
		if (IsWrappingUp)
		{
			return;
		}
		IsWrappingUp = true;
		long started = Environment.TickCount64;
		bool spoken = false;
		long? quietSince = null;
		wrapWatch?.Stop();
		wrapWatch = dq.CreateTimer();
		wrapWatch.Interval = TimeSpan.FromMilliseconds(100L, 0L);
		wrapWatch.Tick += delegate
		{
			if (!IsActive || IsPlanInFlight)
			{
				wrapWatch.Stop();
			}
			else
			{
				VoiceState state = s.Companion.State;
				if (state == VoiceState.Responding)
				{
					spoken = true;
				}
				if (spoken)
				{
					DateTime? dateTime = real?.Canvas.NarrationAudioEndsAt;
					if (dateTime.HasValue)
					{
						DateTime valueOrDefault = dateTime.GetValueOrDefault();
						if (DateTime.UtcNow >= valueOrDefault.AddSeconds(0.2))
						{
							wrapWatch.Stop();
							FinishAndBuildPlan(revealAfter: true);
							return;
						}
					}
				}
				if (state != 0)
				{
					quietSince = null;
				}
				else
				{
					long valueOrDefault2 = quietSince.GetValueOrDefault();
					if (!quietSince.HasValue)
					{
						valueOrDefault2 = Environment.TickCount64;
						quietSince = valueOrDefault2;
					}
					double num = (double)(Environment.TickCount64 - quietSince.Value) / 1000.0;
					bool num2 = spoken && num >= 1.5;
					bool flag = (double)(Environment.TickCount64 - started) / 1000.0 >= 20.0 && num >= 3.0;
					if (num2 || flag)
					{
						wrapWatch.Stop();
						FinishAndBuildPlan(revealAfter: true);
					}
				}
			}
		};
		wrapWatch.Start();
		this.Changed?.Invoke();
	}

	public void FinishAndBuildPlan(bool revealAfter)
	{
		if (IsPlanInFlight || HasBuiltPlan)
		{
			return;
		}
		wrapWatch?.Stop();
		IReadOnlyList<InterviewLine> readOnlyList = Transcript;
		if (!readOnlyList.Any((InterviewLine l) => l.Role == "user") && !UsesNewAgentsUiDemoPlan)
		{
			End(founded: false);
			return;
		}
		IsPlanInFlight = true;
		ClickyAnalytics.TrackProductEvent("onboarding_starter_plan_started", null, null, new JsonObject((JsonNodeOptions?)null)
		{
			["interview_id"] = InterviewId,
			["answer_count"] = AnswerCount
		});
		PortraitAppearance appearance = Appearance;
		End(founded: true);
		if (revealAfter)
		{
			WasCompleted = true;
			this.Completed?.Invoke();
		}
		this.Changed?.Invoke();
		RunPlanAsync(readOnlyList, appearance);
	}

	private async Task RunPlanAsync(IReadOnlyList<InterviewLine> interview, PortraitAppearance initialAppearance)
	{
		bool landed = false;
		Stopwatch sw = Stopwatch.StartNew();
		string analyticsInterviewId = InterviewId;
		try
		{
			StarterPlanResponse plan;
			if (UsesNewAgentsUiDemoPlan)
			{
				StarterPlanResponse starterPlanResponse = NewAgentsUiDemoPlan.Plan();
				if ((object)starterPlanResponse != null)
				{
					Log.Info("interview", "new agents UI demo: founding the fixed starter set");
					plan = starterPlanResponse;
					goto IL_0619;
				}
			}
			RealStack r = real;
			if (r != null)
			{
				IntegrationCatalog.Apps apps = await IntegrationCatalog.FetchAsync(r);
				if (apps.All.Count == 0)
				{
					await Task.Delay(1000);
					apps = await IntegrationCatalog.FetchAsync(r);
				}
				IReadOnlyList<string> available = ((apps.All.Count > 0) ? apps.All : (availableIntegrations ?? Array.Empty<string>()));
				List<string> connected = apps.ConnectedLines.Select((string l) => l.Split(':')[0].Trim()).ToList();
				List<StarterPlanClickyInput> clickys = (from c in ClickyRoster.Shared.Clickys
					where !StarterCast.IsCast(c.Slug)
					select new StarterPlanClickyInput(c.Slug, c.Name, c.RoleText, c.OneLiner, Array.Empty<StarterPlanRoutine>(), Array.Empty<string>())).ToList();
				Log.Info("interview", $"starter plan: {interview.Count} lines, {interview.Count((InterviewLine l) => l.Role == "user")} answers, {available.Count} apps");
				try
				{
					plan = await r.Core.Worker.BuildStarterPlanAsync(new StarterPlanRequest(interview, connected, available, (clickys.Count > 0) ? clickys : null, null, true));
				}
				catch (Exception ex)
				{
					Log.Warn("interview", "starter plan attempt 1 failed: " + ex.Message + "; retrying");
					await Task.Delay(2000);
					plan = await r.Core.Worker.BuildStarterPlanAsync(new StarterPlanRequest(interview, connected, available, (clickys.Count > 0) ? clickys : null, null, true));
				}
			}
			else
			{
				await Task.Delay(1500);
				plan = SamplePlan();
			}
			goto IL_0619;
			IL_0619:
			LastPlan = plan;
			if ((object)plan != null)
			{
				landed = Found(plan, out List<string> foundedSlugs, out Dictionary<string, string> revealLines);
				FoundedSlugs = foundedSlugs;
				RevealLines = revealLines;
			}
			Log.Info("interview", $"starter plan landed={landed} in {sw.Elapsed.TotalSeconds:0.0} s: clickys={(plan?.Clickys?.Count).GetValueOrDefault()} cards={(plan?.SuggestedAgents?.Count).GetValueOrDefault()} goal=\"{plan?.GoalLine}\"");
		}
		catch (Exception ex2)
		{
			PlanError = ex2.Message;
			Log.Error("interview", "starter plan failed", ex2);
		}
		dq.TryEnqueue(delegate
		{
			ClickyAnalytics.TrackProductEvent("onboarding_starter_plan_finished", null, null, new JsonObject((JsonNodeOptions?)null)
			{
				["interview_id"] = analyticsInterviewId,
				["success"] = landed,
				["duration_seconds"] = sw.Elapsed.TotalSeconds,
				["clicky_count"] = FoundedSlugs.Count,
				["suggestion_count"] = ClickyRoster.Shared.StarterCards.Count
			});
			IsPlanInFlight = false;
			HasBuiltPlan = landed;
			if (landed)
			{
				Func<bool>? isOnInterviewStep = IsOnInterviewStep;
				if (isOnInterviewStep != null && isOnInterviewStep())
				{
					Sounds.Play("agent-done", 0.35);
					goto IL_0112;
				}
			}
			if (landed)
			{
				Log.Info("interview", "starter plan landed off the interview step: no chime");
			}
			goto IL_0112;
			IL_0112:
			this.PlanLanded?.Invoke(landed);
			this.Changed?.Invoke();
		});
	}

	private static bool Found(StarterPlanResponse plan, out List<string> foundedSlugs, out Dictionary<string, string> revealLines)
	{
		foundedSlugs = new List<string>();
		revealLines = new Dictionary<string, string>();
		List<StarterTaskCard> list = (from x in plan.SuggestedAgents ?? Array.Empty<StarterPlanSuggestion>()
			where x.Title.Length > 0 && x.Description.Length > 0 && x.AgentPrompt.Length > 0
			select new StarterTaskCard(x.Title, x.Description, x.AgentPrompt, null, x.Routine?.Title, x.Routine?.EveryMinutes, x.SuggestionId, x.Deliverable)).ToList();
		ClickyRoster.Shared.SetStarterPlan(list, string.IsNullOrWhiteSpace(plan.GoalLine) ? null : plan.GoalLine.Trim(), plan.AgentFamiliarity == "familiar");
		Log.Info("interview", $"starter plan landed cards={list.Count}");
		return list.Count > 0;
	}

	public void UseTranscriptForDrawStepTest(IReadOnlyList<InterviewLine> lines)
	{
		lock (gate)
		{
			transcript.Clear();
			transcript.AddRange(lines);
		}
	}

	private async Task PrefetchIntegrationsAsync()
	{
		if (real != null && availableIntegrations == null)
		{
			availableIntegrations = await FetchIntegrationsAsync();
		}
	}

	private async Task<IReadOnlyList<string>> FetchIntegrationsAsync()
	{
		try
		{
			JsonObject n2 = await real.Core.Worker.GetJsonObjectAsync("/agent/integrations");
			List<string> names = new List<string>();
			Walk(n2);
			return names.Distinct().Take(80).ToList();
			void Walk(JsonNode? n)
			{
				if (n is JsonObject jsonObject)
				{
					if (!(jsonObject["display_name"] is JsonValue jsonValue) || !jsonValue.TryGetValue<string>(out string value) || value.Length <= 0)
					{
						foreach (KeyValuePair<string, JsonNode> item in jsonObject)
						{
							Walk(item.Value);
						}
						return;
					}
					names.Add(value);
				}
				else if (n is JsonArray jsonArray)
				{
					foreach (JsonNode item2 in jsonArray)
					{
						Walk(item2);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("interview", "integrations: " + ex.Message);
			return Array.Empty<string>();
		}
	}

	private static StarterPlanResponse SamplePlan()
	{
		return new StarterPlanResponse(new StarterPlanClicky[3]
		{
			new StarterPlanClicky("hook", "Hook Lab", "Script coach", "Finds the hooks that make devlogs land and drafts yours.", new string[1] { "I can break down the hooks the biggest devlog channels open with." }, "Hook Lab studies what makes devlogs click.", new string[1] { "Watch the last 20 devlogs from the top indie channels and rank their hooks" }),
			new StarterPlanClicky("steam", "Steam Scout", "Store page scout", "Builds a Steam page that sells your demo.", new string[1] { "I can pull apart the best demo pages on Steam." }, "Steam Scout gets your demo page ready.", new string[1] { "Read the top 30 indie demo pages and make me a page checklist" }),
			new StarterPlanClicky("pulse", "Channel Pulse", "Growth tracker", "Keeps an eye on what's working on your channel.", new string[1] { "I can tell you every morning what moved on your channel." }, "Channel Pulse watches your numbers.", new string[1] { "Every morning, tell me which video gained the most subscribers" })
		}, new StarterPlanSuggestion[3]
		{
			new StarterPlanSuggestion("Rank the hooks of 20 top devlogs", "A verdict on which openings keep viewers, with three for your next video.", "Watch the last 20 devlogs from the biggest indie game channels and rank their hooks.", "chat", null, "hook", null, null),
			new StarterPlanSuggestion("Build your Steam page checklist", "A live page comparing the best indie demo pages, ready to copy.", "Read the top 30 indie demo pages on Steam and build a checklist page.", "page", null, "steam", null, null),
			new StarterPlanSuggestion("Daily channel pulse", "Every morning: what moved on your channel and why.", "Check which video gained the most subscribers since yesterday.", "routine", null, "pulse", new StarterPlanRoutine("Daily channel pulse", 1440), null)
		}, "unfamiliar", "Getting to 100k subscribers and shipping the Steam demo.");
	}

	private void After(double seconds, Action action)
	{
		DispatcherQueueTimer t = dq.CreateTimer().Rooted();
		t.Interval = TimeSpan.FromSeconds(seconds);
		t.IsRepeating = false;
		pendingTimers.Add(t);
		t.Tick += delegate
		{
			pendingTimers.Remove(t);
			action();
		};
		t.Start();
	}
}
