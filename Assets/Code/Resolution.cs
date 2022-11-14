using System;

namespace RayTracer
{
	[Serializable]
	public struct Resolution
	{
		public int X;
		public int Y;

		public override string ToString()
		{
			return $"({X}, {Y})";
		}
	}
}