using System;
using Unity.Mathematics;

namespace RayTracer
{
	[Serializable]
	public struct MaterialData
	{
		public float3 DiffuseReflectance;
		public float3 AmbientReflectance;
		public float3 MirrorReflectance;
		public float3 SpecularReflectance;
		public float PhongExponent;
		public bool IsMirror;
	}
}