using Vortice.Direct3D11;

namespace HeyClicky.Drawing3D;

public interface IStageSurface
{
	void Present(ID3D11Texture2D frame, int width, int height);

	void WaitUntilReady(int milliseconds)
	{
	}
}
