namespace HeyClicky.Platform.Input;

internal interface ILowLevelInputSink
{
	void OnKey(in LowLevelKeyEvent keyEvent);

	bool ShouldSwallowKey(in LowLevelKeyEvent keyEvent)
	{
		return false;
	}

	void OnMouseButton(LowLevelMouseButtonKind kind, int x, int y, uint timeMs);

	void OnResyncRequested()
	{
	}

	void OnRawMouse(ushort buttonFlags, bool isPhysical, nuint extraInfo)
	{
	}
}
