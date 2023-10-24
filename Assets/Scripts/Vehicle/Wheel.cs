#define DEBUG
using GizmoExtension;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CustomWheelSuspension : Module
{
    private Rigidbody vehicleRigidbody;
    private WheelCollider wheelCollider;

    public Transform movableChildTransform;

    //steering
    private float desiredSteering = 0.0f;
    public float steeringSpeed = 2.0f;
    [Range(0.0f, 90.0f)] public float maxSteeringAngle = 20.0f;

    //acceleration
    public AnimationCurve powerCurve;
    public float topSpeed = 20.0f;
    public float motorTorque = 100.0f;

    public bool isMotor;
    public bool isSteering;

    [SerializeField] private float wheelRadius;
    [SerializeField] private float wheelWidth;

#if DEBUG
    private Vector3 debugAccelerationForce;
#endif

    private void Start()
    {
        vehicleRigidbody = transform.root.GetComponent<Rigidbody>();
        wheelCollider = GetComponent<WheelCollider>();
        //suspensionRestDist = 0.5f; //TODO can be auto calculated

        // Calculate the wheel radius based on the mesh size
        MeshFilter meshFilter = movableChildTransform.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh != null)
            {
                wheelRadius = mesh.bounds.extents.y;
                wheelWidth = mesh.bounds.extents.x;
            }
        }
    }

    private void FixedUpdate()
    {
        float motor = Input.GetAxis("Vertical") * motorTorque;
        float steering = Input.GetAxis("Horizontal") * maxSteeringAngle;

        if (isSteering) Steering(steering);
        if (isMotor) GetAccelerationForce(motor);
        UpdateWheel();
    }

    private void UpdateWheel()
    {
        Quaternion quat;
        Vector3 position;
        wheelCollider.GetWorldPose(out position, out quat);
        movableChildTransform.position = position;
        movableChildTransform.rotation = quat;
    }

    private void Steering(float steeringInput)
    {
        // Calculate the desired rotation for steering
        desiredSteering = Mathf.Lerp(desiredSteering, steeringInput, Time.deltaTime * steeringSpeed);
        wheelCollider.steerAngle = desiredSteering;
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
        GizmosExtend.DrawArrow(transform.position, debugAccelerationForce, Color.blue);
    }
}
