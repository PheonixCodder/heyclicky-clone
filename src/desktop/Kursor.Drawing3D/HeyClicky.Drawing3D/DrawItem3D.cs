using System.Numerics;

namespace HeyClicky.Drawing3D;

public struct DrawItem3D
{
	public Node3D Node;

	public Mesh3D Mesh;

	public Material3D Material;

	public Matrix4x4 ModelMatrix;

	public float Depth;

	public int Order;
}
