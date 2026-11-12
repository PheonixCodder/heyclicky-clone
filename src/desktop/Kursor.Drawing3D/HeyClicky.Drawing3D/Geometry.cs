using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace HeyClicky.Drawing3D;

internal static class Geometry
{
	private static void GridIndices(List<uint> indices, int columns, int rows)
	{
		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < columns; j++)
			{
				uint num = (uint)(i * (columns + 1) + j);
				uint num2 = num + (uint)(columns + 1);
				indices.Add(num);
				indices.Add(num2);
				indices.Add(num + 1);
				indices.Add(num2);
				indices.Add(num2 + 1);
				indices.Add(num + 1);
			}
		}
	}

	public static Mesh3D Sphere(float radius, int widthSegments = 64, int heightSegments = 40)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		for (int i = 0; i <= heightSegments; i++)
		{
			float num = (float)i / (float)heightSegments;
			float x = num * (float)Math.PI;
			for (int j = 0; j <= widthSegments; j++)
			{
				float num2 = (float)j / (float)widthSegments;
				float x2 = num2 * 2f * (float)Math.PI;
				Vector3 vector = new Vector3((0f - MathF.Cos(x2)) * MathF.Sin(x), MathF.Cos(x), MathF.Sin(x2) * MathF.Sin(x));
				meshBuilder.Vertices.Add(new Vertex3D(vector * radius, vector, new Vector2(num2, 1f - num)));
			}
		}
		GridIndices(meshBuilder.Indices, widthSegments, heightSegments);
		meshBuilder.OrientTrianglesToNormals();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Box(Vector3 size)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		Vector3 vector = size / 2f;
		(Vector3, Vector3, Vector3)[] array = new(Vector3, Vector3, Vector3)[6]
		{
			(new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f), new Vector3(0f, 1f, 0f)),
			(new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 0f)),
			(new Vector3(0f, 1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f)),
			(new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f)),
			(new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f)),
			(new Vector3(0f, 0f, -1f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f))
		};
		(float, float)[] array2 = new(float, float)[4]
		{
			(0f, 0f),
			(1f, 0f),
			(1f, 1f),
			(0f, 1f)
		};
		(Vector3, Vector3, Vector3)[] array3 = array;
		for (int i = 0; i < array3.Length; i++)
		{
			(Vector3, Vector3, Vector3) tuple = array3[i];
			Vector3 item = tuple.Item1;
			Vector3 item2 = tuple.Item2;
			Vector3 item3 = tuple.Item3;
			uint count = (uint)meshBuilder.Vertices.Count;
			(float, float)[] array4 = array2;
			for (int j = 0; j < array4.Length; j++)
			{
				(float, float) tuple2 = array4[j];
				float item4 = tuple2.Item1;
				float item5 = tuple2.Item2;
				Vector3 position = (item + item2 * (item4 * 2f - 1f) + item3 * (item5 * 2f - 1f)) * vector;
				meshBuilder.Vertices.Add(new Vertex3D(position, item, new Vector2(item4, item5)));
			}
			meshBuilder.Indices.AddRange(new _003C_003Ez__ReadOnlyArray<uint>(new uint[6]
			{
				count,
				count + 1,
				count + 2,
				count,
				count + 2,
				count + 3
			}));
		}
		meshBuilder.OrientTrianglesToNormals();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Cylinder(float topRadius, float bottomRadius, float height, int radialSegments)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		float num = height / 2f;
		float y = (bottomRadius - topRadius) / MathF.Max(height, 1E-06f);
		for (int i = 0; i <= 1; i++)
		{
			float num2 = i;
			float num3 = num2 * (bottomRadius - topRadius) + topRadius;
			for (int j = 0; j <= radialSegments; j++)
			{
				float num4 = (float)j / (float)radialSegments;
				float x = num4 * 2f * (float)Math.PI;
				float num5 = MathF.Sin(x);
				float num6 = MathF.Cos(x);
				meshBuilder.Vertices.Add(new Vertex3D(new Vector3(num3 * num5, (0f - num2) * height + num, num3 * num6), Vector3.Normalize(new Vector3(num5, y, num6)), new Vector2(num4, 1f - num2)));
			}
		}
		GridIndices(meshBuilder.Indices, radialSegments, 1);
		meshBuilder.OrientTrianglesToNormals();
		(float, float, float)[] array = new(float, float, float)[2]
		{
			(topRadius, num, 1f),
			(bottomRadius, 0f - num, -1f)
		};
		for (int k = 0; k < array.Length; k++)
		{
			var (num7, y2, num8) = array[k];
			if (!(num7 <= 0f))
			{
				MeshBuilder meshBuilder2 = new MeshBuilder();
				Vector3 normal = new Vector3(0f, num8, 0f);
				meshBuilder2.Vertices.Add(new Vertex3D(new Vector3(0f, y2, 0f), normal, new Vector2(0.5f, 0.5f)));
				for (int l = 0; l <= radialSegments; l++)
				{
					float x2 = (float)l / (float)radialSegments * 2f * (float)Math.PI;
					meshBuilder2.Vertices.Add(new Vertex3D(new Vector3(num7 * MathF.Sin(x2), y2, num7 * MathF.Cos(x2)), normal, new Vector2(MathF.Cos(x2) * 0.5f + 0.5f, MathF.Sin(x2) * 0.5f * num8 + 0.5f)));
				}
				for (int m = 0; m < radialSegments; m++)
				{
					meshBuilder2.Indices.AddRange(new _003C_003Ez__ReadOnlyArray<uint>(new uint[3]
					{
						0u,
						(uint)(m + 1),
						(uint)(m + 2)
					}));
				}
				meshBuilder2.OrientTrianglesToNormals();
				meshBuilder.Append(meshBuilder2);
			}
		}
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Torus(float radius, float tube, int radialSegments = 32, int tubularSegments = 128)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		for (int i = 0; i <= radialSegments; i++)
		{
			for (int j = 0; j <= tubularSegments; j++)
			{
				float x = (float)j / (float)tubularSegments * 2f * (float)Math.PI;
				float x2 = (float)i / (float)radialSegments * 2f * (float)Math.PI;
				Vector3 vector = new Vector3((radius + tube * MathF.Cos(x2)) * MathF.Cos(x), (radius + tube * MathF.Cos(x2)) * MathF.Sin(x), tube * MathF.Sin(x2));
				Vector3 vector2 = new Vector3(radius * MathF.Cos(x), radius * MathF.Sin(x), 0f);
				meshBuilder.Vertices.Add(new Vertex3D(vector, Vector3.Normalize(vector - vector2), new Vector2((float)j / (float)tubularSegments, (float)i / (float)radialSegments)));
			}
		}
		GridIndices(meshBuilder.Indices, tubularSegments, radialSegments);
		meshBuilder.OrientTrianglesToNormals();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Ring(float inner, float outer, int thetaSegments = 160)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		Vector3 unitY = Vector3.UnitY;
		for (int i = 0; i <= 1; i++)
		{
			float num = ((i == 0) ? inner : outer);
			for (int j = 0; j <= thetaSegments; j++)
			{
				float x = (float)j / (float)thetaSegments * 2f * (float)Math.PI;
				meshBuilder.Vertices.Add(new Vertex3D(new Vector3(num * MathF.Cos(x), 0f, (0f - num) * MathF.Sin(x)), unitY, new Vector2((MathF.Cos(x) * num / outer + 1f) / 2f, (MathF.Sin(x) * num / outer + 1f) / 2f)));
			}
		}
		GridIndices(meshBuilder.Indices, thetaSegments, 1);
		meshBuilder.OrientTrianglesToNormals();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Capsule(float radius, float length, int capSegments = 8, int radialSegments = 24)
	{
		List<Vector2> list = new List<Vector2>();
		float num = length / 2f;
		for (int i = 0; i <= capSegments; i++)
		{
			float x = -(float)Math.PI / 2f + (float)i / (float)capSegments * ((float)Math.PI / 2f);
			list.Add(new Vector2(radius * MathF.Cos(x), 0f - num + radius * MathF.Sin(x)));
		}
		for (int j = 0; j <= capSegments; j++)
		{
			float x2 = (float)j / (float)capSegments * ((float)Math.PI / 2f);
			list.Add(new Vector2(radius * MathF.Cos(x2), num + radius * MathF.Sin(x2)));
		}
		return Lathe(list, radialSegments);
	}

	public static Mesh3D Lathe(List<Vector2> profile, int segments = 72)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		if (profile.Count < 2)
		{
			return meshBuilder.ToMesh();
		}
		List<Vector2> list = new List<Vector2>();
		Vector2 vector = Vector2.Zero;
		for (int i = 0; i < profile.Count; i++)
		{
			if (i == profile.Count - 1)
			{
				list.Add(vector);
				continue;
			}
			Vector2 vector2 = profile[i + 1] - profile[i];
			Vector2 vector3 = new Vector2(vector2.Y, 0f - vector2.X);
			if (vector3.Length() > 0f)
			{
				vector3 = Vector2.Normalize(vector3);
			}
			list.Add((i == 0) ? vector3 : Vector2.Normalize(vector + vector3 + new Vector2(1E-07f, 0f)));
			vector = vector3;
		}
		for (int j = 0; j <= segments; j++)
		{
			float x = (float)j / (float)segments * 2f * (float)Math.PI;
			float num = MathF.Sin(x);
			float num2 = MathF.Cos(x);
			for (int k = 0; k < profile.Count; k++)
			{
				Vector2 vector4 = profile[k];
				Vector2 vector5 = list[k];
				meshBuilder.Vertices.Add(new Vertex3D(new Vector3(vector4.X * num, vector4.Y, vector4.X * num2), Vector3.Normalize(new Vector3(vector5.X * num, vector5.Y, vector5.X * num2) + new Vector3(0f, 1E-07f, 0f)), new Vector2((float)j / (float)segments, (float)k / (float)(profile.Count - 1))));
			}
		}
		int count = profile.Count;
		for (int l = 0; l < segments; l++)
		{
			for (int m = 0; m < count - 1; m++)
			{
				uint num3 = (uint)(l * count + m);
				uint num4 = num3 + (uint)count;
				meshBuilder.Indices.AddRange(new _003C_003Ez__ReadOnlyArray<uint>(new uint[6]
				{
					num3,
					num4,
					num3 + 1,
					num4,
					num4 + 1,
					num3 + 1
				}));
			}
		}
		meshBuilder.OrientTrianglesToNormals();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Tube(List<Vector3> points, float radius, bool closed, int radialSegments = 14)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		if (points.Count < 2)
		{
			return meshBuilder.ToMesh();
		}
		CatmullRomCurve catmullRomCurve = new CatmullRomCurve(points.ToArray(), closed);
		int num = Math.Max(64, points.Count * 3);
		(Vector3[] tangents, Vector3[] normals, Vector3[] binormals) tuple = catmullRomCurve.FrenetFrames(num, closed);
		Vector3[] item = tuple.normals;
		Vector3[] item2 = tuple.binormals;
		for (int i = 0; i <= num; i++)
		{
			int num2 = ((!closed || i != num) ? i : 0);
			Vector3 vector = catmullRomCurve.PointAt((float)num2 / (float)num);
			Vector3 vector2 = item[num2];
			Vector3 vector3 = item2[num2];
			for (int j = 0; j <= radialSegments; j++)
			{
				float x = (float)j / (float)radialSegments * 2f * (float)Math.PI;
				float num3 = MathF.Sin(x);
				float num4 = 0f - MathF.Cos(x);
				Vector3 vector4 = Vector3.Normalize(vector2 * num4 + vector3 * num3);
				meshBuilder.Vertices.Add(new Vertex3D(vector + vector4 * radius, vector4, new Vector2((float)i / (float)num, (float)j / (float)radialSegments)));
			}
		}
		for (int k = 1; k <= num; k++)
		{
			for (int l = 1; l <= radialSegments; l++)
			{
				uint num5 = (uint)((radialSegments + 1) * (k - 1) + (l - 1));
				uint num6 = (uint)((radialSegments + 1) * k + (l - 1));
				uint num7 = (uint)((radialSegments + 1) * k + l);
				uint num8 = (uint)((radialSegments + 1) * (k - 1) + l);
				meshBuilder.Indices.AddRange(new _003C_003Ez__ReadOnlyArray<uint>(new uint[6] { num5, num6, num8, num6, num7, num8 }));
			}
		}
		meshBuilder.OrientTrianglesToNormals();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Surface(List<List<Vector3>> grid)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		int count = grid.Count;
		int columns = ((count > 0) ? grid[0].Count : 0);
		if (count < 2 || columns < 2 || grid.Any((List<Vector3> r) => r.Count != columns))
		{
			return meshBuilder.ToMesh();
		}
		for (int i = 0; i < count; i++)
		{
			for (int j = 0; j < columns; j++)
			{
				meshBuilder.Vertices.Add(new Vertex3D(grid[i][j], Vector3.UnitY, new Vector2((float)j / (float)(columns - 1), (float)i / (float)(count - 1))));
			}
		}
		for (int k = 0; k < count - 1; k++)
		{
			for (int l = 0; l < columns - 1; l++)
			{
				uint num = (uint)(k * columns + l);
				uint num2 = num + (uint)columns;
				meshBuilder.Indices.AddRange(new _003C_003Ez__ReadOnlyArray<uint>(new uint[6]
				{
					num,
					num2,
					num + 1,
					num2,
					num2 + 1,
					num + 1
				}));
			}
		}
		meshBuilder.ComputeVertexNormalsFromWinding();
		return meshBuilder.ToMesh();
	}

	public static Mesh3D Extrude(string pathData, float depth, float unit)
	{
		MeshBuilder meshBuilder = new MeshBuilder();
		foreach (List<Vector2> item3 in PathFlattener.Outlines(pathData, unit))
		{
			if (item3.Count < 3)
			{
				continue;
			}
			List<Vector2> list = new List<Vector2>(item3);
			if (list[0] == list[list.Count - 1])
			{
				list.RemoveAt(list.Count - 1);
			}
			if (list.Count < 3)
			{
				continue;
			}
			if (SignedArea(list) < 0f)
			{
				list.Reverse();
			}
			List<int> list2 = EarClipping.Triangulate(list);
			(float, Vector3)[] array = new(float, Vector3)[2]
			{
				(depth, Vector3.UnitZ),
				(0f, -Vector3.UnitZ)
			};
			for (int i = 0; i < array.Length; i++)
			{
				(float, Vector3) tuple = array[i];
				float item = tuple.Item1;
				Vector3 item2 = tuple.Item2;
				MeshBuilder meshBuilder2 = new MeshBuilder();
				foreach (Vector2 item4 in list)
				{
					meshBuilder2.Vertices.Add(new Vertex3D(new Vector3(item4.X, item4.Y, item), item2, item4));
				}
				foreach (int item5 in list2)
				{
					meshBuilder2.Indices.Add((uint)item5);
				}
				meshBuilder2.OrientTrianglesToNormals();
				meshBuilder.Append(meshBuilder2);
			}
			MeshBuilder meshBuilder3 = new MeshBuilder();
			for (int j = 0; j < list.Count; j++)
			{
				Vector2 vector = list[j];
				Vector2 vector2 = list[(j + 1) % list.Count];
				Vector2 vector3 = vector2 - vector;
				Vector3 normal = Vector3.Normalize(new Vector3(vector3.Y, 0f - vector3.X, 0f) + new Vector3(0f, 0f, 1E-07f));
				uint count = (uint)meshBuilder3.Vertices.Count;
				(Vector2, float)[] array2 = new(Vector2, float)[4]
				{
					(vector, 0f),
					(vector2, 0f),
					(vector2, depth),
					(vector, depth)
				};
				for (int i = 0; i < array2.Length; i++)
				{
					var (vector4, num) = array2[i];
					meshBuilder3.Vertices.Add(new Vertex3D(new Vector3(vector4.X, vector4.Y, num), normal, new Vector2(vector4.X, num)));
				}
				meshBuilder3.Indices.AddRange(new _003C_003Ez__ReadOnlyArray<uint>(new uint[6]
				{
					count,
					count + 1,
					count + 2,
					count,
					count + 2,
					count + 3
				}));
			}
			meshBuilder3.OrientTrianglesToNormals();
			meshBuilder.Append(meshBuilder3);
		}
		if (meshBuilder.Vertices.Count == 0)
		{
			return meshBuilder.ToMesh();
		}
		Vector3 vector5 = meshBuilder.Vertices[0].Position;
		Vector3 vector6 = vector5;
		foreach (Vertex3D vertex in meshBuilder.Vertices)
		{
			vector5 = Vector3.Min(vector5, vertex.Position);
			vector6 = Vector3.Max(vector6, vertex.Position);
		}
		Vector3 vector7 = (vector5 + vector6) / 2f;
		for (int k = 0; k < meshBuilder.Vertices.Count; k++)
		{
			Vertex3D value = meshBuilder.Vertices[k];
			value.Position -= vector7;
			meshBuilder.Vertices[k] = value;
		}
		return meshBuilder.ToMesh();
	}

	public static float SignedArea(List<Vector2> polygon)
	{
		float num = 0f;
		for (int i = 0; i < polygon.Count; i++)
		{
			Vector2 vector = polygon[i];
			Vector2 vector2 = polygon[(i + 1) % polygon.Count];
			num += vector.X * vector2.Y - vector2.X * vector.Y;
		}
		return num / 2f;
	}

	public static void PaintAlong(Mesh3D mesh, List<Vector4> colors, bool alongLength)
	{
		if (colors.Count == 0 || mesh.Vertices.Length == 0)
		{
			return;
		}
		float num = float.PositiveInfinity;
		float num2 = float.NegativeInfinity;
		if (!alongLength)
		{
			Vertex3D[] vertices = mesh.Vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				Vertex3D vertex3D = vertices[i];
				num = MathF.Min(num, vertex3D.Position.Y);
				num2 = MathF.Max(num2, vertex3D.Position.Y);
			}
		}
		for (int j = 0; j < mesh.Vertices.Length; j++)
		{
			ref Vertex3D reference = ref mesh.Vertices[j];
			float num3 = Math.Clamp(alongLength ? reference.UV.X : ((num2 > num) ? ((reference.Position.Y - num) / (num2 - num)) : 0f), 0f, 0.9999f) * (float)(colors.Count - 1);
			int num4 = (int)num3;
			int index = Math.Min(colors.Count - 1, num4 + 1);
			reference.Color = Vector4.Lerp(colors[num4], colors[index], num3 - (float)num4);
		}
	}
}
