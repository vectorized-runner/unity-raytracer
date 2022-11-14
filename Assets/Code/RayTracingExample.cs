using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct Ray
{
	public float3 Origin;
	public float3 Direction;
}

[Serializable]
public struct Resolution
{
	public int X;
	public int Y;
}

public struct ImageRect
{
	public float3 TopHalfHorizontalLength;
	public float3 TopRight;
	public float3 BottomRight;
	public float3 BottomHalfHorizontalLength;
}

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

	// TODO: All these positions needs to Rotate along with Camera
	public ImageRect GetRect(CameraData cameraData)
	{
		var center = Center(cameraData);

		return new ImageRect
		{
			TopHalfHorizontalLength = center + new float3(HalfHorizontalLength, HalfVerticalLength, 0),
			TopRight = center + new float3(-HalfHorizontalLength, HalfVerticalLength, 0),
			BottomRight = center + new float3(-HalfHorizontalLength, -HalfVerticalLength, 0),
			BottomHalfHorizontalLength = center + new float3(HalfHorizontalLength, -HalfVerticalLength, 0),
		};
	}
}

public struct CameraData
{
	public float3 Position;
	public float3 Forward;
	public float3 Right;
	public float3 Up;
}

public class RayTracingExample : MonoBehaviour
{
	public ImagePlane ImagePlane;

	void Update()
	{
		var cam = Camera.main;
		var cameraData = new CameraData
		{
			Position = cam.transform.position,
			Forward = cam.transform.forward,
			Right = cam.transform.right,
			Up = cam.transform.up
		};
		
		DrawImagePlane(cameraData);
	}

	// TODO: All these positions needs to rotate with camera
	void DrawImagePlane(CameraData cameraData)
	{
		DrawBounds(cameraData);

		var start = ImagePlane.GetRect(cameraData).TopHalfHorizontalLength;
		var resolutionX = ImagePlane.Resolution.X;
		
		// Horizontal Lines
		for (var x = 0; x <= resolutionX; x++)
		{
			var moveDown = ImagePlane.HalfVerticalLength * x / resolutionX;
			var lineStart = start + new float3(0, moveDown, 0);
			var lineEnd = lineStart + new float3(ImagePlane.HorizontalLength, 0, 0);
			Debug.DrawLine(lineStart, lineEnd, Color.blue);
		}
		
		var resolutionY = ImagePlane.Resolution.Y;

		// Vertical Lines
		for (var y = 0; y <= resolutionY; y++)
		{
			var moveRight = ImagePlane.HorizontalLength * y / resolutionX;
			var lineStart = start + new float3(moveRight, 0, 0);
			var lineEnd = lineStart + new float3(0, -ImagePlane.VerticalLength, 0);
			Debug.DrawLine(lineStart, lineEnd, Color.yellow);
		}
	}

	private void DrawBounds(CameraData cameraData)
	{
		var rect = ImagePlane.GetRect(cameraData);
		Debug.DrawLine(rect.TopHalfHorizontalLength, rect.TopRight, Color.red);
		Debug.DrawLine(rect.TopHalfHorizontalLength, rect.BottomHalfHorizontalLength, Color.red);
		Debug.DrawLine(rect.BottomHalfHorizontalLength, rect.BottomRight, Color.red);
		Debug.DrawLine(rect.TopRight, rect.BottomRight, Color.red);
	}

	void DrawIntersections()
	{
	}
}