using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSimplification
{
    [Serializable]
    public struct Triangle
    {
        public int indexA;
        public int indexB;
        public int indexC;

        public Vector3 vertexA;
        public Vector3 vertexB;
        public Vector3 vertexC;

        public Triangle(int indexA, int indexB, int indexC, Vector3 vertexA, Vector3 vertexB, Vector3 vertexC)
        {
            this.indexA = indexA;
            this.indexB = indexB;
            this.indexC = indexC;
            this.vertexA = vertexA;
            this.vertexB = vertexB;
            this.vertexC = vertexC;
        }
    }

    private HashSet<int> edgeTriangleIndices;
    private HashSet<int> nonEdgeTriangleIndices;
    private HashSet<int> scalableTriangleIndices;
    private HashSet<int> removableTriangleIndices;

    //for mesh rebuilding
    public Dictionary<int, Vector3> triangles;
    public HashSet<Triangle> triangleData;

    Bounds meshBounds;
    Bounds innerTriangleBounds;
    Bounds innerTriangleQuarterBounds;

    int[] meshTriangles;
    Vector3[] meshVertices;

    public void SimplifyTerrain(Mesh mesh)
    {
        meshTriangles = mesh.triangles;
        meshVertices = mesh.vertices;

        meshBounds = mesh.bounds;
        innerTriangleBounds = new Bounds(meshBounds.center, new Vector3(meshBounds.size.x - 2, 1, meshBounds.size.z - 2));
        innerTriangleQuarterBounds = new Bounds(innerTriangleBounds.center - innerTriangleBounds.size / 4, innerTriangleBounds.size / 2);

        FillTriangleIndexesToVertices();
        FindTriangles();
        ColorTriangles(mesh);
        RemoveTriangles(mesh, removableTriangleIndices);
        ScaleTriangles(mesh, scalableTriangleIndices, 2.0f);
        //RemoveVerticesWithoutTriangles(mesh);

        mesh.RecalculateBounds();
        mesh.Optimize();
    }

    private void FillTriangleIndexesToVertices()
    {
        triangles = new Dictionary<int, Vector3>(meshTriangles.Length);

        triangleData = new HashSet<Triangle>(meshTriangles.Length / 3);

        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length; triangleIndex += 3)
        {
            int vertexIndexA = meshTriangles[triangleIndex];
            int vertexIndexB = meshTriangles[triangleIndex + 1];
            int vertexIndexC = meshTriangles[triangleIndex + 2];

            Vector3 vertexA = meshVertices[vertexIndexA];
            Vector3 vertexB = meshVertices[vertexIndexB];
            Vector3 vertexC = meshVertices[vertexIndexC];

            triangleData.Add(new Triangle(vertexIndexA, vertexIndexB, vertexIndexC, vertexA, vertexB, vertexC));
        }

        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length / 3; triangleIndex++)
        {
            int vertexIndexA = meshTriangles[triangleIndex * 3];
            int vertexIndexB = meshTriangles[triangleIndex * 3 + 1];
            int vertexIndexC = meshTriangles[triangleIndex * 3 + 2];

            Vector3 vertexA = meshVertices[vertexIndexA];
            Vector3 vertexB = meshVertices[vertexIndexB];
            Vector3 vertexC = meshVertices[vertexIndexC];

            triangles.Add(vertexIndexA, vertexA);
            triangles.Add(vertexIndexB, vertexB);
            triangles.Add(vertexIndexC, vertexC);
        }
    }

    public void FindTriangles()
    {
        scalableTriangleIndices = new HashSet<int>();
        removableTriangleIndices = new HashSet<int>();
        edgeTriangleIndices = new HashSet<int>();
        nonEdgeTriangleIndices = new HashSet<int>();

        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length; triangleIndex += 3)
        {
            int vertexIndexA = meshTriangles[triangleIndex];
            int vertexIndexB = meshTriangles[triangleIndex + 1];
            int vertexIndexC = meshTriangles[triangleIndex + 2];

            Vector3 vertexA = meshVertices[vertexIndexA];
            Vector3 vertexB = meshVertices[vertexIndexB];
            Vector3 vertexC = meshVertices[vertexIndexC];

            //skip edge triangles
            bool isEdgeTriangle = IsVertexOnBounds(vertexA, meshBounds) ||
                                  IsVertexOnBounds(vertexB, meshBounds) ||
                                  IsVertexOnBounds(vertexC, meshBounds);

            if (isEdgeTriangle)
            {
                edgeTriangleIndices.Add(triangleIndex);
                edgeTriangleIndices.Add(triangleIndex + 1);
                edgeTriangleIndices.Add(triangleIndex + 2);
                continue;
            }
            else
            {
                nonEdgeTriangleIndices.Add(triangleIndex);
                nonEdgeTriangleIndices.Add(triangleIndex + 1);
                nonEdgeTriangleIndices.Add(triangleIndex + 2);
            }

            bool isScalableTriangle = IsTriangleInsideBounds(new Vector3[3] { vertexA, vertexB, vertexC }, innerTriangleQuarterBounds); //&& !edgeTriangleIndices.Contains(triangleIndex);

            if (isScalableTriangle) //is scalable and non edge
            {
                scalableTriangleIndices.Add(triangleIndex);
                scalableTriangleIndices.Add(triangleIndex + 1);
                scalableTriangleIndices.Add(triangleIndex + 2);
            }
            else //non edge and non scalable -> remove
            {
                removableTriangleIndices.Add(triangleIndex);
                removableTriangleIndices.Add(triangleIndex + 1);
                removableTriangleIndices.Add(triangleIndex + 2);
            }
        }
    }

    public void RemoveTriangles(Mesh mesh, HashSet<int> trianglesToRemove)
    {
        foreach (int triangleIndex in trianglesToRemove)
        {
            triangles.Remove(triangleIndex);
        }

        List<int> newTriangles = new List<int>(triangles.Count);

        foreach (KeyValuePair<int, Vector3> triangle in triangles)
        {
            newTriangles.Add(triangle.Key);
        }

        mesh.triangles = newTriangles.ToArray();
    }

    public void ScaleTriangles(Mesh mesh, HashSet<int> triangles, float scaleFactor)
    {
        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length; triangleIndex += 3)
        {
            if (!scalableTriangleIndices.Contains(triangleIndex)) continue;
            if (!scalableTriangleIndices.Contains(triangleIndex + 1)) continue;
            if (!scalableTriangleIndices.Contains(triangleIndex + 2)) continue;

            int vertexIndexA = meshTriangles[triangleIndex];
            int vertexIndexB = meshTriangles[triangleIndex + 1];
            int vertexIndexC = meshTriangles[triangleIndex + 2];

            meshVertices[vertexIndexA] *= scaleFactor;
            meshVertices[vertexIndexB] *= scaleFactor;
            meshVertices[vertexIndexC] *= scaleFactor;

            meshVertices[vertexIndexA] = new Vector3(meshVertices[vertexIndexA].x - 1, 0.0f, meshVertices[vertexIndexA].z - 1);
            meshVertices[vertexIndexB] = new Vector3(meshVertices[vertexIndexB].x - 1, 0.0f, meshVertices[vertexIndexB].z - 1);
            meshVertices[vertexIndexC] = new Vector3(meshVertices[vertexIndexC].x - 1, 0.0f, meshVertices[vertexIndexC].z - 1);
        }

        mesh.vertices = meshVertices;
        return;
    }

    //private void RemoveVerticesWithoutTriangles(Mesh mesh)
    //{
    //    HashSet<Vector3> usedVertices = new HashSet<Vector3>(mesh.vertices.Length);
    //    List<Vector3> newVertices = new List<Vector3>(mesh.vertices.Length);

    //    int[] meshTriangles = mesh.triangles;
    //    Vector3[] meshVertices = mesh.vertices;

    //    foreach (int triangleIndex in meshTriangles)
    //    {
    //        usedVertices.Add(meshVertices[triangleIndex]);
    //    }

    //    foreach (Vector3 vertex in meshVertices)
    //    {
    //        if (usedVertices.Contains(vertex)) newVertices.Add(vertex);
    //    }

    //    mesh.RecalculateBounds();
    //    //mesh.Optimize();
    //}

    private bool IsVertexOnBounds(Vector3 vertex, Bounds bounds)
    {
        return Mathf.Approximately(vertex.x, bounds.min.x) ||
               Mathf.Approximately(vertex.x, bounds.max.x) ||
               Mathf.Approximately(vertex.z, bounds.min.z) ||
               Mathf.Approximately(vertex.z, bounds.max.z);
    }

    private bool IsTriangleInsideBounds(Vector3[] vertices, Bounds bounds)
    {
        if (bounds.Contains(vertices[0]) &&
            bounds.Contains(vertices[1]) &&
            bounds.Contains(vertices[2]))
        {
            return true;
        }

        return false;
    }

    public void ColorTriangles(Mesh mesh)
    {
        Color[] colors = new Color[mesh.vertexCount];

        for (int triangleIndex = 0; triangleIndex < meshTriangles.Length; triangleIndex++)
        {
            if (edgeTriangleIndices.Contains(triangleIndex)) colors[mesh.triangles[triangleIndex]] = Color.cyan;
            if (removableTriangleIndices.Contains(triangleIndex)) colors[mesh.triangles[triangleIndex]] = Color.red;
            if (scalableTriangleIndices.Contains(triangleIndex)) colors[mesh.triangles[triangleIndex]] = Color.green;
        }

        mesh.colors = colors;
    }
}
