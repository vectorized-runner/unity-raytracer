using UnityEngine;

namespace RayTracer
{
	public class ScenePointLight : MonoBehaviour
	{
		public float Intensity;

		public PointLightData Light => new PointLightData
		{
			Position = transform.position,
			Intensity = Intensity
		};
	}
}