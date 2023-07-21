using System;
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

    public void GetReferences()
    {
        myTransform = transform;
        myMeshFilter = GetComponent<MeshFilter>();
        myMeshCollider = GetComponent<MeshCollider>();
        myRenderer = GetComponent<MeshRenderer>();
        myNavMeshSurface = GetComponent<NavMeshSurface>();
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
