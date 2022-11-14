using System;
using System.Collections.Generic;

namespace RayTracer
{
	[Serializable]
	public struct SphereData
	{
		public List<Sphere> Spheres;
		public List<MaterialData> Materials;

		public void Clear()
		{
			Spheres.Clear();
			Materials.Clear();
		}
	}
}