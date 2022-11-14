using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace RayTracer
{
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
				Forward = math.normalize(cam.transform.forward),
				Right = math.normalize(cam.transform.right),
				Up = math.normalize(cam.transform.up)
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
					Direction = math.normalize(pixelPosition - cameraPosition)
				};
				var index = GetPixelIndex(new int2(x, y), resX);
				PixelColors[index] = Shade(ray, cameraPosition, 0).Color;
			}
		}

		public IntersectionResult RaySceneIntersection(Ray ray)
		{
			var smallestIntersectionDistance = float.MaxValue;
			var hitObject = new ObjectId
			{
				Index = -1,
				MeshIndex = -1,
				Type = ObjectType.None
			};

			// If ray doesn't intersect with Scene AABB, there's no need to check any object
			if (!RMath.RayAABBIntersection(ray, Scene.AABB))
			{
				return new IntersectionResult
				{
					Distance = smallestIntersectionDistance,
					ObjectId = hitObject
				};
			}

			var meshes = Scene.MeshData.Meshes;
			for (int meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
			{
				var mesh = meshes[meshIndex];
				if (RMath.RayAABBIntersection(ray, mesh.AABB))
				{
					for (var triIndex = 0; triIndex < mesh.Triangles.Length; triIndex++)
					{
						var triangle = mesh.Triangles[triIndex];

						if (RMath.RayTriangleIntersection(ray, triangle, out var intersectionDistance))
						{
							if (smallestIntersectionDistance > intersectionDistance)
							{
								smallestIntersectionDistance = intersectionDistance;
								hitObject.Type = ObjectType.MeshTriangle;
								hitObject.Index = triIndex;
								hitObject.MeshIndex = meshIndex;
							}
						}
					}
				}
			}

			var spheres = Scene.SphereData.Spheres;
			for (var sphereIndex = 0; sphereIndex < spheres.Count; sphereIndex++)
			{
				var sphere = spheres[sphereIndex];
				if (RMath.RaySphereIntersection(ray, sphere, out var closestIntersectionDistance))
				{
					if (smallestIntersectionDistance > closestIntersectionDistance)
					{
						smallestIntersectionDistance = closestIntersectionDistance;
						hitObject.Type = ObjectType.Sphere;
						hitObject.Index = sphereIndex;
					}
				}
			}

			var triangles = Scene.TriangleData.Triangles;
			for (var triIndex = 0; triIndex < triangles.Count; triIndex++)
			{
				var triangle = triangles[triIndex];
				if (RMath.RayTriangleIntersection(ray, triangle, out var intersectionDistance))
				{
					if (smallestIntersectionDistance > intersectionDistance)
					{
						smallestIntersectionDistance = intersectionDistance;
						hitObject.Type = ObjectType.Triangle;
						hitObject.Index = triIndex;
					}
				}
			}

			return new IntersectionResult
			{
				Distance = smallestIntersectionDistance,
				ObjectId = hitObject,
			};
		}

		// TODO-Optimize: Caching of data here.
		// TODO-Optimize: There are math inefficiencies here.
		// TODO: Optimize crash (infinite loop) here.
		private Rgb Shade(Ray pixelRay, float3 cameraPosition, int currentRayBounce)
		{
			Debug.Assert(RMath.IsNormalized(pixelRay.Direction));
			
			var hitResult = RaySceneIntersection(pixelRay);
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
			var (surfaceNormal, material) = GetSurfaceNormalAndMaterial(surfacePoint, pixelRayHitObject);
			var color = CalculateAmbient(material.AmbientReflectance, Scene.AmbientLight.Radiance);
			var cameraDirection = math.normalize(cameraPosition - surfacePoint);

			foreach (var pointLight in Scene.PointLights)
			{
				var lightPosition = pointLight.Position;
				var lightDirection = math.normalize(lightPosition - surfacePoint);
				var shadowRayOrigin = surfacePoint + surfaceNormal * ShadowRayEpsilon;
				var shadowRay = new Ray(shadowRayOrigin, lightDirection);
				var shadowRayHitResult = RaySceneIntersection(shadowRay);
				var lightDistanceSq = math.distancesq(surfacePoint, lightPosition);
				
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
				var diffuseRgb = CalculateDiffuse(receivedIrradiance, material.DiffuseReflectance, surfaceNormal, lightDirection);
				var specularRgb = CalculateSpecular(lightDirection, cameraDirection, surfaceNormal, material.SpecularReflectance, receivedIrradiance, material.PhongExponent);
				color += diffuseRgb + specularRgb;
			}
			
			if (material.IsMirror && currentRayBounce < MaxReflectionBounces)
			{
				var reflectRay = Reflect(surfacePoint, surfaceNormal, cameraDirection);
				var mirrorReflectance = material.MirrorReflectance;
				color += new Rgb(mirrorReflectance * Shade(reflectRay, cameraPosition, currentRayBounce + 1).Value);
			}

			return color;
		}

		private Ray Reflect(float3 surfacePoint, float3 surfaceNormal, float3 cameraDirection)
		{
			var newRayOrigin = surfacePoint + surfaceNormal * ShadowRayEpsilon;
			var newRayNormal = 2 * surfaceNormal * math.dot(cameraDirection, surfaceNormal) - cameraDirection;
			return new Ray(newRayOrigin, newRayNormal);
		}

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
				return new Rgb(0);
			}

			var v = lightDirection + cameraDirection;
			var halfwayVector = v / math.length(v);
			Debug.Assert(RMath.IsNormalized(halfwayVector));

			var cosNormalAndHalfway = math.max(0, math.dot(surfaceNormal, halfwayVector));
			return new Rgb(specularReflectance * math.pow(cosNormalAndHalfway, phongExponent) * receivedIrradiance);
		}

		private float3 GetSurfaceNormal(float3 surfacePoint, ObjectId objectId)
		{
			switch (objectId.Type)
			{
				case ObjectType.Sphere:
					return GetSphereNormal(surfacePoint, objectId.Index);
				case ObjectType.Triangle:
					return Scene.TriangleData.Normals[objectId.Index];
				case ObjectType.MeshTriangle:
					return Scene.MeshData.Meshes[objectId.MeshIndex].TriangleNormals[objectId.Index];
				case ObjectType.None:
				default:
					throw new ArgumentOutOfRangeException(nameof(objectId), objectId, null);
			}
		}

		private float3 GetMirrorReflectance(ObjectId id)
		{
			switch (id.Type)
			{
				case ObjectType.Sphere:
					return Scene.SphereData.Materials[id.Index].MirrorReflectance;
				case ObjectType.Triangle:
					return Scene.TriangleData.Materials[id.Index].MirrorReflectance;
				case ObjectType.MeshTriangle:
					return Scene.MeshData.Meshes[id.MeshIndex].MaterialData.MirrorReflectance;
				case ObjectType.None:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private float3 GetSphereNormal(float3 surfacePoint, int index)
		{
			var sphere = Scene.SphereData.Spheres[index];
			var surfaceNormal = math.normalize(surfacePoint - sphere.Center);
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