using System;

namespace RayTracer
{
	public struct ObjectId : IEquatable<ObjectId>
	{
		public ObjectType Type;
		public int Index;

		public static bool operator ==(ObjectId left, ObjectId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ObjectId left, ObjectId right)
		{
			return !(left == right);
		}

		public bool Equals(ObjectId other)
		{
			return Type == other.Type && Index == other.Index;
		}

		public override bool Equals(object obj)
		{
			return obj is ObjectId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((int)Type, Index);
		}
	}
}