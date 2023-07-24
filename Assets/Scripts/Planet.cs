using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using static StaticUtils;
using System.Threading.Tasks;

public class Planet : Singleton<Planet>
{
    [Serializable]
    public struct BiomeSettings
    {
        public NoiseSettings noiseSettings;
        public List<PrefabBiomeSettings> prefabBiomeSettings;
        public float totalChance { get; private set; }

        public void SortByChances()
        {
            prefabBiomeSettings.Sort((a, b) => b.chance.CompareTo(a.chance));
            CalculateTotalChance();
        }

        private void CalculateTotalChance()
        {
            totalChance = 0f;
            foreach (PrefabBiomeSettings prefabSetting in prefabBiomeSettings)
            {
                totalChance += prefabSetting.chance;
            }
        }
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

    [Serializable]
    public struct PrefabBiomeSettings
    {
        public GameObject prefab;
        [Range(0.0f, 1.0f)] public float chance;
        public MinMax<float, float> height;
    }

    [Serializable]
    public struct PrefabSettings
    {
        public GameObject prefab;
        public MinMax<Vector3, Vector3> rotation;
        public MinMax<Vector3, Vector3> scale;
        public bool alignToNormal;
    }

    public UnityAction OnChunksGenerated;

    public static Vector2Int mapSize = new Vector2Int(256 + 1, 256 + 1);
    public static Vector2Int chunkSize = new Vector2Int(16, 16);

    public GameObject terrainChunk;

    public Dictionary<Vector3, Chunk> ChunkCells = new Dictionary<Vector3, Chunk>();

    public BiomeSettings baseSettings;
    public List<BiomeSettings> biomeSettings;
    public List<PrefabSettings> prefabSettings;
    private Dictionary<GameObject, PrefabSettings> prefabSettingsLUT;

    public bool generateNavmeshes = false;

    public bool debugWorldBounds = true;
    public bool debugChunkBounds = true;
    public bool debugHeightMap = true;
    public bool debugIDWPattern = true;
    private List<Vector3> poissonSamples = new List<Vector3>();
    public bool debugPoisson = false;
    public Gradient debugGradient;

    private Bounds worldBounds = new Bounds();

    private const float idwStepSize = 0.5f;
    private const float idwRange = 4.0f;
    private Vector3[] idwPattern;

    //debug
    //private List<Bounds> placedPrefabBoundsDebug = new List<Bounds>();
    //private List<Bounds> overlappedPrefabBoundsDebug = new List<Bounds>();

    private void Start()
    {
        InitializePlanet();
    }

    public void InitializePlanet()
    {
        DateTime exectime = DateTime.Now;

        baseSettings.noiseSettings = new NoiseSettings(Vector2.zero, baseSettings.noiseSettings.scale, baseSettings.noiseSettings.amplitude, 0, biomeSettings.Count);

        biomeSettings.ForEach(setting => { setting.noiseSettings.offset = new Vector2(Random.Range(0, 999999), Random.Range(0, 999999)); });

        idwPattern = GetPattern(idwStepSize, idwRange);

        Debug.Log("Biomes Initialized in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        //Chunk Generation
        exectime = DateTime.Now;

        for (int z = 0; z < mapSize.y - 1; z += chunkSize.y)
        {
            for (int x = 0; x < mapSize.x - 1; x += chunkSize.x)
            {
                Vector3 worldPosition = new Vector3(x, 0, z);

                ChunkCells.Add(worldPosition, CreateChunk(worldPosition));
                GenerateChunkMeshes(worldPosition, ChunkCells[worldPosition]);

                worldBounds.Encapsulate(ChunkCells[worldPosition].myRenderer.bounds);
            }
        }

        ChunkCells.TrimExcess();

        Debug.Log("Chunks generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        //Build Nav meshes
        if (generateNavmeshes) ChunkCells[Vector3.zero].myNavMeshSurface.BuildNavMesh();

        //Prefab Generation
        exectime = DateTime.Now;

        //organize lists by spawn chances and calc maxChance
        biomeSettings.ForEach((setting) => { setting.SortByChances(); });

        //Fill prefab settings look up table
        prefabSettingsLUT = new Dictionary<GameObject, PrefabSettings>(prefabSettings.Count);
        prefabSettings.ForEach((setting) => { prefabSettingsLUT.Add(setting.prefab, setting); });

        //spawning
        foreach (KeyValuePair<Vector3, Chunk> chunk in ChunkCells)
        {
            SpawnPrefabs(chunk.Value);
        }

        Debug.Log("Prefabs generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        //Release memory for meshes
        foreach (KeyValuePair<Vector3, Chunk> chunk in ChunkCells)
        {
            chunk.Value.SimpleMesh.UploadMeshData(true);
            //chunk.Value.DetailMesh.UploadMeshData(true);
        }

        OnChunksGenerated += ChunksGenerated;
        OnChunksGenerated.Invoke();
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

        currentChunk.GetReferences();

        return currentChunk;
    }

    //TODO Detail and LOD meshes
    private void GenerateChunkMeshes(Vector3 worldPosition, Chunk currentChunk)
    {
        currentChunk.SimpleMesh = GenerateMesh(worldPosition);
        currentChunk.SetMeshTo(Chunk.MeshDetail.Simple, true);
    }

    private Mesh GenerateMesh(Vector3 worldPosition)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(chunkSize.x + 1) * (chunkSize.y + 1)];

        //vertices
        int i = 0;
        for (int d = 0; d <= chunkSize.y; d++)
        {
            for (int w = 0; w <= chunkSize.x; w++)
            {
                Vector3 wPosition = new Vector3(worldPosition.x + w, 0.0f, worldPosition.z + d);
                float height = GetHeight(wPosition);

                vertices[i] = new Vector3(worldPosition.x + w, height, worldPosition.z + d);
                i++;
            }
        }

        //triangles
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
        FlattenTriangleNormalsParallel(mesh);
        //mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.Optimize();
        return mesh;
    }

    private void SpawnPrefabs(Chunk chunk)
    {
        //get poisson disc sampling points then spawn prefabs by chances
        poissonSamples = GetPoissonPoints(1.0f, chunk.myRenderer.bounds, 100);
        //Debug.Log($"Poisson points {poissonSamples.Count}");
        List<Bounds> placedBounds = new List<Bounds>();
        int spawnLayer = LayerMask.GetMask("Planet");
        (int placed, int overlapping, int rayMiss) debug = (0, 0, 0);

        //local to world and get heights
        for (int p = 0; p < poissonSamples.Count; p++)
        {
            poissonSamples[p] += chunk.myRenderer.bounds.min;
            poissonSamples[p] = new Vector3(poissonSamples[p].x, GetHeight(poissonSamples[p]), poissonSamples[p].z);
        }

        foreach (Vector3 point in poissonSamples)
        {
            int biomeIndex = GetBiomeIndex(point);
            float randomValue = Random.Range(0f, biomeSettings[biomeIndex].totalChance);
            GameObject selectedPrefab = null;
            PrefabSettings selectedPrefabSettings = new PrefabSettings();

            foreach (PrefabBiomeSettings chancedPrefab in biomeSettings[biomeIndex].prefabBiomeSettings)
            {
                if (randomValue <= chancedPrefab.chance)
                {
                    selectedPrefab = chancedPrefab.prefab;
                    selectedPrefabSettings = prefabSettingsLUT[chancedPrefab.prefab];
                    break;
                }
                randomValue -= chancedPrefab.chance;
            }

            // Instantiate the selected prefab on the terrain
            if (selectedPrefab != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(point + 1000.0f * Vector3.up, Vector3.down, out hit, Mathf.Infinity, spawnLayer))
                {
                    Vector3 spawnPosition = hit.point;

                    Quaternion randomRotation = Quaternion.Euler(Random.Range(selectedPrefabSettings.rotation.Min.x, selectedPrefabSettings.rotation.Max.x),
                                                                 Random.Range(selectedPrefabSettings.rotation.Min.y, selectedPrefabSettings.rotation.Max.y),
                                                                 Random.Range(selectedPrefabSettings.rotation.Min.z, selectedPrefabSettings.rotation.Max.z));

                    if (selectedPrefabSettings.alignToNormal) randomRotation *= Quaternion.FromToRotation(Vector3.up, hit.normal);

                    Vector3 randomScaleVector = new Vector3(Random.Range(selectedPrefabSettings.scale.Min.x, selectedPrefabSettings.scale.Max.x),
                                                            Random.Range(selectedPrefabSettings.scale.Min.y, selectedPrefabSettings.scale.Max.y),
                                                            Random.Range(selectedPrefabSettings.scale.Min.z, selectedPrefabSettings.scale.Max.z));

                    Vector3 prefabScale = selectedPrefab.transform.localScale;
                    float prefabRadius = GetSphereRadius(prefabScale);

                    bool canBePlaced = true;

                    for (int b = 0; b < placedBounds.Count; b++) //TODO: check only in neighbour chunks!
                    {
                        Vector3 existingPosition = placedBounds[b].center;
                        Vector3 existingScale = placedBounds[b].size;
                        float existingRadius = GetSphereRadius(existingScale);

                        if (CheckSphereOverlap(spawnPosition, prefabRadius, existingPosition, existingRadius))
                        {
                            canBePlaced = false;
                            break;
                        }
                    }

                    if (canBePlaced)
                    {
                        GameObject spawnedPrefab = Instantiate(selectedPrefab, spawnPosition, randomRotation);
                        spawnedPrefab.transform.localScale = randomScaleVector;
                        placedBounds.Add(spawnedPrefab.GetComponent<MeshRenderer>().bounds);
                        debug.placed++;
                    }
                    else
                    {
                        debug.overlapping++;
                    }
                }
                else
                {
                    debug.rayMiss++;
                }
            }
        }
        Debug.Log($"Chunk -> {chunk.chunkWorldPos} Placed {debug.placed} overlapping {debug.overlapping} ray miss {debug.rayMiss}.");
    }

    public void ChunksGenerated()
    {
        Debug.Log("Chunks ready!");
    }

    private float GetHeightMapIDWParallel(Vector3 position, Vector3[] pattern)
    {
        //Inverse distance weighted interpolation
        //https://gisgeography.com/inverse-distance-weighting-idw-interpolation/

        int numThreads = System.Environment.ProcessorCount;
        int chunkSize = Mathf.CeilToInt(pattern.Length / (float)numThreads);

        float[] localHeightValues = new float[numThreads];
        float[] localInverseDistances = new float[numThreads];

        Parallel.For(0, numThreads, threadIndex =>
        {
            int startIndex = threadIndex * chunkSize;
            int endIndex = Mathf.Min((threadIndex + 1) * chunkSize, pattern.Length);

            float localHeightValue = 0;
            float localInverseDistance = 0;

            for (int p = startIndex; p < endIndex; p++)
            {
                Vector3 curentPos = position + pattern[p];
                float distance = Vector3.Distance(curentPos, position);

                // Check map bounds
                if (curentPos.x >= 0.0f && curentPos.x <= mapSize.x && curentPos.y >= 0.0f && curentPos.y <= mapSize.y)
                {
                    distance = distance / distance;
                    float biomeHeight = GetBiomeHeight(curentPos);

                    localHeightValue += biomeHeight / distance;
                    localInverseDistance += 1.0f / distance;
                }
            }

            localHeightValues[threadIndex] = localHeightValue;
            localInverseDistances[threadIndex] = localInverseDistance;
        });

        float heightValue = 0;
        float inverseDistance = 0;

        for (int i = 0; i < numThreads; i++)
        {
            heightValue += localHeightValues[i];
            inverseDistance += localInverseDistances[i];
        }

        return heightValue / inverseDistance;
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

    public Vector3 GetChunkLocalCoord(float x, float y, float z) => new Vector3(x % chunkSize.x, y, z % chunkSize.y);

    private float GetBaseHeight(Vector3 position) => Mathf.Clamp01(GetNoiseHeight(position, baseSettings.noiseSettings));

    private float GetBiomeHeight(Vector3 position) => GetNoiseHeight(position, biomeSettings[GetBiomeIndex(position)].noiseSettings);

    private int GetBiomeIndex(Vector3 position) => (biomeSettings.Count - 1) * (int)GetBaseHeight(position);

    private float GetNoiseHeight(Vector3 position, NoiseSettings noiseSettings)
    {
        float xCoord = position.x / mapSize.x * noiseSettings.scale + noiseSettings.offset.x;
        float zCoord = position.z / mapSize.y * noiseSettings.scale + noiseSettings.offset.y;

        return Mathf.PerlinNoise(xCoord, zCoord) * noiseSettings.maxHeight + noiseSettings.minHeight;
    }

    public float GetHeight(Vector3 position) => GetHeightMapIDWParallel(position, idwPattern);

    private float GetSphereRadius(Vector3 scale) => Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;

    private bool CheckSphereOverlap(Vector3 positionA, float radiusA, Vector3 positionB, float radiusB)
    {
        // Custom collision check between two spheres
        // If the distance between the two sphere centers is less than the sum of their radii, return true; otherwise, return false
        float distanceSquared = (positionA - positionB).sqrMagnitude;
        float combinedRadii = radiusA + radiusB;
        return distanceSquared < combinedRadii * combinedRadii;
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

        //poisson point debugger
        if (debugPoisson)
        {
            Vector3 debugChunk = new Vector3(48f, 0f, 32f);

            if (ChunkCells.ContainsKey(debugChunk) && ChunkCells[debugChunk] != null)
            {
                if (poissonSamples == null || poissonSamples.Count == 0)
                {
                    poissonSamples = GetPoissonPoints(1.0f, ChunkCells[debugChunk].myRenderer.bounds, 100);
                }

                //local to world and get heights
                for (int p = 0; p < poissonSamples.Count; p++)
                {
                    poissonSamples[p] += ChunkCells[debugChunk].myRenderer.bounds.min;
                    poissonSamples[p] = new Vector3(poissonSamples[p].x, GetHeight(poissonSamples[p]), poissonSamples[p].z);
                }

                foreach (Vector3 point in poissonSamples) Gizmos.DrawWireCube(point, Vector3.one * 0.5f);
            }
        }
        else
        {
            poissonSamples.Clear();
        }

        /*
        //debug placed prefab bounding boxes
        if (placedPrefabBoundsDebug != null && placedPrefabBoundsDebug.Count > 0)
        {
            Gizmos.color = Color.magenta;
            foreach (Bounds bounds in placedPrefabBoundsDebug)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
        */

        /*
        //debug overlapping prefab bounding boxes
        if (overlappedPrefabBoundsDebug != null && overlappedPrefabBoundsDebug.Count > 0)
        {
            Gizmos.color = Color.magenta;
            foreach (Bounds bounds in overlappedPrefabBoundsDebug)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
        */
    }
}