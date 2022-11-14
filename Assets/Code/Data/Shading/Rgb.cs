using System;
using Unity.Mathematics;
using UnityEngine;

namespace RayTracer
{
	[Serializable]
	public struct Rgb
	{
		public float3 Value;

		// The Rgb value we get is in (0, 255) range, Unity Color value is (0, 1) range
		public Color Color => new(Value.x / 255f, Value.y / 255f, Value.z / 255f, 1f);

		public Rgb(float3 value)
		{
			Value = value;
		}

		public static Rgb operator +(Rgb a) => a;
		public static Rgb operator -(Rgb a) => new(-a.Value);
		public static Rgb operator +(Rgb a, Rgb b) => new(a.Value + b.Value);
		public static Rgb operator -(Rgb a, Rgb b) => a + -b;

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}