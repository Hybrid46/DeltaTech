using GizmoExtension;
using System.Collections.Generic;
using UnityEngine;

public class Stabilizer : Module
{
    [SerializeField]
    private PIDController[] controllers = new PIDController[] {new PIDController (1f, 1f, 1f, 1f, false, -1f, 1f),
                                                                                new PIDController (1f, 1f, 1f, 1f, false, -1f, 1f),
                                                                                new PIDController (1f, 1f, 1f, 1f, false, -1f, 1f) };

    [SerializeField] private float[] controllerOutputs = new float[3];
    [SerializeField] private float[] velocities = new float[3];

    public override void Start()
    {
        base.Start();
    }

    public override void Update()
    {
        base.Update();
    }

    public override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    public void FixedUpdate()
    {
        if (!m_Vehicle.isBreaking) return;

        List<Module> hoverModules = m_Vehicle.GetAllModulesOfType(typeof(HoverModule));

        Debug.Log($"modules {hoverModules.Count}");

        for (int i = 0; i < 3; i++)
        {
            velocities[i] = m_VehicleRigidbody.linearVelocity[i];
            controllerOutputs[i] = controllers[i].Update(Time.fixedDeltaTime, velocities[i], 0f);

            if (i == 0) foreach (Module module in hoverModules) (module as HoverModule).steering = -controllerOutputs[i];
            if (i == 2) foreach (Module module in hoverModules) (module as HoverModule).acceleration = -controllerOutputs[i];
        }
    }

    private void OnDrawGizmos()
    {
        if (m_VehicleRigidbody)
        {
            GizmosExtend.DrawArrow(transform.position, transform.forward, Color.green * 0.5f);
        }
    }
}
