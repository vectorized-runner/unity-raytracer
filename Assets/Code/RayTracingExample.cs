using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


[Serializable]
public struct Ray
{
	public float3 Origin;
	public float3 Direction;

	public float3 GetPoint(float distance)
	{
		return Origin + Direction * distance;
	}
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

public struct Sphere
{
	public float3 Center;
	public float Radius;
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

	public bool ToggleDrawImagePlane = true;
	public bool ToggleDrawRays = true;
	private bool ToggleDrawIntersections = true;

	public Color ImagePlaneColor = Color.red;
	public Color RayColor = Color.yellow;
	public Color IntersectionColor = Color.cyan;

	private List<Sphere> Spheres;
	
	private void OnDrawGizmos()
	{
		if (!Application.isPlaying)
			return;
		if (ToggleDrawImagePlane)
		{
			var rect = ImagePlane.GetRect(CameraData);

			Gizmos.DrawSphere(rect.TopLeft, 1f);
			Gizmos.DrawSphere(rect.TopRight, 1f);
			Gizmos.DrawSphere(rect.BottomLeft, 1f);
			Gizmos.DrawSphere(rect.BottomRight, 1f);
		}
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

		if (ToggleDrawImagePlane)
		{
			DrawImagePlane(CameraData);
		}

		if (ToggleDrawRays)
		{
			DrawRays(CameraData);
		}

		if (ToggleDrawIntersections)
		{
			UpdateSpheresInScene();
			DrawIntersections(CameraData);
		}
	}

	private void UpdateSpheresInScene()
	{
		Spheres.Clear();
		var meshFiltersInScene = FindObjectsOfType<MeshFilter>();

		foreach (var meshFilter in meshFiltersInScene)
		{
			if (meshFilter.sharedMesh.name.Contains("Sphere", StringComparison.OrdinalIgnoreCase))
			{
				// Sphere with scale 1 has 0.5f radius
				var scale = meshFilter.gameObject.transform.localScale.x;
				var radius = scale * 0.5f;
				Spheres.Add(new Sphere
				{
					Center = meshFilter.gameObject.transform.position,
					Radius = radius
				});
			}
		}
	}

	private void DrawIntersections(CameraData cameraData)
	{
		var resX = ImagePlane.Resolution.X;
		var resY = ImagePlane.Resolution.Y;
		var topLeft = ImagePlane.GetRect(cameraData).TopLeft;
		var up = cameraData.Up;
		var right = cameraData.Right;
		var horizontalLength = ImagePlane.HorizontalLength;
		var verticalLength = ImagePlane.VerticalLength;
		var cameraPosition = cameraData.Position;

		for (int x = 0; x < resX; x++)
		for (int y = 0; y < resY; y++)
		{
			// Get ray
			var rightMove = (x + 0.5f) * horizontalLength / resX;
			var downMove = (y + 0.5f) * verticalLength / resY;
			var pointOnPlane = topLeft + rightMove * right - up * downMove;
			var ray = new Ray
			{
				Origin = cameraPosition,
				Direction = math.normalize(pointOnPlane - cameraPosition)
			};
			
			// Check intersection against each sphere
			
			
		}
		
		throw new NotImplementedException();
	}

	private static bool IsLengthEqual(float3 v, float length)
	{
		return math.abs(math.lengthsq(v) - length * length) < 0.001f;
	}

	// Derived from:
	// https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-sphere-intersection
	private int RaySphereIntersection(Ray ray, Sphere sphere, out float3 intersectA, out float3 intersectB)
	{
		Debug.Assert(IsLengthEqual(ray.Direction, 1f));
		
		var oc = sphere.Center - ray.Origin;
		var rSquared = sphere.Radius * sphere.Radius;
		var a = 1f;
		var b = 2 * math.dot(ray.Direction, oc);
		var c = math.dot(oc, oc) - rSquared;

		switch (SolveQuadraticEquation(a, b, c, out var x0, out var x1))
		{
			case 0:
			{
				intersectA = default;
				intersectB = default;
				return 0;
			}
			case 1:
			{
				intersectA = intersectB = ray.GetPoint(x0);
				return 1;
			}
			case 2:
			{
				intersectA = ray.GetPoint(x0);
				intersectB = ray.GetPoint(x1);
				return 2;
			}
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private int SolveQuadraticEquation(float a, float b, float c, out float x0, out float x1)
	{
		var discriminant = b * b - 4 * a * c;
		if (discriminant < 0)
		{
			x0 = 0;
			x1 = 0;
			return 0;
		}
		if (discriminant == 0)
		{
			x0 = x1 = 0.5f * -b / a;
			return 1;
		}

		x0 = 0.5f * (-b + discriminant) / a;
		x1 = 0.5f * (-b - discriminant) / a;
		return 2;
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
		// This is no longer required, as the lines already draw the bounds
		// DrawBounds(cameraData);
		DrawLines(cameraData);
	}

	private void DrawLines(CameraData cameraData)
	{
		var start = ImagePlane.GetRect(cameraData).TopLeft;
		var resolutionX = ImagePlane.Resolution.X;
		var up = cameraData.Up;
		var right = cameraData.Right;

		// Horizontal Lines
		for (var x = 0; x <= resolutionX; x++)
		{
			var moveRightLength = ImagePlane.HorizontalLength * x / resolutionX;
			var lineStart = start + right * moveRightLength;
			var lineEnd = lineStart - up * ImagePlane.VerticalLength;
			Debug.DrawLine(lineStart, lineEnd, ImagePlaneColor);
		}

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