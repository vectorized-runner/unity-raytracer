using System;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace RayTracer
{
	public static class RMath
	{
		// TODO: Cleanup this code when porting, it uses code taken from internet
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

					// TODO: This can be optimized further (if t0 is greater than zero t1 can't be)
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

			x0 = 0.5f * (-b - sqrt(discriminant)) / a;
			x1 = 0.5f * (-b + sqrt(discriminant)) / a;
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