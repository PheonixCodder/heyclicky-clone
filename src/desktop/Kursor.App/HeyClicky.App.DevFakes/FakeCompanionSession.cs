using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HeyClicky.App.DevFakes;

public sealed class FakeCompanionSession : ICompanionSession
{
	private CancellationTokenSource? cts;

	public const string SampleAnswer = "Here's what I see on your screen:\n\n1. The **notch** sits top-center and grows on a spring.\n2. Everything outside it stays *click-through*.\n\nTry `Ctrl+Alt` to talk, or double-tap **Ctrl** to type. More in the [docs](https://heyclicky.com).";

	public VoiceState State { get; private set; }

	public string ResponseText { get; private set; } = "";

	public INotchPresenter? Presenter { get; set; }

	public event EventHandler? StateChanged;

	public event EventHandler<string>? UserTranscriptUpdated;

	public event EventHandler<string>? ResponseTextUpdated;

	public event EventHandler<CompanionError>? Failed;

	private void Set(VoiceState s)
	{
		State = s;
		this.StateChanged?.Invoke(this, EventArgs.Empty);
	}

	public Task BeginPushToTalkAsync()
	{
		cts?.Cancel();
		Set(VoiceState.Listening);
		return Task.CompletedTask;
	}

	public Task EndPushToTalkAsync()
	{
		this.UserTranscriptUpdated?.Invoke(this, "what's on my screen?");
		return RunTurn("Here's what I see on your screen:\n\n1. The **notch** sits top-center and grows on a spring.\n2. Everything outside it stays *click-through*.\n\nTry `Ctrl+Alt` to talk, or double-tap **Ctrl** to type. More in the [docs](https://heyclicky.com).", voice: true);
	}

	public Task SubmitTextAsync(string text)
	{
		Presenter?.Present(new NotchPresentation.TextResponse(text, "", IsThinking: true));
		return RunTurn("Here's what I see on your screen:\n\n1. The **notch** sits top-center and grows on a spring.\n2. Everything outside it stays *click-through*.\n\nTry `Ctrl+Alt` to talk, or double-tap **Ctrl** to type. More in the [docs](https://heyclicky.com).", voice: false);
	}

	public Task AskFromDrawingAsync(string text, string trigger)
	{
		return RunTurn("Here's what I see on your screen:\n\n1. The **notch** sits top-center and grows on a spring.\n2. Everything outside it stays *click-through*.\n\nTry `Ctrl+Alt` to talk, or double-tap **Ctrl** to type. More in the [docs](https://heyclicky.com).", voice: true);
	}

	public void Cancel()
	{
		cts?.Cancel();
		Set(VoiceState.Idle);
	}

	private async Task RunTurn(string answer, bool voice)
	{
		cts?.Cancel();
		CancellationTokenSource c = (cts = new CancellationTokenSource());
		try
		{
			Set(VoiceState.Processing);
			await Task.Delay(900, c.Token);
			Set(VoiceState.Responding);
			StringBuilder sb = new StringBuilder();
			string[] array = answer.Split(' ');
			foreach (string value in array)
			{
				sb.Append((sb.Length == 0) ? "" : " ").Append(value);
				ResponseText = sb.ToString();
				this.ResponseTextUpdated?.Invoke(this, ResponseText);
				await Task.Delay(voice ? 70 : 35, c.Token);
			}
			Set(VoiceState.Idle);
		}
		catch (OperationCanceledException)
		{
		}
	}
}
