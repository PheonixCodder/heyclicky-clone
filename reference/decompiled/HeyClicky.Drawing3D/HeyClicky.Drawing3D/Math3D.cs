using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace HeyClicky.Drawing3D;

internal static class Math3D
{
	public const float Pi = (float)Math.PI;

	public static Quaternion EulerXYZ(Vector3 degrees)
	{
		Vector3 vector = degrees * ((float)Math.PI / 180f);
		return Quaternion.CreateFromAxisAngle(Vector3.UnitX, vector.X) * Quaternion.CreateFromAxisAngle(Vector3.UnitY, vector.Y) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, vector.Z);
	}

	public static Quaternion Rotation(Vector3 from, Vector3 to)
	{
		Vector3 vector = Vector3.Normalize(from);
		Vector3 vector2 = Vector3.Normalize(to);
		float num = Vector3.Dot(vector, vector2);
		if (num < -0.999999f)
		{
			Vector3 value = Vector3.Cross(Vector3.UnitX, vector);
			if (value.Length() < 1E-06f)
			{
				value = Vector3.Cross(Vector3.UnitY, vector);
			}
			return Quaternion.CreateFromAxisAngle(Vector3.Normalize(value), (float)Math.PI);
		}
		Vector3 vectorPart = Vector3.Cross(vector, vector2);
		return Quaternion.Normalize(new Quaternion(vectorPart, 1f + num));
	}

	public static Matrix4x4 Compose(Vector3 position, Quaternion rotation, float uniformScale)
	{
		return Matrix4x4.CreateScale(uniformScale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(position);
	}

	public static Matrix4x4 LookAt(Vector3 eye, Vector3 target, Vector3 up)
	{
		Vector3 vector = Vector3.Normalize(eye - target);
		Vector3 value = Vector3.Cross(up, vector);
		if (value.Length() < 1E-06f)
		{
			value = Vector3.UnitX;
		}
		value = Vector3.Normalize(value);
		Vector3 vector2 = Vector3.Cross(vector, value);
		return new Matrix4x4(value.X, vector2.X, vector.X, 0f, value.Y, vector2.Y, vector.Y, 0f, value.Z, vector2.Z, vector.Z, 0f, 0f - Vector3.Dot(value, eye), 0f - Vector3.Dot(vector2, eye), 0f - Vector3.Dot(vector, eye), 1f);
	}

	public static Matrix4x4 LookAt(Vector3 eye, Vector3 target)
	{
		return LookAt(eye, target, Vector3.UnitY);
	}

	public static Matrix4x4 Perspective(float verticalFieldOfViewDegrees, float aspect, float near, float far)
	{
		float num = 1f / MathF.Tan(verticalFieldOfViewDegrees * (float)Math.PI / 360f);
		float m = num / MathF.Max(aspect, 0.0001f);
		float num2 = far - near;
		return new Matrix4x4(m, 0f, 0f, 0f, 0f, num, 0f, 0f, 0f, 0f, (0f - far) / num2, -1f, 0f, 0f, (0f - far) * near / num2, 0f);
	}

	public static Matrix4x4 Orthographic(float halfWidth, float halfHeight, float near, float far)
	{
		return new Matrix4x4(1f / halfWidth, 0f, 0f, 0f, 0f, 1f / halfHeight, 0f, 0f, 0f, 0f, -1f / (far - near), 0f, 0f, 0f, (0f - near) / (far - near), 1f);
	}

	public static void NormalColumns(in Matrix4x4 model, out Vector4 c0, out Vector4 c1, out Vector4 c2)
	{
		Matrix4x4 matrix = model;
		matrix.M41 = 0f;
		matrix.M42 = 0f;
		matrix.M43 = 0f;
		if (!Matrix4x4.Invert(matrix, out var result))
		{
			result = Matrix4x4.Identity;
		}
		c0 = new Vector4(result.M11, result.M21, result.M31, 0f);
		c1 = new Vector4(result.M12, result.M22, result.M32, 0f);
		c2 = new Vector4(result.M13, result.M23, result.M33, 0f);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector3 TransformPoint(in Matrix4x4 m, Vector3 p)
	{
		Vector4 vector = Vector4.Transform(new Vector4(p, 1f), m);
		float num = ((vector.W == 0f) ? 1f : vector.W);
		return new Vector3(vector.X, vector.Y, vector.Z) / num;
	}

	public static Vector3 Translation(in Matrix4x4 m)
	{
		return new Vector3(m.M41, m.M42, m.M43);
	}

	public static float LinearFromSRGB(float channel)
	{
		if (!(channel < 0.04045f))
		{
			return MathF.Pow(channel * 0.9478673f + 0.0521327f, 2.4f);
		}
		return channel * 0.07739938f;
	}

	public static Vector4? LinearColor(string? css)
	{
		if (css != null)
		{
			Vector4? vector = CssColor.Parse(css, strict: true);
			if (vector.HasValue)
			{
				Vector4 valueOrDefault = vector.GetValueOrDefault();
				return new Vector4(LinearFromSRGB(valueOrDefault.X), LinearFromSRGB(valueOrDefault.Y), LinearFromSRGB(valueOrDefault.Z), valueOrDefault.W);
			}
		}
		return null;
	}

	public static Vector3 Mix(Vector3 a, Vector3 b, float t)
	{
		return a + (b - a) * t;
	}

	public static float Clamp01(float v)
	{
		if (!(v < 0f))
		{
			if (!(v > 1f))
			{
				return v;
			}
			return 1f;
		}
		return 0f;
	}

	public static double Clamp01(double v)
	{
		if (!(v < 0.0))
		{
			if (!(v > 1.0))
			{
				return v;
			}
			return 1.0;
		}
		return 0.0;
	}

	public static float Smoothstep(float a, float b, float x)
	{
		float num = Clamp01((x - a) / (b - a));
		return num * num * (3f - 2f * num);
	}
}
