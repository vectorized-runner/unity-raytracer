using System;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace RayTracer
{
	public static class RMath
	{
		// TODO-Port: Cleanup this code when porting, it uses code taken from internet
		// TODO-Optimize: Skip Quadratic Equation part, use the most optimized math formula only
		// TODO-Optimize: Store RadiusSquared on Spheres?
		// TODO-Optimize: Only need to return for 1 root, not 2 roots, not used.
		// TODO-Optimize: On 2 root case, if t0 is greater than zero, we don't have to check t1.
		public static int RaySphereIntersection(Ray ray, Sphere sphere, out float3 intersectA, out float3 intersectB)
		{
			Debug.Assert(IsLengthEqual(ray.Direction, 1f));

			var oc = ray.Origin - sphere.Center;
			var a = dot(ray.Direction, ray.Direction);
			var b = 2 * dot(ray.Direction, oc);
			var c = dot(oc, oc) - sphere.Radius * sphere.Radius;

			switch (SolveQuadraticEquation(a, b, c, out var t0, out var t1))
			{
				case 0:
				{
					intersectA = intersectB = default;
					return 0;
				}
				case 1:
				{
					if (t0 < 0)
					{
						intersectA = intersectB = default;
						return 0;
					}

					intersectA = intersectB = ray.GetPoint(t0);
					return 1;
				}
				case 2:
				{
					Debug.Assert(t1 > t0);
					var roots = 0;

					if (t0 < 0)
					{
						intersectA = default;
					}
					else
					{
						intersectA = ray.GetPoint(t0);
						roots++;
					}

					if (t1 < 0)
					{
						intersectB = default;
					}
					else
					{
						intersectB = ray.GetPoint(t1);
						roots++;
					}

					return roots;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// TODO-Port: Disc == 0 floating point errors? We can just ignore that case because it will never happen
		public static int SolveQuadraticEquation(float a, float b, float c, out float x0, out float x1)
		{
			var discriminant = b * b - 4 * a * c;

			if (discriminant < 0)
			{
				x0 = x1 = 0;
				return 0;
			}

			// Ignore Discriminant == 0 because it will not happen with floating point, and we'll use the same point anyway
			// if (discriminant == 0)
			// {
			// 	x0 = x1 = 0.5f * -b / a;
			// 	return 1;
			// }

			var sqrtDisc = sqrt(discriminant);
			x0 = 0.5f * (-b - sqrtDisc) / a;
			x1 = 0.5f * (-b + sqrtDisc) / a;
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