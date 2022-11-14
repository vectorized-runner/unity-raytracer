using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct Mesh
	{
		public Triangle[] Triangles;
		public float3[] TriangleNormals;
		public MaterialData MaterialData;
	}
}