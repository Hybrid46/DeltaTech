using UnityEngine;

public static class MeshUtilities
{
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

    public static void RemoveTriangleAndVertices(Mesh mesh, int triangleIndex, bool normals = false, bool UVs = true)
    {
        int[] triangles = mesh.triangles;
        int vertIndex1 = triangles[triangleIndex * 3];
        int vertIndex2 = triangles[triangleIndex * 3 + 1];
        int vertIndex3 = triangles[triangleIndex * 3 + 2];

        // Remove the triangle vertices, normals, and UVs
        mesh.vertices = RemoveFromArray(mesh.vertices, vertIndex1);
        mesh.vertices = RemoveFromArray(mesh.vertices, vertIndex2);
        mesh.vertices = RemoveFromArray(mesh.vertices, vertIndex3);

        if (normals)
        {
            mesh.normals = RemoveFromArray(mesh.normals, vertIndex1);
            mesh.normals = RemoveFromArray(mesh.normals, vertIndex2);
            mesh.normals = RemoveFromArray(mesh.normals, vertIndex3);
        }

        if (UVs)
        {
            mesh.uv = RemoveFromArray(mesh.uv, vertIndex1);
            mesh.uv = RemoveFromArray(mesh.uv, vertIndex2);
            mesh.uv = RemoveFromArray(mesh.uv, vertIndex3);
        }

        // Remove the triangle indices
        triangles = RemoveTriangleFromArray(triangles, triangleIndex);

        // Update the mesh with the modified arrays
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    private static T[] RemoveFromArray<T>(T[] array, int index)
    {
        T[] newArray = new T[array.Length - 1];
        int newArrayIndex = 0;

        for (int i = 0; i < array.Length; i++)
        {
            if (i != index)
            {
                newArray[newArrayIndex] = array[i];
                newArrayIndex++;
            }
        }

        return newArray;
    }

    private static int[] RemoveTriangleFromArray(int[] array, int triangleIndex)
    {
        int[] newArray = new int[array.Length - 3];
        int newArrayIndex = 0;

        for (int i = 0; i < array.Length; i += 3)
        {
            if (i != triangleIndex * 3)
            {
                newArray[newArrayIndex++] = array[i];
                newArray[newArrayIndex++] = array[i + 1];
                newArray[newArrayIndex++] = array[i + 2];
            }
        }

        return newArray;
    }

    public static void ConvertTrianglesToQuads(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Vector3[] newVertices = new Vector3[vertices.Length];
        int[] newTriangles = new int[triangles.Length];

        int newVertexIndex = 0;
        int newTriangleIndex = 0;

        // Process triangles and convert to quads
        for (int i = 0; i < triangles.Length; i += 3)
        {
            if (i + 5 < triangles.Length)
            {
                int vertexA = triangles[i];
                int vertexB = triangles[i + 1];
                int vertexC = triangles[i + 2];
                int vertexD = triangles[i + 3];
                int vertexE = triangles[i + 4];
                int vertexF = triangles[i + 5];

                // Check if triangles can be converted to a quad
                // (For simplicity, check if shared edges are consecutive)

                if (vertexB == vertexD && vertexC == vertexE)
                {
                    // Convert to quad
                    newVertices[newVertexIndex++] = vertices[vertexA];
                    newVertices[newVertexIndex++] = vertices[vertexB];
                    newVertices[newVertexIndex++] = vertices[vertexC];
                    newVertices[newVertexIndex++] = vertices[vertexF];

                    newTriangles[newTriangleIndex++] = newVertexIndex - 4;
                    newTriangles[newTriangleIndex++] = newVertexIndex - 3;
                    newTriangles[newTriangleIndex++] = newVertexIndex - 2;

                    newTriangles[newTriangleIndex++] = newVertexIndex - 4;
                    newTriangles[newTriangleIndex++] = newVertexIndex - 2;
                    newTriangles[newTriangleIndex++] = newVertexIndex - 1;

                    i += 3; // Skip the next triangle
                }
                else
                {
                    // Keep original triangles
                    newVertices[newVertexIndex++] = vertices[vertexA];
                    newVertices[newVertexIndex++] = vertices[vertexB];
                    newVertices[newVertexIndex++] = vertices[vertexC];

                    newTriangles[newTriangleIndex++] = newVertexIndex - 3;
                    newTriangles[newTriangleIndex++] = newVertexIndex - 2;
                    newTriangles[newTriangleIndex++] = newVertexIndex - 1;
                }
            }
            else
            {
                // Keep the remaining triangles
                newVertices[newVertexIndex++] = vertices[triangles[i]];
                newVertices[newVertexIndex++] = vertices[triangles[i + 1]];
                newVertices[newVertexIndex++] = vertices[triangles[i + 2]];

                newTriangles[newTriangleIndex++] = newVertexIndex - 3;
                newTriangles[newTriangleIndex++] = newVertexIndex - 2;
                newTriangles[newTriangleIndex++] = newVertexIndex - 1;
            }
        }

        mesh.vertices = newVertices;
        mesh.triangles = newTriangles;
    }
}