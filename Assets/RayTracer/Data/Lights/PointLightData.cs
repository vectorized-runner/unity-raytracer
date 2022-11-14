using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct PointLightData
	{
		public float3 Position;
		public float3 Intensity;
	}
}