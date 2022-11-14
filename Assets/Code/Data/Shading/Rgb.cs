using System;
using Unity.Mathematics;
using UnityEngine;

namespace RayTracer
{
	[Serializable]
	public struct Rgb
	{
		public float3 Value;

		public Color Color => new Color(Value.x, Value.y, Value.z, 1f);

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