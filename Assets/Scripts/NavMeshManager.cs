using System;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshManager : Singleton<NavMeshManager>
{
    public NavMeshSurface surfaceForCloning;

    public void GenerateNavMeshForPlanet(Planet planet)
    {
        DateTime exectime = DateTime.Now;
        Debug.Log("NavMesh Generation for a chunk");

        NavMeshSurface navSurface = planet.gameObject.AddComponent<NavMeshSurface>();
        StaticUtils.GetCopyOf(navSurface, surfaceForCloning);
        navSurface.BuildNavMesh();

        Debug.Log("NavMesh generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");
    }
}
