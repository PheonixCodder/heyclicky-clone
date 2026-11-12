using System.Collections.Generic;
using System.Numerics;

namespace HeyClicky.Drawing3D;

internal sealed class MeshBuilder
{
	public readonly List<Vertex3D> Vertices = new List<Vertex3D>();

	public readonly List<uint> Indices = new List<uint>();

	public void Append(MeshBuilder other)
	{
		uint count = (uint)Vertices.Count;
		Vertices.AddRange(other.Vertices);
		foreach (uint index in other.Indices)
		{
			Indices.Add(index + count);
		}
	}

	public void OrientTrianglesToNormals()
	{
		for (int i = 0; i + 2 < Indices.Count; i += 3)
		{
			Vertex3D vertex3D = Vertices[(int)Indices[i]];
			Vertex3D vertex3D2 = Vertices[(int)Indices[i + 1]];
			Vertex3D vertex3D3 = Vertices[(int)Indices[i + 2]];
			if (Vector3.Dot(Vector3.Cross(vertex3D2.Position - vertex3D.Position, vertex3D3.Position - vertex3D.Position), vertex3D.Normal + vertex3D2.Normal + vertex3D3.Normal) < 0f)
			{
				List<uint> indices = Indices;
				int index = i + 1;
				List<uint> indices2 = Indices;
				int index2 = i + 2;
				uint value = Indices[i + 2];
				uint value2 = Indices[i + 1];
				indices[index] = value;
				indices2[index2] = value2;
			}
		}
	}

	public void ComputeVertexNormalsFromWinding()
	{
		Vector3[] array = new Vector3[Vertices.Count];
		for (int i = 0; i + 2 < Indices.Count; i += 3)
		{
			int num = (int)Indices[i];
			int num2 = (int)Indices[i + 1];
			int num3 = (int)Indices[i + 2];
			Vector3 vector = Vector3.Cross(Vertices[num3].Position - Vertices[num2].Position, Vertices[num].Position - Vertices[num2].Position);
			array[num] += vector;
			array[num2] += vector;
			array[num3] += vector;
		}
		for (int j = 0; j < Vertices.Count; j++)
		{
			float num4 = array[j].Length();
			Vertex3D value = Vertices[j];
			value.Normal = ((num4 > 0f) ? (array[j] / num4) : Vector3.UnitY);
			Vertices[j] = value;
		}
	}

	public Mesh3D ToMesh()
	{
		return new Mesh3D
		{
			Vertices = Vertices.ToArray(),
			Indices = Indices.ToArray()
		};
	}
}
