using System;

namespace HeyClicky;

public interface IScreenDrawingCanvas : IDisposable
{
	bool IsShowing { get; }

	bool IsShowingDrawing => IsShowing;

	bool IsStreamingNarration
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	bool IsSwingingFiguresToFront => false;

	bool LatestAnswerPointsAtScreen => false;

	bool HidesLoneSources
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	int InkedMarkCount => 0;

	bool IsDrawingFadedAway => false;

	DisplayInfo? DrawingDisplay => null;

	DateTime? LatestLineReleasedAt => null;

	DateTime? NarrationAudioEndsAt
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	event EventHandler<DrawingAction>? ActionRequested;

	event EventHandler<string>? UserActedOnScreen
	{
		add
		{
		}
		remove
		{
		}
	}

	void BeginProgram(ScreenCaptureFrame screenshot, string cursorColorHex);

	DrawingFeedResult Feed(string codeSoFar, bool done);

	void ReleaseLine(int index);

	void SetLineLength(int index, double seconds);

	void Clear(bool animated = true);

	void Finish()
	{
	}

	bool CanContinueDrawing(ScreenCaptureFrame capture)
	{
		return false;
	}

	DrawingContinuation? ContinueStream()
	{
		return null;
	}

	string? DrawingStateForModel()
	{
		return null;
	}

	ScreenCaptureFrame? CaptureDrawingForModel(ScreenCaptureFrame capture)
	{
		return null;
	}

	void ForgetWhetherLatestAnswerPointsAtScreen()
	{
	}

	void PlayClearSoundIfShowingDrawing()
	{
	}

	void ShowLiveCaption(string line, double revealSeconds, double holdSeconds)
	{
	}

	void FadeDrawingKeepingAskButtons()
	{
	}

	void SetDrawingOpacity(double opacity, double durationSeconds)
	{
	}

	void SetBelowNotch(bool isBelow)
	{
	}
}
