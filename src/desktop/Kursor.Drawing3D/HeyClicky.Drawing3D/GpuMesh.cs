using System;
using Vortice.Direct3D11;

namespace HeyClicky.Drawing3D;

internal sealed class GpuMesh : IDisposable
{
	public ID3D11Buffer? Vertices;

	public ID3D11Buffer? Indices;

	public uint IndexCount;

	public unsafe static GpuMesh? Create(ID3D11Device device, Mesh3D mesh)
	{
		if (mesh.Vertices.Length == 0 || mesh.Indices.Length == 0)
		{
			return null;
		}
		GpuMesh obj = new GpuMesh
		{
			IndexCount = (uint)mesh.Indices.Length
		};
		fixed (Vertex3D* vertices = mesh.Vertices)
		{
			obj.Vertices = device.CreateBuffer(new BufferDescription((uint)(mesh.Vertices.Length * 48), BindFlags.VertexBuffer, ResourceUsage.Immutable), new SubresourceData(vertices));
		}
		fixed (uint* indices = mesh.Indices)
		{
			obj.Indices = device.CreateBuffer(new BufferDescription((uint)(mesh.Indices.Length * 4), BindFlags.IndexBuffer, ResourceUsage.Immutable), new SubresourceData(indices));
		}
		return obj;
	}

	public void Dispose()
	{
		Vertices?.Dispose();
		Vertices = null;
		Indices?.Dispose();
		Indices = null;
		GC.SuppressFinalize(this);
	}

	~GpuMesh()
	{
		Vertices?.Dispose();
		Indices?.Dispose();
	}
}
