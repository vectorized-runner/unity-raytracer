using UnityEngine;

namespace RayTracer
{
	public class SceneSphere : MonoBehaviour
	{
		public MaterialData Material;

		public Sphere Sphere
		{
			get
			{
				// Sphere with scale 1 has 0.5f radius
				var scale = transform.localScale.x;
				var radius = scale * 0.5f;

				return new Sphere
				{
					Center = transform.position,
					RadiusSquared = radius * radius
				};
			}
		}
	}
}