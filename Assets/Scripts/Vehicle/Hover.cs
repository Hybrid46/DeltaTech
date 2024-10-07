#define DEBUG
using GizmoExtension;
using UnityEngine;

[DisallowMultipleComponent]
public class HoverModule : Module
{
    public Transform movableChildTransform;
    //suspension
    [Range(0f, 0.999f)] public float suspensionDampening = 0.1f;
    public float springStrength = 10.0f;
    public float springRestDist = 0.5f;

    public float rotationSpeed = 100.0f;
    public float maxVerticalAngle = 45.0f;
    public float maxHorizontalAngle = 45.0f;

    [SerializeField] private float currentVerticalAngle = 0.0f;
    [SerializeField] private float currentHorizontalAngle = 0.0f;

    [SerializeField] public float acceleration;
    [SerializeField] public float steering;

    public bool isMotor;
    public bool isSteering;

#if DEBUG
    private Vector3 debugSuspensionForce;
    private Vector3 debugSteeringForce;
    private Vector3 debugAccelerationForce;
#endif

    public override void Start()
    {
        base.Start();
    }

    private void FixedUpdate()
    {
        acceleration = m_Vehicle.isBreaking ? acceleration : m_Vehicle.verticalInput;
        steering = isSteering ? m_Vehicle.horizontalInput : 0f;

        Spin(steering, acceleration);
        RaycastHit hit;

        if (Physics.Raycast(movableChildTransform.position, -movableChildTransform.up, out hit, springRestDist))
        {
            if (isMotor) AddSuspensionForce(hit);
        }
    }

    private void Spin(float horizontalInput, float verticalInput)
    {
        if (acceleration == 0f && steering == 0f)
        {
            currentHorizontalAngle = Mathf.Lerp(currentHorizontalAngle, 0f, rotationSpeed * Time.fixedDeltaTime);
            currentVerticalAngle = Mathf.Lerp(currentVerticalAngle, 0f, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Calculate rotation deltas
            float horizontalRotation = horizontalInput * rotationSpeed * Time.fixedDeltaTime;
            float verticalRotation = verticalInput * rotationSpeed * Time.fixedDeltaTime;

            // Update current angles with clamping
            currentHorizontalAngle = Mathf.Clamp(currentHorizontalAngle + horizontalRotation, -maxHorizontalAngle, maxHorizontalAngle);
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle - verticalRotation, -maxVerticalAngle, maxVerticalAngle); // Inverted for natural pitch behavior
        }

        // Apply rotation to the object
        movableChildTransform.localRotation = Quaternion.Euler(currentVerticalAngle, 0, currentHorizontalAngle);
    }

    private void AddSuspensionForce(RaycastHit hitInfo)
    {
        // World-space direction of the spring force
        Vector3 springDir = movableChildTransform.up;

        // World-space velocity of this tire
        Vector3 worldVelocity = m_VehicleRigidbody.GetPointVelocity(movableChildTransform.position);

        // Calculate offset from the raycast (displacement from rest position)
        float offset = springRestDist - hitInfo.distance;

        // Calculate velocity along the spring direction
        float springVelocity = Vector3.Dot(springDir, worldVelocity);

        // Calculate spring force using Hooke's Law (F = -kx)
        float springForce = offset * springStrength;

        // Calculate damping force (F = -bv)
        float dampingForce = springVelocity * suspensionDampening;

        // Combine the forces to get the total force
        float totalForce = springForce - dampingForce;

        // Apply the force to the rigidbody
        m_VehicleRigidbody.AddForceAtPosition(springDir * totalForce, movableChildTransform.position);

        // For debugging purposes
        debugSuspensionForce = springDir * totalForce;
    }

    private void OnDrawGizmos()
    {
        GizmosExtend.DrawArrow(transform.position, debugSuspensionForce, Color.green);
        GizmosExtend.DrawArrow(transform.position, debugSteeringForce, Color.red);
        GizmosExtend.DrawArrow(transform.position, debugAccelerationForce, Color.blue);

        RaycastHit hit;

        if (Physics.Raycast(transform.position, -transform.up, out hit, 1000f))
        {
            GizmosExtend.DrawLine(transform.position, transform.position + (-transform.up * 1f), Color.yellow);
        }
        else
        {
            GizmosExtend.DrawLine(transform.position, transform.position + (-transform.up * 1f), Color.magenta);
        }
    }
}
