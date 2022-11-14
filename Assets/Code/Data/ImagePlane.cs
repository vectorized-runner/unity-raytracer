using System;
using Unity.Mathematics;

namespace RayTracer
{
	/// <summary>
	/// TopLeft Coordinates is (0, 0), Going right goes in +x coordinates, going down goes in +y coordinates
	/// X is Horizontal, Y is Vertical
	/// </summary>
	[Serializable]
	public struct ImagePlane
	{
		public Resolution Resolution;

		public float DistanceToCamera;

		// Going in +x direction
		public float HalfHorizontalLength;

		// Going in +y direction
		public float HalfVerticalLength;

		public float HorizontalLength => HalfHorizontalLength * 2f;
		public float VerticalLength => HalfVerticalLength * 2f;

		public float3 Center(CameraData cameraData)
		{
			return cameraData.Position + cameraData.Forward * DistanceToCamera;
		}

		public ImageRect GetRect(CameraData cameraData)
		{
			var center = Center(cameraData);
			var halfUp = cameraData.Up * HalfVerticalLength;
			var halfRight = cameraData.Right * HalfHorizontalLength;

			return new ImageRect
			{
				TopLeft = center - halfRight + halfUp,
				TopRight = center + halfRight + halfUp,
				BottomRight = center + halfRight - halfUp,
				BottomLeft = center - halfRight - halfUp,
			};
		}
	}
}