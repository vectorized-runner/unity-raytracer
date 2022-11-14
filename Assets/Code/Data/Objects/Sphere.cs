using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct Sphere
	{
		public float3 Center;
		public float RadiusSquared;

		public override string ToString()
		{
			return $"Center: {Center}, RadiusSq: {RadiusSquared}";
		}
	}
}