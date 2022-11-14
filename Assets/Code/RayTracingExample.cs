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
		DrawRays(CameraData);
	}

	private void DrawRays(CameraData cameraData)
	{
		var resX = ImagePlane.Resolution.X;
		var resY = ImagePlane.Resolution.Y;
		var topLeft = ImagePlane.GetRect(cameraData).TopLeft;
		var up = cameraData.Up;
		var right = cameraData.Right;
		var horizontalLength = ImagePlane.HorizontalLength;
		var verticalLength = ImagePlane.VerticalLength;

		for (int x = 0; x < resX; x++)
		for (int y = 0; y < resY; y++)
		{
			// Draw (x,y) pixel
			var rightMove = (x + 0.5f) * horizontalLength / resX;
			var downMove = (y + 0.5f) * verticalLength / resY;
			var point = topLeft + rightMove * right - up * downMove;
			Debug.DrawLine(cameraData.Position, point, RayColor);
		}
	}

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

		var up = cameraData.Up;
		var right = cameraData.Right;

		if (ToggleDrawHorizontalLines)
		{
			// Horizontal Lines
			for (var x = 0; x <= resolutionX; x++)
			{
				var moveRightLength = ImagePlane.HorizontalLength * x / resolutionX;
				var lineStart = start + right * moveRightLength;
				var lineEnd = lineStart - up * ImagePlane.VerticalLength;
				Debug.DrawLine(lineStart, lineEnd, ImagePlaneColor);
			}
		}

		if (ToggleDrawVerticalLines)
		{
			var resolutionY = ImagePlane.Resolution.Y;
			// Vertical Lines
			for (var y = 0; y <= resolutionY; y++)
			{
				var moveDownLength = ImagePlane.VerticalLength * y / resolutionY;
				var lineStart = start - up * moveDownLength;
				var lineEnd = lineStart + right * ImagePlane.HorizontalLength;
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
}