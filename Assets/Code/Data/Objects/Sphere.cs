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

		public AABB AABB =>
			new()
			{
				Min = Center - math.sqrt(RadiusSquared),
				Max = Center + math.sqrt(RadiusSquared),
			};
	}
}