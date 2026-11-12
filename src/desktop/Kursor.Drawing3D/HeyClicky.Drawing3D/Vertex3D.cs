using System.Numerics;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing3D;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vertex3D
{
	public Vector3 Position;

	public Vector3 Normal;

	public Vector2 UV;

	public Vector4 Color;

	public const int Stride = 48;

	public Vertex3D(Vector3 position, Vector3 normal, Vector2 uv)
	{
		Position = position;
		Normal = normal;
		UV = uv;
		Color = Vector4.One;
	}
}
