using System.Collections.Generic;
using UnityEngine;
using static MeshUtilities;

public class TerrainSimplification
{
    public HashSet<int> edgeTriangleIndices { get; private set; }
    public HashSet<int> nonEdgeTriangleIndices { get; private set; }

    public void FindEdgeTriangles(Mesh mesh)
    {
        edgeTriangleIndices = new HashSet<int>();
        nonEdgeTriangleIndices = new HashSet<int>();

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

            if (isEdgeTriangle) 
                edgeTriangleIndices.Add(triangleIndex / 3);
            else
                nonEdgeTriangleIndices.Add(triangleIndex / 3);
        }
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

    public void SimplifyTerrain(Mesh mesh)
    {
        FindEdgeTriangles(mesh);
        RemoveEdgeTriangles(mesh);
        ScaleTriangles(mesh, 0.25f);
        RemoveOutsideOfBoundsTriangles(mesh);
        //AddEdgeTriangles(mesh);
    }

    //Rescale triangles
    public void AdjustTriangleScales(Mesh mesh, float scaleFactor)
    {
        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;

        foreach (int triangleIndex in meshTriangles)
        {
            int vertexIndexA = meshTriangles[triangleIndex * 3];
            int vertexIndexB = meshTriangles[triangleIndex * 3 + 1];
            int vertexIndexC = meshTriangles[triangleIndex * 3 + 2];

            Vector3 vertexA = meshVertices[vertexIndexA];
            Vector3 vertexB = meshVertices[vertexIndexB];
            Vector3 vertexC = meshVertices[vertexIndexC];

            Vector3 centroid = (vertexA + vertexB + vertexC) / 3.0f;
            Vector3 scaledCentroid = centroid * scaleFactor;

            meshVertices[vertexIndexA] = Vector3.Lerp(vertexA, scaledCentroid, scaleFactor);
            meshVertices[vertexIndexB] = Vector3.Lerp(vertexB, scaledCentroid, scaleFactor);
            meshVertices[vertexIndexC] = Vector3.Lerp(vertexC, scaledCentroid, scaleFactor);
        }

        mesh.vertices = meshVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    public void ScaleTriangles(Mesh mesh, float scaleFactor)
    {
        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;

        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length / 3; triangleIndex++)
        {
            int vertexIndexA = meshTriangles[triangleIndex * 3];
            int vertexIndexB = meshTriangles[triangleIndex * 3 + 1];
            int vertexIndexC = meshTriangles[triangleIndex * 3 + 2];

            Vector3 vertexA = meshVertices[vertexIndexA];
            Vector3 vertexB = meshVertices[vertexIndexB];
            Vector3 vertexC = meshVertices[vertexIndexC];

            Vector3 centroid = (vertexA + vertexB + vertexC) / 3.0f;

            Vector3 centroidNormalA = (vertexA - centroid);
            Vector3 centroidNormalB = (vertexB - centroid);
            Vector3 centroidNormalC = (vertexC - centroid);

            Vector3 scaledA = vertexA + centroidNormalA * scaleFactor;
            Vector3 scaledB = vertexB + centroidNormalB * scaleFactor;
            Vector3 scaledC = vertexC + centroidNormalC * scaleFactor;

            meshVertices[vertexIndexA] = scaledA;
            meshVertices[vertexIndexB] = scaledB;
            meshVertices[vertexIndexC] = scaledC;
        }

        mesh.vertices = meshVertices;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }
}
