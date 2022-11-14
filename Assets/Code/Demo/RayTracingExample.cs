using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace RayTracer
{
	public class RayTracingExample : MonoBehaviour
	{
		public ImagePlane ImagePlane;
		public Color BackgroundColor = Color.black;
		
		public bool ToggleDrawImagePlane = true;
		public bool ToggleDrawPixelRays = true;
		public bool ToggleDrawIntersections = true;
		public bool ToggleDrawPixelColors = true;
		public bool ToggleDrawSurfaceNormals = true;

		// These are for debug drawing
		public Color ImagePlaneColor = Color.red;
		public Color RayColor = Color.yellow;
		public Color IntersectionColor = Color.cyan;
		public Color TriangleColor = Color.blue;
		public Color SurfaceNormalColor = Color.green;

		// Lights
		public List<PointLightData> PointLights;
		public AmbientLightData AmbientLight;

		// Spheres
		// Separate hot and cold data
		public List<Sphere> Spheres;
		public List<MaterialData> SphereMaterials;

		// Triangles
		public List<Triangle> Triangles;
		public List<MaterialData> TriangleMaterials;
		public List<float3> TriangleNormals;

		private CameraData CameraData;
		private Color[] PixelColors = Array.Empty<Color>();

		private void Start()
		{
			Spheres = new List<Sphere>();
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
				return;

			var color = Gizmos.color;
			Gizmos.color = Color.yellow;
			foreach (var pointLight in PointLights)
			{
				Gizmos.DrawWireSphere(pointLight.Position, 1f);
			}

			Gizmos.color = color;

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

		private void FetchSceneComponents()
		{
			FetchTriangles();
			FetchSpheres();
			FetchPointLights();
			FetchAmbientLights();
		}

		private void FetchAmbientLights()
		{
			AmbientLight = default;
			var ambientLights = FindObjectsOfType<SceneAmbientLight>();

			switch (ambientLights.Length)
			{
				case > 1:
					Debug.LogError("There are more than Single Ambient Lights in the Scene.");
					return;
				case 0:
					return;
				default:
					AmbientLight = ambientLights[0].AmbientLight;
					break;
			}
		}

		private void FetchPointLights()
		{
			PointLights.Clear();

			var pointLights = FindObjectsOfType<ScenePointLight>();
			foreach (var sceneLight in pointLights)
			{
				PointLights.Add(sceneLight.Light);
			}
		}

		private void FetchTriangles()
		{
			Triangles.Clear();
			TriangleNormals.Clear();
			TriangleMaterials.Clear();

			foreach (var triangle in FindObjectsOfType<SceneTriangle>())
			{
				Triangles.Add(triangle.Triangle);
				TriangleNormals.Add(triangle.Triangle.Normal);
				TriangleMaterials.Add(triangle.Material);
			}
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
			FetchSceneComponents();
			DrawTriangles();

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

				if (ToggleDrawSurfaceNormals)
				{
					Debug.DrawRay(triangle.Center, triangle.Normal * 5f, SurfaceNormalColor);
				}
			}
		}

		private int GetPixelIndex(int2 pixelPosition, int resolutionX)
		{
			return pixelPosition.x + pixelPosition.y * resolutionX;
		}

		private void FetchSpheres()
		{
			Spheres.Clear();
			SphereMaterials.Clear();

			var spheres = FindObjectsOfType<SceneSphere>();
			foreach (var sphere in spheres)
			{
				Spheres.Add(sphere.Sphere);
				SphereMaterials.Add(sphere.Material);
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
				var pixelRay = new Ray
				{
					Origin = cameraPosition,
					Direction = math.normalize(pixelPosition - cameraPosition)
				};

				// Check intersection against each object
				{
					var index = GetPixelIndex(new int2(x, y), resX);
					var intersectionResult = GetRayIntersectionWithScene(pixelRay);
					
					if (intersectionResult.ObjectType != ObjectType.None)
					{
						var intersectionPoint = pixelRay.GetPoint(intersectionResult.Distance);
						PixelColors[index] = CalculatePixelColor(cameraPosition, intersectionPoint, intersectionResult.ObjectType, intersectionResult.ObjectIndex);

						if (ToggleDrawIntersections)
						{
							Debug.DrawLine(pixelRay.Origin, intersectionPoint, IntersectionColor);
						}
					}
					else
					{
						PixelColors[index] = BackgroundColor;
					}
				}
			}
		}

		public IntersectionResult GetRayIntersectionWithScene(Ray ray)
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

			return new IntersectionResult
			{
				Distance = smallestIntersectionDistance,
				ObjectType = hitObject,
				ObjectIndex = hitObjectIndex
			};
		}

		// TODO-Implement: Handle multiple lights
		// TODO-Implement: Handle non-diffuse types
		// TODO-Implement: Cache anything that can be cached here.
		// TODO-Optimize: There are math inefficiencies here.
		private Color CalculatePixelColor(float3 cameraPosition, float3 pointOnSurface, ObjectType objectType,
			int objectIndex)
		{
			var (surfaceNormal, material) = GetSurfaceNormalAndMaterial(pointOnSurface, objectType, objectIndex);
			var finalRgb = CalculateAmbient(material.AmbientReflectance, AmbientLight.Radiance);

			foreach (var pointLight in PointLights)
			{
				var lightPosition = pointLight.Position;
				var lightDirection = math.normalize(lightPosition - pointOnSurface);

				const float shadowRayEpsilon = 0.0001f;
				var shadowRayOrigin = pointOnSurface + surfaceNormal * shadowRayEpsilon;
				var shadowRay = new Ray(shadowRayOrigin, lightDirection);
				var intersectResult = GetRayIntersectionWithScene(shadowRay);

				if (intersectResult.ObjectType != ObjectType.None)
				{
					// This pixel is under shadow for that light
					continue;
				}
				
				// Shadow ray hit this object again, shouldn't happen
				Debug.Assert(!(intersectResult.ObjectType == objectType && intersectResult.ObjectIndex == objectIndex));
				
				var cameraDirection = math.normalize(cameraPosition - pointOnSurface);
				var lightDistanceSq = math.distancesq(pointOnSurface, lightPosition);
				var receivedIrradiance = pointLight.Intensity / lightDistanceSq;
				var diffuseRgb = CalculateDiffuse(receivedIrradiance, material.DiffuseReflectance, surfaceNormal, lightDirection);
				var specularRgb = CalculateSpecular(lightDirection, cameraDirection, surfaceNormal, material.SpecularReflectance, receivedIrradiance, material.PhongExponent);
				finalRgb += diffuseRgb + specularRgb;
			}

			return finalRgb.Color;
		}

		// TODO: Handle angle greater than 90, it's zero in that case.
		private Rgb CalculateSpecular(float3 lightDirection, float3 cameraDirection, float3 surfaceNormal,
			float3 specularReflectance, float receivedIrradiance, float phongExponent)
		{
			Debug.Assert(RMath.IsNormalized(lightDirection));
			Debug.Assert(RMath.IsNormalized(cameraDirection));
			Debug.Assert(RMath.IsNormalized(surfaceNormal));

			var lightDotNormal = math.dot(lightDirection, surfaceNormal);
			// Angle works like this since both vectors are normalized
			var angle = math.degrees(math.acos(lightDotNormal));
			// If this assertion fails, take the absolute of angle
			Debug.Assert(angle > 0f);

			// Light is coming from behind the surface 
			if (angle > 90f)
			{
				return new Rgb(float3.zero);
			}

			var v = lightDirection + cameraDirection;
			var halfwayVector = v / math.length(v);
			Debug.Assert(RMath.IsNormalized(halfwayVector));

			var cosNormalAndHalfway = math.max(0, math.dot(surfaceNormal, halfwayVector));
			return new Rgb(specularReflectance * math.pow(cosNormalAndHalfway, phongExponent) * receivedIrradiance);
		}

		private (float3 surfaceNormal, MaterialData material) GetSurfaceNormalAndMaterial(float3 pointOnSurface,
			ObjectType objectType, int objectIndex)
		{
			switch (objectType)
			{
				case ObjectType.Sphere:
				{
					var sphere = Spheres[objectIndex];
					var surfaceNormal = math.normalize(pointOnSurface - sphere.Center);
					var material = SphereMaterials[objectIndex];
					return (surfaceNormal, material);
				}
				case ObjectType.Triangle:
				{
					var surfaceNormal = TriangleNormals[objectIndex];
					var material = TriangleMaterials[objectIndex];
					return (surfaceNormal, material);
				}
				case ObjectType.None:
				default:
					throw new ArgumentOutOfRangeException(nameof(objectType), objectType, null);
			}
		}

		private Rgb CalculateAmbient(float3 ambientReflectance, float3 ambientRadiance)
		{
			return new Rgb(ambientRadiance * ambientReflectance);
		}

		private Rgb CalculateDiffuse(float receivedIrradiance, float3 diffuseReflectance, float3 surfaceNormal,
			float3 lightDirection)
		{
			Debug.Assert(receivedIrradiance >= 0f);
			Debug.Assert(RMath.IsNormalized(surfaceNormal));
			Debug.Assert(RMath.IsNormalized(lightDirection));

			var cosNormalAndLightDir = math.max(0, math.dot(lightDirection, surfaceNormal));

			return new Rgb
			{
				Value = diffuseReflectance * cosNormalAndLightDir * receivedIrradiance
			};
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