using System;
using Unity.Mathematics;
using UnityEngine;

public enum DrawingMode
{
	
}

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
	public float3 TopLeft;
	public float3 TopRight;
	public float3 BottomRight;
	public float3 BottomLeft;
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
			TopLeft = center + new float3(-HalfHorizontalLength, HalfVerticalLength, 0),
			TopRight = center + new float3(HalfHorizontalLength, HalfVerticalLength, 0),
			BottomRight = center + new float3(HalfHorizontalLength, -HalfVerticalLength, 0),
			BottomLeft = center + new float3(-HalfHorizontalLength, -HalfVerticalLength, 0),
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

	private CameraData CameraData;

	public bool ToggleDrawBounds = true;
	public bool ToggleDrawHorizontalLines = true;
	public bool ToggleDrawVerticalLines = true;

	public Color ImagePlaneColor = Color.red;
	public Color RayColor = Color.yellow;
	private void OnDrawGizmos()
	{
		if (!Application.isPlaying)
			return;
		
		var rect = ImagePlane.GetRect(CameraData);

		Gizmos.DrawSphere(rect.TopLeft, 1f);
		Gizmos.DrawSphere(rect.TopRight, 1f);
		Gizmos.DrawSphere(rect.BottomLeft, 1f);
		Gizmos.DrawSphere(rect.BottomRight, 1f);
		
		// Gizmos.DrawLine(rect.TopLeft, rect.TopRight);
		// Gizmos.DrawLine(rect.TopLeft, rect.BottomLeft);
		// Gizmos.DrawLine(rect.TopRight, rect.BottomRight);
		// Gizmos.DrawLine(rect.BottomLeft, rect.BottomRight);
	}

	void Update()
	{
		var cam = Camera.main;
		CameraData = new CameraData
		{
			Position = cam.transform.position,
			Forward = cam.transform.forward,
			Right = cam.transform.right,
			Up = cam.transform.up
		};

		DrawImagePlane(CameraData);
	}

	// TODO: All these positions needs to rotate with camera
	void DrawImagePlane(CameraData cameraData)
	{
		if (ToggleDrawBounds)
		{
			DrawBounds(cameraData);
		}

		DrawLines(cameraData);
	}

	private void DrawLines(CameraData cameraData)
	{
		var start = ImagePlane.GetRect(cameraData).TopLeft;
		var resolutionX = ImagePlane.Resolution.X;

		if (ToggleDrawHorizontalLines)
		{
			// Horizontal Lines
			for (var x = 0; x <= resolutionX; x++)
			{
				var moveDownLength = ImagePlane.VerticalLength * x / resolutionX;
				var lineStart = start + new float3(0, -moveDownLength, 0);
				var lineEnd = lineStart + new float3(ImagePlane.HorizontalLength, 0, 0);
				Debug.DrawLine(lineStart, lineEnd, ImagePlaneColor);
			}
		}

		if (ToggleDrawVerticalLines)
		{
			var resolutionY = ImagePlane.Resolution.Y;

			// Vertical Lines
			for (var y = 0; y <= resolutionY; y++)
			{
				var moveRightLength = ImagePlane.HorizontalLength * y / resolutionY;
				var lineStart = start + new float3(moveRightLength, 0, 0);
				var lineEnd = lineStart + new float3(0, -ImagePlane.VerticalLength, 0);
				Debug.DrawLine(lineStart, lineEnd, ImagePlaneColor);
			}
		}
	}

	private void DrawBounds(CameraData cameraData)
	{
		var rect = ImagePlane.GetRect(cameraData);
		var color = ImagePlaneColor;
		Debug.DrawLine(rect.TopLeft, rect.TopRight, color);
		Debug.DrawLine(rect.TopLeft, rect.BottomLeft, color);
		Debug.DrawLine(rect.TopRight, rect.BottomRight, color);
		Debug.DrawLine(rect.BottomLeft, rect.BottomRight, color);
	}

	void DrawIntersections()
	{
	}
}