using Unity.Mathematics;

namespace RayTracer
{
	public struct AABB
	{
		public float3 Min;
		public float3 Max;

		public void Encapsulate(float3 point)
		{
			Min = math.min(point, Min);
			Max = math.max(point, Max);
		}

		public void Encapsulate(AABB other)
		{
			Min = math.min(Min, other.Min);
			Max = math.max(Max, other.Max);
		}
	}
}