using System.Collections.Generic;
using UnityEngine;
using static StaticUtils;
using BiomeSettings = Planet.BiomeSettings;

public class PrefabInstancer : MonoBehaviour
{
    private List<Bounds> instantiatedBounds = new List<Bounds>();

    public PrefabInstancer(Chunk chunk)
    {
        /*
        Bounds bounds = chunk.myRenderer.bounds;

        const int sampleCount = 100;
        List<Vector2> poissonPoints = GeneratePoissonPoints();

        for (int i = 0; i < sampleCount; i++)
        {
            Vector3 position = new Vector3(Random.Range(bounds.min.x, bounds.max.x), bounds.center.y, Random.Range(bounds.min.z, bounds.max.z));

            Quaternion rotation = Quaternion.Euler(Random.Range(minRot.x, maxRot.x), Random.Range(minRot.y, maxRot.y), Random.Range(minRot.z, maxRot.z));
            Vector3 scale = new Vector3(Random.Range(minScl.x, maxScl.x), Random.Range(minScl.y, maxScl.y), Random.Range(minScl.z, maxScl.z));

            Bounds newBounds = new Bounds(position, Vector3.Scale(prefab.transform.localScale, prefab.GetComponent<MeshRenderer>().bounds.size));

            bool overlaps = CheckOverlap(newBounds);

            if (overlaps)
            {
                position.y += 1f;
                newBounds.center = position;
            }

            GameObject instance = Instantiate(prefab, position, rotation, chunk.myTransform);
            instance.transform.localScale = scale;
            instantiatedBounds.Add(newBounds);
        }
    }

    private bool CheckOverlap(Bounds newBounds)
    {
        foreach (Bounds existingBounds in instantiatedBounds)
        {
            if (newBounds.Intersects(existingBounds)) return true;
        }

        return false;
        */
    }
}
