using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Random = UnityEngine.Random;

public static class StaticUtils
{
    public static float Remap(float input, float inputMin, float inputMax, float targetMin, float targetMax)
    {
        return targetMin + (input - inputMin) * (targetMax - targetMin) / (inputMax - inputMin);
    }

    public static int Array3DTo1D(int x, int y, int z, int xMax, int yMax) => (z * xMax * yMax) + (y * xMax) + x;

    public static Vector3Int Array1Dto3D(int idx, int xMax, int yMax)
    {
        int z = idx / (xMax * yMax);
        idx -= (z * xMax * yMax);
        int y = idx / xMax;
        int x = idx % xMax;
        return new Vector3Int(x, y, z);
    }

    public static int Array2dTo1d(int x, int y, int width) => y * width + x;

    public static Vector2Int Array1dTo2d(int i, int width) => new Vector2Int { x = i % width, y = i / width };

    public static bool PointInsideSphere(Vector3 Ppoint, Vector3 Ccenter, float Cradius) => (Vector3.Distance(Ppoint, Ccenter) <= Cradius);

    public static float Rounder(float x, float g = 16) => Mathf.Floor((x + g / 2) / g) * g;

    public static int RounderInt(float x, float g = 16) => (int)((Mathf.Floor((x + g / 2) / g) * g));

    public static int RounderInt(int x, int g = 16) => (int)Mathf.Floor((x + g / 2) / g) * g;

    public static bool IsTooSteep(Vector3 normal, Vector3 direction, float steepness) => Mathf.Abs(Vector3.Dot(normal, direction)) < steepness;

    public static Vector3 Snap(Vector3 pos, int v)
    {
        float x = pos.x;
        float y = pos.y;
        float z = pos.z;
        x = Mathf.FloorToInt(x / v) * v;
        y = Mathf.FloorToInt(y / v) * v;
        z = Mathf.FloorToInt(z / v) * v;
        return new Vector3(x, y, z);
    }

    public static T GetCopyOf<T>(this Component comp, T other) where T : Component
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null;

        //maybe static should be skipped!
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Default;
        PropertyInfo[] pinfos = type.GetProperties(flags);

        foreach (var pinfo in pinfos)
        {
            if (pinfo.CanWrite)
            {
                try
                {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch
                {
                    //It doesn't matter if we can't write here! This means the field not exist here.
                }
            }
        }

        FieldInfo[] finfos = type.GetFields(flags);

        foreach (var finfo in finfos)
        {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    public static List<Vector2> GeneratePoissonPoints(float minDistance, Vector2 bounds, int maxAttempts)
    {
        List<Vector2> points = new List<Vector2>();
        float cellSize = minDistance / Mathf.Sqrt(2);
        int[,] grid = new int[Mathf.CeilToInt(bounds.x / cellSize), Mathf.CeilToInt(bounds.y / cellSize)];
        List<Vector2> activeList = new List<Vector2>();

        Vector2 firstPoint = new Vector2(Random.Range(0, bounds.x), Random.Range(0, bounds.y));
        points.Add(firstPoint);
        activeList.Add(firstPoint);
        grid[Mathf.FloorToInt(firstPoint.x / cellSize), Mathf.FloorToInt(firstPoint.y / cellSize)] = points.Count;

        while (activeList.Count > 0)
        {
            int randomIndex = Random.Range(0, activeList.Count);
            Vector2 point = activeList[randomIndex];
            bool foundPoint = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                float radius = Random.Range(minDistance, minDistance * 2);
                Vector2 newPoint = new Vector2(point.x + radius * Mathf.Cos(angle), point.y + radius * Mathf.Sin(angle));

                if (newPoint.x >= 0 && newPoint.x < bounds.x && newPoint.y >= 0 && newPoint.y < bounds.y)
                {
                    int cellX = Mathf.FloorToInt(newPoint.x / cellSize);
                    int cellY = Mathf.FloorToInt(newPoint.y / cellSize);
                    bool canPlace = true;

                    for (int x = Mathf.Max(0, cellX - 2); x < Mathf.Min(grid.GetLength(0), cellX + 3); x++)
                    {
                        for (int y = Mathf.Max(0, cellY - 2); y < Mathf.Min(grid.GetLength(1), cellY + 3); y++)
                        {
                            if (grid[x, y] > 0 && Vector2.Distance(points[grid[x, y] - 1], newPoint) < minDistance)
                            {
                                canPlace = false;
                                break;
                            }
                        }
                    }

                    if (canPlace)
                    {
                        points.Add(newPoint);
                        activeList.Add(newPoint);
                        grid[cellX, cellY] = points.Count;
                        foundPoint = true;
                    }
                }
            }

            if (!foundPoint)
            {
                activeList.RemoveAt(randomIndex);
            }
        }

        return points;
    }

    public static Texture2D GradientToTexture(Gradient gradient, int width)
    {
        Texture2D texture = new Texture2D(width, 1, TextureFormat.RGB24, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int i = 0; i < texture.width; i++)
        {
            float t = i / (float)(texture.width - 1);
            Color color = gradient.Evaluate(t);
            texture.SetPixel(i, 0, color);
        }

        texture.Apply();

        return texture;
    }

    public static void AddLayerToCameraCullingMask(Camera camera, string layerName) => camera.cullingMask |= (1 << LayerMask.NameToLayer(layerName));
    public static void RemoveLayerFromCameraCullingMask(Camera camera, string layerName) => camera.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));

    #region Mesh modify functions
    public static void SplitVerticesWithUV(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;

        Vector3[] newVertices = new Vector3[triangles.Length];
        Vector2[] newUVs = new Vector2[triangles.Length];
        int[] newTriangles = new int[triangles.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            newVertices[i] = vertices[triangles[i]];
            newVertices[i + 1] = vertices[triangles[i + 1]];
            newVertices[i + 2] = vertices[triangles[i + 2]];

            newUVs[i] = uvs[triangles[i]];
            newUVs[i + 1] = uvs[triangles[i + 1]];
            newUVs[i + 2] = uvs[triangles[i + 2]];

            newTriangles[i] = i;
            newTriangles[i + 1] = i + 1;
            newTriangles[i + 2] = i + 2;
        }

        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;
        mesh.uv = newUVs;
    }

    public static void SplitVertices(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Vector3[] splitVertices = new Vector3[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            splitVertices[i] = vertices[triangles[i]];
        }

        mesh.vertices = splitVertices;
        mesh.triangles = GenerateTriangleIndices(triangles.Length / 3);
    }

    private static int[] GenerateTriangleIndices(int numTriangles)
    {
        int[] indices = new int[numTriangles * 3];
        for (int i = 0; i < numTriangles; i++)
        {
            indices[i * 3] = i * 3;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;
        }
        return indices;
    }

    public static void WeldVertices(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        int vertexCount = vertices.Length;
        Vector3[] mergedVertices = new Vector3[vertexCount];
        Vector2[] mergedUVs = new Vector2[vertexCount];
        int[] vertexRemap = new int[vertexCount];
        int mergedVertexCount = 0;

        for (int i = 0; i < vertexCount; i++)
        {
            bool foundDuplicate = false;

            for (int j = 0; j < mergedVertexCount; j++)
            {
                if (Vector3.Distance(vertices[i], mergedVertices[j]) < 0.0001f)
                {
                    vertexRemap[i] = j;
                    foundDuplicate = true;
                    break;
                }
            }

            if (!foundDuplicate)
            {
                mergedVertices[mergedVertexCount] = vertices[i];
                mergedUVs[mergedVertexCount] = uvs[i];
                vertexRemap[i] = mergedVertexCount;
                mergedVertexCount++;
            }
        }

        int[] mergedTriangles = new int[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            mergedTriangles[i] = vertexRemap[triangles[i]];
        }

        mesh.vertices = mergedVertices;
        mesh.uv = mergedUVs;
        mesh.triangles = mergedTriangles;
    }

    /*
    public static Mesh GenerateDetailMesh(Vector3 worldPosition, int width, int height, float unitLength)
    {
        width++;
        height++;

        Vector3 direction = Vector3.forward;
        int verticesCount = width * height;
        int triangleCount = (width - 1) * (height - 1) * 2;
        Vector3[] vertices = new Vector3[verticesCount];
        Vector2[] uvs = new Vector2[verticesCount];
        int[] triangles = new int[triangleCount * 3];
        int trisIndex = 0;

        for (int w = 0; w < width; w++)
        {
            for (int h = 0; h < height; h++)
            {
                int vertIndex = h * width + w;
                Vector3 localPosition = Vector3.right * w * unitLength + direction * h * unitLength;

                Vector3 heightMapY = new Vector3(0.0f, GetHeightMapIDW(new Vector2(worldPosition.x + localPosition.x, worldPosition.z + localPosition.z), unitLength), 0.0f);

                vertices[vertIndex] = worldPosition + localPosition + heightMapY;
                uvs[vertIndex] = new Vector2(w / (width - 1.0f), h / (height - 1.0f));

                if (w == width - 1 || h == height - 1) continue;

                triangles[trisIndex++] = vertIndex;
                triangles[trisIndex++] = vertIndex + width;
                triangles[trisIndex++] = vertIndex + width + 1;
                triangles[trisIndex++] = vertIndex;
                triangles[trisIndex++] = vertIndex + width + 1;
                triangles[trisIndex++] = vertIndex + 1;
            }
        }

        Mesh mesh = new Mesh { vertices = vertices, triangles = triangles, uv = uvs };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.Optimize();
        return mesh;
    }
    */

    #endregion

    /*
    //small square
    public static float GetHeightMapIDW(Vector2 point, float unitLength)
    {
        Vector2Int[] corners = new Vector2Int[4]
        {
            new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y)),
            new Vector2Int(Mathf.CeilToInt(point.x), Mathf.CeilToInt(point.y)),
            new Vector2Int(Mathf.FloorToInt(point.x), Mathf.CeilToInt(point.y)),
            new Vector2Int(Mathf.CeilToInt(point.x), Mathf.FloorToInt(point.y))
        };

        //Inverse distance weighted interpolation
        //https://gisgeography.com/inverse-distance-weighting-idw-interpolation/

        float value = 0;
        float inverseDistance = 0;

        for (int c = 0; c < corners.Length; c++)
        {
            float distance = Vector2.Distance(corners[c], point);

            //the corner is out of heith map bounds
            if (corners[c].x < 0 || corners[c].x > mapSize.x || corners[c].y < 0 || corners[c].y > mapSize.z) continue;

            //let's check if the point is on the corner
            if (distance < unitLength) return heightMap[corners[c].x, corners[c].y];
            //or
            //distance += 0.0001f;
            distance = distance * distance * distance * distance;
            value += heightMap[corners[c].x, corners[c].y] / distance;
            inverseDistance += 1.0f / distance;
        }

        return value / inverseDistance;
    }
    */
}
