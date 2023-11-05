using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public Rigidbody vehicleRigidbody { get; private set; }

    public float verticalInput;
    public float horizontalInput;

    public Module control;
    private HashSet<Module> modules = new HashSet<Module>();

    private float vehicleMass;
    private float vehicleHp;

    private void Start()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();

        GetAllModules();

        (float mass, float hp) stats = GetAllStats();

        vehicleMass = stats.mass;
        vehicleHp = stats.hp;

        vehicleRigidbody.mass = vehicleMass;
    }

    private void Update()
    {
        if (modules.Count == 0 || vehicleHp <= 0) SelfDestruct();

        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        //Unstuck
        if (Input.GetKeyDown(KeyCode.R))
        {
            RaycastHit hit;

            if (Physics.Raycast(vehicleRigidbody.gameObject.transform.position + Vector3.up * 100.0f, -Vector3.up, out hit))
            {
                vehicleRigidbody.gameObject.transform.rotation = Quaternion.identity;
                vehicleRigidbody.gameObject.transform.position = hit.point + hit.normal * 10.0f;
            }
        }
    }

    private void GetAllModules()
    {
        Module currentModule;
        modules.Clear();

        foreach (Transform child in transform)
        {
            currentModule = child.gameObject.GetComponent<Module>();

            if (currentModule != null)
            {
                modules.Add(currentModule);
                currentModule = null;
            }
        }
    }

    public void RemoveModule(Module module)
    {
        ModifyStats(-module.mass, -module.hp);
        modules.Remove(module);
    }

    public void AddModule(Module module)
    {
        ModifyStats(module.mass, module.hp);
        modules.Add(module);
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }

    private (float mass, float hp) GetAllStats()
    {
        float hp = 0;
        float mass = 0;

        foreach (Module module in modules)
        {
            mass += module.mass;
            hp += module.hp;
        }

        return (mass, hp);
    }

    public void ModifyStats(float mass, float hp)
    {
        vehicleMass += mass;
        vehicleHp += hp;
    }
}
