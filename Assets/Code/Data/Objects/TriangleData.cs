using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct TriangleData
	{
		public List<Triangle> Triangles;
		public List<float3> Normals;
		public List<MaterialData> Materials;

		public void Clear()
		{
			Triangles.Clear();
			Normals.Clear();
			Materials.Clear();
		}
	}
}