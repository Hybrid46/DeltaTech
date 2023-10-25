using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class StaticUtils
{
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

    //Returns local coords!
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
    public static List<Vector3> GeneratePoissonPoints(float minDistance, Bounds bounds, int maxAttempts)
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

    public static float CalcCylinderVolume(float height, float radius) => height * Mathf.PI * radius * radius;
}