using Unity.Mathematics;
using UnityEngine;

namespace RayTracer
{
	public class SceneMesh : MonoBehaviour
	{
		public MeshFilter MeshFilter;
		public MaterialData Material;

		public Mesh Mesh
		{
			get
			{
				var unityMesh = MeshFilter.sharedMesh;
				var unityVerts = unityMesh.vertices;
				var unityTris = unityMesh.triangles;
				var triangleCount = unityTris.Length;
				Debug.Assert(triangleCount % 3 == 0);

				var tris = new Triangle[triangleCount / 3];
				var normals = new float3[triangleCount / 3];

				for (int i = 0; i < triangleCount; i += 3)
				{
					var p0 = unityVerts[unityTris[i]];
					var p1 = unityVerts[unityTris[i + 1]];
					var p2 = unityVerts[unityTris[i + 2]];
					var triangle = new Triangle { Vertex0 = p0, Vertex1 = p1, Vertex2 = p2 };
					tris[i / 3] = triangle;
					normals[i / 3] = triangle.Normal;
				}

				return new Mesh
				{
					MaterialData = Material,
					Triangles = tris,
					TriangleNormals = normals,
				};
			}
		}
	}
}