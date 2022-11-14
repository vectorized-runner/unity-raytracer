using System;
using System.Collections.Generic;

namespace RayTracer
{
	[Serializable]
	public struct MeshData
	{
		public List<Mesh> Meshes;

		public void Clear()
		{
			Meshes.Clear();
		}
	}
}