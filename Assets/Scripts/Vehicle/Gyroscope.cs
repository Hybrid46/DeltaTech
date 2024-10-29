using GizmoExtension;
using System;
using UnityEngine;

public class Gyroscope : Module
{
    [SerializeField]
    private PIDController[] controllers = { new PIDController(1f, 1f, 1f, 1f, true, -1f, 1f) ,
                                                             new PIDController(1f, 1f, 1f, 1f, true, -1f, 1f) ,
                                                             new PIDController(1f, 1f, 1f, 1f, true, -1f, 1f) };
    public float power = 10;

    private Vector3 targetDirY;
    private Vector3 currentDirY;

    private Vector3 targetDirX;
    private Vector3 currentDirX;

    private Vector3 targetDirZ;
    private Vector3 currentDirZ;

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
        AlignY();
        AlignX();
        AlignZ();
    }

    private void AlignZ()
    {
        targetDirZ = Vector3.up;
        currentDirZ = m_VehicleRigidbody.rotation * transform.up;

        float currentAngle = Vector3.SignedAngle(transform.up, currentDirZ, Vector3.forward);
        float targetAngle = Vector3.SignedAngle(transform.up, targetDirZ, Vector3.forward);

        float input = controllers[2].Update(Time.fixedDeltaTime, currentAngle, targetAngle);
        m_VehicleRigidbody.AddTorque(0f, 0f, input * power, ForceMode.Force);
    }

    private void AlignX()
    {
        targetDirX = Vector3.up;
        currentDirX = m_VehicleRigidbody.rotation * transform.up;

        float currentAngle = Vector3.SignedAngle(transform.up, currentDirX, Vector3.right);
        float targetAngle = Vector3.SignedAngle(transform.up, targetDirX, Vector3.right);

        float input = controllers[0].Update(Time.fixedDeltaTime, currentAngle, targetAngle);
        m_VehicleRigidbody.AddTorque(input * power, 0f, 0f, ForceMode.Force);
    }

    private void AlignY()
    {
        targetDirY = m_VehicleRigidbody.rotation * transform.forward;
        currentDirY = m_VehicleRigidbody.rotation * transform.forward;

        bool turnRight = m_Vehicle.rotationInput < 0f;
        bool turnLeft = m_Vehicle.rotationInput > 0f;

        if (turnRight) targetDirY = m_VehicleRigidbody.rotation * transform.right;
        if (turnLeft) targetDirY = m_VehicleRigidbody.rotation * -transform.right;

        float currentAngle = Vector3.SignedAngle(transform.forward, currentDirY, Vector3.up);
        float targetAngle = Vector3.SignedAngle(transform.forward, targetDirY, Vector3.up);

        float input = controllers[2].Update(Time.fixedDeltaTime, currentAngle, targetAngle);
        m_VehicleRigidbody.AddTorque(0f, input * power, 0f, ForceMode.Force);
    }

    public PIDController[] GetControllers() => controllers;

    private void OnDrawGizmos()
    {
        if (m_VehicleRigidbody)
        {
            GizmosExtend.DrawArrow(transform.position - new Vector3(0.5f, 0f, 0f), currentDirX, Color.red * 1.5f);
            GizmosExtend.DrawArrow(transform.position - new Vector3(0.5f, 0f, 0f), targetDirX, Color.magenta * 1.5f);

            GizmosExtend.DrawArrow(transform.position, currentDirY, Color.green * 0.5f);
            GizmosExtend.DrawArrow(transform.position, targetDirY, Color.yellow * 0.5f);

            GizmosExtend.DrawArrow(transform.position + new Vector3(0.5f, 0f, 0f), currentDirZ, Color.blue * 1f);
            GizmosExtend.DrawArrow(transform.position + new Vector3(0.5f, 0f, 0f), targetDirZ, Color.cyan * 1f);
        }
    }
}
