using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct MaterialData
	{
		public float3 DiffuseReflectance;
		public float3 AmbientReflectance;
	}
}