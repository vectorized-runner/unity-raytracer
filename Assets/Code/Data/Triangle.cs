using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct Triangle
	{
		public float3 Vertex0;
		public float3 Vertex1;
		public float3 Vertex2;
	}
}