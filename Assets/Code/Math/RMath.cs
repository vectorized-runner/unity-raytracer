using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace RayTracer
{
	public static class RMath
	{
		// TODO-Port: Code taken from the internet, you know what to do.
		public static bool RayTriangleIntersection(Ray ray, Triangle triangle, out float3 intersection)
		{
			const float epsilon = 0.0000001f;

			var vertex0 = triangle.Vertices.x;
			var vertex1 = triangle.Vertices.y;
			var vertex2 = triangle.Vertices.z;
			float3 edge1, edge2, h, s, q;
			float a, f, u, v;
			edge1 = vertex1 - vertex0;
			edge2 = vertex2 - vertex0;
			h = cross(ray.Direction, edge2);
			a = dot(edge1, h);

			if (a > -epsilon && a < epsilon)
			{
				intersection = default;
				return false; // This ray is parallel to this triangle.
			}

			f = 1.0f / a;
			s = ray.Origin - vertex0;
			u = f * dot(s, h);
			if (u < 0.0 || u > 1.0)
			{
				intersection = default;
				return false;
			}

			q = cross(s, edge1);
			v = f * dot(ray.Direction, q);
			if (v < 0.0 || u + v > 1.0)
			{
				intersection = default;
				return false;
			}

			// At this stage we can compute t to find out where the intersection point is on the line.
			var t = f * dot(edge2, q);
			if (t > epsilon) // ray intersection
			{
				intersection = ray.GetPoint(t);
				return true;
			}

			// This means that there is a line intersection but not a ray intersection.
			intersection = default;
			return false;
		}


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