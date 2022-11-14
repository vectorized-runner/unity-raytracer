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

		public IntersectionResult IntersectRay(Ray ray)
		{
			var smallestIntersectionDistance = float.MaxValue;
			var hitObject = new ObjectId
			{
				Index = -1,
				MeshIndex = -1,
				Type = ObjectType.None
			};

			// If ray doesn't intersect with Scene AABB, there's no need to check any object
			if (!RMath.RayAABBIntersection(ray, AABB))
			{
				return new IntersectionResult
				{
					Distance = smallestIntersectionDistance,
					ObjectId = hitObject
				};
			}

			var meshes = MeshData.Meshes;
			for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
			{
				var mesh = meshes[meshIndex];
				if (RMath.RayAABBIntersection(ray, mesh.AABB))
				{
					for (var triIndex = 0; triIndex < mesh.Triangles.Length; triIndex++)
					{
						var triangle = mesh.Triangles[triIndex];

						if (RMath.RayTriangleIntersection(ray, triangle, out var intersectionDistance))
						{
							if (smallestIntersectionDistance > intersectionDistance)
							{
								smallestIntersectionDistance = intersectionDistance;
								hitObject.Type = ObjectType.MeshTriangle;
								hitObject.Index = triIndex;
								hitObject.MeshIndex = meshIndex;
							}
						}
					}
				}
			}

			var spheres = SphereData.Spheres;
			for (var sphereIndex = 0; sphereIndex < spheres.Count; sphereIndex++)
			{
				var sphere = spheres[sphereIndex];
				if (RMath.RaySphereIntersection(ray, sphere, out var closestIntersectionDistance))
				{
					if (smallestIntersectionDistance > closestIntersectionDistance)
					{
						smallestIntersectionDistance = closestIntersectionDistance;
						hitObject.Type = ObjectType.Sphere;
						hitObject.Index = sphereIndex;
					}
				}
			}

			var triangles = TriangleData.Triangles;
			for (var triIndex = 0; triIndex < triangles.Count; triIndex++)
			{
				var triangle = triangles[triIndex];
				if (RMath.RayTriangleIntersection(ray, triangle, out var intersectionDistance))
				{
					if (smallestIntersectionDistance > intersectionDistance)
					{
						smallestIntersectionDistance = intersectionDistance;
						hitObject.Type = ObjectType.Triangle;
						hitObject.Index = triIndex;
					}
				}
			}

			return new IntersectionResult
			{
				Distance = smallestIntersectionDistance,
				ObjectId = hitObject,
			};
		}
	}
}