using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RayTracer
{
	public class RayTracingExample : MonoBehaviour
	{
		public ImagePlane ImagePlane;
		public Color BackgroundColor = Color.black;

		private CameraData CameraData;

		public bool ToggleDrawImagePlane = true;
		public bool ToggleDrawPixelRays = true;
		public bool ToggleDrawIntersections = true;
		public bool ToggleDrawPixelColors = true;

		// These are for debug drawing
		public Color ImagePlaneColor = Color.red;
		public Color RayColor = Color.yellow;
		public Color IntersectionColor = Color.cyan;
		public Color TriangleColor = Color.green;

		// Separate hot and cold data
		public List<Sphere> Spheres;
		public List<Color> SphereColors;

		public List<Triangle> Triangles;

		private Color[] PixelColors = Array.Empty<Color>();

		private void Start()
		{
			Spheres = new List<Sphere>();
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
				return;

			foreach (var sphere in Spheres)
			{
				Gizmos.DrawWireSphere(sphere.Center, math.sqrt(sphere.RadiusSquared));
			}

			if (ToggleDrawPixelColors)
			{
				var origColor = Gizmos.color;
				Gizmos.color = Color.black;

				var resX = ImagePlane.Resolution.X;
				var resY = ImagePlane.Resolution.Y;
				var topLeft = ImagePlane.GetRect(CameraData).TopLeft;
				var up = CameraData.Up;
				var right = CameraData.Right;
				var horizontalLength = ImagePlane.HorizontalLength;
				var verticalLength = ImagePlane.VerticalLength;

				var size = new float3(horizontalLength / resX, verticalLength / resY, 0.01f);

				for (var pixelIndex = 0; pixelIndex < PixelColors.Length; pixelIndex++)
				{
					var pixelCoordinates = GetPixelCoordinates(pixelIndex, resX);
					var rightMove = (pixelCoordinates.x + 0.5f) * horizontalLength / resX;
					var downMove = (pixelCoordinates.y + 0.5f) * verticalLength / resY;
					var pixelPosition = topLeft + rightMove * right - up * downMove;
					Gizmos.color = PixelColors[pixelIndex];
					Gizmos.DrawCube(pixelPosition, size);
				}

				Gizmos.color = origColor;
			}
		}

		private int2 GetPixelCoordinates(int pixelIndex, int resolutionX)
		{
			return new int2(pixelIndex % resolutionX, pixelIndex / resolutionX);
		}

		void Update()
		{
			var cam = Camera.main;

			CameraData = new CameraData
			{
				Position = cam.transform.position,
				Forward = math.normalize(cam.transform.forward),
				Right = math.normalize(cam.transform.right),
				Up = math.normalize(cam.transform.up)
			};

			ClearScreen();

			if (Input.GetKeyDown(KeyCode.Space))
			{
				Triangles.Clear();
				var center = CameraData.Position + new float3(0f, 0f, 15f);
				Triangles = CreateTriangles(center);

				Debug.Log("Created Triangles!");
			}

			DrawTriangles();
			UpdateSpheresInScene();

			if (ToggleDrawImagePlane)
			{
				DrawImagePlane(CameraData);
			}

			if (ToggleDrawPixelRays)
			{
				DrawRays(CameraData);
			}

			HandleIntersections(CameraData);
		}

		private void ClearScreen()
		{
			var requiredPixelCount = ImagePlane.Resolution.X * ImagePlane.Resolution.Y;
			if (requiredPixelCount != PixelColors.Length)
			{
				Array.Resize(ref PixelColors, requiredPixelCount);
			}

			Array.Clear(PixelColors, 0, requiredPixelCount);
		}

		private void DrawTriangles()
		{
			foreach (var triangle in Triangles)
			{
				Debug.DrawLine(triangle.Vertex0, triangle.Vertex1, TriangleColor);
				Debug.DrawLine(triangle.Vertex1, triangle.Vertex2, TriangleColor);
				Debug.DrawLine(triangle.Vertex2, triangle.Vertex0, TriangleColor);
			}
		}

		private Triangle CreateRandomTriangle(float3 center, float distanceMin, float distanceMax)
		{
			var r0 = Random.insideUnitCircle * Random.Range(distanceMin, distanceMax);
			var r1 = Random.insideUnitCircle * Random.Range(distanceMin, distanceMax);
			var r2 = Random.insideUnitCircle * Random.Range(distanceMin, distanceMax);
			var p0 = center + new float3(r0.x, r0.y, 0f);
			var p1 = center + new float3(r1.x, r1.y, 0f);
			var p2 = center + new float3(r2.x, r2.y, 0f);

			return new Triangle
			{
				Vertex0 = p0,
				Vertex1 = p1,
				Vertex2 = p2
			};
		}

		private List<Triangle> CreateTriangles(float3 center)
		{
			return new List<Triangle>
			{
				CreateRandomTriangle(center, 15f, 30f)
			};
		}

		private int GetPixelIndex(int2 pixelPosition, int resolutionX)
		{
			return pixelPosition.x + pixelPosition.y * resolutionX;
		}

		private void UpdateSpheresInScene()
		{
			Spheres.Clear();
			SphereColors.Clear();

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
						RadiusSquared = radius * radius
					});

					var color = meshFilter.GetComponent<MeshRenderer>().sharedMaterial.color;
					SphereColors.Add(color);
				}
			}
		}

		// TODO-Optimize: How to make this better for cache? 
		// 1. Convert 2d to 1d loop
		// 2. Invert Loop and Run vs. Spheres first, then Run vs. other shapes
		// Another idea: Run Sphere vs. Pixels first, Then Run Triangle vs. Pixels etc... (homogenous)
		// TODO-Optimize: We can collect all intersection distances, and find the smallest of them in a separate loop?
		private void HandleIntersections(CameraData cameraData)
		{
			var resX = ImagePlane.Resolution.X;
			var resY = ImagePlane.Resolution.Y;
			var topLeft = ImagePlane.GetRect(cameraData).TopLeft;
			var up = cameraData.Up;
			var right = cameraData.Right;
			var horizontalLength = ImagePlane.HorizontalLength;
			var verticalLength = ImagePlane.VerticalLength;
			var cameraPosition = cameraData.Position;

			// Traverse order swapped for better cache usage
			// arrayIndex = x + y * resX
			for (int y = 0; y < resY; y++)
			for (int x = 0; x < resX; x++)
			{
				var rightMove = (x + 0.5f) * horizontalLength / resX;
				var downMove = (y + 0.5f) * verticalLength / resY;
				var pixelPosition = topLeft + rightMove * right - up * downMove;
				var ray = new Ray
				{
					Origin = cameraPosition,
					Direction = math.normalize(pixelPosition - cameraPosition)
				};

				// Check intersection against each object
				{
					var smallestIntersectionDistance = float.MaxValue;
					var hitObject = ObjectType.None;
					var hitObjectIndex = -1;

					for (var sphereIndex = 0; sphereIndex < Spheres.Count; sphereIndex++)
					{
						var sphere = Spheres[sphereIndex];
						if (RMath.RaySphereIntersection(ray, sphere, out var closestIntersectionDistance))
						{
							if (smallestIntersectionDistance > closestIntersectionDistance)
							{
								smallestIntersectionDistance = closestIntersectionDistance;
								hitObject = ObjectType.Sphere;
								hitObjectIndex = sphereIndex;
							}
						}
					}

					for (var triIndex = 0; triIndex < Triangles.Count; triIndex++)
					{
						var triangle = Triangles[triIndex];
						if (RMath.RayTriangleIntersection(ray, triangle, out var intersectionDistance))
						{
							if (smallestIntersectionDistance > intersectionDistance)
							{
								smallestIntersectionDistance = intersectionDistance;
								hitObject = ObjectType.Triangle;
								hitObjectIndex = triIndex;
							}
						}
					}

					var index = GetPixelIndex(new int2(x, y), resX);
					if (hitObject != ObjectType.None)
					{
						var intersectionPoint = ray.GetPoint(smallestIntersectionDistance);
						PixelColors[index] = CalculatePixelColor(hitObject, hitObjectIndex);

						if (ToggleDrawIntersections)
						{
							Debug.DrawLine(ray.Origin, intersectionPoint, IntersectionColor);
						}
					}
					else
					{
						PixelColors[index] = BackgroundColor;
					}
				}
			}
		}

		private Color CalculatePixelColor(ObjectType objectType, int objectIndex)
		{
			return Color.white;
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
}