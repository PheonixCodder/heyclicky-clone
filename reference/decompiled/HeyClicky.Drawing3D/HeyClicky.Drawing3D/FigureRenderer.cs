using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace HeyClicky.Drawing3D;

internal sealed class FigureRenderer : IDisposable
{
	private struct MarkerFrame
	{
		public Matrix4x4 ViewProjection;

		public Vector4 Eye;

		public Vector4 Style;
	}

	private struct StudioFrame
	{
		public Matrix4x4 ViewProjection;

		public Matrix4x4 LightViewProjection;

		public Vector4 Eye;

		public Vector4 Key;

		public Vector4 KeyColor;

		public Vector4 Rim;

		public Vector4 RimColor;

		public Vector4 Environment;

		public Vector4 Shadow;

		public Vector4 Ground;

		public Vector4 GroundColor;

		public Vector4 Occlusion;

		public Vector4 Target;

		public Vector4 ViewForward;

		public Vector4 RayBase;

		public Vector4 RayStepX;

		public Vector4 RayStepY;

		public Vector4 Grade;

		public Vector4 CutHatch;
	}

	private struct OutlineConstants
	{
		public Vector4 InkColor;

		public float HalfWidth;

		public float SeamHalfWidth;

		public float SeamStrength;

		public float SeamShade;

		public uint NeighbourCount;

		public uint ReadsClear;

		public uint Pad0;

		public uint Pad1;

		public unsafe fixed float Neighbours[320];
	}

	private sealed class ItemOrder : IComparer<int>
	{
		public List<DrawItem3D> Items;

		public bool BackToFront;

		public int Compare(int a, int b)
		{
			Span<DrawItem3D> span = CollectionsMarshal.AsSpan(Items);
			ref DrawItem3D reference = ref span[a];
			ref DrawItem3D reference2 = ref span[b];
			if (reference.Depth != reference2.Depth)
			{
				if (!BackToFront)
				{
					return reference.Depth.CompareTo(reference2.Depth);
				}
				return reference2.Depth.CompareTo(reference.Depth);
			}
			return reference.Order.CompareTo(reference2.Order);
		}
	}

	public struct Capsule
	{
		public Vector2 A;

		public Vector2 B;

		public float Radius;

		public float HaloWidth;

		private Vector2 pad;

		public Vector4 Color;

		public Vector4 Halo;
	}

	public struct Sheet
	{
		public Vector2 Center;

		public Vector2 HalfSize;

		public float Angle;

		public float CornerRadius;

		private Vector2 pad;

		public Vector4 Color;
	}

	private struct OverlayConstants
	{
		public Vector2 TargetSize;

		public uint SheetIndex;

		public uint Pad;
	}

	public const int SampleCount = 4;

	public const int ShadowMapSize = 1024;

	private static readonly Vector3 KeyDirection = Vector3.Normalize(new Vector3(-0.55f, 0.62f, 0.56f));

	private static readonly Vector3 RimDirection = Vector3.Normalize(new Vector3(0.35f, 0.45f, -0.85f));

	private static readonly Vector3 ShadowDirection = Vector3.Normalize(new Vector3(-0.15f, 1f, 0.12f));

	private static readonly Vector3 KeyColor = new Vector3(1f, 0.97f, 0.92f);

	private static readonly Vector3 RimColor = new Vector3(0.78f, 0.86f, 1f);

	private readonly Pipelines p;

	private readonly ID3D11Device1 device;

	private readonly ID3D11DeviceContext1 context;

	private readonly ID3D11Buffer markerFrameBuffer;

	private readonly ID3D11Buffer studioFrameBuffer;

	private readonly ID3D11Buffer outlineBuffer;

	private ID3D11Buffer? objectBuffer;

	private ID3D11ShaderResourceView? objectView;

	private int objectCapacity;

	private int width;

	private int height;

	private ID3D11Texture2D? msaaColor;

	private ID3D11RenderTargetView? msaaColorView;

	private ID3D11Texture2D? msaaDepth;

	private ID3D11DepthStencilView? msaaDepthView;

	private ID3D11Texture2D? opaqueIds;

	private ID3D11Texture2D? clearIds;

	private ID3D11RenderTargetView? opaqueIdsTarget;

	private ID3D11RenderTargetView? clearIdsTarget;

	private ID3D11ShaderResourceView? opaqueIdsView;

	private ID3D11ShaderResourceView? clearIdsView;

	private ID3D11Texture2D? idDepth;

	private ID3D11DepthStencilView? idDepthView;

	private int halfWidth;

	private int halfHeight;

	private ID3D11Texture2D? distance;

	private ID3D11Texture2D? distanceDepth;

	private ID3D11Texture2D? rawOcclusion;

	private ID3D11Texture2D? occlusion;

	private ID3D11RenderTargetView? distanceTarget;

	private ID3D11RenderTargetView? rawOcclusionTarget;

	private ID3D11RenderTargetView? occlusionTarget;

	private ID3D11ShaderResourceView? distanceView;

	private ID3D11ShaderResourceView? rawOcclusionView;

	private ID3D11ShaderResourceView? occlusionView;

	private ID3D11DepthStencilView? distanceDepthView;

	private ID3D11Texture2D? shadowMap;

	private ID3D11DepthStencilView? shadowDepthView;

	private ID3D11ShaderResourceView? shadowView;

	private readonly List<float> shadowDrawnFrom = new List<float>();

	private float[] shadowDrawnFromCache = Array.Empty<float>();

	private int shadowDrawnFromCount = -1;

	public readonly List<DrawItem3D> Items = new List<DrawItem3D>();

	private readonly List<int> opaque = new List<int>();

	private readonly List<int> transparent = new List<int>();

	private readonly ItemOrder opaqueOrder;

	private readonly ItemOrder transparentOrder;

	private readonly Comparison<int> opaqueComparison;

	private readonly Comparison<int> transparentComparison;

	private float[] edgeTable = Array.Empty<float>();

	private float edgeTableReach = -1f;

	private readonly object targetsLock = new object();

	private static readonly ID3D11ShaderResourceView[] NullViews = new ID3D11ShaderResourceView[5];

	private ID3D11Buffer? capsuleBuffer;

	private ID3D11Buffer? sheetBuffer;

	private ID3D11Buffer? overlayConstants;

	private ID3D11ShaderResourceView? capsuleView;

	private ID3D11ShaderResourceView? sheetView;

	private int capsuleCapacity;

	private int sheetCapacity;

	public unsafe FigureRenderer(Pipelines pipelines)
	{
		Interlocked.Increment(ref ThreeStage.liveRenderers);
		p = pipelines;
		device = pipelines.Device;
		context = pipelines.Gpu.Context;
		markerFrameBuffer = DynamicConstants(sizeof(MarkerFrame));
		studioFrameBuffer = DynamicConstants(sizeof(StudioFrame));
		outlineBuffer = DynamicConstants(sizeof(OutlineConstants));
		opaqueOrder = new ItemOrder
		{
			Items = Items
		};
		opaqueComparison = opaqueOrder.Compare;
		transparentOrder = new ItemOrder
		{
			Items = Items,
			BackToFront = true
		};
		transparentComparison = transparentOrder.Compare;
	}

	private ID3D11Buffer DynamicConstants(int size)
	{
		return device.CreateBuffer(new BufferDescription((uint)((size + 15) & -16), BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write), (SubresourceData?)null);
	}

	private unsafe void Upload<T>(ID3D11Buffer buffer, in T value) where T : unmanaged
	{
		Unsafe.Write((void*)context.Map(buffer, 0u, MapMode.WriteDiscard).DataPointer, value);
		context.Unmap(buffer, 0u);
	}

	private unsafe void PrepareItems(Scene3D scene)
	{
		scene.DrawItems(Items);
		opaque.Clear();
		transparent.Clear();
		Span<DrawItem3D> span = CollectionsMarshal.AsSpan(Items);
		for (int i = 0; i < span.Length; i++)
		{
			(span[i].Material.IsTransparent ? transparent : opaque).Add(i);
			Mesh3D mesh = span[i].Mesh;
			Mesh3D mesh3D = mesh;
			if (mesh3D.Gpu == null)
			{
				mesh3D.Gpu = GpuMesh.Create(device, mesh);
			}
		}
		CollectionsMarshal.AsSpan(opaque).Sort(opaqueComparison);
		CollectionsMarshal.AsSpan(transparent).Sort(transparentComparison);
		if (objectCapacity < span.Length || (object)objectBuffer == null)
		{
			objectView?.Dispose();
			objectBuffer?.Dispose();
			objectCapacity = Math.Max(64, span.Length * 2);
			objectBuffer = device.CreateBuffer(new BufferDescription((uint)(objectCapacity * 464), BindFlags.ShaderResource, ResourceUsage.Dynamic, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured, 464u), (SubresourceData?)null);
			objectView = device.CreateShaderResourceView(objectBuffer, new ShaderResourceViewDescription
			{
				Format = Format.Unknown,
				ViewDimension = ShaderResourceViewDimension.Buffer,
				Buffer = new BufferShaderResourceView
				{
					FirstElement = 0u,
					NumElements = (uint)objectCapacity
				}
			});
		}
		if (span.Length > 0)
		{
			ObjectData* dataPointer = (ObjectData*)context.Map(objectBuffer, 0u, MapMode.WriteDiscard).DataPointer;
			for (int j = 0; j < span.Length; j++)
			{
				Unsafe.InitBlockUnaligned(dataPointer + j, 0, 464u);
				ObjectData.Write(dataPointer + j, in span[j]);
			}
			context.Unmap(objectBuffer, 0u);
		}
		context.IASetInputLayout(p.Layout);
		context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
		context.IASetVertexBuffer(1u, p.InstanceIndices(span.Length), 4u);
		context.VSSetShaderResource(8u, objectView);
		context.PSSetShaderResource(8u, objectView);
	}

	private void DrawItem(int index, uint? overrideIndexCount = null)
	{
		GpuMesh gpu = CollectionsMarshal.AsSpan(Items)[index].Mesh.Gpu;
		if ((object)gpu?.Vertices != null)
		{
			context.IASetVertexBuffer(0u, gpu.Vertices, 48u);
			context.IASetIndexBuffer(gpu.Indices, Format.R32_UInt, 0u);
			context.DrawIndexedInstanced(overrideIndexCount ?? gpu.IndexCount, 1u, 0u, 0, (uint)index);
		}
	}

	private ID3D11RasterizerState Cull(in Material3D material, bool cutShowsInside)
	{
		if (!material.IsDoubleSided && (!cutShowsInside || !material.IsCut))
		{
			return p.CullBack;
		}
		return p.CullNone;
	}

	private RawRect? PartsPixelBounds(in Matrix4x4 viewProjection, int w, int h, float margin)
	{
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		Span<Vector3> span = stackalloc Vector3[8];
		Span<DrawItem3D> span2 = CollectionsMarshal.AsSpan(Items);
		for (int i = 0; i < span2.Length; i++)
		{
			ref DrawItem3D reference = ref span2[i];
			(Vector3, Vector3)? localBounds = reference.Mesh.LocalBounds;
			if (!localBounds.HasValue)
			{
				continue;
			}
			var (vector, vector2) = localBounds.GetValueOrDefault();
			span[0] = new Vector3(vector.X, vector.Y, vector.Z);
			span[1] = new Vector3(vector2.X, vector.Y, vector.Z);
			span[2] = new Vector3(vector.X, vector2.Y, vector.Z);
			span[3] = new Vector3(vector2.X, vector2.Y, vector.Z);
			span[4] = new Vector3(vector.X, vector.Y, vector2.Z);
			span[5] = new Vector3(vector2.X, vector.Y, vector2.Z);
			span[6] = new Vector3(vector.X, vector2.Y, vector2.Z);
			span[7] = new Vector3(vector2.X, vector2.Y, vector2.Z);
			Matrix4x4 matrix = reference.ModelMatrix * viewProjection;
			Span<Vector3> span3 = span;
			for (int j = 0; j < span3.Length; j++)
			{
				Vector4 vector3 = Vector4.Transform(new Vector4(span3[j], 1f), matrix);
				if (vector3.W <= 0.0001f)
				{
					return null;
				}
				float y = (vector3.X / vector3.W * 0.5f + 0.5f) * (float)w;
				float y2 = (0.5f - vector3.Y / vector3.W * 0.5f) * (float)h;
				num = MathF.Min(num, y);
				num3 = MathF.Max(num3, y);
				num2 = MathF.Min(num2, y2);
				num4 = MathF.Max(num4, y2);
			}
		}
		if (num > num3)
		{
			return new RawRect(0, 0, 0, 0);
		}
		return new RawRect(Math.Clamp((int)MathF.Floor(num - margin), 0, w), Math.Clamp((int)MathF.Floor(num2 - margin), 0, h), Math.Clamp((int)MathF.Ceiling(num3 + margin), 0, w), Math.Clamp((int)MathF.Ceiling(num4 + margin), 0, h));
	}

	private void ScissorFullscreen(RawRect? rect)
	{
		if (rect.HasValue)
		{
			RawRect valueOrDefault = rect.GetValueOrDefault();
			context.RSSetState(p.CullNoneScissored);
			context.RSSetScissorRect(valueOrDefault.Left, valueOrDefault.Top, valueOrDefault.Right - valueOrDefault.Left, valueOrDefault.Bottom - valueOrDefault.Top);
		}
		else
		{
			context.RSSetState(p.CullNone);
		}
	}

	public void PrepareAhead(int w, int h, bool studio)
	{
		lock (targetsLock)
		{
			PrepareTargets(w, h);
			if (studio)
			{
				PrepareOcclusionTargets();
				PrepareShadowMap();
			}
		}
	}

	private void PrepareTargets(int w, int h)
	{
		Monitor.Enter(targetsLock);
		try
		{
			PrepareTargetsLocked(w, h);
		}
		finally
		{
			Monitor.Exit(targetsLock);
		}
	}

	private void PrepareTargetsLocked(int w, int h)
	{
		if (w != width || h != height || (object)msaaColor == null)
		{
			DisposeTargets();
			width = w;
			height = h;
			ID3D11Device1 iD3D11Device = device;
			Texture2DDescription description = new Texture2DDescription
			{
				Width = (uint)w,
				Height = (uint)h,
				MipLevels = 1u,
				ArraySize = 1u,
				Format = Format.B8G8R8A8_UNorm,
				SampleDescription = new SampleDescription(4u, 0u),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.RenderTarget
			};
			msaaColor = iD3D11Device.CreateTexture2D(in description);
			msaaColorView = device.CreateRenderTargetView(msaaColor, null);
			ID3D11Device1 iD3D11Device2 = device;
			description = new Texture2DDescription
			{
				Width = (uint)w,
				Height = (uint)h,
				MipLevels = 1u,
				ArraySize = 1u,
				Format = Format.D32_Float,
				SampleDescription = new SampleDescription(4u, 0u),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil
			};
			msaaDepth = iD3D11Device2.CreateTexture2D(in description);
			msaaDepthView = device.CreateDepthStencilView(msaaDepth, null);
			(ID3D11Texture2D, ID3D11RenderTargetView, ID3D11ShaderResourceView) tuple = ColourTarget(w, h, Format.R32_UInt);
			opaqueIds = tuple.Item1;
			opaqueIdsTarget = tuple.Item2;
			opaqueIdsView = tuple.Item3;
			tuple = ColourTarget(w, h, Format.R32_UInt);
			clearIds = tuple.Item1;
			clearIdsTarget = tuple.Item2;
			clearIdsView = tuple.Item3;
			idDepth = DepthTexture(w, h);
			idDepthView = device.CreateDepthStencilView(idDepth, null);
			halfWidth = (w + 1) / 2;
			halfHeight = (h + 1) / 2;
		}
	}

	private (ID3D11Texture2D, ID3D11RenderTargetView, ID3D11ShaderResourceView) ColourTarget(int w, int h, Format format)
	{
		ID3D11Device1 iD3D11Device = device;
		Texture2DDescription description = new Texture2DDescription
		{
			Width = (uint)w,
			Height = (uint)h,
			MipLevels = 1u,
			ArraySize = 1u,
			Format = format,
			SampleDescription = new SampleDescription(1u, 0u),
			Usage = ResourceUsage.Default,
			BindFlags = (BindFlags.ShaderResource | BindFlags.RenderTarget)
		};
		ID3D11Texture2D iD3D11Texture2D = iD3D11Device.CreateTexture2D(in description);
		return (iD3D11Texture2D, device.CreateRenderTargetView(iD3D11Texture2D, null), device.CreateShaderResourceView(iD3D11Texture2D, null));
	}

	private ID3D11Texture2D DepthTexture(int w, int h)
	{
		ID3D11Device1 iD3D11Device = device;
		Texture2DDescription description = new Texture2DDescription
		{
			Width = (uint)w,
			Height = (uint)h,
			MipLevels = 1u,
			ArraySize = 1u,
			Format = Format.D32_Float,
			SampleDescription = new SampleDescription(1u, 0u),
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.DepthStencil
		};
		return iD3D11Device.CreateTexture2D(in description);
	}

	private bool PrepareOcclusionTargets()
	{
		lock (targetsLock)
		{
			return PrepareOcclusionTargetsLocked();
		}
	}

	private bool PrepareOcclusionTargetsLocked()
	{
		if ((object)distance != null)
		{
			return true;
		}
		(ID3D11Texture2D, ID3D11RenderTargetView, ID3D11ShaderResourceView) tuple = ColourTarget(halfWidth, halfHeight, Format.R32_Float);
		distance = tuple.Item1;
		distanceTarget = tuple.Item2;
		distanceView = tuple.Item3;
		distanceDepth = DepthTexture(halfWidth, halfHeight);
		distanceDepthView = device.CreateDepthStencilView(distanceDepth, null);
		(rawOcclusion, rawOcclusionTarget, rawOcclusionView) = ColourTarget(halfWidth, halfHeight, Format.R16_Float);
		(occlusion, occlusionTarget, occlusionView) = ColourTarget(halfWidth, halfHeight, Format.R16_Float);
		return true;
	}

	private void PrepareShadowMap()
	{
		lock (targetsLock)
		{
			PrepareShadowMapLocked();
		}
	}

	private void PrepareShadowMapLocked()
	{
		if ((object)shadowMap == null)
		{
			ID3D11Device1 iD3D11Device = device;
			Texture2DDescription description = new Texture2DDescription
			{
				Width = 1024u,
				Height = 1024u,
				MipLevels = 1u,
				ArraySize = 1u,
				Format = Format.R32_Typeless,
				SampleDescription = new SampleDescription(1u, 0u),
				Usage = ResourceUsage.Default,
				BindFlags = (BindFlags.ShaderResource | BindFlags.DepthStencil)
			};
			shadowMap = iD3D11Device.CreateTexture2D(in description);
			shadowDepthView = device.CreateDepthStencilView(shadowMap, new DepthStencilViewDescription(DepthStencilViewDimension.Texture2D, Format.D32_Float));
			shadowView = device.CreateShaderResourceView(shadowMap, new ShaderResourceViewDescription(shadowMap, ShaderResourceViewDimension.Texture2D, Format.R32_Float));
		}
	}

	private void DisposeTargets()
	{
		IDisposable[] array = new IDisposable[23]
		{
			msaaColorView, msaaColor, msaaDepthView, msaaDepth, opaqueIdsTarget, opaqueIdsView, opaqueIds, clearIdsTarget, clearIdsView, clearIds,
			idDepthView, idDepth, distanceTarget, distanceView, distance, distanceDepthView, distanceDepth, rawOcclusionTarget, rawOcclusionView, rawOcclusion,
			occlusionTarget, occlusionView, occlusion
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i]?.Dispose();
		}
		msaaColor = null;
		distance = null;
	}

	private void UnbindPixelResources()
	{
		context.PSSetShaderResources(0u, NullViews);
	}

	public void EncodeMarker(Scene3D scene, ID3D11Texture2D target, ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, Viewport viewport, in InkOutline outline, bool illustrated)
	{
		PrepareTargets(targetWidth, targetHeight);
		PrepareItems(scene);
		Matrix4x4 viewMatrix = scene.ViewMatrix;
		MarkerFrame markerFrame = default(MarkerFrame);
		markerFrame.ViewProjection = viewMatrix * scene.ProjectionMatrix;
		markerFrame.Eye = new Vector4(scene.Eye, 1f);
		MarkerFrame value = markerFrame;
		if (illustrated)
		{
			Vector3 vector = new Vector3(viewMatrix.M11, viewMatrix.M21, viewMatrix.M31);
			Vector3 vector2 = new Vector3(viewMatrix.M12, viewMatrix.M22, viewMatrix.M32);
			Vector3 vector3 = new Vector3(viewMatrix.M13, viewMatrix.M23, viewMatrix.M33);
			Vector3 vector4 = Vector3.Normalize(-0.5f * vector + 0.85f * vector2 + 0.6f * vector3);
			value.Style = new Vector4(1f, vector4.X, vector4.Y, vector4.Z);
		}
		Upload(markerFrameBuffer, in value);
		((ID3D11DeviceContext)context).ClearRenderTargetView(msaaColorView, new Color4(0f, 0f, 0f, 0f));
		context.ClearDepthStencilView(msaaDepthView, DepthStencilClearFlags.Depth, 1f, 0);
		context.OMSetRenderTargets(msaaColorView, msaaDepthView);
		context.RSSetViewport(viewport);
		context.VSSetShader(p.MarkerVertex);
		context.PSSetShader(p.MarkerPixel);
		context.VSSetConstantBuffer(0u, markerFrameBuffer);
		context.PSSetConstantBuffer(0u, markerFrameBuffer);
		context.OMSetBlendState(p.BlendAlpha);
		context.OMSetDepthStencilState(p.DepthWriting);
		Span<DrawItem3D> span = CollectionsMarshal.AsSpan(Items);
		foreach (int item in opaque)
		{
			context.RSSetState(span[item].Material.IsDoubleSided ? p.CullNone : p.CullBack);
			DrawItem(item, null);
		}
		context.OMSetDepthStencilState(p.DepthReading);
		foreach (int item2 in transparent)
		{
			if (span[item2].Material.IsDoubleSided)
			{
				context.RSSetState(p.CullFront);
				DrawItem(item2, null);
				context.RSSetState(p.CullBack);
				DrawItem(item2, null);
			}
			else
			{
				context.RSSetState(p.CullBack);
				DrawItem(item2, null);
			}
		}
		context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
		context.ResolveSubresource(target, 0u, msaaColor, 0u, Format.B8G8R8A8_UNorm);
		GpuPassTimer.Current?.Mark(context, "marker colour + resolve");
		if (outline.Width > 0f)
		{
			EncodeInkOutline(targetView, viewport, markerFrameBuffer, in outline, PartsPixelBounds(value.ViewProjection * ViewportAdjustment(viewport, targetWidth, targetHeight), targetWidth, targetHeight, OutlineReach(in outline)));
		}
	}

	private static float OutlineReach(in InkOutline outline)
	{
		return MathF.Max(outline.Width, outline.SeamWidth ?? outline.Width) / 2f + 3f;
	}

	private unsafe void EncodeInkOutline(ID3D11RenderTargetView targetView, Viewport viewport, ID3D11Buffer frameBuffer, in InkOutline outline, RawRect? partsRect)
	{
		bool flag = transparent.Count > 0;
		Span<DrawItem3D> span = CollectionsMarshal.AsSpan(Items);
		context.VSSetShader(p.MarkerVertex);
		context.VSSetConstantBuffer(0u, frameBuffer);
		context.PSSetConstantBuffer(0u, frameBuffer);
		context.OMSetBlendState(p.BlendNone);
		context.OMSetDepthStencilState(p.DepthWriting);
		context.RSSetViewport(viewport);
		context.ClearDepthStencilView(idDepthView, DepthStencilClearFlags.Depth, 1f, 0);
		for (int i = 0; i < ((!flag) ? 1 : 2); i++)
		{
			ID3D11RenderTargetView renderTargetView = ((i == 0) ? opaqueIdsTarget : clearIdsTarget);
			((ID3D11DeviceContext)context).ClearRenderTargetView(renderTargetView, new Color4(0f, 0f, 0f, 0f));
			context.OMSetRenderTargets(renderTargetView, idDepthView);
			bool? flag2 = null;
			foreach (int item in (i == 0) ? opaque : transparent)
			{
				ref DrawItem3D reference = ref span[item];
				bool isCut = reference.Material.IsCut;
				if (flag2 != isCut)
				{
					context.PSSetShader(isCut ? p.PickingPixel : p.UncutIdPixel);
					flag2 = isCut;
				}
				context.RSSetState(Cull(in reference.Material, cutShowsInside: true));
				DrawItem(item, null);
			}
		}
		GpuPassTimer.Current?.Mark(context, "ink ids");
		context.OMSetRenderTargets(targetView);
		float num = (outline.SeamWidth ?? outline.Width) / 2f;
		OutlineConstants outlineConstants = default(OutlineConstants);
		outlineConstants.InkColor = outline.Color;
		outlineConstants.HalfWidth = outline.Width / 2f;
		outlineConstants.SeamHalfWidth = num;
		outlineConstants.ReadsClear = (flag ? 1u : 0u);
		OutlineConstants value = outlineConstants;
		float? seamShade = outline.SeamShade;
		bool flag3 = seamShade.HasValue && outline.SeamStrength > 0f;
		float seamStrength;
		if (!flag3)
		{
			seamShade = outline.SeamShade;
			seamStrength = ((!seamShade.HasValue) ? outline.SeamStrength : 0f);
		}
		else
		{
			seamStrength = outline.SeamStrength;
		}
		value.SeamStrength = seamStrength;
		value.SeamShade = (flag3 ? outline.SeamShade.Value : 1f);
		float[] array = EdgeNeighbours(MathF.Max(outline.Width / 2f, num));
		value.NeighbourCount = (uint)(array.Length / 4);
		for (int j = 0; j < array.Length && j < 320; j++)
		{
			value.Neighbours[j] = array[j];
		}
		Upload(outlineBuffer, in value);
		context.PSSetConstantBuffer(1u, outlineBuffer);
		context.PSSetShaderResource(0u, opaqueIdsView);
		context.PSSetShaderResource(1u, clearIdsView);
		context.VSSetShader(p.OutlineVertex);
		context.PSSetShader(flag3 ? p.InkAndSeamPixel : p.OutlinePixel);
		context.OMSetBlendState(flag3 ? p.BlendInkAndSeam : p.BlendPremultiplied);
		context.OMSetDepthStencilState(p.DepthOff);
		ScissorFullscreen(partsRect);
		context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
		context.Draw(3u, 0u);
		context.RSSetState(p.CullNone);
		UnbindPixelResources();
		GpuPassTimer.Current?.Mark(context, "ink outline");
		context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
	}

	private float[] EdgeNeighbours(float reach)
	{
		if (edgeTableReach == reach)
		{
			return edgeTable;
		}
		HashSet<(int, int)> hashSet = new HashSet<(int, int)>();
		List<float> list = new List<float>();
		for (int i = 1; i <= 5; i++)
		{
			float num = (reach + 1f) * (float)i / 5f;
			for (int j = 0; j < 16; j++)
			{
				float x = ((float)j + ((i % 2 == 0) ? 0.5f : 0f)) * ((float)Math.PI / 8f);
				int num2 = (int)MathF.Round(MathF.Cos(x) * num, MidpointRounding.AwayFromZero);
				int num3 = (int)MathF.Round(MathF.Sin(x) * num, MidpointRounding.AwayFromZero);
				if ((num2 != 0 || num3 != 0) && hashSet.Add((num2, num3)))
				{
					list.AddRange(new _003C_003Ez__ReadOnlyArray<float>(new float[4] { num2, num3, num, 0f }));
				}
			}
		}
		edgeTable = list.Take(320).ToArray();
		edgeTableReach = reach;
		return edgeTable;
	}

	private static float CutHatchSpacing(Scene3D scene, float viewportHeight, int targetWidth)
	{
		if (viewportHeight <= 0f)
		{
			return 1f;
		}
		float num = 2f * scene.CameraDistance * MathF.Tan(scene.Camera.FieldOfView * (float)Math.PI / 360f) / viewportHeight;
		return 9f * MathF.Max(1f, (float)targetWidth / 1600f) * num;
	}

	private static Matrix4x4 ViewportAdjustment(Viewport viewport, int w, int h)
	{
		float m = viewport.Width / (float)w;
		float m2 = viewport.Height / (float)h;
		float m3 = (2f * viewport.X + viewport.Width) / (float)w - 1f;
		float m4 = 1f - (2f * viewport.Y + viewport.Height) / (float)h;
		return new Matrix4x4(m, 0f, 0f, 0f, 0f, m2, 0f, 0f, 0f, 0f, 1f, 0f, m3, m4, 0f, 1f);
	}

	public void EncodeStudio(Scene3D scene, ID3D11Texture2D target, ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, Viewport viewport, in InkOutline outline)
	{
		StudioLook current = StudioLook.Current;
		PrepareTargets(targetWidth, targetHeight);
		p.BakeEnvironmentIfNeeded(context, current.RoomLevel);
		PrepareItems(scene);
		Span<DrawItem3D> span = CollectionsMarshal.AsSpan(Items);
		Matrix4x4 viewMatrix = scene.ViewMatrix;
		Vector3 vector = new Vector3(viewMatrix.M13, viewMatrix.M23, viewMatrix.M33);
		float num = MathF.Atan2(vector.X, vector.Z);
		Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num);
		Vector3 value = Vector3.Normalize(Vector3.Transform(KeyDirection, rotation));
		Vector3 value2 = Vector3.Normalize(Vector3.Transform(RimDirection, rotation));
		bool flag = true;
		(Vector3, Vector3)? objectBox = scene.ObjectBox;
		Vector3 vector2;
		Vector3 vector3;
		if (objectBox.HasValue)
		{
			(vector2, vector3) = objectBox.GetValueOrDefault();
		}
		else
		{
			vector2 = new Vector3(float.PositiveInfinity);
			vector3 = new Vector3(float.NegativeInfinity);
			Span<DrawItem3D> span2 = span;
			for (int i = 0; i < span2.Length; i++)
			{
				Vector3 value3 = Math3D.Translation(in span2[i].ModelMatrix);
				vector2 = Vector3.Min(vector2, value3);
				vector3 = Vector3.Max(vector3, value3);
			}
			flag = vector2.X <= vector3.X;
		}
		Vector3 vector4 = (flag ? ((vector2 + vector3) / 2f) : Vector3.Zero);
		float num2 = (flag ? MathF.Max(0.001f, (vector3 - vector2).Length() / 2f) : 1f);
		float num3 = (flag ? vector2.Y : 0f);
		Vector3 shadowDirection = ShadowDirection;
		Matrix4x4 matrix4x = Math3D.LookAt(vector4 + shadowDirection * num2 * 4f, vector4, (MathF.Abs(shadowDirection.Y) > 0.98f) ? Vector3.UnitZ : Vector3.UnitY);
		float num4 = num2 * 2.4f;
		Matrix4x4 m = matrix4x * Math3D.Orthographic(num4, num4, num2 * 0.5f, num2 * 8f);
		Matrix4x4 viewProjection = viewMatrix * scene.ProjectionMatrix * ViewportAdjustment(viewport, targetWidth, targetHeight);
		RawRect? partsRect = PartsPixelBounds(in viewProjection, targetWidth, targetHeight, OutlineReach(in outline));
		StudioFrame studioFrame = default(StudioFrame);
		studioFrame.ViewProjection = viewProjection;
		studioFrame.LightViewProjection = m;
		studioFrame.Eye = new Vector4(scene.Eye, 1f);
		studioFrame.Key = new Vector4(value, (num2 * 8f - num2 * 0.5f) * shadowDirection.Y);
		studioFrame.KeyColor = new Vector4(KeyColor * current.KeyIntensity, 0f);
		studioFrame.Rim = new Vector4(value2, current.Debug);
		studioFrame.RimColor = new Vector4(RimColor * current.RimIntensity, 0f);
		studioFrame.Environment = new Vector4(MathF.Cos(num), MathF.Sin(num), current.EnvironmentIntensity, current.Exposure);
		studioFrame.Shadow = new Vector4(0.0009765625f, 0.0015f, num2 * 0.004f, current.ShadowSoftness);
		studioFrame.Ground = new Vector4(num3 - num2 * 0.004f, vector4.X, vector4.Z, num2 * 1.9f);
		studioFrame.GroundColor = new Vector4(0.2f, 0.25f, 0.36f, current.GroundShadowOpacity);
		studioFrame.Occlusion = new Vector4(num2 * current.OcclusionRadius, current.OcclusionStrength, (current.OcclusionStrength > 0f) ? 1 : 0, current.DebugDepth ? 1 : 0);
		studioFrame.Target = new Vector4(targetWidth, targetHeight, 1f / (float)targetWidth, 1f / (float)targetHeight);
		studioFrame.Grade = new Vector4(current.AgX ? 1 : 0, current.LookPower, current.LookSaturation, 0f);
		studioFrame.CutHatch = new Vector4(CutHatchSpacing(scene, viewport.Height, targetWidth), 0f, 0f, 0f);
		StudioFrame value4 = studioFrame;
		Vector3 forward = -vector;
		float hw = (targetWidth + 1) / 2;
		float hh = (targetHeight + 1) / 2;
		Matrix4x4.Invert(viewProjection, out var inverse);
		Vector3 eye = scene.Eye;
		Vector3 vector5 = RayThrough(0f, 0f);
		value4.ViewForward = new Vector4(forward, 0f);
		value4.RayBase = new Vector4(vector5, 0f);
		value4.RayStepX = new Vector4(RayThrough(1f, 0f) - vector5, 0f);
		value4.RayStepY = new Vector4(RayThrough(0f, 1f) - vector5, 0f);
		Upload(studioFrameBuffer, in value4);
		context.VSSetConstantBuffer(0u, studioFrameBuffer);
		context.PSSetConstantBuffer(0u, studioFrameBuffer);
		PrepareShadowMap();
		shadowDrawnFrom.Clear();
		AppendMatrix(shadowDrawnFrom, in m);
		foreach (int item in opaque)
		{
			AppendMatrix(shadowDrawnFrom, in span[item].ModelMatrix);
		}
		foreach (int item2 in opaque)
		{
			if (span[item2].Material.IsCut)
			{
				Vector4 cutPlane = span[item2].Material.CutPlane;
				shadowDrawnFrom.Add(cutPlane.X);
				shadowDrawnFrom.Add(cutPlane.Y);
				shadowDrawnFrom.Add(cutPlane.Z);
				shadowDrawnFrom.Add(cutPlane.W);
			}
		}
		Span<float> span3 = CollectionsMarshal.AsSpan(shadowDrawnFrom);
		if (shadowDrawnFromCount != span3.Length || !span3.SequenceEqual(shadowDrawnFromCache.AsSpan(0, Math.Min(shadowDrawnFromCache.Length, span3.Length))))
		{
			if (shadowDrawnFromCache.Length < span3.Length)
			{
				shadowDrawnFromCache = new float[span3.Length * 2];
			}
			span3.CopyTo(shadowDrawnFromCache);
			shadowDrawnFromCount = span3.Length;
			context.ClearDepthStencilView(shadowDepthView, DepthStencilClearFlags.Depth, 1f, 0);
			context.OMSetRenderTargets((ID3D11RenderTargetView)null, shadowDepthView);
			context.RSSetViewport(new Viewport(0f, 0f, 1024f, 1024f));
			context.RSSetState(p.ShadowRaster);
			context.OMSetDepthStencilState(p.DepthWriting);
			context.OMSetBlendState(p.BlendNone);
			bool? flag2 = null;
			foreach (int item3 in opaque)
			{
				bool isCut = span[item3].Material.IsCut;
				if (flag2 != isCut)
				{
					context.VSSetShader(isCut ? p.ShadowCutVertex : p.ShadowVertex);
					context.PSSetShader(isCut ? p.ShadowCutPixel : null);
					flag2 = isCut;
				}
				DrawItem(item3, null);
			}
			context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
			GpuPassTimer.Current?.Mark(context, "shadow map");
		}
		ID3D11ShaderResourceView noOcclusionView = p.NoOcclusionView;
		ID3D11ShaderResourceView noOcclusionView2 = p.NoOcclusionView;
		if (value4.Occlusion.Z > 0.5f && opaque.Count > 0 && PrepareOcclusionTargets())
		{
			((ID3D11DeviceContext)context).ClearRenderTargetView(distanceTarget, new Color4(0f, 0f, 0f, 0f));
			context.ClearDepthStencilView(distanceDepthView, DepthStencilClearFlags.Depth, 1f, 0);
			context.OMSetRenderTargets(distanceTarget, distanceDepthView);
			context.RSSetViewport(new Viewport(0f, 0f, halfWidth, halfHeight));
			context.OMSetDepthStencilState(p.DepthWriting);
			context.OMSetBlendState(p.BlendNone);
			context.VSSetShader(p.StudioVertex);
			bool? flag3 = null;
			foreach (int item4 in opaque)
			{
				ref DrawItem3D reference = ref span[item4];
				bool isCut2 = reference.Material.IsCut;
				if (flag3 != isCut2)
				{
					context.PSSetShader(isCut2 ? p.DistancePixel : p.UncutDistancePixel);
					flag3 = isCut2;
				}
				context.RSSetState(Cull(in reference.Material, cutShowsInside: true));
				DrawItem(item4, null);
			}
			context.OMSetRenderTargets(rawOcclusionTarget);
			context.OMSetDepthStencilState(p.DepthOff);
			RawRect? rect;
			if (partsRect.HasValue)
			{
				RawRect valueOrDefault = partsRect.GetValueOrDefault();
				rect = new RawRect(Math.Max(0, valueOrDefault.Left / 2 - 16), Math.Max(0, valueOrDefault.Top / 2 - 16), Math.Min(halfWidth, valueOrDefault.Right / 2 + 17), Math.Min(halfHeight, valueOrDefault.Bottom / 2 + 17));
			}
			else
			{
				rect = null;
			}
			ScissorFullscreen(rect);
			context.VSSetShader(p.FullscreenVertex);
			context.PSSetShader(p.OcclusionPixel);
			context.PSSetShaderResource(0u, distanceView);
			context.Draw(3u, 0u);
			context.OMSetRenderTargets(occlusionTarget);
			context.PSSetShader(p.OcclusionBlurPixel);
			context.PSSetShaderResource(1u, rawOcclusionView);
			context.Draw(3u, 0u);
			context.RSSetState(p.CullNone);
			UnbindPixelResources();
			context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
			noOcclusionView = occlusionView;
			noOcclusionView2 = distanceView;
		}
		GpuPassTimer.Current?.Mark(context, "ambient occlusion");
		((ID3D11DeviceContext)context).ClearRenderTargetView(msaaColorView, new Color4(0f, 0f, 0f, 0f));
		context.ClearDepthStencilView(msaaDepthView, DepthStencilClearFlags.Depth, 1f, 0);
		context.OMSetRenderTargets(msaaColorView, msaaDepthView);
		context.RSSetViewport(new Viewport(0f, 0f, targetWidth, targetHeight));
		context.PSSetShaderResource(0u, p.EnvironmentView);
		context.PSSetShaderResource(1u, p.IrradianceView);
		context.PSSetShaderResource(2u, shadowView);
		context.PSSetShaderResource(3u, noOcclusionView);
		context.PSSetShaderResource(4u, noOcclusionView2);
		context.PSSetSampler(0u, p.EnvironmentSampler);
		context.PSSetSampler(1u, p.ShadowSampler);
		context.PSSetSampler(2u, p.PointClamp);
		context.VSSetShader(p.StudioVertex);
		context.OMSetDepthStencilState(p.DepthWriting);
		bool? flag4 = null;
		foreach (int item5 in opaque)
		{
			ref DrawItem3D reference2 = ref span[item5];
			bool isCut3 = reference2.Material.IsCut;
			if (flag4 != isCut3)
			{
				context.PSSetShader(isCut3 ? p.StudioPixel : p.StudioSolidPixel);
				context.OMSetBlendState(isCut3 ? p.BlendAlpha : p.BlendNone);
				flag4 = isCut3;
			}
			context.RSSetState(Cull(in reference2.Material, cutShowsInside: true));
			DrawItem(item5, null);
		}
		GpuPassTimer.Current?.Mark(context, "opaque parts (MSAA)");
		if (current.GroundShadowOpacity > 0f && opaque.Count > 0)
		{
			context.VSSetShader(p.GroundVertex);
			context.PSSetShader(p.GroundPixel);
			context.OMSetBlendState(p.BlendAlpha);
			context.OMSetDepthStencilState(p.DepthReading);
			context.RSSetState(p.CullNone);
			context.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
			context.Draw(4u, 0u);
			context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
		}
		GpuPassTimer.Current?.Mark(context, "ground shadow");
		context.VSSetShader(p.StudioVertex);
		context.PSSetShader(p.StudioPixel);
		context.OMSetBlendState(p.BlendAlpha);
		context.OMSetDepthStencilState(p.DepthReading);
		foreach (int item6 in transparent)
		{
			ref DrawItem3D reference3 = ref span[item6];
			if (reference3.Material.IsDoubleSided)
			{
				context.RSSetState(p.CullFront);
				DrawItem(item6, null);
				context.RSSetState(p.CullBack);
				DrawItem(item6, null);
			}
			else
			{
				context.RSSetState(Cull(in reference3.Material, cutShowsInside: true));
				DrawItem(item6, null);
			}
		}
		GpuPassTimer.Current?.Mark(context, "glass");
		UnbindPixelResources();
		context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
		context.ResolveSubresource(target, 0u, msaaColor, 0u, Format.B8G8R8A8_UNorm);
		GpuPassTimer.Current?.Mark(context, "resolve");
		if (outline.Width > 0f)
		{
			ID3D11Buffer buffer = markerFrameBuffer;
			MarkerFrame value5 = new MarkerFrame
			{
				ViewProjection = viewProjection,
				Eye = value4.Eye
			};
			Upload(buffer, in value5);
			EncodeInkOutline(targetView, new Viewport(0f, 0f, targetWidth, targetHeight), markerFrameBuffer, in outline, partsRect);
		}
		Vector3 RayThrough(float x, float y)
		{
			Vector4 vector6 = Vector4.Transform(new Vector4((x + 0.5f) / hw * 2f - 1f, 1f - (y + 0.5f) / hh * 2f, 0.5f, 1f), inverse);
			Vector3 vector7 = new Vector3(vector6.X, vector6.Y, vector6.Z) / vector6.W - eye;
			return vector7 / Vector3.Dot(vector7, forward);
		}
	}

	private static void AppendMatrix(List<float> list, in Matrix4x4 m)
	{
		list.Add(m.M11);
		list.Add(m.M12);
		list.Add(m.M13);
		list.Add(m.M14);
		list.Add(m.M21);
		list.Add(m.M22);
		list.Add(m.M23);
		list.Add(m.M24);
		list.Add(m.M31);
		list.Add(m.M32);
		list.Add(m.M33);
		list.Add(m.M34);
		list.Add(m.M41);
		list.Add(m.M42);
		list.Add(m.M43);
		list.Add(m.M44);
	}

	public unsafe string?[] PartIdentifiers(Scene3D scene, ReadOnlySpan<(double x, double y)> points, double pixelScale)
	{
		string[] array = new string[points.Length];
		int num = Math.Max(1, (int)(scene.Rect.Width * pixelScale));
		int num2 = Math.Max(1, (int)(scene.Rect.Height * pixelScale));
		var (iD3D11Texture2D, iD3D11RenderTargetView, iD3D11ShaderResourceView) = ColourTarget(num, num2, Format.R32_UInt);
		using (iD3D11Texture2D)
		{
			using (iD3D11RenderTargetView)
			{
				using (iD3D11ShaderResourceView)
				{
					using ID3D11Texture2D resource = DepthTexture(num, num2);
					using ID3D11DepthStencilView depthStencilView = device.CreateDepthStencilView(resource, null);
					ID3D11Device1 iD3D11Device = device;
					Texture2DDescription description = new Texture2DDescription
					{
						Width = (uint)Math.Max(1, points.Length),
						Height = 1u,
						MipLevels = 1u,
						ArraySize = 1u,
						Format = Format.R32_UInt,
						SampleDescription = new SampleDescription(1u, 0u),
						Usage = ResourceUsage.Staging,
						CPUAccessFlags = CpuAccessFlags.Read
					};
					using ID3D11Texture2D iD3D11Texture2D2 = iD3D11Device.CreateTexture2D(in description);
					PrepareItems(scene);
					ID3D11Buffer buffer = markerFrameBuffer;
					MarkerFrame value = new MarkerFrame
					{
						ViewProjection = scene.ViewMatrix * scene.ProjectionMatrix,
						Eye = new Vector4(scene.Eye, 1f)
					};
					Upload(buffer, in value);
					((ID3D11DeviceContext)context).ClearRenderTargetView(iD3D11RenderTargetView, new Color4(0f, 0f, 0f, 0f));
					context.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1f, 0);
					context.OMSetRenderTargets(iD3D11RenderTargetView, depthStencilView);
					context.RSSetViewport(new Viewport(0f, 0f, num, num2));
					context.VSSetShader(p.MarkerVertex);
					context.PSSetShader(p.PickingPixel);
					context.VSSetConstantBuffer(0u, markerFrameBuffer);
					context.OMSetBlendState(p.BlendNone);
					context.OMSetDepthStencilState(p.DepthWriting);
					Span<DrawItem3D> span = CollectionsMarshal.AsSpan(Items);
					for (int i = 0; i < span.Length; i++)
					{
						context.RSSetState(Cull(in span[i].Material, cutShowsInside: true));
						DrawItem(i, null);
					}
					context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
					for (int j = 0; j < points.Length; j++)
					{
						int num3 = Math.Clamp((int)(points[j].x * pixelScale), 0, num - 1);
						int num4 = Math.Clamp((int)(points[j].y * pixelScale), 0, num2 - 1);
						context.CopySubresourceRegion(iD3D11Texture2D2, 0u, (uint)j, 0u, 0u, iD3D11Texture2D, 0u, new Box(num3, num4, 0, num3 + 1, num4 + 1, 1));
					}
					uint* dataPointer = (uint*)context.Map(iD3D11Texture2D2, 0u).DataPointer;
					for (int k = 0; k < points.Length; k++)
					{
						uint num5 = dataPointer[k] & 0x7FFFFFFF;
						array[k] = ((num5 != 0 && num5 <= span.Length) ? span[(int)(num5 - 1)].Node.Id : null);
					}
					context.Unmap(iD3D11Texture2D2, 0u);
					return array;
				}
			}
		}
	}

	private unsafe ID3D11ShaderResourceView StructuredUpload<T>(ref ID3D11Buffer? buffer, ref ID3D11ShaderResourceView? view, ref int capacity, ReadOnlySpan<T> data) where T : unmanaged
	{
		int num = sizeof(T);
		if ((object)buffer == null || capacity < data.Length)
		{
			view?.Dispose();
			buffer?.Dispose();
			capacity = Math.Max(16, data.Length * 2);
			buffer = device.CreateBuffer(new BufferDescription((uint)(capacity * num), BindFlags.ShaderResource, ResourceUsage.Dynamic, CpuAccessFlags.Write, ResourceOptionFlags.BufferStructured, (uint)num), (SubresourceData?)null);
			view = device.CreateShaderResourceView(buffer, new ShaderResourceViewDescription
			{
				Format = Format.Unknown,
				ViewDimension = ShaderResourceViewDimension.Buffer,
				Buffer = new BufferShaderResourceView
				{
					FirstElement = 0u,
					NumElements = (uint)capacity
				}
			});
		}
		data.CopyTo(new Span<T>((void*)context.Map(buffer, 0u, MapMode.WriteDiscard).DataPointer, data.Length));
		context.Unmap(buffer, 0u);
		return view;
	}

	public unsafe void DrawCapsules(ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, ReadOnlySpan<Capsule> capsules)
	{
		if (!capsules.IsEmpty)
		{
			ID3D11ShaderResourceView shaderResourceView = StructuredUpload(ref capsuleBuffer, ref capsuleView, ref capsuleCapacity, capsules);
			if ((object)overlayConstants == null)
			{
				overlayConstants = DynamicConstants(sizeof(OverlayConstants));
			}
			Upload<OverlayConstants>(overlayConstants, new OverlayConstants
			{
				TargetSize = new Vector2(targetWidth, targetHeight)
			});
			context.OMSetRenderTargets(targetView);
			context.RSSetViewport(new Viewport(0f, 0f, targetWidth, targetHeight));
			context.RSSetState(p.CullNone);
			context.OMSetDepthStencilState(p.DepthOff);
			context.OMSetBlendState(p.BlendPremultiplied);
			context.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
			context.VSSetShader(p.CapsuleVertex);
			context.PSSetShader(p.CapsulePixel);
			context.VSSetConstantBuffer(0u, overlayConstants);
			context.VSSetShaderResource(0u, shaderResourceView);
			context.PSSetShaderResource(0u, shaderResourceView);
			context.DrawInstanced(4u, (uint)capsules.Length, 0u, 0u);
			context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
			UnbindPixelResources();
			context.VSSetShaderResource(0u, null);
			context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
		}
	}

	public void DrawSheetsUnder(ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, ReadOnlySpan<Sheet> sheets)
	{
		DrawSheets(targetView, targetWidth, targetHeight, sheets, p.BlendUnder);
	}

	public void DrawSheetsOver(ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, ReadOnlySpan<Sheet> sheets)
	{
		DrawSheets(targetView, targetWidth, targetHeight, sheets, p.BlendPremultiplied);
	}

	private unsafe void DrawSheets(ID3D11RenderTargetView targetView, int targetWidth, int targetHeight, ReadOnlySpan<Sheet> sheets, ID3D11BlendState blend)
	{
		if (!sheets.IsEmpty)
		{
			ID3D11ShaderResourceView shaderResourceView = StructuredUpload(ref sheetBuffer, ref sheetView, ref sheetCapacity, sheets);
			if ((object)overlayConstants == null)
			{
				overlayConstants = DynamicConstants(sizeof(OverlayConstants));
			}
			context.OMSetRenderTargets(targetView);
			context.RSSetViewport(new Viewport(0f, 0f, targetWidth, targetHeight));
			context.RSSetState(p.CullNone);
			context.OMSetDepthStencilState(p.DepthOff);
			context.OMSetBlendState(blend);
			context.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
			context.VSSetShader(p.SheetVertex);
			context.PSSetShader(p.SheetPixel);
			context.VSSetConstantBuffer(0u, overlayConstants);
			context.VSSetShaderResource(1u, shaderResourceView);
			context.PSSetShaderResource(1u, shaderResourceView);
			for (int i = 0; i < sheets.Length; i++)
			{
				Upload<OverlayConstants>(overlayConstants, new OverlayConstants
				{
					TargetSize = new Vector2(targetWidth, targetHeight),
					SheetIndex = (uint)i
				});
				context.Draw(4u, 0u);
			}
			context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
			UnbindPixelResources();
			context.VSSetShaderResource(1u, null);
			context.OMSetRenderTargets((ID3D11RenderTargetView)null, (ID3D11DepthStencilView?)null);
		}
	}

	public void Dispose()
	{
		Interlocked.Decrement(ref ThreeStage.liveRenderers);
		capsuleView?.Dispose();
		capsuleBuffer?.Dispose();
		sheetView?.Dispose();
		sheetBuffer?.Dispose();
		overlayConstants?.Dispose();
		DisposeTargets();
		shadowView?.Dispose();
		shadowDepthView?.Dispose();
		shadowMap?.Dispose();
		objectView?.Dispose();
		objectBuffer?.Dispose();
		markerFrameBuffer.Dispose();
		studioFrameBuffer.Dispose();
		outlineBuffer.Dispose();
	}
}
