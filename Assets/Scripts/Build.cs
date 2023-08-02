using UnityEngine;

public class Build : MonoBehaviour
{
    public int snapLayer = LayerMask.NameToLayer("SnapPoints");
    public int RayCastDistance = 100;
    public GameObject objectToSpawn;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.current.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, RayCastDistance, snapLayer))
            {
                Debug.DrawRay(ray.origin, ray.direction * RayCastDistance, Color.yellow);

                Vector3 spawnPoint = hit.collider.transform.position;
                Quaternion spawnRotation = hit.collider.transform.rotation;

                Instantiate(objectToSpawn, spawnPoint, spawnRotation, hit.collider.transform.root);
            }
            else
            {
                Debug.DrawRay(ray.origin, ray.direction * RayCastDistance, Color.red);
            }

        }
    }
}
