using System.Collections.Generic;

namespace RayTracer
{
	public struct Scene
	{
		public TriangleData TriangleData;
		public MeshData MeshData;
		public SphereData SphereData;
		
		public List<PointLightData> PointLights;
		public AmbientLightData AmbientLight;

		public AABB AABB;

		public void CalculateAABB()
		{
			var aabb = new AABB();
			aabb.Min = float.MaxValue;
			aabb.Max = float.MinValue;

			foreach (var mesh in MeshData.Meshes)
			{
				aabb.Encapsulate(mesh.AABB);
			}

			foreach (var triangle in TriangleData.Triangles)
			{
				aabb.Encapsulate(triangle.Vertex0);
				aabb.Encapsulate(triangle.Vertex1);
				aabb.Encapsulate(triangle.Vertex2);
			}

			foreach (var sphere in SphereData.Spheres)
			{
				aabb.Encapsulate(sphere.AABB);
			}

			AABB = aabb;
		}
	}
}