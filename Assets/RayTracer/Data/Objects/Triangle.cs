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

		public float3 Normal
		{
			get
			{
				// This might be -v / length(v)
				var v = math.cross(Vertex2 - Vertex0, Vertex1 - Vertex0);
				return v / math.length(v);
			}
		}

		public float3 Center => (Vertex0 + Vertex1 + Vertex2) * 0.3333f;
	}
}