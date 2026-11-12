using System;
using System.Numerics;

namespace HeyClicky.Drawing3D;

internal sealed class CatmullRomCurve
{
	private readonly Vector3[] points;

	private readonly bool closed;

	private readonly float[] arcLengths;

	public CatmullRomCurve(Vector3[] points, bool closed)
	{
		this.points = points;
		this.closed = closed;
		arcLengths = new float[201];
		Vector3 value = Point(0f);
		float num = 0f;
		for (int i = 1; i <= 200; i++)
		{
			Vector3 vector = Point((float)i / 200f);
			num += Vector3.Distance(vector, value);
			arcLengths[i] = num;
			value = vector;
		}
	}

	public Vector3 Point(float t)
	{
		int count = points.Length;
		float num = (float)(count - ((!closed) ? 1 : 0)) * t;
		int num2 = (int)MathF.Floor(num);
		float weight = num - (float)num2;
		if (closed)
		{
			num2 += ((num2 <= 0) ? ((Math.Abs(num2) / count + 1) * count) : 0);
		}
		else if (weight == 0f && num2 == count - 1)
		{
			num2 = count - 2;
			weight = 1f;
		}
		Vector3 vector = Control(num2);
		Vector3 vector2 = Control(num2 + 1);
		Vector3 vector3 = ((closed || num2 > 0) ? Control(num2 - 1) : (vector - (vector2 - vector)));
		Vector3 vector4 = ((closed || num2 + 2 < count) ? Control(num2 + 2) : (vector2 - (vector - vector2)));
		float dt0 = MathF.Pow((vector3 - vector).LengthSquared(), 0.25f);
		float dt1 = MathF.Pow((vector - vector2).LengthSquared(), 0.25f);
		float dt2 = MathF.Pow((vector2 - vector4).LengthSquared(), 0.25f);
		if (dt1 < 0.0001f)
		{
			dt1 = 1f;
		}
		if (dt0 < 0.0001f)
		{
			dt0 = dt1;
		}
		if (dt2 < 0.0001f)
		{
			dt2 = dt1;
		}
		return new Vector3(Component(vector3.X, vector.X, vector2.X, vector4.X), Component(vector3.Y, vector.Y, vector2.Y, vector4.Y), Component(vector3.Z, vector.Z, vector2.Z, vector4.Z));
		float Component(float x0, float x1, float x2, float x3)
		{
			float num3 = (x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1;
			float num4 = (x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2;
			float num5 = num3 * dt1;
			float num6 = num4 * dt1;
			float num7 = num5;
			float num8 = -3f * x1 + 3f * x2 - 2f * num5 - num6;
			float num9 = 2f * x1 - 2f * x2 + num5 + num6;
			return x1 + num7 * weight + num8 * weight * weight + num9 * weight * weight * weight;
		}
		Vector3 Control(int index)
		{
			if (!closed)
			{
				return points[Math.Clamp(index, 0, count - 1)];
			}
			return points[(index % count + count) % count];
		}
	}

	public Vector3 PointAt(float u)
	{
		return Point(Parameter(u));
	}

	private float Parameter(float u)
	{
		float num = arcLengths[^1];
		if (num <= 0f)
		{
			return u;
		}
		float num2 = u * num;
		int num3 = 0;
		int num4 = arcLengths.Length - 1;
		while (num3 <= num4)
		{
			int num5 = (num3 + num4) / 2;
			if (arcLengths[num5] < num2)
			{
				num3 = num5 + 1;
				continue;
			}
			if (arcLengths[num5] > num2)
			{
				num4 = num5 - 1;
				continue;
			}
			num4 = num5;
			break;
		}
		int num6 = Math.Max(0, num4);
		if (num6 >= arcLengths.Length - 1)
		{
			return 1f;
		}
		float num7 = arcLengths[num6];
		float num8 = arcLengths[num6 + 1];
		float num9 = ((num8 > num7) ? ((num2 - num7) / (num8 - num7)) : 0f);
		return ((float)num6 + num9) / (float)(arcLengths.Length - 1);
	}

	public Vector3 TangentAt(float u)
	{
		float num = Parameter(u);
		float t = MathF.Max(0f, num - 0.0001f);
		float t2 = MathF.Min(1f, num + 0.0001f);
		Vector3 value = Point(t2) - Point(t);
		if (!(value.Length() > 0f))
		{
			return Vector3.UnitY;
		}
		return Vector3.Normalize(value);
	}

	public (Vector3[] tangents, Vector3[] normals, Vector3[] binormals) FrenetFrames(int segments, bool closedCurve)
	{
		Vector3[] array = new Vector3[segments + 1];
		for (int i = 0; i <= segments; i++)
		{
			array[i] = TangentAt((float)i / (float)segments);
		}
		Vector3[] array2 = new Vector3[segments + 1];
		Vector3[] array3 = new Vector3[segments + 1];
		Vector3 vector = array[0];
		Vector3 vector2 = Vector3.Abs(vector);
		Vector3 vector3 = Vector3.UnitX;
		float num = vector2.X;
		if (vector2.Y <= num)
		{
			num = vector2.Y;
			vector3 = Vector3.UnitY;
		}
		if (vector2.Z <= num)
		{
			vector3 = Vector3.UnitZ;
		}
		Vector3 vector4 = Vector3.Normalize(Vector3.Cross(vector, vector3));
		array2[0] = Vector3.Cross(vector, vector4);
		array3[0] = Vector3.Cross(vector, array2[0]);
		for (int j = 1; j <= segments; j++)
		{
			array2[j] = array2[j - 1];
			Vector3 value = Vector3.Cross(array[j - 1], array[j]);
			if (value.Length() > float.Epsilon)
			{
				float angle = MathF.Acos(Math.Clamp(Vector3.Dot(array[j - 1], array[j]), -1f, 1f));
				array2[j] = Vector3.Transform(array2[j], Quaternion.CreateFromAxisAngle(Vector3.Normalize(value), angle));
			}
			array3[j] = Vector3.Cross(array[j], array2[j]);
		}
		if (closedCurve)
		{
			float num2 = MathF.Acos(Math.Clamp(Vector3.Dot(array2[0], array2[segments]), -1f, 1f)) / (float)segments;
			if (Vector3.Dot(array[0], Vector3.Cross(array2[0], array2[segments])) > 0f)
			{
				num2 = 0f - num2;
			}
			for (int k = 1; k <= segments; k++)
			{
				array2[k] = Vector3.Transform(array2[k], Quaternion.CreateFromAxisAngle(array[k], num2 * (float)k));
				array3[k] = Vector3.Cross(array[k], array2[k]);
			}
		}
		return (tangents: array, normals: array2, binormals: array3);
	}
}
