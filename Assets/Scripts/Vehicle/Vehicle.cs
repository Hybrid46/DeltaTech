using System.Collections.Generic;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public float verticalInput;
    public float horizontalInput;

    public Module control;
    private HashSet<Module> modules = new HashSet<Module>();
    private HashSet<Module> turrets = new HashSet<Module>();
    private HashSet<Module> drive = new HashSet<Module>();
    private HashSet<Module> ballasts = new HashSet<Module>();
    private HashSet<Module> effectors = new HashSet<Module>();

    private float vehicleMass;
    private float vehicleHp;
    private float vehicleBuoyancy;

    void Start()
    {
        (float buoyancy, float mass, float hp) stats = GetStats();

        vehicleBuoyancy = stats.buoyancy;
        vehicleMass = stats.mass;
        vehicleHp = stats.hp;
    }

    void Update()
    {
        if (modules.Count == 0) SelfDestruct();

        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
    }

    public void RemoveModule(Module module)
    {
        modules.Remove(module);
        turrets.Remove(module);
        drive.Remove(module);
        ballasts.Remove(module);
        effectors.Remove(module);
    }

    private void SelfDestruct()
    {
        Destroy(gameObject);
    }

    private (float buoyancy, float mass, float hp) GetStats()
    {
        float hp = 0;
        float buoyancy = 0;
        float mass = 0;

        foreach (Module module in modules)
        {
            buoyancy += module.buoyancy;
            mass += module.mass;
            hp += module.hp;
        }

        return (buoyancy, mass, hp);
    }

    public void AddStats(float buoyancy, float mass, float hp)
    {
        vehicleBuoyancy += buoyancy;
        vehicleMass += mass;
        vehicleHp += hp;
    }
}
