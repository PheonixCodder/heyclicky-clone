using System.Numerics;
using System.Runtime.InteropServices;

namespace HeyClicky.Drawing3D;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct ObjectData
{
	public Matrix4x4 Model;

	public Vector4 Normal0;

	public Vector4 Normal1;

	public Vector4 Normal2;

	public Vector4 BaseColor;

	public Vector4 Emissive;

	public Vector4 Surface;

	public Vector4 Coat;

	public Vector4 Flags;

	public Vector4 Cut;

	public unsafe fixed float Bands[64];

	public const int Size = 464;

	public unsafe static void Write(ObjectData* destination, in DrawItem3D item)
	{
		ref readonly Material3D material = ref item.Material;
		destination->Model = item.ModelMatrix;
		Math3D.NormalColumns(in item.ModelMatrix, out destination->Normal0, out destination->Normal1, out destination->Normal2);
		destination->BaseColor = material.Color;
		destination->Emissive = new Vector4(material.Emissive, 0f);
		destination->Surface = new Vector4(material.Roughness, material.Metalness, material.Opacity, material.BandCount);
		destination->Coat = new Vector4(material.Clearcoat, material.ClearcoatRoughness, material.Sheen, material.IsGlass ? 1 : 0);
		destination->Flags = new Vector4(material.UsesVertexColors ? 1 : 0, material.IsFlatShaded ? 1 : 0, material.IsDoubleSided ? 1 : 0, 0f);
		destination->Cut = material.CutPlane;
		Vector4[] bands = material.Bands;
		if (bands != null)
		{
			Vector4* ptr = (Vector4*)destination->Bands;
			for (int i = 0; i < bands.Length && i < 16; i++)
			{
				ptr[i] = bands[i];
			}
		}
	}
}
