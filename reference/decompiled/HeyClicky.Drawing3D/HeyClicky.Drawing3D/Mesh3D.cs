using System;
using System.Numerics;

namespace HeyClicky.Drawing3D;

public sealed class Mesh3D
{
	private sealed class Box
	{
		public Vector3 Minimum;

		public Vector3 Maximum;
	}

	public readonly struct BoundingPoints
	{
		private readonly Mesh3D mesh;

		private readonly int maximumCount;

		public BoundingPoints(Mesh3D mesh, int maximumCount)
		{
			this.mesh = mesh;
			this.maximumCount = maximumCount;
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(mesh, maximumCount);
		}
	}

	public struct Enumerator
	{
		private readonly Vector3[]? samples;

		private readonly Vertex3D[] vertices;

		private readonly int kept;

		private readonly int step;

		private readonly int room;

		private readonly int count;

		private int position;

		private int emitted;

		public Vector3 Current { get; private set; }

		public Enumerator(Mesh3D mesh, int maximumCount)
		{
			Vector3[] samplePoints = mesh.SamplePoints;
			samples = ((samplePoints != null && samplePoints.Length > 0) ? samplePoints : null);
			vertices = mesh.Vertices;
			position = -1;
			emitted = 0;
			Current = default(Vector3);
			if (samples != null)
			{
				count = samples.Length;
				if (count <= maximumCount)
				{
					kept = count;
					step = 1;
					room = 0;
				}
				else
				{
					kept = Math.Min(6, maximumCount);
					int num = count - kept;
					room = Math.Max(0, maximumCount - kept);
					step = Math.Max(1, num / Math.Max(1, room));
				}
			}
			else
			{
				count = vertices.Length;
				kept = 0;
				room = int.MaxValue;
				step = Math.Max(1, count / Math.Max(1, maximumCount));
			}
		}

		public bool MoveNext()
		{
			if (samples != null)
			{
				if (position + 1 < kept)
				{
					position++;
					Current = samples[position];
					return true;
				}
				if (position + 1 == kept && kept == count)
				{
					return false;
				}
				int num = ((position < kept) ? kept : (position + step));
				if (num >= count || emitted >= room)
				{
					return false;
				}
				position = num;
				emitted++;
				Current = samples[position];
				return true;
			}
			int num2 = ((position >= 0) ? (position + step) : 0);
			if (num2 >= count)
			{
				return false;
			}
			position = num2;
			Current = vertices[position].Position;
			return true;
		}
	}

	public Vertex3D[] Vertices = Array.Empty<Vertex3D>();

	public uint[] Indices = Array.Empty<uint>();

	public Vector3[]? SamplePoints;

	internal GpuMesh? Gpu;

	private Box? box;

	public bool IsEmpty => Indices.Length == 0;

	public long ByteSize
	{
		get
		{
			long num = (long)Vertices.Length * 48L + (long)Indices.Length * 4L;
			Vector3[]? samplePoints = SamplePoints;
			return num + (long)((samplePoints != null) ? samplePoints.Length : 0) * 16L;
		}
	}

	internal (Vector3 Minimum, Vector3 Maximum)? LocalBounds
	{
		get
		{
			if (this.box == null && Vertices.Length != 0)
			{
				Box box = new Box
				{
					Minimum = Vertices[0].Position,
					Maximum = Vertices[0].Position
				};
				Span<Vertex3D> span = Vertices.AsSpan();
				for (int i = 0; i < span.Length; i++)
				{
					ref Vertex3D reference = ref span[i];
					box.Minimum = Vector3.Min(box.Minimum, reference.Position);
					box.Maximum = Vector3.Max(box.Maximum, reference.Position);
				}
				this.box = box;
			}
			Box box2 = this.box;
			if (box2 == null)
			{
				return null;
			}
			return (box2.Minimum, box2.Maximum);
		}
	}

	public BoundingPoints Bounding(int maximumCount)
	{
		return new BoundingPoints(this, maximumCount);
	}
}
