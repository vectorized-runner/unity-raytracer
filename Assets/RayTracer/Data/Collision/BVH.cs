
using System;
using RayTracer;
using Unity.Mathematics;

public class BVH
{
	public BVHNode[] Nodes;
	public Triangle[] Triangles;
	private uint RootNodeIndex = 0;
	private uint NodesUsed = 1;

	public void BuildBVH(Triangle[] triangles)
	{
		// TODO: Handle primitive count properly.
		var primitiveCount = triangles.Length;
		
		ref var rootNode = ref Nodes[RootNodeIndex];
		rootNode.LeftChildIndex = 0;
		rootNode.RightChildIndex = 0;
		rootNode.FirstPrimitive = 0;
		rootNode.PrimitiveCount = primitiveCount;
		
		UpdateNodeBounds(RootNodeIndex);
		Subdivide(RootNodeIndex);
	}

	public void UpdateNodeBounds(uint nodeIndex)
	{
		ref var node = ref Nodes[nodeIndex];
		node.AABB.Min = float.MaxValue;
		node.AABB.Max = float.MinValue;

		var first = node.FirstPrimitive;

		for (int i = 0; i < node.PrimitiveCount; i++)
		{
			ref var tri = ref Triangles[first + i];
			node.AABB.Min = math.min(node.AABB.Min, tri.Vertex0);
			node.AABB.Min = math.min(node.AABB.Min, tri.Vertex1);
			node.AABB.Min = math.min(node.AABB.Min, tri.Vertex2);

			node.AABB.Max = math.max(node.AABB.Max, tri.Vertex0);
			node.AABB.Max = math.max(node.AABB.Max, tri.Vertex1);
			node.AABB.Max = math.max(node.AABB.Max, tri.Vertex2);
		}
	}

	public void Subdivide(uint nodeIndex)
	{
		ref var node = ref Nodes[nodeIndex];
		var extents = node.AABB.Max - node.AABB.Min;
		var axis = 0;
		if (extents.y > extents.x)
			axis = 1;
		// This is smart (using extents[axis])
		if (extents.z > extents[axis])
			axis = 2;

		var splitPosition = node.AABB.Min[axis] + extents[axis] * 0.5f;
		var i = node.FirstPrimitive;
		var j = i + node.PrimitiveCount - 1;
		while (i <= j)
		{
			if (Triangles[i].Center[axis] < splitPosition)
			{
				i++;
			}
			else
			{
				// This swaps properly!
				(Triangles[j], Triangles[i]) = (Triangles[i], Triangles[j]);
				j--;
			}
		}
		
		// TODO: This implementation isn't finished!

		throw new NotImplementedException();

	}
}

public struct BVHNode
{
	public AABB AABB;
	public uint LeftChildIndex;
	public uint RightChildIndex;
	public bool IsLeft;
	public int FirstPrimitive;
	public int PrimitiveCount;
}
