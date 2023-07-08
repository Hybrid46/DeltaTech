using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Vector3 chunkWorldPos;
    public bool saved = false;

    public Transform myTransform;
    public MeshFilter myMeshFilter;
    public Mesh myMesh;
    public MeshCollider myMeshCollider;
    public MeshRenderer myRenderer;
    //public Bounds myBounds;

    public Mesh SimpleMesh;
    public Mesh DetailMesh;

    public enum MeshDetail { Simple, Detailed }
    public MeshDetail meshDetail;
    public bool updateChunk = false;

    public void Init()
    {
        myTransform = transform;
        myMeshFilter = GetComponent<MeshFilter>();
        myMesh = myMeshFilter.sharedMesh;
        myMeshCollider = GetComponent<MeshCollider>();
        myRenderer = GetComponent<MeshRenderer>();
        //myBounds = myRenderer.bounds;
        //myBounds.Encapsulate(new Vector3(chunkWorldPos.x, 100.0f, chunkWorldPos.z));
    }

    private void Update()
    {
        /*
        distanceToPlayer = Vector3.Distance(MapGen.instance.playerTransform.position, myTransform.position);

        if (distanceToPlayer > MapGen.instance.renderDistance.x * 2.0f)
        {
            gameObject.SetActive(false);
        }
        */

        if (updateChunk)
        {
            updateChunk = false;

            switch (meshDetail)
            {
                case MeshDetail.Simple:
                    myMeshFilter.sharedMesh = SimpleMesh;
                    break;
                case MeshDetail.Detailed:
                    myMeshFilter.sharedMesh = DetailMesh;
                    break;
            }

            myMeshCollider.sharedMesh = myMeshFilter.sharedMesh;
        }
    }

    public void SetMeshTo(bool useDetail, bool setCollider)
    {
        myMeshFilter.sharedMesh = useDetail ? DetailMesh : SimpleMesh;
        if (setCollider) myMeshCollider.sharedMesh = myMeshFilter.sharedMesh;
    }

    public void SaveChunkToDisk()
    {
        Debug.Log("Chunk not saved");
    }

    public void LoadChunkFromDisk()
    {
        Debug.Log("Chunk not loaded");
    }

    public void DestroyChunk()
    {
        //Planet.ChunkCells.Remove(chunkWorldPos);
#if UNITY_EDITOR
        DestroyImmediate(myMesh);
        DestroyImmediate(SimpleMesh);
        DestroyImmediate(DetailMesh);
#else
        Destroy(myMesh);
        Destroy(SimpleMesh);
        Destroy(DetailMesh);
#endif
    }

    private void OnDestroy()
    {
        DestroyChunk();
    }
}
