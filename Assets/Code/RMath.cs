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
		public static bool RaySphereIntersection(Ray ray, Sphere sphere, out float3 closestIntersection)
		{
			Debug.Assert(IsLengthEqual(ray.Direction, 1f));

			var oc = ray.Origin - sphere.Center;
			var uoc = dot(ray.Direction, oc);
			var discriminant = uoc * uoc - (lengthsq(oc) - sphere.RadiusSquared);

			if (discriminant < 0)
			{
				closestIntersection = default;
				return false;
			}

			// Ignore discriminant == 0 because it won't practically happen
			var sqrtDiscriminant = sqrt(discriminant);
			var bigRoot = -uoc + sqrtDiscriminant;
			
			if (bigRoot < 0)
			{
				closestIntersection = default;
				return false;
			}

			var smallRoot = -uoc - sqrtDiscriminant;
			var result = smallRoot < 0 ? bigRoot : smallRoot;
			closestIntersection = ray.GetPoint(result);
			return true;
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