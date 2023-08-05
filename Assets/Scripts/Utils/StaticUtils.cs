using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class StaticUtils
{
    [Serializable]
    public struct MinMax<T1, T2>
    {
        public T1 Min;
        public T2 Max;

        public MinMax(T1 min, T2 max)
        {
            Min = min;
            Max = max;
        }
    }

    public static float Remap(float input, float inputMin, float inputMax, float targetMin, float targetMax) => targetMin + (input - inputMin) * (targetMax - targetMin) / (inputMax - inputMin);

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

    public static int RounderInt(float x, float g = 16) => (int)(Mathf.Floor((x + g / 2) / g) * g);

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

    //Return local coords!
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

    //Returns Local coords!
    public static List<Vector3> GetPoissonPoints(float minDistance, Bounds bounds, int maxAttempts)
    {
        List<Vector3> points = new List<Vector3>();
        float cellSize = minDistance / Mathf.Sqrt(2);
        int[,] grid = new int[Mathf.CeilToInt(bounds.size.x / cellSize), Mathf.CeilToInt(bounds.size.z / cellSize)];
        List<Vector3> activeList = new List<Vector3>();

        Vector3 firstPoint = new Vector3(Random.Range(0, bounds.size.x), 0.0f, Random.Range(0, bounds.size.z));
        points.Add(firstPoint);
        activeList.Add(firstPoint);
        grid[Mathf.FloorToInt(firstPoint.x / cellSize), Mathf.FloorToInt(firstPoint.z / cellSize)] = points.Count;

        while (activeList.Count > 0)
        {
            int randomIndex = Random.Range(0, activeList.Count);
            Vector3 point = activeList[randomIndex];
            bool foundPoint = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                float angle = Random.Range(0, Mathf.PI * 2);
                float radius = Random.Range(minDistance, minDistance * 2);
                Vector3 newPoint = new Vector3(point.x + radius * Mathf.Cos(angle), 0.0f, point.z + radius * Mathf.Sin(angle));

                if (newPoint.x >= 0 && newPoint.x < bounds.size.x && newPoint.z >= 0 && newPoint.z < bounds.size.z)
                {
                    int cellX = Mathf.FloorToInt(newPoint.x / cellSize);
                    int cellY = Mathf.FloorToInt(newPoint.z / cellSize);
                    bool canPlace = true;

                    for (int x = Mathf.Max(0, cellX - 2); x < Mathf.Min(grid.GetLength(0), cellX + 3); x++)
                    {
                        for (int y = Mathf.Max(0, cellY - 2); y < Mathf.Min(grid.GetLength(1), cellY + 3); y++)
                        {
                            if (grid[x, y] > 0 && Vector3.Distance(points[grid[x, y] - 1], newPoint) < minDistance)
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

        points.TrimExcess();
        return points;
    }

    /*
    /Maybe this one can be used to get a randomized pattern like formations for multiple agents
    public static List<Vector3> GetPoissonPoints(Chunk chunk, float minDistance = 1.0f, int attempts = 10)
    {
        List<Vector3> points = new List<Vector3>();
        Bounds bounds = chunk.myRenderer.bounds;

        Vector3 initialPoint = new Vector3(Random.Range(bounds.min.x, bounds.max.x), bounds.center.y, Random.Range(bounds.min.z, bounds.max.z));
        points.Add(initialPoint);

        Queue<Vector3> activePoints = new Queue<Vector3>();
        activePoints.Enqueue(initialPoint);

        float sqrMinDistance = minDistance * minDistance;

        while (activePoints.Count > 0)
        {
            Vector3 activePoint = activePoints.Dequeue();

            for (int i = 0; i < attempts; i++)
            {
                Vector3 offset = Random.onUnitSphere * minDistance;
                Vector3 candidate = activePoint + new Vector3(offset.x, 0f, offset.y);

                if (bounds.Contains(candidate) && !IsTooClose(candidate, sqrMinDistance))
                {
                    points.Add(candidate);
                    activePoints.Enqueue(candidate);
                }
            }
        }

        bool IsTooClose(Vector3 point, float sqrMinDistance)
        {
            foreach (Vector3 p in points)
            {
                if ((point - p).sqrMagnitude < sqrMinDistance)
                {
                    return true;
                }
            }
            return false;
        }

        return points;
    }
    */

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

    public static Mesh CopyMesh(Mesh sourceMesh)
    {
        // Create a copy of the mesh
        Mesh newMesh = new Mesh();
        newMesh.vertices = sourceMesh.vertices;
        newMesh.triangles = sourceMesh.triangles;
        //newMesh.normals = sourceMesh.normals;
        newMesh.uv = sourceMesh.uv;
        //newMesh.tangents = sourceMesh.tangents;
        //newMesh.colors = sourceMesh.colors;
        //newMesh.colors32 = sourceMesh.colors32;
        //newMesh.uv2 = sourceMesh.uv2;
        //newMesh.uv3 = sourceMesh.uv3;
        //newMesh.uv4 = sourceMesh.uv4;
        //newMesh.subMeshCount = sourceMesh.subMeshCount;

        /*
        for (int i = 0; i < sourceMesh.subMeshCount; i++)
        {
            newMesh.SetTriangles(sourceMesh.GetTriangles(i), i);
        }
        */

        // Assign the new mesh to the destination MeshFilter
        return newMesh;
    }

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

    public static void FlattenTriangleNormals(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = new Vector3[mesh.vertices.Length];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexIndex1 = triangles[i];
            int vertexIndex2 = triangles[i + 1];
            int vertexIndex3 = triangles[i + 2];

            Vector3 vertex1 = vertices[vertexIndex1];
            Vector3 vertex2 = vertices[vertexIndex2];
            Vector3 vertex3 = vertices[vertexIndex3];

            Vector3 edge1 = vertex2 - vertex1;
            Vector3 edge2 = vertex3 - vertex1;

            Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

            normals[vertexIndex1] = normal;
            normals[vertexIndex2] = normal;
            normals[vertexIndex3] = normal;

            mesh.SetNormals(normals);
        }
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

    //IDW
    public static float GetHeightMapIDW(Vector3 position, Vector3[] pattern, Bounds bounds)
    {
        //Inverse distance weighted interpolation
        //https://gisgeography.com/inverse-distance-weighting-idw-interpolation/

        float heightValue = 0;
        float inverseDistance = 0;

        for (int p = 0; p < pattern.Length; p++)
        {
            Vector3 curentPos = position + pattern[p];
            float distance = Vector3.Distance(curentPos, position);

            //check map bounds
            if (!bounds.Contains(curentPos)) continue;

            distance = distance / distance;
            heightValue += //height sampling and dividing by distance -> for example: planet.GetBiomeHeight(curentPos) / distance;
            inverseDistance += 1.0f / distance;
        }

        return heightValue / inverseDistance;
    }

    //Builds 2D MxM matrix pattern, distance based, circle
    public static Vector3[] GetPattern(float stepSize, float range)
    {
        int matrixSize = Mathf.CeilToInt(range / stepSize);
        List<Vector3> pattern = new List<Vector3>(matrixSize * matrixSize);

        for (float y = -range; y <= range; y += stepSize)
        {
            for (float x = -range; x <= range; x += stepSize)
            {
                Vector3 currentPos = new Vector3(x, 0.0f, y);

                if (currentPos == Vector3.zero) continue; //we must skip the center point because the IDW on 0 zero distance will cause some problem -> zero distance weight will be extra powerful and division by zero!

                if (Vector3.Distance(Vector3.zero, currentPos) <= range) pattern.Add(currentPos);
            }
        }

        pattern.TrimExcess();

        return pattern.ToArray();
    }
}