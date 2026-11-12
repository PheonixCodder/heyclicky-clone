using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace HeyClicky.Drawing3D;

public sealed class Scene3D
{
	public struct CameraSettings
	{
		public float Distance;

		public float Tilt;

		public float FieldOfView;

		public float Spin;

		public float Zoom;

		public bool Fits;

		public static CameraSettings Default
		{
			get
			{
				CameraSettings result = default(CameraSettings);
				result.Distance = 6f;
				result.Tilt = 15f;
				result.FieldOfView = 35f;
				result.Spin = 0.3f;
				result.Fits = true;
				result.Zoom = 1f;
				return result;
			}
		}
	}

	private sealed class Motion
	{
		public required Node3D Node;

		public required string Kind;

		public Vector3 Axis;

		public double Period;

		public float Amount;

		public Vector3 Center;

		public CatmullRomCurve? Path;

		public double Start;

		public Vector3 BasePosition;

		public Quaternion BaseRotation;

		public Vector3? SpinPivot;

		public bool Once;

		public Vector3? TargetPosition;

		public Quaternion? TargetRotation;

		public bool? SetShown;

		public float? SetGlow;

		public Vector3? SetColor;

		public float? SetScale;

		public bool IsFinished(double time)
		{
			if (!(Kind == "set"))
			{
				if (Once || Kind == "tween")
				{
					return time >= Start + Period + 1.5;
				}
				return false;
			}
			return time >= Start;
		}
	}

	private sealed class Label
	{
		public required string Id;

		public required string Text;

		public Vector3 Point;

		public string? FollowedNodeId;

		public double Start;

		public string? CarrierNodeId;

		public Vector3 CarrierOriginThen;
	}

	private sealed class OutermostFirst : IComparer<Node3D>
	{
		public Vector3 Center;

		public int Compare(Node3D? a, Node3D? b)
		{
			return FarthestCornerDistanceSquared(b, Center).CompareTo(FarthestCornerDistanceSquared(a, Center));
		}
	}

	public readonly string Id;

	public readonly Rect2 Rect;

	private CameraSettings camera;

	private readonly Dictionary<string, Node3D> nodes = new Dictionary<string, Node3D>();

	private readonly List<Node3D> rootNodes = new List<Node3D>();

	private readonly List<Motion> motions = new List<Motion>();

	private readonly List<Label> labels = new List<Label>();

	private Vector3 orbitTarget;

	private float orbitRadius;

	private float orbitTheta;

	private float orbitPhi;

	private float dragThetaDelta;

	private float dragPhiDelta;

	private bool isUserDragging;

	private (Vector3 center, float distance)? framing;

	public const double SettledBandHeight = 2.0;

	private bool needsRefit;

	private double lastTravelRefitTime = double.NegativeInfinity;

	private double? lastUpdateTime;

	public float SpinScale = 1f;

	private float restingYaw;

	private bool hasRestingView;

	private bool hasViewerFacingCut;

	private double? swayStartTime;

	public static readonly float CutawaySwayAmplitude = EnvFloat("CANVAS_3D_CUT_SWAY", 28f) * (float)Math.PI / 180f;

	private readonly Dictionary<string, double> highlightStarts = new Dictionary<string, double>();

	private readonly List<string> scratchIds = new List<string>();

	private const double HighlightDuration = 0.7;

	private (double start, float from, float to, float startSpeed, double duration)? frontSwing;

	private readonly List<Vector2> samplePointScratch = new List<Vector2>();

	private readonly HashSet<string> tickedPartIds = new HashSet<string>();

	private double lastTickRefitTime = double.NegativeInfinity;

	private (Vector3 minimum, Vector3 maximum, float[] reaches)? tickEnvelope;

	private readonly Dictionary<int, Node3D?> nodeByNumber = new Dictionary<int, Node3D>();

	private string? nodeByNumberPrefix;

	private readonly List<Node3D> refitParts = new List<Node3D>();

	private readonly List<Node3D> refitSampled = new List<Node3D>();

	private readonly List<Node3D> refitOrdered = new List<Node3D>();

	private readonly (Vector3 right, Vector3 up, Vector3 forward)[] refitTurnedAxes = new(Vector3, Vector3, Vector3)[8];

	private readonly Vector3[] refitDirections = new Vector3[32];

	private float[] refitReaches = Array.Empty<float>();

	private readonly OutermostFirst outermostOrder = new OutermostFirst();

	private Comparison<Node3D>? outermostComparison;

	public const float ClosestZoom = 1.1f;

	private const int FewestPartsWorthSkipping = 12;

	private readonly Dictionary<string, (Vector3 point, double time)> labelAnchorWorld = new Dictionary<string, (Vector3, double)>();

	public CameraSettings Camera => camera;

	public (Vector3 Minimum, Vector3 Maximum)? ObjectBox { get; private set; }

	public Rect2? SettledExtentTarget { get; private set; }

	public Rect2? SettledExtent { get; private set; }

	public float[] SettledLeft { get; private set; } = Array.Empty<float>();

	public float[] SettledRight { get; private set; } = Array.Empty<float>();

	public int FramingRevision { get; private set; }

	public float NearPlane { get; private set; } = 0.05f;

	public float FarPlane { get; private set; } = 500f;

	public bool IsAnimatingGrowth
	{
		get
		{
			foreach (Node3D value in nodes.Values)
			{
				if (value.GrowScale < 0.999f)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool HasParts => nodes.Count > 0;

	public bool TurnsOnItsOwn => camera.Spin != 0f;

	public float RestingYaw
	{
		get
		{
			return restingYaw;
		}
		set
		{
			if (value != restingYaw)
			{
				needsRefit = true;
			}
			restingYaw = value;
		}
	}

	public float CameraDistance => orbitRadius;

	public bool HasActiveHighlight => highlightStarts.Count > 0;

	public bool IsSwingingToFront
	{
		get
		{
			(double, float, float, float, double)? tuple = frontSwing;
			return tuple.HasValue;
		}
	}

	private float CurrentTurnSpeed => dragThetaDelta * 0.05f * 60f;

	public bool HasSettledDrag
	{
		get
		{
			if (!isUserDragging && MathF.Abs(dragThetaDelta) < 0.03f)
			{
				return MathF.Abs(dragPhiDelta) < 0.03f;
			}
			return false;
		}
	}

	public bool HasSettledMotion
	{
		get
		{
			double valueOrDefault = lastUpdateTime.GetValueOrDefault();
			foreach (Motion motion in motions)
			{
				if (!motion.IsFinished(valueOrDefault))
				{
					return false;
				}
			}
			if ((camera.Spin == 0f || SpinScale == 0f) && MathF.Abs(dragThetaDelta) < 0.0001f && MathF.Abs(dragPhiDelta) < 0.0001f)
			{
				(double, float, float, float, double)? tuple = frontSwing;
				return !tuple.HasValue;
			}
			return false;
		}
	}

	public double GrowthEndTime
	{
		get
		{
			double num = 0.0;
			bool flag = false;
			foreach (Node3D value in nodes.Values)
			{
				num = (flag ? Math.Max(num, value.GrowStart + value.GrowDuration) : (value.GrowStart + value.GrowDuration));
				flag = true;
			}
			return num;
		}
	}

	public Vector3 Eye => orbitTarget + orbitRadius * new Vector3(MathF.Sin(orbitPhi) * MathF.Sin(orbitTheta), MathF.Cos(orbitPhi), MathF.Sin(orbitPhi) * MathF.Cos(orbitTheta));

	public Matrix4x4 ViewMatrix => Math3D.LookAt(Eye, orbitTarget);

	public Matrix4x4 ProjectionMatrix => Math3D.Perspective(camera.FieldOfView, (float)(Rect.Width / Rect.Height), NearPlane, FarPlane);

	internal static float EnvFloat(string name, float fallback)
	{
		if (!float.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return fallback;
		}
		return result;
	}

	private static float RoundAway(float v)
	{
		return MathF.Round(v, MidpointRounding.AwayFromZero);
	}

	public void Highlight(string partId, double time)
	{
		if (nodes.ContainsKey(partId))
		{
			highlightStarts[partId] = time;
		}
	}

	public string? FollowedPartId(string labelId)
	{
		foreach (Label label in labels)
		{
			if (label.Id == labelId)
			{
				return label.FollowedNodeId;
			}
		}
		return null;
	}

	public void SwingToFront(double time, float yaw, double radiansPerSecond, double quickest, double longest)
	{
		if (!isUserDragging)
		{
			if (!hasRestingView && camera.Spin == 0f)
			{
				needsRefit = true;
			}
			hasRestingView = true;
			float currentTurnSpeed = CurrentTurnSpeed;
			float num = (float)Math.PI * 2f;
			float num2 = RoundAway((orbitTheta + currentTurnSpeed * 0.5f - yaw) / num) * num + yaw;
			double num3 = MathF.Abs(num2 - orbitTheta);
			dragThetaDelta = 0f;
			if (num3 <= 0.002)
			{
				orbitTheta = num2;
				frontSwing = null;
			}
			else
			{
				frontSwing = (time, orbitTheta, num2, currentTurnSpeed, Math.Min(longest, Math.Max(quickest, num3 / radiansPerSecond)));
			}
		}
	}

	public Rect2? CurrentExtent()
	{
		ProjectedSamplePoints(samplePointScratch);
		if (samplePointScratch.Count == 0)
		{
			return null;
		}
		Rect2 value = new Rect2(samplePointScratch[0].X, samplePointScratch[0].Y, 0.0, 0.0);
		foreach (Vector2 item in samplePointScratch)
		{
			value = value.Union(item.X, item.Y);
		}
		return value;
	}

	public Scene3D(string id, Rect2 rect, JsonElement cameraOp)
	{
		Id = id;
		Rect = new Rect2(rect.X, rect.Y, Math.Max(1.0, rect.Width), Math.Max(1.0, rect.Height));
		CameraSettings @default = CameraSettings.Default;
		if (cameraOp.ValueKind == JsonValueKind.Object)
		{
			float? num = cameraOp.Number("distance");
			if (num.HasValue)
			{
				float valueOrDefault = num.GetValueOrDefault();
				@default.Distance = valueOrDefault;
			}
			num = cameraOp.Number("tilt");
			if (num.HasValue)
			{
				float valueOrDefault2 = num.GetValueOrDefault();
				@default.Tilt = valueOrDefault2;
			}
			num = cameraOp.Number("fov");
			if (num.HasValue)
			{
				float valueOrDefault3 = num.GetValueOrDefault();
				@default.FieldOfView = valueOrDefault3;
			}
			num = cameraOp.Number("spin");
			if (num.HasValue)
			{
				float valueOrDefault4 = num.GetValueOrDefault();
				@default.Spin = valueOrDefault4;
			}
			@default.Fits = cameraOp.Bool("fit") != false;
		}
		camera = @default;
		orbitRadius = @default.Distance;
		orbitPhi = (float)Math.PI / 2f - @default.Tilt * (float)Math.PI / 180f;
	}

	public void AddPart(JsonElement op, double start, double duration, Func<string, string, Mesh3D?> kernelMesh)
	{
		string text = op.Str("id");
		if (text == null)
		{
			return;
		}
		string text2 = op.Str("kind") ?? "";
		JsonElement jsonElement = op.Obj("params");
		JsonElement jsonElement2 = op.Obj("material");
		bool flag = op.IsTrue("three");
		Material3D material = (flag ? Material3D.Studio(jsonElement2) : Material3D.Plain(jsonElement2));
		Mesh3D mesh = null;
		if (text2 != "group" && flag)
		{
			Mesh3D mesh3D = kernelMesh(text2, op.Str("recipe") ?? "{}");
			if (mesh3D == null || mesh3D.IsEmpty)
			{
				return;
			}
			mesh = mesh3D;
		}
		else if (text2 != "group")
		{
			Mesh3D mesh3D2 = BuildMesh(text2, jsonElement);
			if (mesh3D2 == null || mesh3D2.IsEmpty)
			{
				return;
			}
			List<Vector4> list = new List<Vector4>();
			foreach (string item in jsonElement2.Strings("colors"))
			{
				Vector4? vector = Math3D.LinearColor(item);
				if (vector.HasValue)
				{
					Vector4 valueOrDefault = vector.GetValueOrDefault();
					list.Add(valueOrDefault);
				}
			}
			if (list.Count > 0)
			{
				Geometry.PaintAlong(mesh3D2, list, text2 == "tube" || text2 == "surface");
			}
			mesh = mesh3D2;
		}
		Vector3 position = op.Vec3("at") ?? op.Vec3("pos") ?? Vector3.Zero;
		Quaternion rotation = Math3D.EulerXYZ(op.Vec3("rotate") ?? op.Vec3("rotate3") ?? Vector3.Zero);
		if (text2 == "cylinder" && !flag)
		{
			Vector3? vector2 = jsonElement.Vec3("from");
			if (vector2.HasValue)
			{
				Vector3 valueOrDefault2 = vector2.GetValueOrDefault();
				vector2 = jsonElement.Vec3("to");
				if (vector2.HasValue)
				{
					Vector3 valueOrDefault3 = vector2.GetValueOrDefault();
					if (Vector3.Distance(valueOrDefault2, valueOrDefault3) > 0f)
					{
						position = (valueOrDefault2 + valueOrDefault3) / 2f;
						rotation = Math3D.Rotation(Vector3.UnitY, valueOrDefault3 - valueOrDefault2);
					}
				}
			}
		}
		float scale = op.Number("scale") ?? op.Number("scale3") ?? 1f;
		if (nodes.TryGetValue(text, out Node3D value))
		{
			Node3D node3D = new Node3D(text, text2, mesh, material, position, rotation, scale, value.GrowStart, value.GrowDuration, value.CreationOrder)
			{
				Parent = value.Parent,
				Name = (op.Str("name") ?? value.Name),
				Children = value.Children
			};
			foreach (Node3D child in value.Children)
			{
				child.Parent = node3D;
			}
			Node3D parent = value.Parent;
			if (parent != null)
			{
				int num = parent.Children.IndexOf(value);
				if (num >= 0)
				{
					parent.Children[num] = node3D;
				}
			}
			else
			{
				int num2 = rootNodes.IndexOf(value);
				if (num2 >= 0)
				{
					rootNodes[num2] = node3D;
				}
			}
			foreach (Motion motion in motions)
			{
				if (motion.Node == value)
				{
					motion.Node = node3D;
				}
			}
			nodes[text] = node3D;
			needsRefit = true;
		}
		else
		{
			Node3D node3D2 = new Node3D(text, text2, mesh, material, position, rotation, scale, start, Math.Max(0.0001, duration), nodes.Count)
			{
				Name = op.Str("name")
			};
			string text3 = op.Str("parent");
			if (text3 != null && nodes.TryGetValue(text3, out Node3D value2))
			{
				node3D2.Parent = value2;
				value2.Children.Add(node3D2);
			}
			else
			{
				rootNodes.Add(node3D2);
			}
			nodes[text] = node3D2;
			needsRefit = true;
		}
	}

	public void AddMotion(JsonElement op, double start)
	{
		string text = op.Str("target");
		if (text == null || !nodes.TryGetValue(text, out Node3D value))
		{
			return;
		}
		string text2 = op.Str("kind") ?? "spin";
		Vector3 value2 = op.Vec3("axis") ?? Vector3.UnitY;
		value2 = ((value2.Length() > 0f) ? Vector3.Normalize(value2) : Vector3.UnitY);
		List<Vector3> list = op.Points3("points");
		bool flag = op.IsTrue("once");
		if (flag && list.Count > 0 && list[0].Length() < 0.0001f && value.Position.Length() > 0.001f)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i] += value.Position;
			}
		}
		Motion obj = new Motion
		{
			Node = value,
			Kind = text2,
			Axis = value2,
			Period = Math.Max(0.05, op.Double("period") ?? 6.0),
			Amount = (op.Number("amount") ?? 0.1f),
			Center = (op.Vec3("center") ?? Vector3.Zero),
			Path = ((list.Count >= 2) ? new CatmullRomCurve(list.ToArray(), !flag && op.Bool("closed") != false) : null),
			Start = start,
			BasePosition = value.Position,
			BaseRotation = value.Rotation,
			SpinPivot = ((text2 == "spin") ? SpinPivot(value, value2) : ((Vector3?)null)),
			Once = flag,
			TargetPosition = ((text2 == "tween") ? op.Vec3("to") : ((Vector3?)null))
		};
		Quaternion? targetRotation;
		if (text2 == "tween")
		{
			Vector3? vector = op.Vec3("rot");
			if (vector.HasValue)
			{
				Vector3 valueOrDefault = vector.GetValueOrDefault();
				targetRotation = Math3D.EulerXYZ(valueOrDefault);
				goto IL_02a1;
			}
		}
		targetRotation = null;
		goto IL_02a1;
		IL_03d6:
		float? setScale;
		obj.SetScale = (float?)setScale;
		Motion motion = obj;
		motions.Add(motion);
		bool flag2;
		switch (text2)
		{
		case "orbit":
		case "path":
		case "bob":
			flag2 = true;
			break;
		default:
			flag2 = false;
			break;
		}
		if (flag2)
		{
			LetLabelsRide(value);
		}
		if (text2 == "tween")
		{
			Vector3? vector = motion.TargetPosition;
			if (vector.HasValue)
			{
				Vector3 valueOrDefault2 = vector.GetValueOrDefault();
				value.FramingPosition = valueOrDefault2;
			}
			Quaternion? targetRotation2 = motion.TargetRotation;
			if (targetRotation2.HasValue)
			{
				Quaternion valueOrDefault3 = targetRotation2.GetValueOrDefault();
				value.FramingRotation = valueOrDefault3;
			}
			needsRefit = true;
		}
		return;
		IL_0385:
		Vector3? setColor;
		obj.SetColor = (Vector3?)setColor;
		if (text2 == "set")
		{
			float? num = op.Number("scale");
			if (num.HasValue)
			{
				float valueOrDefault4 = num.GetValueOrDefault();
				setScale = MathF.Max(0.0001f, valueOrDefault4);
				goto IL_03d6;
			}
		}
		setScale = null;
		goto IL_03d6;
		IL_0321:
		float? setGlow;
		obj.SetGlow = (float?)setGlow;
		if (text2 == "set")
		{
			Vector4? vector2 = Math3D.LinearColor(op.Str("color"));
			if (vector2.HasValue)
			{
				Vector4 valueOrDefault5 = vector2.GetValueOrDefault();
				setColor = new Vector3(valueOrDefault5.X, valueOrDefault5.Y, valueOrDefault5.Z);
				goto IL_0385;
			}
		}
		setColor = null;
		goto IL_0385;
		IL_02a1:
		obj.TargetRotation = targetRotation;
		obj.SetShown = ((text2 == "set") ? op.Bool("show") : ((bool?)null));
		if (text2 == "set")
		{
			float? num = op.Number("glow");
			if (num.HasValue)
			{
				float valueOrDefault6 = num.GetValueOrDefault();
				setGlow = Math.Clamp(valueOrDefault6, 0f, 1f);
				goto IL_0321;
			}
		}
		setGlow = null;
		goto IL_0321;
	}

	private static Vector3? SpinPivot(Node3D node, Vector3 axis)
	{
		Matrix4x4 result = Matrix4x4.Identity;
		Node3D parent = node.Parent;
		if (parent != null)
		{
			Matrix4x4.Invert(parent.WorldMatrix(atFullSize: true), out result);
		}
		Vector3 vector = new Vector3(float.PositiveInfinity);
		Vector3 vector2 = new Vector3(float.NegativeInfinity);
		foreach (Node3D item in Subtree(node))
		{
			if (item.Mesh != null)
			{
				Matrix4x4 m = item.WorldMatrix(atFullSize: true) * result;
				Mesh3D.Enumerator enumerator2 = item.Mesh.Bounding(200).GetEnumerator();
				while (enumerator2.MoveNext())
				{
					Vector3 current2 = enumerator2.Current;
					Vector3 value = Math3D.TransformPoint(in m, current2);
					vector = Vector3.Min(vector, value);
					vector2 = Vector3.Max(vector2, value);
				}
			}
		}
		if (vector.X > vector2.X)
		{
			return null;
		}
		float value2 = (vector2 - vector).Length() * 0.05f + 0.0001f;
		vector -= new Vector3(value2);
		vector2 += new Vector3(value2);
		Vector3 basePosition = node.BasePosition;
		float num = float.NegativeInfinity;
		float num2 = float.PositiveInfinity;
		for (int i = 0; i < 3; i++)
		{
			float num3 = axis[i];
			float num4 = basePosition[i];
			if (MathF.Abs(num3) < 1E-06f)
			{
				if (num4 < vector[i] || num4 > vector2[i])
				{
					return (vector + vector2) / 2f;
				}
			}
			else
			{
				float x = (vector[i] - num4) / num3;
				float y = (vector2[i] - num4) / num3;
				num = MathF.Max(num, MathF.Min(x, y));
				num2 = MathF.Min(num2, MathF.Max(x, y));
			}
		}
		if (!(num <= num2))
		{
			return (vector + vector2) / 2f;
		}
		return null;
	}

	private void LetLabelsRide(Node3D moving)
	{
		Vector3 carrierOriginThen = Math3D.TransformPoint(moving.WorldMatrix(atFullSize: true), Vector3.Zero);
		List<Node3D> list = Subtree(moving);
		foreach (Label label in labels)
		{
			if (label.FollowedNodeId == null && label.CarrierNodeId == null)
			{
				Node3D node3D = PartOwning(label.Point);
				if (node3D != null && list.Contains(node3D))
				{
					label.CarrierNodeId = moving.Id;
					label.CarrierOriginThen = carrierOriginThen;
				}
			}
		}
	}

	private Node3D? PartOwning(Vector3 point)
	{
		Node3D node3D = null;
		float num = 0f;
		foreach (Node3D value2 in nodes.Values)
		{
			if (value2.Mesh == null)
			{
				continue;
			}
			Matrix4x4 m = value2.WorldMatrix(atFullSize: true);
			Vector3 vector = new Vector3(float.PositiveInfinity);
			Vector3 vector2 = new Vector3(float.NegativeInfinity);
			Mesh3D.Enumerator enumerator2 = value2.Mesh.Bounding(200).GetEnumerator();
			while (enumerator2.MoveNext())
			{
				Vector3 current2 = enumerator2.Current;
				Vector3 value = Math3D.TransformPoint(in m, current2);
				vector = Vector3.Min(vector, value);
				vector2 = Vector3.Max(vector2, value);
			}
			if (vector.X > vector2.X)
			{
				continue;
			}
			Vector3 vector3 = vector2 - vector;
			float num2 = MathF.Max(0.08f, 0.08f * MathF.Max(vector3.X, MathF.Max(vector3.Y, vector3.Z)));
			if (!(point.X < vector.X - num2) && !(point.Y < vector.Y - num2) && !(point.Z < vector.Z - num2) && !(point.X > vector2.X + num2) && !(point.Y > vector2.Y + num2) && !(point.Z > vector2.Z + num2))
			{
				Vector3 vector4 = vector3 + new Vector3(2f * num2);
				float num3 = vector4.X * vector4.Y * vector4.Z;
				if (node3D == null || num3 < num)
				{
					node3D = value2;
					num = num3;
				}
			}
		}
		return node3D;
	}

	private Vector3 Ride(Label label)
	{
		if (label.CarrierNodeId == null || !nodes.TryGetValue(label.CarrierNodeId, out Node3D value))
		{
			return Vector3.Zero;
		}
		return Math3D.TransformPoint(value.WorldMatrix(atFullSize: true), Vector3.Zero) - label.CarrierOriginThen;
	}

	public void ApplyCamera(JsonElement op)
	{
		float? num = op.Number("distance");
		if (num.HasValue)
		{
			float valueOrDefault = num.GetValueOrDefault();
			camera.Distance = valueOrDefault;
			orbitRadius = valueOrDefault;
		}
		num = op.Number("tilt");
		if (num.HasValue)
		{
			float valueOrDefault2 = num.GetValueOrDefault();
			camera.Tilt = valueOrDefault2;
			orbitPhi = (float)Math.PI / 2f - valueOrDefault2 * (float)Math.PI / 180f;
			needsRefit = true;
		}
		num = op.Number("fov");
		if (num.HasValue)
		{
			float valueOrDefault3 = num.GetValueOrDefault();
			camera.FieldOfView = valueOrDefault3;
			needsRefit = true;
		}
		num = op.Number("spin");
		if (num.HasValue)
		{
			float valueOrDefault4 = num.GetValueOrDefault();
			camera.Spin = valueOrDefault4;
		}
		Vector3? vector = op.Vec3("target");
		if (vector.HasValue)
		{
			Vector3 valueOrDefault5 = vector.GetValueOrDefault();
			orbitTarget = valueOrDefault5;
		}
		num = op.Number("zoom");
		if (num.HasValue)
		{
			float valueOrDefault6 = num.GetValueOrDefault();
			if (valueOrDefault6 > 0f)
			{
				camera.Zoom = Math.Clamp(valueOrDefault6, 0.5f, 3f);
				needsRefit = true;
			}
		}
	}

	public void ApplyTickPoses(ReadOnlySpan<float> packed, string partPrefix)
	{
		if (nodeByNumberPrefix != partPrefix)
		{
			nodeByNumber.Clear();
			nodeByNumberPrefix = partPrefix;
		}
		for (int i = 0; i + 14 <= packed.Length; i += 14)
		{
			int num = (int)packed[i];
			int num2 = (int)packed[i + 1];
			if (num <= 0)
			{
				continue;
			}
			if (!nodeByNumber.TryGetValue(num, out Node3D value) || value == null || !nodes.ContainsKey(value.Id) || nodes[value.Id] != value)
			{
				nodes.TryGetValue(partPrefix + num.ToString(CultureInfo.InvariantCulture), out value);
				nodeByNumber[num] = value;
			}
			if (value != null)
			{
				tickedPartIds.Add(value.Id);
				if ((num2 & 1) != 0)
				{
					value.TickPosition = new Vector3(packed[i + 2], packed[i + 3], packed[i + 4]);
					value.FramingPosition = null;
				}
				if ((num2 & 2) != 0)
				{
					value.TickRotation = Math3D.EulerXYZ(new Vector3(packed[i + 5], packed[i + 6], packed[i + 7]));
					value.FramingRotation = null;
				}
				if ((num2 & 4) != 0)
				{
					value.TickScale = MathF.Max(0.0001f, packed[i + 8]);
				}
				if ((num2 & 8) != 0)
				{
					value.IsHiddenByTick = packed[i + 9] < 0.5f;
				}
				if ((num2 & 0x10) != 0)
				{
					value.TickGlow = Math.Clamp(packed[i + 10], 0f, 1f);
				}
				if ((num2 & 0x20) != 0)
				{
					value.TickColor = new Vector3(Math3D.LinearFromSRGB(packed[i + 11]), Math3D.LinearFromSRGB(packed[i + 12]), Math3D.LinearFromSRGB(packed[i + 13]));
				}
			}
		}
		if (tickedPartIds.Count <= 0)
		{
			return;
		}
		double? num3 = lastUpdateTime;
		if (num3.HasValue)
		{
			double valueOrDefault = num3.GetValueOrDefault();
			if (!(valueOrDefault - lastTickRefitTime >= 0.5))
			{
				return;
			}
		}
		lastTickRefitTime = lastUpdateTime.GetValueOrDefault();
		needsRefit = true;
	}

	public void ClearTickPoses()
	{
		foreach (string tickedPartId in tickedPartIds)
		{
			if (nodes.TryGetValue(tickedPartId, out Node3D value))
			{
				value.TickPosition = null;
				value.TickRotation = null;
				value.TickScale = 1f;
				value.IsHiddenByTick = false;
				value.TickGlow = 0f;
				value.TickColor = null;
				value.Position = value.BasePosition;
				value.Rotation = value.BaseRotation;
			}
		}
		tickedPartIds.Clear();
		tickEnvelope = null;
		needsRefit = true;
	}

	public void AddLabel(string labelId, string text, Vector3? point, string? follow, double start)
	{
		Label label = new Label
		{
			Id = labelId,
			Text = text,
			Point = (point ?? Vector3.Zero),
			FollowedNodeId = follow,
			Start = start
		};
		if (label.FollowedNodeId == null)
		{
			Node3D node3D = PartOwning(label.Point);
			if (node3D != null)
			{
				foreach (Motion motion in motions)
				{
					bool flag;
					switch (motion.Kind)
					{
					case "orbit":
					case "path":
					case "bob":
						flag = true;
						break;
					default:
						flag = false;
						break;
					}
					if (flag && Subtree(motion.Node).Contains(node3D))
					{
						label.CarrierNodeId = motion.Node.Id;
						label.CarrierOriginThen = Math3D.TransformPoint(motion.Node.WorldMatrix(atFullSize: true), Vector3.Zero);
						break;
					}
				}
			}
		}
		labels.Add(label);
	}

	private static List<Node3D> Subtree(Node3D node)
	{
		List<Node3D> list = new List<Node3D>();
		Collect(node);
		return list;
		void Collect(Node3D n)
		{
			list.Add(n);
			foreach (Node3D child in n.Children)
			{
				Collect(child);
			}
		}
	}

	public void RemovePart(string partId)
	{
		if (nodes.TryGetValue(partId, out Node3D value))
		{
			Forget(value);
			Node3D parent = value.Parent;
			if (parent != null)
			{
				parent.Children.Remove(value);
			}
			else
			{
				rootNodes.Remove(value);
			}
			labels.RemoveAll((Label l) => l.FollowedNodeId != null && !nodes.ContainsKey(l.FollowedNodeId));
			nodeByNumber.Clear();
			needsRefit = true;
		}
		void Forget(Node3D removed)
		{
			nodes.Remove(removed.Id);
			motions.RemoveAll((Motion m) => m.Node == removed);
			foreach (Node3D child in removed.Children)
			{
				Forget(child);
			}
		}
	}

	public void RemoveLabel(string labelId)
	{
		labels.RemoveAll((Label l) => l.Id == labelId);
	}

	public void BeginDrag()
	{
		isUserDragging = true;
	}

	public void Drag(double dx, double dy)
	{
		float num = (float)Math.Max(1.0, Rect.Height);
		dragThetaDelta -= (float)Math.PI * 2f * (float)dx / num;
		dragPhiDelta -= (float)Math.PI * 2f * (float)dy / num;
	}

	public void EndDrag()
	{
		isUserDragging = false;
	}

	public void Update(double time)
	{
		float num = (float)Math.Min(4.0, Math.Max(0.0, (time - (lastUpdateTime ?? time)) * 60.0));
		lastUpdateTime = time;
		foreach (Node3D value5 in nodes.Values)
		{
			double num2 = Math3D.Clamp01((time - value5.GrowStart) / value5.GrowDuration);
			value5.GrowScale = (float)Math.Max(0.0001, 1.0 - Math.Pow(1.0 - num2, 3.0));
		}
		string key;
		foreach (Motion motion in motions)
		{
			float num3 = (float)((time - motion.Start) / motion.Period * 2.0 * Math.PI);
			Node3D node = motion.Node;
			key = motion.Kind;
			switch (key)
			{
			case "spin":
			{
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(motion.Axis, num3);
				node.Rotation = quaternion * motion.BaseRotation;
				Vector3? targetPosition = motion.SpinPivot;
				if (targetPosition.HasValue)
				{
					Vector3 valueOrDefault3 = targetPosition.GetValueOrDefault();
					node.Position = valueOrDefault3 + Vector3.Transform(motion.BasePosition - valueOrDefault3, quaternion);
				}
				break;
			}
			case "bob":
				node.Position = motion.BasePosition + motion.Axis * (MathF.Sin(num3) * motion.Amount);
				break;
			case "orbit":
				node.Position = motion.Center + Vector3.Transform(motion.BasePosition - motion.Center, Quaternion.CreateFromAxisAngle(motion.Axis, num3));
				break;
			case "path":
			{
				CatmullRomCurve path = motion.Path;
				if (path != null)
				{
					double num5 = (time - motion.Start) / motion.Period;
					if (motion.Once)
					{
						float num6 = (float)Math3D.Clamp01(num5);
						node.Position = path.PointAt(num6 * num6 * (3f - 2f * num6));
					}
					else
					{
						node.Position = path.PointAt((float)(num5 - Math.Floor(num5)));
					}
				}
				break;
			}
			case "pulse":
				node.PulseScale = 1f + motion.Amount * (0.5f - 0.5f * MathF.Cos(num3));
				break;
			case "set":
				if (time >= motion.Start && !tickedPartIds.Contains(node.Id))
				{
					bool? setShown = motion.SetShown;
					if (setShown.HasValue)
					{
						bool valueOrDefault4 = setShown == true;
						node.IsHiddenByTick = !valueOrDefault4;
					}
					float? setGlow = motion.SetGlow;
					if (setGlow.HasValue)
					{
						float valueOrDefault5 = setGlow.GetValueOrDefault();
						node.TickGlow = valueOrDefault5;
					}
					Vector3? targetPosition = motion.SetColor;
					if (targetPosition.HasValue)
					{
						Vector3 valueOrDefault6 = targetPosition.GetValueOrDefault();
						node.TickColor = valueOrDefault6;
					}
					setGlow = motion.SetScale;
					if (setGlow.HasValue)
					{
						float valueOrDefault7 = setGlow.GetValueOrDefault();
						node.TickScale = valueOrDefault7;
					}
				}
				break;
			case "tween":
			{
				float num4 = (float)Math3D.Clamp01((time - motion.Start) / motion.Period);
				float amount = num4 * num4 * (3f - 2f * num4);
				if (time >= motion.Start)
				{
					Vector3? targetPosition = motion.TargetPosition;
					if (targetPosition.HasValue)
					{
						Vector3 valueOrDefault = targetPosition.GetValueOrDefault();
						node.Position = Vector3.Lerp(motion.BasePosition, valueOrDefault, amount);
					}
					Quaternion? targetRotation = motion.TargetRotation;
					if (targetRotation.HasValue)
					{
						Quaternion valueOrDefault2 = targetRotation.GetValueOrDefault();
						node.Rotation = Quaternion.Slerp(motion.BaseRotation, valueOrDefault2, amount);
					}
				}
				break;
			}
			}
		}
		foreach (string tickedPartId in tickedPartIds)
		{
			if (nodes.TryGetValue(tickedPartId, out Node3D value))
			{
				Vector3? targetPosition = value.TickPosition;
				if (targetPosition.HasValue)
				{
					Vector3 valueOrDefault8 = targetPosition.GetValueOrDefault();
					value.Position = valueOrDefault8;
				}
				Quaternion? targetRotation = value.TickRotation;
				if (targetRotation.HasValue)
				{
					Quaternion valueOrDefault9 = targetRotation.GetValueOrDefault();
					value.Rotation = valueOrDefault9;
				}
			}
		}
		if (highlightStarts.Count > 0)
		{
			scratchIds.Clear();
			foreach (KeyValuePair<string, double> highlightStart in highlightStarts)
			{
				highlightStart.Deconstruct(out key, out var value2);
				string text = key;
				double num7 = value2;
				double num8 = time - num7;
				Node3D value4;
				if (num8 < 0.0 || num8 > 0.7)
				{
					if (nodes.TryGetValue(text, out Node3D value3))
					{
						value3.Highlight = 0f;
					}
					scratchIds.Add(text);
				}
				else if (nodes.TryGetValue(text, out value4))
				{
					value4.Highlight = (float)Math.Sin(Math.PI * num8 / 0.7);
				}
			}
			foreach (string scratchId in scratchIds)
			{
				highlightStarts.Remove(scratchId);
			}
		}
		bool flag = false;
		foreach (Motion motion2 in motions)
		{
			if (motion2.Once && time >= motion2.Start && time <= motion2.Start + motion2.Period + 0.3)
			{
				flag = true;
				break;
			}
		}
		if (flag && time - lastTravelRefitTime >= 0.25)
		{
			lastTravelRefitTime = time;
			needsRefit = true;
		}
		if (needsRefit)
		{
			needsRefit = false;
			Refit();
		}
		float amount2 = 1f - MathF.Pow(0.94f, MathF.Max(num, 0f));
		ApplyFraming(amount2);
		EaseSettledExtent(amount2);
		(double, float, float, float, double)? tuple;
		if (camera.Spin != 0f && !isUserDragging)
		{
			tuple = frontSwing;
			if (!tuple.HasValue)
			{
				if (hasViewerFacingCut && CutawaySwayAmplitude > 0f)
				{
					if (SpinScale > 0f)
					{
						float cutawaySwayAmplitude = CutawaySwayAmplitude;
						double num9 = swayStartTime ?? time;
						swayStartTime = num9;
						float num10 = (float)Math.PI / 30f * camera.Spin * 2.5f;
						float x = (float)(time - num9) * MathF.Abs(num10) / cutawaySwayAmplitude;
						float num11 = (float)Math.PI * 2f;
						float num12 = restingYaw + RoundAway((orbitTheta - restingYaw) / num11) * num11;
						dragThetaDelta = num12 - (float)((!(num10 < 0f)) ? 1 : (-1)) * cutawaySwayAmplitude * MathF.Sin(x) - orbitTheta;
					}
				}
				else
				{
					dragThetaDelta -= 0.0017453294f * (camera.Spin * 2.5f * SpinScale) * num;
				}
			}
		}
		float num13 = 1f - MathF.Pow(0.95f, MathF.Max(num, 0f));
		orbitTheta += dragThetaDelta * num13;
		orbitPhi = Math.Clamp(orbitPhi + dragPhiDelta * num13, 1E-06f, 3.1415918f);
		dragThetaDelta *= 1f - num13;
		dragPhiDelta *= 1f - num13;
		if (!isUserDragging && MathF.Abs(dragPhiDelta) < 0.001f)
		{
			float num14 = Math.Clamp(90f - orbitPhi * 180f / (float)Math.PI, -89f, 89f);
			if (MathF.Abs(num14 - camera.Tilt) > 2f)
			{
				camera.Tilt = num14;
				needsRefit = true;
			}
		}
		tuple = frontSwing;
		if (!tuple.HasValue)
		{
			return;
		}
		(double, float, float, float, double) valueOrDefault10 = tuple.GetValueOrDefault();
		if (isUserDragging)
		{
			frontSwing = null;
			return;
		}
		float num15 = (float)Math3D.Clamp01((time - valueOrDefault10.Item1) / valueOrDefault10.Item5);
		float num16 = num15 * num15;
		float num17 = num16 * num15;
		orbitTheta = (2f * num17 - 3f * num16 + 1f) * valueOrDefault10.Item2 + (num17 - 2f * num16 + num15) * (float)valueOrDefault10.Item5 * valueOrDefault10.Item4 + (3f * num16 - 2f * num17) * valueOrDefault10.Item3;
		dragThetaDelta = 0f;
		if (num15 >= 1f)
		{
			frontSwing = null;
		}
	}

	public void Settle(double time)
	{
		Update(time);
		Refit();
		ApplyFraming(1f);
		EaseSettledExtent(1f);
	}

	public (double x, double y)? SettledProjection(Vector3 world)
	{
		if (needsRefit)
		{
			needsRefit = false;
			Refit();
		}
		(Vector3, float)? tuple = framing;
		if (tuple.HasValue)
		{
			(Vector3, float) valueOrDefault = tuple.GetValueOrDefault();
			Vector3 vector = new Vector3(MathF.Sin(orbitPhi) * MathF.Sin(orbitTheta), MathF.Cos(orbitPhi), MathF.Sin(orbitPhi) * MathF.Cos(orbitTheta));
			Matrix4x4 matrix4x = Math3D.LookAt(valueOrDefault.Item1 + valueOrDefault.Item2 * vector, valueOrDefault.Item1);
			Vector4 vector2 = Vector4.Transform(new Vector4(world, 1f), matrix4x * ProjectionMatrix);
			if (vector2.W <= 0f)
			{
				return null;
			}
			return ((double)((vector2.X / vector2.W + 1f) / 2f) * Rect.Width, (double)((1f - vector2.Y / vector2.W) / 2f) * Rect.Height);
		}
		return Project(world);
	}

	public Vector3? PartCenter(string partId)
	{
		if (!nodes.TryGetValue(partId, out Node3D value))
		{
			return null;
		}
		Matrix4x4 m = value.WorldMatrix(atFullSize: true);
		if (value.Mesh == null)
		{
			return Math3D.TransformPoint(in m, Vector3.Zero);
		}
		Vector3 vector = new Vector3(float.PositiveInfinity);
		Vector3 vector2 = new Vector3(float.NegativeInfinity);
		Mesh3D.Enumerator enumerator = value.Mesh.Bounding(200).GetEnumerator();
		while (enumerator.MoveNext())
		{
			Vector3 current = enumerator.Current;
			Vector3 value2 = Math3D.TransformPoint(in m, current);
			vector = Vector3.Min(vector, value2);
			vector2 = Vector3.Max(vector2, value2);
		}
		return (vector.X <= vector2.X) ? ((vector + vector2) / 2f) : Math3D.TransformPoint(in m, Vector3.Zero);
	}

	public Vector3? LabelPoint(string labelId)
	{
		foreach (Label label in labels)
		{
			if (!(label.Id != labelId))
			{
				string followedNodeId = label.FollowedNodeId;
				if (followedNodeId != null && nodes.TryGetValue(followedNodeId, out Node3D value))
				{
					return Math3D.TransformPoint(value.WorldMatrix(atFullSize: true), Vector3.Zero);
				}
				return label.Point + Ride(label);
			}
		}
		return null;
	}

	private void EaseSettledExtent(float amount)
	{
		Rect2? settledExtentTarget = SettledExtentTarget;
		if (settledExtentTarget.HasValue)
		{
			Rect2 valueOrDefault = settledExtentTarget.GetValueOrDefault();
			settledExtentTarget = SettledExtent;
			if (settledExtentTarget.HasValue)
			{
				Rect2 valueOrDefault2 = settledExtentTarget.GetValueOrDefault();
				double num = amount;
				SettledExtent = new Rect2(valueOrDefault2.X + (valueOrDefault.X - valueOrDefault2.X) * num, valueOrDefault2.Y + (valueOrDefault.Y - valueOrDefault2.Y) * num, valueOrDefault2.Width + (valueOrDefault.Width - valueOrDefault2.Width) * num, valueOrDefault2.Height + (valueOrDefault.Height - valueOrDefault2.Height) * num);
			}
			else
			{
				SettledExtent = valueOrDefault;
			}
		}
	}

	public (double x, double y)? Project(Vector3 point)
	{
		return Project(point, ViewMatrix * ProjectionMatrix);
	}

	private (double x, double y)? Project(Vector3 point, in Matrix4x4 viewProjection)
	{
		Vector4 vector = Vector4.Transform(new Vector4(point, 1f), viewProjection);
		if (vector.W <= 0f)
		{
			return null;
		}
		return ((double)((vector.X / vector.W + 1f) / 2f) * Rect.Width, (double)((1f - vector.Y / vector.W) / 2f) * Rect.Height);
	}

	private void ApplyFraming(float amount)
	{
		(Vector3, float)? tuple = framing;
		if (tuple.HasValue)
		{
			(Vector3, float) valueOrDefault = tuple.GetValueOrDefault();
			if (!(amount <= 0f))
			{
				orbitTarget = Vector3.Lerp(orbitTarget, valueOrDefault.Item1, amount);
				orbitRadius += (valueOrDefault.Item2 - orbitRadius) * amount;
			}
		}
	}

	private void Refit()
	{
		hasViewerFacingCut = false;
		foreach (Node3D value4 in nodes.Values)
		{
			if (value4.Material.CutFacesViewer)
			{
				hasViewerFacingCut = true;
				break;
			}
		}
		if (!camera.Fits || nodes.Count == 0)
		{
			return;
		}
		List<Node3D> list = refitParts;
		list.Clear();
		foreach (Node3D rootNode in rootNodes)
		{
			CollectMeshParts(rootNode, list);
		}
		float x = camera.Tilt * (float)Math.PI / 180f;
		float num = (float)(Rect.Width / Rect.Height);
		Matrix4x4 matrix4x = Math3D.Perspective(camera.FieldOfView, num, 0.001f, 10000000f);
		float m = matrix4x.M11;
		float m2 = matrix4x.M22;
		int num2 = ((camera.Spin == 0f) ? 1 : 8);
		float num3 = ((num2 == 1 && hasRestingView) ? restingYaw : 0f);
		Matrix4x4 matrix4x2 = Math3D.LookAt(new Vector3(0f, MathF.Sin(x), MathF.Cos(x)), Vector3.Zero);
		Vector3 value = new Vector3(matrix4x2.M11, matrix4x2.M21, matrix4x2.M31);
		Vector3 value2 = new Vector3(matrix4x2.M12, matrix4x2.M22, matrix4x2.M32);
		Vector3 value3 = new Vector3(matrix4x2.M13, matrix4x2.M23, matrix4x2.M33);
		(Vector3, Vector3, Vector3)[] array = refitTurnedAxes;
		Vector3[] array2 = refitDirections;
		for (int i = 0; i < 8; i++)
		{
			Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num3 - (float)i * (float)Math.PI / 4f);
			(Vector3, Vector3, Vector3) tuple = (array[i] = (Vector3.Transform(value, rotation), Vector3.Transform(value2, rotation), Vector3.Transform(value3, rotation)));
			array2[i * 4] = tuple.Item3 + m / 0.88f * tuple.Item1;
			array2[i * 4 + 1] = tuple.Item3 - m / 0.88f * tuple.Item1;
			array2[i * 4 + 2] = tuple.Item3 + m2 / 0.88f * tuple.Item2;
			array2[i * 4 + 3] = tuple.Item3 - m2 / 0.88f * tuple.Item2;
		}
		Vector4 key = new Vector4(x, camera.FieldOfView, num, num3);
		foreach (Node3D item in list)
		{
			RefreshFraming(item, array2, key);
		}
		List<Node3D> list2 = refitSampled;
		list2.Clear();
		foreach (Node3D item2 in list)
		{
			if (item2.FramingSamples.Length != 0)
			{
				list2.Add(item2);
			}
		}
		if (list2.Count == 0)
		{
			return;
		}
		Vector3 vector = list2[0].FramingBoxMinimum;
		Vector3 vector2 = list2[0].FramingBoxMaximum;
		for (int j = 1; j < list2.Count; j++)
		{
			vector = Vector3.Min(vector, list2[j].FramingBoxMinimum);
			vector2 = Vector3.Max(vector2, list2[j].FramingBoxMaximum);
		}
		if (refitReaches.Length != num2 * 4)
		{
			refitReaches = new float[num2 * 4];
		}
		float[] array3 = refitReaches;
		Array.Fill(array3, float.NegativeInfinity);
		for (int k = 0; k < array3.Length; k++)
		{
			foreach (Node3D item3 in list2)
			{
				array3[k] = MathF.Max(array3[k], item3.FramingReaches[k]);
			}
		}
		float[] array4;
		if (tickedPartIds.Count > 0)
		{
			(Vector3, Vector3, float[])? tuple2 = tickEnvelope;
			if (tuple2.HasValue)
			{
				(Vector3, Vector3, float[]) valueOrDefault = tuple2.GetValueOrDefault();
				if (valueOrDefault.Item3.Length == array3.Length)
				{
					vector = Vector3.Min(vector, valueOrDefault.Item1);
					vector2 = Vector3.Max(vector2, valueOrDefault.Item2);
					for (int l = 0; l < array3.Length; l++)
					{
						array3[l] = MathF.Max(array3[l], valueOrDefault.Item3[l]);
					}
				}
			}
			tuple2 = tickEnvelope;
			if (tuple2.HasValue)
			{
				(Vector3, Vector3, float[]) valueOrDefault2 = tuple2.GetValueOrDefault();
				if (valueOrDefault2.Item3.Length == array3.Length)
				{
					array4 = valueOrDefault2.Item3;
					goto IL_0520;
				}
			}
			array4 = new float[array3.Length];
			goto IL_0520;
		}
		goto IL_0542;
		IL_0520:
		float[] array5 = array4;
		array3.CopyTo(array5, 0);
		tickEnvelope = (vector, vector2, array5);
		goto IL_0542;
		IL_0542:
		ObjectBox = (vector, vector2);
		Vector3 vector3 = (vector + vector2) / 2f;
		float num4 = float.NegativeInfinity;
		for (int n = 0; n < num2 * 4; n++)
		{
			num4 = MathF.Max(num4, array3[n] - Vector3.Dot(array2[n], vector3));
		}
		float num5 = 0.1f;
		float num6 = 1000f;
		for (int num7 = 0; num7 < 26; num7++)
		{
			float num8 = (num5 + num6) / 2f;
			if (num8 >= num4)
			{
				num6 = num8;
			}
			else
			{
				num5 = num8;
			}
		}
		if (camera.Zoom != 1f)
		{
			num6 /= MathF.Min(camera.Zoom, 1.1f);
		}
		framing = (vector3, num6);
		NearPlane = MathF.Max(0.01f, num6 / 500f);
		FarPlane = MathF.Max(500f, num6 * 20f);
		float width = (float)Rect.Width;
		float height = (float)Rect.Height;
		float bandHeight = 2f;
		int num9 = (int)Math.Ceiling(Rect.Height / 2.0) + 1;
		bool flag = list2.Count >= 12;
		List<Node3D> list3 = list2;
		if (flag)
		{
			list3 = refitOrdered;
			list3.Clear();
			list3.AddRange(list2);
			outermostOrder.Center = vector3;
			list3.Sort(outermostComparison ?? (outermostComparison = outermostOrder.Compare));
		}
		if (SettledLeft.Length != num9)
		{
			SettledLeft = new float[num9];
			SettledRight = new float[num9];
		}
		(float, float, float, float, float[], float[]) tuple3 = ProjectSilhouette(list3, array, num2, m, m2, vector3, num6, width, height, bandHeight, num9, flag, SettledLeft, SettledRight);
		FramingRevision++;
		if (tuple3.Item1 <= tuple3.Item2 && tuple3.Item3 <= tuple3.Item4)
		{
			SettledExtentTarget = new Rect2((double)((tuple3.Item1 + 1f) / 2f) * Rect.Width, (double)((1f - tuple3.Item4) / 2f) * Rect.Height, (double)((tuple3.Item2 - tuple3.Item1) / 2f) * Rect.Width, (double)((tuple3.Item4 - tuple3.Item3) / 2f) * Rect.Height);
		}
	}

	private static void CollectMeshParts(Node3D node, List<Node3D> parts)
	{
		if (node.Mesh != null)
		{
			parts.Add(node);
		}
		foreach (Node3D child in node.Children)
		{
			CollectMeshParts(child, parts);
		}
	}

	private static float FarthestCornerDistanceSquared(Node3D part, Vector3 center)
	{
		return Vector3.Max(Vector3.Abs(part.FramingBoxMinimum - center), Vector3.Abs(part.FramingBoxMaximum - center)).LengthSquared();
	}

	private static (float lowX, float highX, float lowY, float highY, float[] left, float[] right) ProjectSilhouette(List<Node3D> parts, (Vector3 right, Vector3 up, Vector3 forward)[] turnedAxes, int steps, float xScale, float yScale, Vector3 center, float far, float width, float height, float bandHeight, int bandCount, bool skipsInnerParts, float[] left, float[] rightReach)
	{
		float num = float.PositiveInfinity;
		float num2 = float.PositiveInfinity;
		float num3 = float.NegativeInfinity;
		float num4 = float.NegativeInfinity;
		Array.Fill(left, float.PositiveInfinity);
		Array.Fill(rightReach, float.NegativeInfinity);
		for (int i = 0; i < steps; i++)
		{
			(Vector3 right, Vector3 up, Vector3 forward) tuple = turnedAxes[i];
			float num5 = tuple.right.X * xScale;
			float num6 = tuple.right.Y * xScale;
			float num7 = tuple.right.Z * xScale;
			float num8 = tuple.up.X * yScale;
			float num9 = tuple.up.Y * yScale;
			float num10 = tuple.up.Z * yScale;
			float x = tuple.forward.X;
			float y = tuple.forward.Y;
			float z = tuple.forward.Z;
			float num11 = num5 * center.X + num6 * center.Y + num7 * center.Z;
			float num12 = num8 * center.X + num9 * center.Y + num10 * center.Z;
			float num13 = x * center.X + y * center.Y + z * center.Z;
			foreach (Node3D part in parts)
			{
				if (skipsInnerParts && num <= num3 && num2 <= num4)
				{
					Vector3 framingBoxMinimum = part.FramingBoxMinimum;
					Vector3 framingBoxMaximum = part.FramingBoxMaximum;
					float num14 = float.PositiveInfinity;
					float num15 = float.NegativeInfinity;
					float num16 = float.PositiveInfinity;
					float num17 = float.NegativeInfinity;
					bool flag = true;
					for (int j = 0; j < 8; j++)
					{
						float num18 = (((j & 1) == 0) ? framingBoxMinimum.X : framingBoxMaximum.X);
						float num19 = (((j & 2) == 0) ? framingBoxMinimum.Y : framingBoxMaximum.Y);
						float num20 = (((j & 4) == 0) ? framingBoxMinimum.Z : framingBoxMaximum.Z);
						float num21 = far - (x * num18 + y * num19 + z * num20 - num13);
						if (num21 <= far * 0.001f)
						{
							flag = false;
							break;
						}
						float y2 = (num5 * num18 + num6 * num19 + num7 * num20 - num11) / num21;
						float y3 = (num8 * num18 + num9 * num19 + num10 * num20 - num12) / num21;
						num14 = MathF.Min(num14, y2);
						num15 = MathF.Max(num15, y2);
						num16 = MathF.Min(num16, y3);
						num17 = MathF.Max(num17, y3);
					}
					num14 -= 0.0001f;
					num15 += 0.0001f;
					num16 -= 0.0001f;
					num17 += 0.0001f;
					if (flag && float.IsFinite(num14) && float.IsFinite(num15) && float.IsFinite(num16) && float.IsFinite(num17) && num14 > num && num15 < num3 && num16 > num2 && num17 < num4)
					{
						float num22 = (num14 + 1f) / 2f * width;
						float num23 = (num15 + 1f) / 2f * width;
						int num24 = Math.Clamp((int)((1f - num17) / 2f * height / bandHeight), 0, bandCount - 1);
						int num25 = Math.Clamp((int)((1f - num16) / 2f * height / bandHeight), 0, bandCount - 1);
						bool flag2 = true;
						for (int k = num24; k <= num25; k++)
						{
							if (!(left[k] < num22) || !(rightReach[k] > num23))
							{
								flag2 = false;
								break;
							}
						}
						if (flag2)
						{
							continue;
						}
					}
				}
				float[] framingSamples = part.FramingSamples;
				for (int l = 0; l + 2 < framingSamples.Length; l += 3)
				{
					float num26 = framingSamples[l];
					float num27 = framingSamples[l + 1];
					float num28 = framingSamples[l + 2];
					float num29 = far - (x * num26 + y * num27 + z * num28 - num13);
					if (!(num29 <= 0f))
					{
						float num30 = (num5 * num26 + num6 * num27 + num7 * num28 - num11) / num29;
						float num31 = (num8 * num26 + num9 * num27 + num10 * num28 - num12) / num29;
						if (num30 < num)
						{
							num = num30;
						}
						if (num30 > num3)
						{
							num3 = num30;
						}
						if (num31 < num2)
						{
							num2 = num31;
						}
						if (num31 > num4)
						{
							num4 = num31;
						}
						float num32 = (num30 + 1f) / 2f * width;
						int num33 = Math.Clamp((int)((1f - num31) / 2f * height / bandHeight), 0, bandCount - 1);
						if (num32 < left[num33])
						{
							left[num33] = num32;
						}
						if (num32 > rightReach[num33])
						{
							rightReach[num33] = num32;
						}
					}
				}
			}
		}
		return (lowX: num, highX: num3, lowY: num2, highY: num4, left: left, right: rightReach);
	}

	private static void RefreshFraming(Node3D part, Vector3[] directions, Vector4 key)
	{
		if (part.Mesh == null)
		{
			return;
		}
		Matrix4x4 m = part.FramingWorldMatrix();
		if (part.FramingSamplesMatrix != m)
		{
			int num = 0;
			Mesh3D.Enumerator enumerator = part.Mesh.Bounding(600).GetEnumerator();
			while (enumerator.MoveNext())
			{
				_ = enumerator.Current;
				num++;
			}
			if (part.FramingSamples.Length != num * 3)
			{
				part.FramingSamples = new float[num * 3];
			}
			float[] framingSamples = part.FramingSamples;
			Vector3 vector = new Vector3(float.PositiveInfinity);
			Vector3 vector2 = new Vector3(float.NegativeInfinity);
			int num2 = 0;
			enumerator = part.Mesh.Bounding(600).GetEnumerator();
			while (enumerator.MoveNext())
			{
				Vector3 current = enumerator.Current;
				Vector3 value = Math3D.TransformPoint(in m, current);
				framingSamples[num2++] = value.X;
				framingSamples[num2++] = value.Y;
				framingSamples[num2++] = value.Z;
				vector = Vector3.Min(vector, value);
				vector2 = Vector3.Max(vector2, value);
			}
			part.FramingSamplesMatrix = m;
			part.FramingBoxMinimum = vector;
			part.FramingBoxMaximum = vector2;
			part.FramingReachesKey = null;
		}
		if (part.FramingReachesKey == key)
		{
			return;
		}
		if (part.FramingReaches.Length != directions.Length)
		{
			part.FramingReaches = new float[directions.Length];
		}
		float[] framingReaches = part.FramingReaches;
		float[] framingSamples2 = part.FramingSamples;
		for (int i = 0; i < directions.Length; i++)
		{
			float x = directions[i].X;
			float y = directions[i].Y;
			float z = directions[i].Z;
			float num3 = float.NegativeInfinity;
			for (int j = 0; j + 2 < framingSamples2.Length; j += 3)
			{
				float num4 = x * framingSamples2[j] + y * framingSamples2[j + 1] + z * framingSamples2[j + 2];
				if (num4 > num3)
				{
					num3 = num4;
				}
			}
			framingReaches[i] = num3;
		}
		part.FramingReachesKey = key;
	}

	public void DrawItems(List<DrawItem3D> items)
	{
		items.Clear();
		Matrix4x4 viewProjection = ViewMatrix * ProjectionMatrix;
		Vector4 facingCut = (((int)RoundAway(((camera.Spin != 0f || hasRestingView) ? restingYaw : 0f) / ((float)Math.PI / 2f)) % 4 + 4) % 4) switch
		{
			1 => new Vector4(1f, 0f, 0f, 0f), 
			2 => new Vector4(0f, 0f, -1f, 0f), 
			3 => new Vector4(-1f, 0f, 0f, 0f), 
			_ => new Vector4(0f, 0f, 1f, 0f), 
		};
		foreach (Node3D rootNode in rootNodes)
		{
			Visit(rootNode, Matrix4x4.Identity, 0f, items, in viewProjection, facingCut);
		}
	}

	private static void Visit(Node3D node, in Matrix4x4 parentMatrix, float parentHighlight, List<DrawItem3D> items, in Matrix4x4 viewProjection, Vector4 facingCut)
	{
		if (node.IsHiddenByTick)
		{
			return;
		}
		Matrix4x4 parentMatrix2 = node.LocalMatrix(atFullSize: false) * parentMatrix;
		float num = MathF.Max(node.Highlight, parentHighlight);
		Mesh3D mesh = node.Mesh;
		if (mesh != null)
		{
			Vector4 vector = Vector4.Transform(new Vector4(parentMatrix2.M41, parentMatrix2.M42, parentMatrix2.M43, 1f), viewProjection);
			Material3D material = node.Material;
			if (material.CutFacesViewer)
			{
				material.CutPlane = facingCut;
			}
			Vector3? tickColor = node.TickColor;
			if (tickColor.HasValue)
			{
				Vector3 valueOrDefault = tickColor.GetValueOrDefault();
				material.Color = new Vector4(valueOrDefault, material.Color.W);
			}
			Vector3 vector2 = new Vector3(material.Color.X, material.Color.Y, material.Color.Z);
			if (num > 0f)
			{
				material.Emissive += (vector2 * 0.42f + new Vector3(0.1f)) * num;
			}
			if (node.TickGlow > 0f)
			{
				material.Emissive += (vector2 * 0.9f + new Vector3(0.08f)) * node.TickGlow;
			}
			items.Add(new DrawItem3D
			{
				Node = node,
				Mesh = mesh,
				Material = material,
				ModelMatrix = parentMatrix2,
				Depth = ((vector.W != 0f) ? (vector.Z / vector.W) : 0f),
				Order = node.CreationOrder
			});
		}
		foreach (Node3D child in node.Children)
		{
			Visit(child, in parentMatrix2, num, items, in viewProjection, facingCut);
		}
	}

	public void LabelPlacements(double time, List<LabelPlacement3D> placements)
	{
		placements.Clear();
		Matrix4x4 viewProjection = ViewMatrix * ProjectionMatrix;
		foreach (Label label in labels)
		{
			Vector3 vector = label.Point + Ride(label);
			Node3D node3D = null;
			string followedNodeId = label.FollowedNodeId;
			if (followedNodeId != null && nodes.TryGetValue(followedNodeId, out Node3D value))
			{
				node3D = value;
				vector = Math3D.TransformPoint(value.WorldMatrix(atFullSize: false), Vector3.Zero);
			}
			(double, double)? tuple = Project(vector, in viewProjection);
			if (!tuple.HasValue)
			{
				continue;
			}
			(double, double) valueOrDefault = tuple.GetValueOrDefault();
			(double, double) tuple2 = valueOrDefault;
			if (node3D != null && IsMovedByTick(node3D))
			{
				Vector3 vector2 = vector;
				if (labelAnchorWorld.TryGetValue(label.Id, out (Vector3, double) value2))
				{
					float amount = (float)(1.0 - Math.Exp((0.0 - Math.Max(0.0, time - value2.Item2)) / 1.1));
					vector2 = Vector3.Lerp(value2.Item1, vector, amount);
				}
				labelAnchorWorld[label.Id] = (vector2, time);
				tuple2 = Project(vector2, in viewProjection) ?? valueOrDefault;
			}
			LabelPlacement3D item = new LabelPlacement3D
			{
				Id = label.Id
			};
			(item.PointX, item.PointY) = valueOrDefault;
			(item.AnchorX, item.AnchorY) = tuple2;
			placements.Add(item);
		}
	}

	private bool IsMovedByTick(Node3D node)
	{
		if (tickedPartIds.Count == 0)
		{
			return false;
		}
		for (Node3D node3D = node; node3D != null; node3D = node3D.Parent)
		{
			if (tickedPartIds.Contains(node3D.Id))
			{
				return true;
			}
		}
		return false;
	}

	public void ProjectedSamplePoints(List<Vector2> points, int samplesPerPart = 160)
	{
		points.Clear();
		Matrix4x4 viewProjection = ViewMatrix * ProjectionMatrix;
		foreach (Node3D rootNode in rootNodes)
		{
			VisitSamples(rootNode, Matrix4x4.Identity, in viewProjection, points, samplesPerPart);
		}
	}

	private void VisitSamples(Node3D node, in Matrix4x4 parentMatrix, in Matrix4x4 viewProjection, List<Vector2> points, int samplesPerPart)
	{
		Matrix4x4 parentMatrix2 = node.LocalMatrix(atFullSize: false) * parentMatrix;
		Mesh3D mesh = node.Mesh;
		if (mesh != null && node.GrowScale > 0.05f)
		{
			Matrix4x4 matrix = parentMatrix2 * viewProjection;
			Mesh3D.Enumerator enumerator = mesh.Bounding(samplesPerPart).GetEnumerator();
			while (enumerator.MoveNext())
			{
				Vector4 vector = Vector4.Transform(new Vector4(enumerator.Current, 1f), matrix);
				if (!(vector.W <= 0f))
				{
					points.Add(new Vector2((float)((double)((vector.X / vector.W + 1f) / 2f) * Rect.Width), (float)((double)((1f - vector.Y / vector.W) / 2f) * Rect.Height)));
				}
			}
		}
		foreach (Node3D child in node.Children)
		{
			VisitSamples(child, in parentMatrix2, in viewProjection, points, samplesPerPart);
		}
	}

	public Node3D? Node(string identifier)
	{
		return nodes.GetValueOrDefault(identifier);
	}

	public static Mesh3D? BuildMesh(string kind, JsonElement p)
	{
		switch (kind)
		{
		case "sphere":
			return Geometry.Sphere(Num("r", 1f));
		case "box":
			return Geometry.Box(p.Vec3("size") ?? Vector3.One);
		case "cylinder":
		{
			float num = Num("r", 0.5f);
			Vector3? vector = p.Vec3("from");
			if (vector.HasValue)
			{
				Vector3 valueOrDefault = vector.GetValueOrDefault();
				vector = p.Vec3("to");
				if (vector.HasValue)
				{
					Vector3 valueOrDefault2 = vector.GetValueOrDefault();
					return Geometry.Cylinder(num, num, Vector3.Distance(valueOrDefault, valueOrDefault2), 20);
				}
			}
			return Geometry.Cylinder(num, num, Num("h", 1f), 40);
		}
		case "cone":
			return Geometry.Cylinder(0f, Num("r", 0.5f), Num("h", 1f), 40);
		case "torus":
			return Geometry.Torus(Num("r", 1f), Num("tube", 0.25f));
		case "ring":
			return Geometry.Ring(Num("inner", 1f), Num("outer", 1.5f));
		case "capsule":
			return Geometry.Capsule(Num("r", 0.3f), Num("h", 1f));
		case "tube":
			return Geometry.Tube(p.Points3("points"), Num("r", 0.08f), p.IsTrue("closed"));
		case "lathe":
			return Geometry.Lathe(p.Points2("profile"));
		case "extrude":
			return Geometry.Extrude(p.Str("d") ?? "", Num("depth", 0.3f), Num("unit", 0.01f));
		case "surface":
		{
			List<List<Vector3>> list = new List<List<Vector3>>();
			if (p.Has("grid", out var value) && value.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement item in value.EnumerateArray())
				{
					list.Add(J.Points3(item));
				}
			}
			return Geometry.Surface(list);
		}
		default:
			return null;
		}
		float Num(string key, float fallback)
		{
			return p.Number(key) ?? fallback;
		}
	}
}
