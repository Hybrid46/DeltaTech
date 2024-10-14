using System;
using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public Rigidbody vehicleRigidbody { get; private set; }

    public float verticalInput;
    public float horizontalInput;

    public bool isBreaking;

    private HashSet<Module> modules = new HashSet<Module>();
    private Dictionary<Type, List<Module>> moduleTypes = new Dictionary<Type, List<Module>>();

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

        isBreaking = Input.GetKey(KeyCode.Space);
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
                vehicleRigidbody.gameObject.transform.position = hit.point + hit.normal * 5.0f;
            }
        }
    }

    private void GetAllModules()
    {
        Module currentModule;
        modules.Clear();

        foreach (Transform child in transform)
        {
            if (child.gameObject.TryGetComponent(out currentModule))
            {
                modules.Add(currentModule);

                Type type = currentModule.GetType();
                if (!moduleTypes.ContainsKey(type))
                {
                    moduleTypes.Add(type, new List<Module>());
                    moduleTypes[type].Add(currentModule);
                }
                else
                {
                    moduleTypes[type].Add(currentModule);
                }

                currentModule = null;
            }
        }
    }

    public List<Module> GetAllModulesOfType(Type type)
    {
        return moduleTypes[type];
    }

    public void RemoveModule(Module module)
    {
        ModifyStats(-module.mass, -module.hp);
        modules.Remove(module);
        moduleTypes[module.GetType()].Remove(module);
    }

    public void AddModule(Module module)
    {
        ModifyStats(module.mass, module.hp);
        modules.Add(module);
        moduleTypes[module.GetType()].Add(module);
    }

    private void SelfDestruct()
    {
        foreach (Module module in modules) module.Destruct();
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
