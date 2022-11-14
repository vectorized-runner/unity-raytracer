using NUnit.Framework;
using Unity.Mathematics;

namespace RayTracer
{
	public static class RayTracerTests
	{
		[Test]
		public static void Case1()
		{
			var ray = new Ray
			{
				Origin = new float3(0, 0, 0),
				Direction = new float3(1, 0, 0),
			};

			var sphere = new Sphere
			{
				Center = new float3(300, 0, 0),
				Radius = 1f
			};
		}
	}
}