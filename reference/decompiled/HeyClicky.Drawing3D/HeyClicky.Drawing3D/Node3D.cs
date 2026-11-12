using System;
using System.Collections.Generic;
using System.Numerics;

namespace HeyClicky.Drawing3D;

public sealed class Node3D
{
	public readonly string Id;

	public readonly string Kind;

	public readonly Mesh3D? Mesh;

	public readonly Material3D Material;

	public Node3D? Parent;

	public List<Node3D> Children = new List<Node3D>();

	public Vector3 BasePosition;

	public Quaternion BaseRotation;

	public Vector3 Position;

	public Quaternion Rotation;

	public readonly float FullScale;

	public float GrowScale = 0.0001f;

	public float PulseScale = 1f;

	public float Highlight;

	public Vector3? TickPosition;

	public Quaternion? TickRotation;

	public float TickScale = 1f;

	public bool IsHiddenByTick;

	public float TickGlow;

	public Vector3? TickColor;

	public string? Name;

	public Vector3? FramingPosition;

	public Quaternion? FramingRotation;

	internal float[] FramingSamples = Array.Empty<float>();

	internal Matrix4x4? FramingSamplesMatrix;

	internal Vector3 FramingBoxMinimum;

	internal Vector3 FramingBoxMaximum;

	internal float[] FramingReaches = Array.Empty<float>();

	internal Vector4? FramingReachesKey;

	public readonly double GrowStart;

	public readonly double GrowDuration;

	public readonly int CreationOrder;

	public Node3D(string id, string kind, Mesh3D? mesh, Material3D material, Vector3 position, Quaternion rotation, float scale, double growStart, double growDuration, int creationOrder)
	{
		Id = id;
		Kind = kind;
		Mesh = mesh;
		Material = material;
		BasePosition = position;
		BaseRotation = rotation;
		Position = position;
		Rotation = rotation;
		FullScale = scale;
		GrowStart = growStart;
		GrowDuration = growDuration;
		CreationOrder = creationOrder;
	}

	public Matrix4x4 LocalMatrix(bool atFullSize)
	{
		return Math3D.Compose(Position, Rotation, atFullSize ? (FullScale * TickScale) : (FullScale * TickScale * GrowScale * PulseScale));
	}

	public Matrix4x4 WorldMatrix(bool atFullSize)
	{
		Matrix4x4 matrix4x = LocalMatrix(atFullSize);
		if (Parent != null)
		{
			return matrix4x * Parent.WorldMatrix(atFullSize);
		}
		return matrix4x;
	}

	public Matrix4x4 FramingWorldMatrix()
	{
		Vector3? framingPosition = FramingPosition;
		Matrix4x4 matrix4x;
		if (!framingPosition.HasValue)
		{
			Quaternion? framingRotation = FramingRotation;
			if (!framingRotation.HasValue)
			{
				matrix4x = LocalMatrix(atFullSize: true);
				goto IL_0079;
			}
		}
		matrix4x = Math3D.Compose(FramingPosition ?? Position, FramingRotation ?? Rotation, FullScale * TickScale);
		goto IL_0079;
		IL_0079:
		Matrix4x4 matrix4x2 = matrix4x;
		if (Parent != null)
		{
			return matrix4x2 * Parent.FramingWorldMatrix();
		}
		return matrix4x2;
	}
}
