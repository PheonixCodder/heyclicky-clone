namespace HeyClicky.App.Onboarding;

public static class DrawStepStagePolicy
{
	public static bool PanelStepsAside(bool keysHeld, bool narrationStreaming, bool showingDrawing, bool sawThisPressNarrate)
	{
		if (!keysHeld)
		{
			if (!narrationStreaming)
			{
				return showingDrawing && sawThisPressNarrate;
			}
			return true;
		}
		return false;
	}
}
