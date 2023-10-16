using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using static StaticUtils;
//using static MeshUtilities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

public class Planet : Singleton<Planet>
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

    [Serializable]
    public struct BiomeSettings
    {
        public NoiseSettings noiseSettings;
        public List<PrefabBiomeSettings> prefabBiomeSettings;
        public float totalChance => CalculateTotalChance();

        public void SortByChances()
        {
            prefabBiomeSettings.Sort((a, b) => b.chance.CompareTo(a.chance));
        }

        private float CalculateTotalChance()
        {
            float total = 0f;
            foreach (PrefabBiomeSettings prefabSetting in prefabBiomeSettings)
            {
                total += prefabSetting.chance;
            }
            //Debug.Log($"Total Chance: {total}");
            return total;
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
            this.offset = offset;
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

    [Range(100.0f, 1000.0f)] public float renderDistance = 100.0f;
    private HashSet<Vector3> activeChunks;

    public UnityAction OnChunksGenerated;

    public static readonly int mapSize = 256 + 1;
    public static readonly int chunkSize = 32;

    public GameObject terrainChunk;

    public Dictionary<Vector3, Chunk> ChunkCells = new Dictionary<Vector3, Chunk>();

    public BiomeSettings baseSettings;
    public List<BiomeSettings> biomeSettings;
    public List<PrefabSettings> prefabSettings;
    private Dictionary<GameObject, PrefabSettings> prefabSettingsLUT;

    public float poissonSampleMinDistance = 10.0f;

    public bool generateNavmeshes = false;

    public bool debugWorldBounds = true;
    public bool debugChunkBounds = true;
    public bool debugHeightMap = true;
    public bool debugIDWPattern = true;
    private List<Vector3> poissonSamples = new List<Vector3>();
    public bool debugPoisson = false;
    public Gradient debugGradient;

    private Bounds worldBounds = new Bounds();

    private const float idwStepSize = 1.0f;
    private const float idwRange = 5.0f;
    private Vector3[] idwPattern;

    [SerializeField] private Mesh baseMesh;
    [SerializeField] private Mesh simpleMesh;

    NativeArray<Vector3> verticesNative;
    NativeArray<Vector3> verticesHeightNative;
    NativeArray<Vector3> simpleVerticesNative;
    NativeArray<Vector3> simpleVerticesHeightNative;
    NativeArray<NoiseSettings> biomeNoiseSettingsNative;
    NativeArray<Vector3> idwPatternNative;

    //debug
    //private List<Bounds> placedPrefabBoundsDebug = new List<Bounds>();
    //private List<Bounds> overlappedPrefabBoundsDebug = new List<Bounds>();

    private void Start()
    {
        InitializePlanet();
    }

    private void Update()
    {
        ActivateChunks();
    }

    private void ActivateChunks()
    {
        float chunkRenderDistance = Mathf.CeilToInt(renderDistance / chunkSize + 1) * chunkSize; //+1 to be sure they are out of rendering range and will turn off
        Vector3 currentChunkPosition;
        Vector3 currentWorldPosition;

        //currentChunkPosition = GetChunkByWorldPosition(Camera.main.position).chunkWorldPos;
        currentChunkPosition = GetChunkPosition(Camera.main.transform.position);

        for (float z = currentChunkPosition.z - chunkRenderDistance; z <= currentChunkPosition.z + chunkRenderDistance; z += chunkSize)
        {
            for (float x = currentChunkPosition.x - chunkRenderDistance; x <= currentChunkPosition.x + chunkRenderDistance; x += chunkSize)
            {
                currentWorldPosition = new Vector3(x, 0.0f, z);

                if (ChunkCells.ContainsKey(currentWorldPosition))
                {
                    //if they out of render distance deactivate them, else make them active. TODO -> respawn objects on deactivating and don't forget to check overlapping units!
                    bool inRenderingDistance = Vector3.Distance(ChunkCells[currentWorldPosition].myRenderer.bounds.center, Camera.main.transform.position) < renderDistance;
                    bool inDetailDistance = Vector3.Distance(ChunkCells[currentWorldPosition].myRenderer.bounds.center, Camera.main.transform.position) < renderDistance * 0.5f;

                    ChunkCells[currentWorldPosition].gameObject.SetActive(inRenderingDistance);

                    if (inRenderingDistance)
                    {
                        activeChunks.Add(currentWorldPosition);
                        if (inDetailDistance)
                        {
                            ChunkCells[currentWorldPosition].SetMeshTo(Chunk.MeshDetail.Detailed, true);
                        }
                        else
                        {
                            ChunkCells[currentWorldPosition].SetMeshTo(Chunk.MeshDetail.Simple, false);
                        }
                    }
                    else
                    {
                        if (activeChunks.Contains(currentWorldPosition)) activeChunks.Remove(currentWorldPosition);
                    }

                    
                    //chunk frustrum culling
                    bool isVisible = GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), ChunkCells[currentWorldPosition].myRenderer.bounds);

                    if (isVisible)
                    {
                        ChunkCells[currentWorldPosition].myRenderer.enabled = isVisible;
                    }                    
                }
            }
        }
    }

    public void InitializePlanet()
    {
        DateTime exectime = DateTime.Now;

        baseSettings.noiseSettings = new NoiseSettings(baseSettings.noiseSettings.offset == Vector2.zero ? new Vector2(Random.Range(0, 999999), Random.Range(0, 999999)) : baseSettings.noiseSettings.offset,
                                                       baseSettings.noiseSettings.scale,
                                                       baseSettings.noiseSettings.amplitude,
                                                       0,
                                                       biomeSettings.Count);

        biomeSettings.ForEach(setting =>
        {
            setting.noiseSettings = new NoiseSettings(setting.noiseSettings.offset == Vector2.zero ? new Vector2(Random.Range(0, 999999), Random.Range(0, 999999)) : setting.noiseSettings.offset,
                                                      setting.noiseSettings.scale,
                                                      setting.noiseSettings.amplitude,
                                                      setting.noiseSettings.minHeight,
                                                      setting.noiseSettings.maxHeight);

            //setting.noiseSettings.offset = setting.noiseSettings.offset == Vector2.zero ? new Vector2(Random.Range(0, 999999), Random.Range(0, 999999)) : setting.noiseSettings.offset;
        });

        biomeNoiseSettingsNative = new NativeArray<NoiseSettings>(biomeSettings.Count, Allocator.Persistent);

        for (int b = 0; b < biomeSettings.Count; b++)
        {
            biomeNoiseSettingsNative[b] = biomeSettings[b].noiseSettings;
        }

        idwPattern = GetPattern(idwStepSize, idwRange);
        idwPatternNative = new NativeArray<Vector3>(idwPattern.Length, Allocator.Persistent);
        idwPattern.CopyTo(idwPatternNative);

        Debug.Log("Biomes Initialized in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        //Chunk Generation
        exectime = DateTime.Now;

        //baseMesh.RecalculateBounds();
        //simpleMesh.RecalculateBounds();

        //baseMesh = GenerateMesh();
        verticesNative = new NativeArray<Vector3>(baseMesh.vertices.Length, Allocator.Persistent);
        verticesHeightNative = new NativeArray<Vector3>(baseMesh.vertices.Length, Allocator.Persistent);
        baseMesh.vertices.CopyTo(verticesNative);

        //TerrainSimplification terrainSimplification = new TerrainSimplification();
        //simpleMesh = Instantiate(baseMesh);
        //terrainSimplification.SimplifyTerrain(simpleMesh);

        simpleVerticesNative = new NativeArray<Vector3>(simpleMesh.vertices.Length, Allocator.Persistent);
        simpleVerticesHeightNative = new NativeArray<Vector3>(simpleMesh.vertices.Length, Allocator.Persistent);
        simpleMesh.vertices.CopyTo(simpleVerticesNative);

        for (int z = 0; z < mapSize - 1; z += chunkSize)
        {
            for (int x = 0; x < mapSize - 1; x += chunkSize)
            {
                Vector3 worldPosition = new Vector3(x, 0, z);

                ChunkCells.Add(worldPosition, CreateChunk(worldPosition));
                GenerateChunkMeshes(worldPosition, ChunkCells[worldPosition]);

                worldBounds.Encapsulate(ChunkCells[worldPosition].myRenderer.bounds);
            }
        }

        ChunkCells.TrimExcess();
        activeChunks = new HashSet<Vector3>(ChunkCells.Count);
        DisposeNatives();

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
        int spawnLayer = LayerMask.GetMask("Planet");
        Dictionary<Chunk, List<Bounds>> placedBounds = new Dictionary<Chunk, List<Bounds>>();

        //init placed Bounds -> prepare for spatial partitioning
        foreach (KeyValuePair<Vector3, Chunk> chunk in ChunkCells)
        {
            placedBounds.Add(chunk.Value, new List<Bounds>());
        }

        foreach (KeyValuePair<Vector3, Chunk> chunk in ChunkCells)
        {
            SpawnPrefabs(chunk.Value, placedBounds, spawnLayer);
            //StaticBatchingUtility.Combine(chunk.Value.gameObject);
            chunk.Value.gameObject.SetActive(false);
        }


        Debug.Log("Prefabs generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        //Mesh combining
        exectime = DateTime.Now;

        foreach (KeyValuePair<Vector3, Chunk> chunk in ChunkCells)
        {
            chunk.Value.GetChildrenMeshFilters();
            chunk.Value.CombineMeshes();
        }

        Debug.Log("Meshes combined in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        OnChunksGenerated += ChunksGenerated;
        OnChunksGenerated.Invoke();

        //To debug coordinate functions
        //Debug.Log("chunk -> " + GetChunkByWorldPosition(new Vector3(123.0f, 0.0f, 71.653f)).name);
        //Debug.Log("chunk local pos-> " + GetChunkLocalCoord(new Vector3(123.0f, 0.0f, 71.653f)));
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
        currentChunk.myTransform.localPosition = worldPosition;
        currentChunk.DetailMesh = Instantiate(baseMesh);
        GenerateHeightMesh(worldPosition, currentChunk.DetailMesh);

        currentChunk.SimpleMesh = Instantiate(simpleMesh);
        currentChunk.SimpleMesh = Instantiate(simpleMesh);
        GenerateSimplifiedHeightMesh(worldPosition, currentChunk.SimpleMesh);

        //This is only for faster map generation! -> less GC & inaccurate prefab placement
        //currentChunk.SetMeshTo(Chunk.MeshDetail.Simple, true);
        currentChunk.SetMeshTo(Chunk.MeshDetail.Detailed, true);
        currentChunk.SimpleMesh.UploadMeshData(true);
        currentChunk.DetailMesh.UploadMeshData(true);
    }

    /*
    private Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(chunkSize + 1) * (chunkSize + 1)];

        //vertices
        int i = 0;
        for (int d = 0; d <= chunkSize; d++)
        {
            for (int w = 0; w <= chunkSize; w++)
            {
                vertices[i] = new Vector3(w, 0.0f, d);
                i++;
            }
        }

        //triangles
        int[] triangles = new int[chunkSize * chunkSize * 6];

        for (int d = 0; d < chunkSize; d++)
        {
            for (int w = 0; w < chunkSize; w++)
            {
                int ti = (d * (chunkSize) + w) * 6;

                triangles[ti] = (d * (chunkSize + 1)) + w;
                triangles[ti + 1] = ((d + 1) * (chunkSize + 1)) + w;
                triangles[ti + 2] = ((d + 1) * (chunkSize + 1)) + w + 1;

                triangles[ti + 3] = (d * (chunkSize + 1)) + w;
                triangles[ti + 4] = ((d + 1) * (chunkSize + 1)) + w + 1;
                triangles[ti + 5] = (d * (chunkSize + 1)) + w + 1;
            }
        }

        //UV
        Vector2[] uv = new Vector2[(chunkSize + 1) * (chunkSize + 1)];

        i = 0;
        for (int d = 0; d <= chunkSize; d++)
        {
            for (int w = 0; w <= chunkSize; w++)
            {
                uv[i] = new Vector2(w / (float)chunkSize, d / (float)chunkSize);
                i++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        SplitVerticesWithUV(mesh);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.Optimize();
        return mesh;
    }
    */

    [BurstCompile]
    struct HeightJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<NoiseSettings> biomeNoiseSettings;
        [ReadOnly] public NativeArray<Vector3> idwPattern;
        [ReadOnly] public int chunkSize;
        [ReadOnly] public int mapSize;
        [ReadOnly] public Vector3 worldPosition;
        [ReadOnly] public NoiseSettings baseNoiseSettings;
        [WriteOnly] public NativeArray<Vector3> verticesHeight;

        public void Execute(int i)
        {
            Vector3 currentWorldPosition = worldPosition + vertices[i];

            verticesHeight[i] = new Vector3(vertices[i].x, GetHeight(currentWorldPosition), vertices[i].z);
        }

        private float GetBaseHeight(Vector3 position) => Mathf.Clamp01(GetNoiseHeight(position, baseNoiseSettings));

        private float GetBiomeHeight(Vector3 position) => GetNoiseHeight(position, biomeNoiseSettings[GetBiomeIndex(position)]);

        private int GetBiomeIndex(Vector3 position) => (biomeNoiseSettings.Length - 1) * (int)GetBaseHeight(position);

        private float GetNoiseHeight(Vector3 position, NoiseSettings noiseSettings)
        {
            float xCoord = position.x / mapSize * noiseSettings.scale + noiseSettings.offset.x;
            float zCoord = position.z / mapSize * noiseSettings.scale + noiseSettings.offset.y;

            //return Mathf.PerlinNoise(xCoord, zCoord) * noiseSettings.maxHeight + noiseSettings.minHeight;
            return noise.cnoise(new float2(xCoord, zCoord)) * noiseSettings.maxHeight + noiseSettings.minHeight;
        }

        private float GetHeight(Vector3 position) => GetHeightMapIDW(position, idwPattern);

        private float GetHeightMapIDW(Vector3 position, NativeArray<Vector3> pattern)
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
                if (curentPos.x < 0.0f || curentPos.x > mapSize || curentPos.y < 0.0f || curentPos.y > mapSize) continue;

                distance = distance / distance;
                heightValue += GetBiomeHeight(curentPos) / distance;
                inverseDistance += 1.0f / distance;
            }

            return heightValue / inverseDistance;
        }
    }

    private void GenerateHeightMesh(Vector3 worldPosition, Mesh mesh)
    {
        HeightJob heightJob = new HeightJob()
        {
            vertices = verticesNative,
            verticesHeight = verticesHeightNative,
            worldPosition = worldPosition,
            baseNoiseSettings = baseSettings.noiseSettings,
            biomeNoiseSettings = biomeNoiseSettingsNative,
            idwPattern = idwPatternNative,
            chunkSize = chunkSize,
            mapSize = mapSize,
        };

        JobHandle heightJobHandle = heightJob.Schedule(mesh.vertices.Length, 32);

        heightJobHandle.Complete();

        mesh.vertices = verticesHeightNative.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.Optimize();
    }

    private void GenerateSimplifiedHeightMesh(Vector3 worldPosition, Mesh mesh)
    {
        HeightJob heightJob = new HeightJob()
        {
            vertices = simpleVerticesNative,
            verticesHeight = simpleVerticesHeightNative,
            worldPosition = worldPosition,
            baseNoiseSettings = baseSettings.noiseSettings,
            biomeNoiseSettings = biomeNoiseSettingsNative,
            idwPattern = idwPatternNative,
            chunkSize = chunkSize,
            mapSize = mapSize,
        };

        JobHandle heightJobHandle = heightJob.Schedule(mesh.vertices.Length, 32);

        heightJobHandle.Complete();

        mesh.vertices = simpleVerticesHeightNative.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.Optimize();
    }

    private void SpawnPrefabs(Chunk chunk, Dictionary<Chunk, List<Bounds>> placedBounds, int spawnLayer)
    {
        //get poisson disc sampling points then spawn prefabs by chances
        poissonSamples = GeneratePoissonPoints(poissonSampleMinDistance, chunk.myRenderer.bounds, 100);
        //Debug.Log($"Poisson points {poissonSamples.Count}");
        //(int placed, int overlapping, int rayMiss) debug = (0, 0, 0);

        //local to world and get heights
        for (int p = 0; p < poissonSamples.Count; p++)
        {
            poissonSamples[p] += chunk.myRenderer.bounds.min;
            poissonSamples[p] = new Vector3(poissonSamples[p].x, 0.0f, poissonSamples[p].z);
        }

        foreach (Vector3 point in poissonSamples)
        {
            int biomeIndex = GetBiomeIndex(point);
            //float randomValue = Random.Range(0f, biomeSettings[biomeIndex].totalChance);
            float randomValue = Random.value * biomeSettings[biomeIndex].totalChance;

            //Debug.Log($"Point: {point}, Biome Index: {biomeIndex}, Random Value: {randomValue}");

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

                    foreach (Vector3 neighbourPostion in GetNeighbourCoords(chunk))
                    {
                        if (!ChunkCells.ContainsKey(neighbourPostion)) continue;

                        foreach (Bounds placedBound in placedBounds[ChunkCells[neighbourPostion]])
                        {
                            Vector3 existingPosition = placedBound.center;
                            Vector3 existingScale = placedBound.size;
                            float existingRadius = GetSphereRadius(existingScale);

                            if (CheckSphereOverlap(spawnPosition, prefabRadius, existingPosition, existingRadius))
                            {
                                canBePlaced = false;
                                break;
                            }
                        }

                        if (canBePlaced == false) break;
                    }

                    if (canBePlaced)
                    {
                        GameObject spawnedPrefab = Instantiate(selectedPrefab, spawnPosition, randomRotation);
                        spawnedPrefab.transform.localScale = randomScaleVector;
                        spawnedPrefab.transform.SetParent(chunk.myTransform);
                        placedBounds[chunk].Add(spawnedPrefab.GetComponent<MeshRenderer>().bounds);
                        //debug.placed++;
                    }
                    else
                    {
                        //debug.overlapping++;
                    }
                }
                else
                {
                    //debug.rayMiss++;
                }
            }
        }
        //Debug.Log($"Chunk -> {chunk.chunkWorldPos} Placed {debug.placed} overlapping {debug.overlapping} ray miss {debug.rayMiss}.");
    }

    public void ChunksGenerated()
    {
        Debug.Log("Chunks ready!");
    }

    //Chunk Functions-------------------------------------
    public Vector3[] GetNeighbourCoords(Chunk chunk)
    {
        Vector3[] neighbours = new Vector3[9];
        Vector3 chunkPos = chunk.chunkWorldPos;

        int index = 0;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                //if (x == 0 && y == 0) continue; // Skip the current chunk position

                neighbours[index] = new Vector3(chunkPos.x + x * chunkSize, 0.0f, chunkPos.z + y * chunkSize);
                index++;
            }
        }

        return neighbours;
    }

    public Chunk GetChunkByWorldPosition(Vector3 worldPos) => ChunkCells[worldPos - GetChunkLocalCoord(worldPos)];

    public Vector3 GetChunkLocalCoord(Vector3 worldPos) => new Vector3(worldPos.x % chunkSize, 0.0f, worldPos.z % chunkSize);

    public Vector3 GetChunkPosition(Vector3 worldPos)
    {
        int chunkX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(worldPos.z / chunkSize);

        Vector3 chunkPosition = new Vector3(chunkX * chunkSize, 0.0f, chunkZ * chunkSize);

        return chunkPosition;
    }

    //--------------------

    //Height Functions---------------------
    private float GetBaseHeight(Vector3 position) => Mathf.Clamp01(GetNoiseHeight(position, baseSettings.noiseSettings));

    private float GetBiomeHeight(Vector3 position) => GetNoiseHeight(position, biomeSettings[GetBiomeIndex(position)].noiseSettings);

    private int GetBiomeIndex(Vector3 position) => (biomeSettings.Count - 1) * (int)GetBaseHeight(position);

    private float GetNoiseHeight(Vector3 position, NoiseSettings noiseSettings)
    {
        float xCoord = position.x / mapSize * noiseSettings.scale + noiseSettings.offset.x;
        float zCoord = position.z / mapSize * noiseSettings.scale + noiseSettings.offset.y;

        return Mathf.PerlinNoise(xCoord, zCoord) * noiseSettings.maxHeight + noiseSettings.minHeight;
    }

    public float GetHeight(Vector3 position) => GetHeightMapIDW(position, idwPattern);

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
            if (curentPos.x < 0.0f || curentPos.x > mapSize || curentPos.y < 0.0f || curentPos.y > mapSize) continue;

            distance = distance / distance;
            heightValue += GetBiomeHeight(curentPos) / distance;
            inverseDistance += 1.0f / distance;
        }

        return heightValue / inverseDistance;
    }

    //--------------------

    //Collision functions ------------------------
    private float GetSphereRadius(Vector3 scale) => Mathf.Max(scale.x, scale.y, scale.z) * 0.5f;

    private bool CheckSphereOverlap(Vector3 positionA, float radiusA, Vector3 positionB, float radiusB)
    {
        // Custom collision check between two spheres
        // If the distance between the two sphere centers is less than the sum of their radii, return true; otherwise, return false
        float distanceSquared = (positionA - positionB).sqrMagnitude;
        float combinedRadii = radiusA + radiusB;
        return distanceSquared < combinedRadii * combinedRadii;
    }

    //--------------------

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
            Vector3 cameraPos = Camera.current.transform.position;
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
                    poissonSamples = GeneratePoissonPoints(1.0f, ChunkCells[debugChunk].myRenderer.bounds, 100);
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

    private void OnDestroy()
    {
        DisposeNatives();
    }

    private void OnDisable()
    {
        DisposeNatives();
    }

    private void DisposeNatives()
    {
        if (verticesNative.IsCreated) verticesNative.Dispose();
        if (verticesHeightNative.IsCreated) verticesHeightNative.Dispose();
        if (simpleVerticesNative.IsCreated) simpleVerticesNative.Dispose();
        if (simpleVerticesHeightNative.IsCreated) simpleVerticesHeightNative.Dispose();
        if (biomeNoiseSettingsNative.IsCreated) biomeNoiseSettingsNative.Dispose();
        if (idwPatternNative.IsCreated) idwPatternNative.Dispose();
    }
}