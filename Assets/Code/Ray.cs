using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct Ray
	{
		public float3 Origin;
		public float3 Direction;

		public float3 GetPoint(float distance)
		{
			return Origin + Direction * distance;
		}

		public override string ToString()
		{
			return $"Origin: {Origin}, Direction: {Direction}";
		}
	}
}