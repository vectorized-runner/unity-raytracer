using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace RayTracer
{
	public static class RMath
	{
		// This algorithm currently doesn't work! It's on hold!
		// Derived from:
		// https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
		public static int RaySphereIntersectionAnalytical(Ray ray, Sphere sphere, out float3 intersectA, out float3 intersectB)
		{
			// Debug.Assert(IsLengthEqual(ray.Direction, 1f));
			// Debug.Assert(Math.Abs(math.dot(ray.Direction, ray.Direction) - 1f) < 0.001f);

			var oc = ray.Origin - sphere.Center;
			var a = dot(ray.Direction, ray.Direction);
			var b = 2 * dot(ray.Direction, oc);
			var c = dot(oc, oc) - sphere.Radius * sphere.Radius;

			switch (SolveQuadraticEquation(a, b, c, out var x0, out var x1))
			{
				case 0:
				{
					intersectA = default;
					intersectB = default;
					return 0;
				}
				case 1:
				{
					intersectA = intersectB = ray.GetPoint(x0);
					return 1;
				}
				case 2:
				{
					intersectA = ray.GetPoint(x0);
					intersectB = ray.GetPoint(x1);
					return 2;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static int SolveQuadraticEquation(float a, float b, float c, out float x0, out float x1)
		{
			var discriminant = b * b - 4 * a * c;

			if (discriminant < 0)
			{
				x0 = 0;
				x1 = 0;
				return 0;
			}

			if (discriminant == 0)
			{
				x0 = x1 = 0.5f * -b / a;
				return 1;
			}

			x0 = 0.5f * (-b + sqrt(discriminant)) / a;
			x1 = 0.5f * (-b - sqrt(discriminant)) / a;
			return 2;
		}

		public static bool AreEqual(float3 a, float3 b)
		{
			return all(abs(a - b) < 0.0001f);
		}

		public static bool IsLengthEqual(float3 v, float length)
		{
			return abs(lengthsq(v) - length * length) < 0.0001f;
		}
	}
}