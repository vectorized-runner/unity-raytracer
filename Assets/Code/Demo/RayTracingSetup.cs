using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static RayTracer.RMath;

namespace RayTracer
{
	/// <summary>
	/// Future Optimization Choices:
	/// Implementing a BVH
	/// Cache MaterialIds, instead of storing Materials directly
	/// Multithreading
	/// Burst Compile
	/// </summary>
	// TODO-Optimization: Consider using MaterialId's, since there are very few materials
	// TODO-Optimization: Consider not storing Triangle normals
	public class RayTracingSetup : MonoBehaviour
	{
		public ImagePlane ImagePlane;
		public Color BackgroundColor = Color.black;
		public int MaxReflectionBounces = 0;

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

		public Scene Scene;
		private CameraData CameraData;
		private Color[] PixelColors = Array.Empty<Color>();

		const float ShadowRayEpsilon = 0.0001f;

		private void Start()
		{
			Scene.SphereData = new SphereData
			{
				Materials = new List<MaterialData>(),
				Spheres = new List<Sphere>()
			};

			Scene.TriangleData = new TriangleData
			{
				Normals = new List<float3>(),
				Triangles = new List<Triangle>(),
				Materials = new List<MaterialData>(),
			};

			Scene.MeshData = new MeshData
			{
				Meshes = new List<Mesh>(),
			};

			Scene.PointLights = new List<PointLightData>();
		}

		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
				return;

			var color = Gizmos.color;
			Gizmos.color = Color.yellow;
			foreach (var pointLight in Scene.PointLights)
			{
				Gizmos.DrawWireSphere(pointLight.Position, 1f);
			}

			Gizmos.color = color;

			foreach (var sphere in Scene.SphereData.Spheres)
			{
				Gizmos.DrawWireSphere(sphere.Center, sqrt(sphere.RadiusSquared));
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

		private void UpdateScene()
		{
			FetchTriangles();
			FetchSpheres();
			FetchMeshes();
			FetchPointLights();
			FetchAmbientLights();
			Scene.CalculateAABB();
		}

		private void FetchAmbientLights()
		{
			Scene.AmbientLight = default;
			var ambientLights = FindObjectsOfType<SceneAmbientLight>();

			switch (ambientLights.Length)
			{
				case > 1:
					Debug.LogError("There are more than Single Ambient Lights in the Scene.");
					return;
				case 0:
					return;
				default:
					Scene.AmbientLight = ambientLights[0].AmbientLight;
					break;
			}
		}

		private void FetchPointLights()
		{
			Scene.PointLights.Clear();

			var pointLights = FindObjectsOfType<ScenePointLight>();
			foreach (var sceneLight in pointLights)
			{
				Scene.PointLights.Add(sceneLight.Light);
			}
		}

		private void FetchTriangles()
		{
			Scene.TriangleData.Clear();

			foreach (var triangle in FindObjectsOfType<SceneTriangle>())
			{
				Scene.TriangleData.Triangles.Add(triangle.Triangle);
				Scene.TriangleData.Normals.Add(triangle.Triangle.Normal);
				Scene.TriangleData.Materials.Add(triangle.Material);
			}
		}

		void Update()
		{
			var cam = Camera.main;

			CameraData = new CameraData
			{
				Position = cam.transform.position,
				Forward = normalize(cam.transform.forward),
				Right = normalize(cam.transform.right),
				Up = normalize(cam.transform.up)
			};

			ClearScreen();
			UpdateScene();
			DrawTriangles();
			DrawMeshTriangles();

			if (ToggleDrawImagePlane)
			{
				DrawImagePlane(CameraData);
			}

			if (ToggleDrawPixelRays)
			{
				DrawRays(CameraData);
			}

			CastPixelRays(CameraData);
		}

		private void DrawMeshTriangles()
		{
			foreach (var mesh in Scene.MeshData.Meshes)
			{
				foreach (var triangle in mesh.Triangles)
				{
					DrawTriangle(triangle);
				}
			}
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
			foreach (var triangle in Scene.TriangleData.Triangles)
			{
				DrawTriangle(triangle);
			}
		}

		private void DrawTriangle(Triangle triangle)
		{
			Debug.DrawLine(triangle.Vertex0, triangle.Vertex1, TriangleColor);
			Debug.DrawLine(triangle.Vertex1, triangle.Vertex2, TriangleColor);
			Debug.DrawLine(triangle.Vertex2, triangle.Vertex0, TriangleColor);

			if (ToggleDrawSurfaceNormals)
			{
				Debug.DrawRay(triangle.Center, triangle.Normal * 5f, SurfaceNormalColor);
			}
		}

		private int GetPixelIndex(int2 pixelPosition, int resolutionX)
		{
			return pixelPosition.x + pixelPosition.y * resolutionX;
		}

		private void FetchMeshes()
		{
			Scene.MeshData.Clear();

			foreach (var mesh in FindObjectsOfType<SceneMesh>())
			{
				Scene.MeshData.Meshes.Add(mesh.Mesh);
			}
		}

		private void FetchSpheres()
		{
			Scene.SphereData.Clear();
			var spheres = FindObjectsOfType<SceneSphere>();

			foreach (var sphere in spheres)
			{
				Scene.SphereData.Spheres.Add(sphere.Sphere);
				Scene.SphereData.Materials.Add(sphere.Material);
			}
		}

		// TODO-Optimize: How to make this better for cache? 
		// 1. Convert 2d to 1d loop
		// 2. Invert Loop and Run vs. Spheres first, then Run vs. other shapes
		// Another idea: Run Sphere vs. Pixels first, Then Run Triangle vs. Pixels etc... (homogenous)
		// TODO-Optimize: We can collect all intersection distances, and find the smallest of them in a separate loop?
		private void CastPixelRays(CameraData cameraData)
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
					Direction = normalize(pixelPosition - cameraPosition)
				};
				var index = GetPixelIndex(new int2(x, y), resX);
				PixelColors[index] = Shade(ray, 0).Color;
			}
		}

		private Rgb Shade(Ray pixelRay, int currentRayBounce)
		{
			Debug.Assert(IsNormalized(pixelRay.Direction));

			var hitResult = Scene.IntersectRay(pixelRay);
			var pixelRayHitObject = hitResult.ObjectId;
			if (pixelRayHitObject.Type == ObjectType.None)
				return new Rgb(BackgroundColor);

			Debug.Assert(pixelRayHitObject.Type != ObjectType.None);

			if (ToggleDrawIntersections)
			{
				var intersectionPoint = pixelRay.GetPoint(hitResult.Distance);
				Debug.DrawLine(pixelRay.Origin, intersectionPoint, IntersectionColor);
			}

			var surfacePoint = pixelRay.GetPoint(hitResult.Distance);
			var rayOrigin = pixelRay.Origin;
			var (surfaceNormal, material) = GetSurfaceNormalAndMaterial(surfacePoint, pixelRayHitObject);
			var color = CalculateAmbient(material.AmbientReflectance, Scene.AmbientLight.Radiance);
			var rayDirection = normalize(rayOrigin - surfacePoint);

			foreach (var pointLight in Scene.PointLights)
			{
				var lightPosition = pointLight.Position;
				var lightDirection = normalize(lightPosition - surfacePoint);
				var shadowRayOrigin = surfacePoint + surfaceNormal * ShadowRayEpsilon;
				var shadowRay = new Ray(shadowRayOrigin, lightDirection);
				var shadowRayHitResult = Scene.IntersectRay(shadowRay);
				var lightDistanceSq = distancesq(surfacePoint, lightPosition);

				// TODO-Optimize: We can remove this branch, if ray-scene intersection returns infinite distance by default
				if (shadowRayHitResult.ObjectId.Type != ObjectType.None)
				{
					var hitDistanceSq = shadowRayHitResult.Distance * shadowRayHitResult.Distance;
					if (hitDistanceSq < lightDistanceSq)
					{
						// Shadow ray intersects with an object before light, no contribution from this light
						continue;
					}
				}

				// Shadow ray hit this object again, shouldn't happen
				Debug.Assert(shadowRayHitResult.ObjectId != pixelRayHitObject);

				var receivedIrradiance = pointLight.Intensity / lightDistanceSq;
				var diffuseRgb = CalculateDiffuse(receivedIrradiance, material.DiffuseReflectance, surfaceNormal,
					lightDirection);
				var specularRgb = CalculateSpecular(lightDirection, rayDirection, surfaceNormal,
					material.SpecularReflectance, receivedIrradiance, material.PhongExponent);
				color += diffuseRgb + specularRgb;
			}

			if (material.IsMirror && currentRayBounce < MaxReflectionBounces)
			{
				var reflectRay = Reflect(surfacePoint, surfaceNormal, rayDirection);
				var mirrorReflectance = material.MirrorReflectance;
				color += new Rgb(mirrorReflectance * Shade(reflectRay, currentRayBounce + 1).Value);
			}

			return color;
		}

		private Ray Reflect(float3 surfacePoint, float3 surfaceNormal, float3 rayDirection)
		{
			var newRayOrigin = surfacePoint + surfaceNormal * ShadowRayEpsilon;
			var newRayNormal = 2 * surfaceNormal * dot(rayDirection, surfaceNormal) - rayDirection;
			return new Ray(newRayOrigin, newRayNormal);
		}

		private Rgb CalculateSpecular(float3 lightDirection, float3 rayDirection, float3 surfaceNormal,
			float3 specularReflectance, float3 receivedIrradiance, float phongExponent)
		{
			Debug.Assert(IsNormalized(lightDirection));
			Debug.Assert(IsNormalized(rayDirection));
			Debug.Assert(IsNormalized(surfaceNormal));

			var lightDotNormal = dot(lightDirection, surfaceNormal);
			// Angle works like this since both vectors are normalized
			var angle = degrees(acos(lightDotNormal));
			// If this assertion fails, take the absolute of angle
			Debug.Assert(angle > 0f);

			// Light is coming from behind the surface 
			if (angle > 90f)
			{
				return new Rgb(0);
			}

			var v = lightDirection + rayDirection;
			var halfwayVector = v / length(v);
			Debug.Assert(IsNormalized(halfwayVector));

			var cosNormalAndHalfway = max(0, dot(surfaceNormal, halfwayVector));
			return new Rgb(specularReflectance * pow(cosNormalAndHalfway, phongExponent) * receivedIrradiance);
		}

		private float3 GetSphereNormal(float3 surfacePoint, int index)
		{
			var sphere = Scene.SphereData.Spheres[index];
			var surfaceNormal = normalize(surfacePoint - sphere.Center);
			return surfaceNormal;
		}

		private (float3 surfaceNormal, MaterialData material) GetSurfaceNormalAndMaterial(float3 surfacePoint,
			ObjectId id)
		{
			switch (id.Type)
			{
				case ObjectType.Sphere:
				{
					var surfaceNormal = GetSphereNormal(surfacePoint, id.Index);
					var material = Scene.SphereData.Materials[id.Index];
					return (surfaceNormal, material);
				}
				case ObjectType.Triangle:
				{
					var surfaceNormal = Scene.TriangleData.Normals[id.Index];
					var material = Scene.TriangleData.Materials[id.Index];
					return (surfaceNormal, material);
				}
				case ObjectType.MeshTriangle:
				{
					var surfaceNormal = Scene.MeshData.Meshes[id.MeshIndex].TriangleNormals[id.Index];
					var material = Scene.MeshData.Meshes[id.MeshIndex].MaterialData;
					return (surfaceNormal, material);
				}
				case ObjectType.None:
				default:
					throw new ArgumentOutOfRangeException(nameof(id), id, null);
			}
		}

		private Rgb CalculateAmbient(float3 ambientReflectance, float3 ambientRadiance)
		{
			return new Rgb(ambientRadiance * ambientReflectance);
		}

		private Rgb CalculateDiffuse(float3 receivedIrradiance, float3 diffuseReflectance, float3 surfaceNormal,
			float3 lightDirection)
		{
			Debug.Assert(IsNormalized(surfaceNormal));
			Debug.Assert(IsNormalized(lightDirection));

			var cosNormalAndLightDir = max(0, dot(lightDirection, surfaceNormal));

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
	}
}