using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Vector3 chunkWorldPos;
    public bool saved;

    public Transform myTransform;
    public MeshFilter myMeshFilter;
    public MeshCollider myMeshCollider;
    public MeshRenderer myRenderer;
    public NavMeshSurface myNavMeshSurface;

    public Mesh SimpleMesh;
    public Mesh DetailMesh;

    public enum MeshDetail { Simple, Detailed }
    public MeshDetail meshDetail;

    private struct CombinableRefs
    {
        public Material material;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        public CombinableRefs(Material material, MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            this.material = material;
            this.meshFilter = meshFilter;
            this.meshRenderer = meshRenderer;
        }
    }

    private Dictionary<GameObject, CombinableRefs> childrenRefsLUT;
    private Dictionary<Material, List<GameObject>> combinedObjectsLUT;

    public void GetReferences()
    {
        myTransform = transform;
        myMeshFilter = GetComponent<MeshFilter>();
        myMeshCollider = GetComponent<MeshCollider>();
        myRenderer = GetComponent<MeshRenderer>();
        myNavMeshSurface = GetComponent<NavMeshSurface>();
    }

    public void GetChildrenMeshFilters()
    {
        childrenRefsLUT = new Dictionary<GameObject, CombinableRefs>(transform.childCount);

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Grass")) continue;

            childrenRefsLUT.Add(child.gameObject, new CombinableRefs(child.gameObject.GetComponent<MeshRenderer>().sharedMaterial, child.gameObject.GetComponent<MeshFilter>(), child.gameObject.GetComponent<MeshRenderer>()));
        }
    }

    public void CombineMeshes()
    {
        //organize meshfilters to materials
        Dictionary<Material, List<MeshFilter>> meshesToCombine = new Dictionary<Material, List<MeshFilter>>();

        foreach (KeyValuePair<GameObject, CombinableRefs> childRefs in childrenRefsLUT)
        {
            if (!meshesToCombine.ContainsKey(childRefs.Value.material))
            {
                meshesToCombine.Add(childRefs.Value.material, new List<MeshFilter>() { childRefs.Value.meshFilter });
            }
            else
            {
                meshesToCombine[childRefs.Value.material].Add(childRefs.Value.meshFilter);
            }
        }

        //combine the meshfilters
        combinedObjectsLUT = new Dictionary<Material, List<GameObject>>();

        foreach (KeyValuePair<Material, List<MeshFilter>> meshRefs in meshesToCombine)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            List<CombineInstance> currentChunkInstances = new List<CombineInstance>();
            int verticesCount = 0;
            int chunkCount = 0;

            foreach (MeshFilter meshFilter in meshRefs.Value)
            {
                int meshVertices = meshFilter.sharedMesh.vertexCount;

                if (verticesCount + meshVertices > 65000 && currentChunkInstances.Count > 0)
                {
                    // Create a new chunk when reaching the vertex limit
                    CreateCombinedObject(meshRefs.Key, currentChunkInstances, chunkCount);
                    currentChunkInstances.Clear();
                    chunkCount++;
                    verticesCount = 0;
                }

                CombineInstance combineInstance = new CombineInstance();
                combineInstance.mesh = meshFilter.sharedMesh;
                combineInstance.subMeshIndex = 0;
                combineInstance.transform = meshFilter.transform.localToWorldMatrix;
                combineInstances.Add(combineInstance);
                currentChunkInstances.Add(combineInstance);
                verticesCount += meshVertices;

                childrenRefsLUT[meshFilter.gameObject].meshRenderer.enabled = false;
            }

            // Create the last chunk if any combineInstances remain
            if (currentChunkInstances.Count > 0)
            {
                CreateCombinedObject(meshRefs.Key, currentChunkInstances, chunkCount);
            }
        }
    }

    private void CreateCombinedObject(Material material, List<CombineInstance> combineInstances, int chunkIndex)
    {
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true, true);

        GameObject combinedObject = new GameObject($"CombinedMesh_{material.name}_{chunkIndex}");
        combinedObject.transform.SetParent(transform);
        combinedObject.transform.localPosition = Vector3.zero;
        combinedObject.transform.localRotation = Quaternion.identity;
        combinedObject.layer = transform.gameObject.layer;

        MeshFilter combinedMeshFilter = combinedObject.AddComponent<MeshFilter>();
        combinedMeshFilter.sharedMesh = combinedMesh;

        MeshRenderer meshRenderer = combinedObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;

        if (!combinedObjectsLUT.ContainsKey(material)) combinedObjectsLUT.Add(material, new List<GameObject>());
        combinedObjectsLUT[material].Add(combinedObject);
    }

    public void SetMeshTo(MeshDetail meshDetail, bool setCollider)
    {
        switch (meshDetail)
        {
            case MeshDetail.Simple:
                myMeshFilter.sharedMesh = SimpleMesh;
                break;
            case MeshDetail.Detailed:
                myMeshFilter.sharedMesh = DetailMesh;
                break;
        }

        if (setCollider) myMeshCollider.sharedMesh = myMeshFilter.sharedMesh;
    }

    public void SaveChunkToDisk()
    {
        throw new NotImplementedException("Chunk not saved");
    }

    public void LoadChunkFromDisk()
    {
        throw new NotImplementedException("Chunk not loaded");
    }

    public void DestroyChunk()
    {
        //Planet.ChunkCells.Remove(chunkWorldPos);
#if UNITY_EDITOR
        DestroyImmediate(SimpleMesh);
        DestroyImmediate(DetailMesh);
#else
        Destroy(SimpleMesh);
        Destroy(DetailMesh);
#endif
    }

    private void OnDestroy()
    {
        DestroyChunk();
    }
}
