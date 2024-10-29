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
        //Debug.DrawRay(transform.position, m_VehicleRigidbody.linearVelocity, Color.red, m_VehicleRigidbody.linearVelocity.magnitude * 10f);
    }

    public override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    public void FixedUpdate()
    {
        if (!m_Vehicle.isBreaking) return;

        List<Module> hoverModules = m_Vehicle.GetAllModulesOfType(typeof(HoverModule));

        for (int i = 0; i < 3; i++)
        {
            if (i == 0) //X forward
            {
                velocities[i] = Vector3.Project(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.forward).magnitude;
                controllerOutputs[i] = controllers[i].Update(Time.fixedDeltaTime, velocities[i], 0f);
                foreach (Module module in hoverModules) (module as HoverModule).acceleration = -controllerOutputs[i] * Vector3.Dot(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.forward);
            }

            if (i == 1) //Y up
            {

            }

            if (i == 2) //Z right
            {
                velocities[i] = Vector3.Project(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.right).magnitude;
                controllerOutputs[i] = controllers[i].Update(Time.fixedDeltaTime, velocities[i], 0f);
                foreach (Module module in hoverModules) (module as HoverModule).steering = -controllerOutputs[i] * Vector3.Dot(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.right);
            }
        }
    }

    public PIDController[] GetControllers() => controllers;

    private void OnDrawGizmos()
    {
        if (m_VehicleRigidbody)
        {
            GizmosExtend.DrawArrow(transform.position, Vector3.Project(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.forward), Color.green * 0.5f);
            GizmosExtend.DrawArrow(transform.position, Vector3.Project(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.up), Color.green * 0.5f);
            GizmosExtend.DrawArrow(transform.position, Vector3.Project(m_VehicleRigidbody.linearVelocity, m_Vehicle.transform.right), Color.green * 0.5f);
        }
    }
}
