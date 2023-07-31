#define DEBUG
using GizmoExtension;
using System;
using UnityEngine;

[DisallowMultipleComponent]
/*
public class Wheel : MonoBehaviour
{
    private float radius;
    private float wheelSuspensionDistance;
    private float RayCastDistance = 10.0f;

    private Transform m_Transform;
    private Collider m_Collider;

    public Suspension suspension;

    [Serializable]
    public struct Suspension
    {
        public Vector3 suspensionPoint; //local
        public float suspensionStrength;
        public float suspensionDampening;
    }

    void Start()
    {
        radius = gameObject.GetComponent<MeshRenderer>().bounds.size.x * 0.5f;
        m_Transform = transform;
        m_Collider = GetComponent<Collider>();
    }

    void Update()
    {
        wheelSuspensionDistance = Vector3.Distance(m_Transform.position, m_Transform.TransformPoint(suspension.suspensionPoint));

        RaycastHit hit;
        if (Physics.Raycast(m_Transform.position, m_Transform.TransformDirection(Vector3.down), out hit, RayCastDistance))
        {
            Debug.DrawRay(m_Transform.position, m_Transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
        }
        else
        {
            Debug.DrawRay(m_Transform.position, m_Transform.TransformDirection(Vector3.forward) * 1000, Color.white);
        }
    }

    private void OnDrawGizmos()
    {
        if (!m_Transform) return;

        GizmosExtend.DrawCircle(m_Transform.position, Vector3.forward, Color.red, radius);
        GizmosExtend.DrawLine(m_Transform.position, m_Transform.TransformPoint(suspension.suspensionPoint), Color.magenta);
        GizmosExtend.DrawLine(m_Transform.localPosition, new Vector3(m_Transform.localPosition.x, m_Transform.localPosition.y * wheelSuspensionDistance, m_Transform.localPosition.z), Color.yellow);
    }
}*/

public class CustomWheelSuspension : MonoBehaviour
{
    private Rigidbody vehicleRigidbody;

    //suspension
    public float suspensionDampening = 0.1f;
    public float suspensionSpringStrength = 10.0f;
    public float suspensionRestDist = 0.5f;

    //steering
    private float desiredSteering = 0.0f;
    public float steeringSpeed = 2.0f;
    [Range(0.0f, 90.0f)] public float maxSteeringAngle = 20.0f;
    public float tireMass = 1.0f;
    [Range(0.0f, 1.0f)] public float tireGripFactor = 0.5f;

    //acceleration
    public AnimationCurve powerCurve;
    public float topSpeed = 10.0f;
    public float motorTorque = 10.0f;

    public bool isMotor;
    public bool isSteering;

    private float wheelRadius;

#if DEBUG
    private Vector3 debugSuspensionForce;
    private Vector3 debugSteeringForce;
    private Vector3 debugAccelerationForce;
#endif

    private void Start()
    {
        vehicleRigidbody = transform.root.GetComponent<Rigidbody>();
        //suspensionRestDist = 0.5f; //TODO can be auto calculated

        // Calculate the wheel radius based on the mesh size
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh != null)
            {
                wheelRadius = mesh.bounds.extents.y;
            }
        }
    }

    private void FixedUpdate()
    {
        float motor = Input.GetAxis("Vertical") * motorTorque;
        float steering = Input.GetAxis("Horizontal") * maxSteeringAngle;

        RaycastHit hit;

        if (Physics.Raycast(transform.position, -transform.up, out hit, wheelRadius * 10))
        {
            if (isSteering) Steering(steering);
            GetSuspensionForce(hit);
            GetSteeringForce();
            if (isMotor) GetAccelerationForce(motor);
        }
    }

    private void Steering(float steeringInput)
    {
        // Calculate the desired rotation for steering
        desiredSteering = Mathf.Lerp(desiredSteering, steeringInput, Time.deltaTime * steeringSpeed);
        Quaternion targetRotation = Quaternion.Euler(0.0f, desiredSteering, 0.0f);
        transform.localRotation = targetRotation;
    }

    private void GetSuspensionForce(RaycastHit tireRay)
    {
        // world-space direction of the spring force.
        Vector3 springDir = transform.up;
        // world-space velocity of this tire
        Vector3 tireWorldVel = vehicleRigidbody.GetPointVelocity(transform.position);
        // calculate offset from the raycast
        float offset = suspensionRestDist - tireRay.distance;
        // calculate velocity along the spring direction
        // note that springDir is a unit vector, so this returns the magnitude of tireWorldVel // as projected onto springDir
        float springVelocity = Vector3.Dot(springDir, tireWorldVel);
        // calculate the magnitude of the dampened spring force!
        float force = (offset * suspensionSpringStrength) - (springVelocity * suspensionDampening);
        // apply the force at the location of this tire, in the direction of the suspension
        vehicleRigidbody.AddForceAtPosition(springDir * force, transform.position);
        debugSuspensionForce = springDir * force;
    }

    private void GetSteeringForce()
    {
        // world-space direction of the spring force.
        Vector3 steeringDir = transform.right;
        // world-space velocity of the suspension
        Vector3 tireWorldVel = vehicleRigidbody.GetPointVelocity(transform.position);
        // what it's the tire's velocity in the steering direction?
        // note that steeringDir is a unit vector, so this returns the magnitude of tireWorldVel // as projected onto steeringDir
        float steeringVelocity = Vector3.Dot(steeringDir, tireWorldVel);
        // gripFactor is in range 0-1, 0 means no grip, 1 means full grip
        float desiredVelocityChange = -steeringVelocity * tireGripFactor;
        // turn change in velocity into an acceleration (acceleration = change in vel / time)
        // this will produce the acceleration necessary to change the velocity by desiredVelocityChange in 1 physics step
        float desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
        // Force = Mass * Acceleration, so multiply by the mass of the tire and apply as a force!
        vehicleRigidbody.AddForceAtPosition(steeringDir * tireMass * desiredAcceleration, transform.position);
        debugSteeringForce = steeringDir * tireMass * desiredAcceleration;
    }

    private void GetAccelerationForce(float accelerationInput)
    {
        if (accelerationInput != 0.0f)
        {
            // world-space direction of the acceleration/braking force.
            Vector3 accelerationDir = transform.forward;
            // acceleration torque
            // forward speed of the car (in the direction of driving)
            float carSpeed = Vector3.Dot(transform.forward, vehicleRigidbody.velocity);
            // normalized car speed
            float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);
            // available torque
            float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelerationInput;

            vehicleRigidbody.AddForceAtPosition(accelerationDir * availableTorque, transform.position);
            debugAccelerationForce = accelerationDir * availableTorque;
        }
    }

    private void OnDrawGizmos()
    {
        GizmosExtend.DrawArrow(transform.position, debugSuspensionForce, Color.green);
        GizmosExtend.DrawArrow(transform.position, debugSteeringForce, Color.red);
        GizmosExtend.DrawArrow(transform.position, debugAccelerationForce, Color.blue);

        RaycastHit hit;

        if (Physics.Raycast(transform.position, -transform.up, out hit, wheelRadius * 10))
        {
            GizmosExtend.DrawLine(transform.position, transform.position + (-transform.up * wheelRadius * 10), Color.red);
        }
        else
        {
            GizmosExtend.DrawLine(transform.position, transform.position + (-transform.up * wheelRadius * 10), Color.magenta);
        }
    }
}
