using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using static StaticUtils;

public class Planet : Singleton<Planet>
{
    [Serializable]
    public struct BiomeSettings
    {
        public NoiseSettings noiseSettings;
    }

    [Serializable]
    public struct NoiseSettings
    {
        public Vector2 offset;
        public float scale;
        public float amplitude;
        public float minHeight;
        public float maxHeight;

        public NoiseSettings(Vector2 offset, float scale, float amplitude, float minHeight, float maxHeight)
        {
            this.offset = offset == Vector2.zero ? new Vector2(Random.Range(0, 999999), Random.Range(0, 999999)) : offset;
            this.scale = scale;
            this.amplitude = amplitude;
            this.minHeight = minHeight;
            this.maxHeight = maxHeight;
        }
    }

    public UnityAction OnChunksGenerated;

    public static Vector2Int mapSize = new Vector2Int(256 + 1, 256 + 1);
    public static Vector2Int chunkSize = new Vector2Int(16, 16);

    public GameObject terrainChunk;

    public Dictionary<Vector3, Chunk> ChunkCells = new Dictionary<Vector3, Chunk>();

    public BiomeSettings baseSettings;
    public List<BiomeSettings> biomeSettings;

    public bool generateNavmeshes = false;

    public bool debugWorldBounds = true;
    public bool debugChunkBounds = true;
    public bool debugHeightMap = true;
    public bool debugIDWPattern = true;
    public Gradient debugGradient;

    private Bounds worldBounds = new Bounds();

    private const float idwStepSize = 0.5f;
    private const float idwRange = 4.0f;
    private Vector3[] idwPattern;

    private void Start()
    {
        InitializePlanet();
    }

    public void InitializePlanet()
    {
        //Fill up height map
        DateTime exectime = DateTime.Now;

        baseSettings.noiseSettings = new NoiseSettings(Vector2.zero, baseSettings.noiseSettings.scale, baseSettings.noiseSettings.amplitude, 0, biomeSettings.Count);

        biomeSettings.ForEach(setting => { setting.noiseSettings.offset = new Vector2(Random.Range(0, 999999), Random.Range(0, 999999)); });

        idwPattern = GetPattern(idwStepSize, idwRange);

        exectime = DateTime.Now;

        //Chunk Generation
        exectime = DateTime.Now;

        for (int z = 0; z < mapSize.y - 1; z += chunkSize.y)
        {
            for (int x = 0; x < mapSize.x - 1; x += chunkSize.x)
            {
                Vector3 worldPosition = new Vector3(x, 0, z);

                ChunkCells.Add(worldPosition, CreateChunk(worldPosition));
            }
        }

        Debug.Log("Chunks generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        if (generateNavmeshes) ChunkCells[Vector3.zero].myNavMeshSurface.BuildNavMesh();

        OnChunksGenerated += ChunksGenerated;
        OnChunksGenerated.Invoke();
    }

    private float GetBaseHeight(Vector3 position) => Mathf.Clamp01(GetHeight(position, baseSettings.noiseSettings));

    private float GetBiomeHeight(Vector3 position) => GetHeight(position, biomeSettings[GetBiomeIndex(position)].noiseSettings);

    private int GetBiomeIndex(Vector3 position) => (biomeSettings.Count - 1) * (int)GetBaseHeight(position);

    private float GetHeight(Vector3 position, NoiseSettings noiseSettings)
    {
        float xCoord = position.x / mapSize.x * noiseSettings.scale + noiseSettings.offset.x;
        float zCoord = position.z / mapSize.y * noiseSettings.scale + noiseSettings.offset.y;

        return Mathf.PerlinNoise(xCoord, zCoord) * noiseSettings.maxHeight + noiseSettings.minHeight;
    }

    public Chunk CreateChunk(Vector3 worldPosition)
    {
        GameObject chunkObj = Instantiate(terrainChunk);

        chunkObj.transform.parent = transform;
        chunkObj.transform.position = worldPosition;
        chunkObj.transform.localPosition = Vector3.zero;
        chunkObj.layer = LayerMask.NameToLayer("Planet");
        chunkObj.name = "TerrainChunk " + worldPosition;

        Chunk currentChunk = chunkObj.GetComponent<Chunk>();
        currentChunk.chunkWorldPos = worldPosition;

        currentChunk.SimpleMesh = GenerateMesh(worldPosition);
        currentChunk.GetReferences();
        currentChunk.myMeshFilter.sharedMesh = currentChunk.SimpleMesh;

        worldBounds.Encapsulate(currentChunk.myRenderer.bounds);

        return currentChunk;
    }

    public Vector3 GetChunkLocalCoord(float x, float y, float z) => new Vector3(x % chunkSize.x, y, z % chunkSize.y);

    private Mesh GenerateMesh(Vector3 worldPosition)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(chunkSize.x + 1) * (chunkSize.y + 1)];

        int i = 0;
        for (int d = 0; d <= chunkSize.y; d++)
        {
            for (int w = 0; w <= chunkSize.x; w++)
            {
                Vector3 wPosition = new Vector3(worldPosition.x + w, 0.0f, worldPosition.z + d);
                //float height = GetBiomeHeight(wPosition);
                float height = GetHeightMapIDW(wPosition, idwPattern);
                vertices[i] = new Vector3(worldPosition.x + w, height, worldPosition.z + d);
                i++;
            }
        }

        int[] triangles = new int[chunkSize.x * chunkSize.y * 6];

        for (int d = 0; d < chunkSize.y; d++)
        {
            for (int w = 0; w < chunkSize.x; w++)
            {
                int ti = (d * (chunkSize.x) + w) * 6;

                triangles[ti] = (d * (chunkSize.x + 1)) + w;
                triangles[ti + 1] = ((d + 1) * (chunkSize.x + 1)) + w;
                triangles[ti + 2] = ((d + 1) * (chunkSize.x + 1)) + w + 1;

                triangles[ti + 3] = (d * (chunkSize.x + 1)) + w;
                triangles[ti + 4] = ((d + 1) * (chunkSize.x + 1)) + w + 1;
                triangles[ti + 5] = (d * (chunkSize.x + 1)) + w + 1;
            }
        }

        //UV
        Vector2[] uv = new Vector2[(chunkSize.x + 1) * (chunkSize.y + 1)];

        i = 0;
        for (int d = 0; d <= chunkSize.y; d++)
        {
            for (int w = 0; w <= chunkSize.x; w++)
            {
                uv[i] = new Vector2(w / (float)chunkSize.x, d / (float)chunkSize.y);
                i++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        SplitVerticesWithUV(mesh);
        FlattenTriangleNormals(mesh);
        mesh.RecalculateTangents();
        //mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();
        return mesh;
    }

    public void ChunksGenerated()
    {
        Debug.Log("Chunks ready!");
    }

    private float GetHeightMapIDW(Vector3 position, Vector3[] pattern)
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
            if (curentPos.x < 0.0f || curentPos.x > mapSize.x || curentPos.y < 0.0f || curentPos.y > mapSize.y) continue;

            distance = distance / distance;
            heightValue += GetBiomeHeight(curentPos) / distance;
            inverseDistance += 1.0f / distance;
        }

        return heightValue / inverseDistance;
    }

    private void OnDrawGizmos()
    {
        if (debugChunkBounds)
            //chunk bounds
            foreach (KeyValuePair<Vector3, Chunk> chunk in ChunkCells)
            {
                if (chunk.Value.isActiveAndEnabled)
                {
                    GizmoExtension.GizmosExtend.DrawBounds(chunk.Value.myRenderer.bounds, Color.green);
                }
                else
                {
                    GizmoExtension.GizmosExtend.DrawBounds(chunk.Value.myRenderer.bounds, Color.red);
                }
            }

        if (debugWorldBounds)
            //world bouds
            Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

        //biome and height map
        if (debugHeightMap && idwPattern != null && idwPattern.Length > 0)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            const int drawDistance = 20;

            for (int z = (int)cameraPos.z - drawDistance; z <= (int)cameraPos.z + drawDistance; z++)
            {
                for (int x = (int)cameraPos.x - drawDistance; x <= (int)cameraPos.x + drawDistance; x++)
                {
                    Vector3 currentPosition = new Vector3(x, 0.0f, z);

                    currentPosition.y = GetBaseHeight(currentPosition);

                    int biomeIndex = GetBiomeIndex(currentPosition);
                    float time = biomeIndex / (float)biomeSettings.Count;
                    Gizmos.color = debugGradient.Evaluate(time);

                    Gizmos.DrawWireCube(currentPosition, Vector3.one * 0.5f);
                    Gizmos.DrawCube(currentPosition, Vector3.one * 0.5f);

                    currentPosition.y = GetBiomeHeight(currentPosition);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(currentPosition, Vector3.one * 0.5f);
                    Gizmos.DrawCube(currentPosition, Vector3.one * 0.5f);
                    /*
                    currentPosition.y = GetHeightMapIDW(currentPosition, idwPattern);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(currentPosition, Vector3.one * 0.5f);
                    Gizmos.DrawCube(currentPosition, Vector3.one * 0.5f);
                    */
                }
            }
        }

        //idw pattern
        if (debugIDWPattern && idwPattern != null && idwPattern.Length > 0)
        {
            foreach (Vector3 idwPatternPoint in idwPattern)
            {
                Gizmos.color += new Color(0.5f, 0.5f, 0.5f, 0.25f);
                Gizmos.DrawWireCube(idwPatternPoint + Vector3.down * 10, Vector3.one * 0.25f);
                Gizmos.color += new Color(0.5f, 0.5f, 0.5f, 0.1f);
                Gizmos.DrawCube(idwPatternPoint + Vector3.down * 10, Vector3.one * 0.25f);
            }
        }
    }
}