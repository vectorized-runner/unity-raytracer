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

			var roots = RMath.RaySphereIntersectionAnalytical(ray, sphere, out var p0, out var p1);
			Assert.AreEqual(2, roots);

			Assert.IsTrue(RMath.AreEqual(p0, new float3(299, 0, 0)), $"NotEqual: {p0}");
			Assert.IsTrue(RMath.AreEqual(p1, new float3(301, 0, 0)), $"NotEqual: {p1}");
		}
	}
}