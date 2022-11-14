using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace RayTracer
{
	public static class RMath
	{
		private const float Epsilon = 0.00001f;

		// TODO-Port: Code taken from the internet
		public static bool RayAABBIntersection(Ray ray, AABB box)
		{
			var inverseDir = rcp(ray.Direction);
			var tmin = 0.0f;
			var tmax = INFINITY;
			
			for (var i = 0; i < 3; ++i) {
				var t1 = (box.Min[i] - ray.Origin[i]) * inverseDir[i];
				var t2 = (box.Max[i] - ray.Origin[i]) * inverseDir[i];
				tmin = min(max(t1, tmin), max(t2, tmin));
				tmax = max(min(t1, tmax), min(t2, tmax));
			}

			return tmin <= tmax;
		}

		// TODO-Port: Code taken from the internet, you know what to do.
		public static bool RayTriangleIntersection(Ray ray, Triangle triangle, out float intersectionDistance)
		{
			var vertex0 = triangle.Vertex0;
			var vertex1 = triangle.Vertex1;
			var vertex2 = triangle.Vertex2;
			var edge1 = vertex1 - vertex0;
			var edge2 = vertex2 - vertex0;
			var h = cross(ray.Direction, edge2);
			var a = dot(edge1, h);

			if (a > -Epsilon && a < Epsilon)
			{
				intersectionDistance = 0f;
				return false; // This ray is parallel to this triangle.
			}

			var f = 1.0f / a;
			var s = ray.Origin - vertex0;
			var u = f * dot(s, h);
			if (u < 0.0f || u > 1.0f)
			{
				intersectionDistance = 0f;
				return false;
			}

			var q = cross(s, edge1);
			var v = f * dot(ray.Direction, q);
			if (v < 0.0f || u + v > 1.0f)
			{
				intersectionDistance = 0f;
				return false;
			}

			// At this stage we can compute t to find out where the intersection point is on the line.
			var t = f * dot(edge2, q);
			if (t > Epsilon) // ray intersection
			{
				intersectionDistance = t;
				return true;
			}

			// This means that there is a line intersection but not a ray intersection.
			intersectionDistance = 0f;
			return false;
		}


		// TODO-Port: Cleanup this code when porting, it uses code taken from internet
		// TODO-Optimize: Skip Quadratic Equation part, use the most optimized math formula only
		// TODO-Optimize: Store RadiusSquared on Spheres?
		// TODO-Optimize: Only need to return for 1 root, not 2 roots, not used.
		// TODO-Optimize: On 2 root case, if t0 is greater than zero, we don't have to check t1.
		public static bool RaySphereIntersection(Ray ray, Sphere sphere, out float closestIntersectionDistance)
		{
			Debug.Assert(IsNormalized(ray.Direction));

			var oc = ray.Origin - sphere.Center;
			var uoc = dot(ray.Direction, oc);
			var discriminant = uoc * uoc - (lengthsq(oc) - sphere.RadiusSquared);

			if (discriminant < 0)
			{
				closestIntersectionDistance = 0f;
				return false;
			}

			// Ignore discriminant == 0 because it won't practically happen
			var sqrtDiscriminant = sqrt(discriminant);
			var bigRoot = -uoc + sqrtDiscriminant;

			if (bigRoot < 0)
			{
				closestIntersectionDistance = 0f;
				return false;
			}

			var smallRoot = -uoc - sqrtDiscriminant;
			closestIntersectionDistance = smallRoot < 0 ? bigRoot : smallRoot;
			return true;
		}

		public static bool AreEqual(float3 a, float3 b)
		{
			return all(abs(a - b) < Epsilon);
		}

		public static bool IsLengthEqual(float3 v, float length)
		{
			return abs(lengthsq(v) - length * length) < Epsilon;
		}

		public static bool IsNormalized(float3 v)
		{
			return IsLengthEqual(v, 1f);
		}
	}
}