using System.Collections.Generic;
using UnityEngine;

public class TerrainSimplification
{
    public HashSet<int> edgeTriangleIndices { get; private set; }

    public void FindEdgeTriangles(Mesh mesh)
    {
        edgeTriangleIndices = new HashSet<int>();

        Bounds meshBounds = mesh.bounds;

        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;

        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length; triangleIndex += 3)
        {
            int vertexIndexA = meshTriangles[triangleIndex];
            int vertexIndexB = meshTriangles[triangleIndex + 1];
            int vertexIndexC = meshTriangles[triangleIndex + 2];

            Vector3 vertexA = meshVertices[vertexIndexA];
            Vector3 vertexB = meshVertices[vertexIndexB];
            Vector3 vertexC = meshVertices[vertexIndexC];

            bool isEdgeTriangle = IsVertexOnBounds(vertexA, meshBounds) ||
                                  IsVertexOnBounds(vertexB, meshBounds) ||
                                  IsVertexOnBounds(vertexC, meshBounds);

            if (isEdgeTriangle) edgeTriangleIndices.Add(triangleIndex / 3);
        }

        edgeTriangleIndices.TrimExcess();
    }

    private bool IsVertexOnBounds(Vector3 vertex, Bounds bounds)
    {
        return Mathf.Approximately(vertex.x, bounds.min.x) ||
               Mathf.Approximately(vertex.x, bounds.max.x) ||
               Mathf.Approximately(vertex.z, bounds.min.z) ||
               Mathf.Approximately(vertex.z, bounds.max.z);
    }

    public void ColorEdgeTriangles(Mesh mesh)
    {
        Color[] colors = new Color[mesh.vertexCount];

        foreach (int edgeTriangleIndex in edgeTriangleIndices)
        {
            int triangleIndex = edgeTriangleIndex * 3;

            colors[mesh.triangles[triangleIndex]] = Color.cyan;
            colors[mesh.triangles[triangleIndex + 1]] = Color.cyan;
            colors[mesh.triangles[triangleIndex + 2]] = Color.cyan;
        }

        mesh.colors = colors;
    }

    public void RemoveEdgeTriangles(Mesh mesh)
    {
        // Step 1: Identify Edge Triangles
        List<int> nonEdgeTriangles = new List<int>();
        for (int triangleIndex = 0; triangleIndex < mesh.triangles.Length / 3; triangleIndex++)
        {
            if (!edgeTriangleIndices.Contains(triangleIndex))
                nonEdgeTriangles.Add(triangleIndex);
        }

        // Step 2: Create New Lists
        List<int> newTriangles = new List<int>();

        // Step 3: Populate New Lists
        foreach (int triangleIndex in nonEdgeTriangles)
        {
            int vertexIndexA = mesh.triangles[triangleIndex * 3];
            int vertexIndexB = mesh.triangles[triangleIndex * 3 + 1];
            int vertexIndexC = mesh.triangles[triangleIndex * 3 + 2];

            // Add the vertex indices directly without modifying
            newTriangles.Add(vertexIndexA);
            newTriangles.Add(vertexIndexB);
            newTriangles.Add(vertexIndexC);
        }

        mesh.triangles = newTriangles.ToArray();
        mesh.RecalculateNormals();
    }
}
