using Unity.Mathematics;
using UnityEngine;

namespace RayTracer
{
	public class SceneTriangle : MonoBehaviour
	{
		public float3 Offset0;
		public float3 Offset1;
		public float3 Offset2;

		public MaterialData Material;

		private float3 Center => transform.position;

		public Triangle Triangle =>
			new Triangle
			{
				Vertex0 = Center + Offset0,
				Vertex1 = Center + Offset1,
				Vertex2 = Center + Offset2,
			};
	}
}