using GizmoExtension;
using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CustomWheelSuspension : Module
{
    private WheelCollider m_WheelCollider;
    public Transform m_WheelTransform;
    public MeshFilter m_WheelMeshFilter;

    [Header("Steering")]
    public bool isSteering;
    public float steeringSpeed = 10.0f;
    [Range(0.0f, 90.0f)] public float maxSteeringAngle = 25.0f;

    [Header("Motor")]
    public bool isMotor;
    public float topSpeed = 20.0f;
    public float kilowatts = 50.0f;

    [Header("Break")]
    public bool hasBreak;
    public float breakRestTorque = 10.0f;
    public float breakTorque = 50.0f;

    [SerializeField] private float wheelRadius;
    [SerializeField] private float wheelWidth;

    [SerializeField] private float acceleration;
    [SerializeField] private float steering;
    [SerializeField] private bool breaking;

    [SerializeField] private float forwardSpeed;
    [SerializeField] private float speed;

    [SerializeField] private float actualSteering = 0.0f;
    [SerializeField] private float actualMotorTorque = 0.0f;
    [SerializeField] private float actualBreakTorque = 0.0f;

    private protected override void Start()
    {
        base.Start();
        m_WheelCollider = GetComponent<WheelCollider>();

        wheelRadius = m_WheelMeshFilter.sharedMesh.bounds.extents.y;
        wheelWidth = m_WheelMeshFilter.sharedMesh.bounds.extents.x;

        m_WheelCollider.radius = wheelRadius;
        m_WheelCollider.mass = StaticUtils.CalcCylinderVolume(wheelWidth, wheelRadius) * 1000;
    }

    private protected override void Update()
    {
        base.Update();
    }

    private protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    private void FixedUpdate()
    {
        acceleration = Input.GetAxis("Vertical");
        steering = Input.GetAxis("Horizontal") * maxSteeringAngle;
        breaking = Input.GetKey(KeyCode.Space);

        if (isSteering) Steering();
        if (isMotor) Acceleration();
        if (hasBreak) Break();
        UpdateWheel();

        speed = m_VehicleRigidbody.velocity.magnitude;
        forwardSpeed = Vector3.Project(m_VehicleRigidbody.velocity, m_VehicleRigidbody.transform.forward).magnitude;
    }

    private void UpdateWheel()
    {
        Quaternion wheelRotation;
        Vector3 wheelPosition;
        m_WheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);
        m_WheelTransform.position = wheelPosition;
        m_WheelTransform.rotation = wheelRotation;
    }

    private void Steering()
    {
        actualSteering = Mathf.Lerp(actualSteering, steering, Time.deltaTime * steeringSpeed);
        m_WheelCollider.steerAngle = actualSteering;
    }

    private void Acceleration()
    {
        actualMotorTorque = acceleration * StaticUtils.GetElectricMotorTorque(kilowatts, speed);
        m_WheelCollider.motorTorque = actualMotorTorque;
    }

    private void Break()
    {
        actualBreakTorque = 0.0f;
        actualBreakTorque += (acceleration == 0.0f) ? breakRestTorque : 0.0f;
        actualBreakTorque += breaking ? breakTorque : 0.0f;
        m_WheelCollider.brakeTorque = actualBreakTorque;
    }

    private void OnDrawGizmos()
    {
        GizmosExtend.DrawArrow(transform.position, transform.forward * actualMotorTorque, Color.blue);
        GizmosExtend.DrawArrow(transform.position, transform.forward * actualBreakTorque, Color.yellow);
    }
}
