using GizmoExtension;
using UnityEngine;

public class Gyroscope : Module
{
    [SerializeField] private PIDController controller = new PIDController(1f, 1f, 1f, 1f, true, -1f, 1f);
    public float power = 10;

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
        Align();
    }

    private void Align()
    {
        Vector3 targetDir = m_VehicleRigidbody.rotation * transform.forward;
        Vector3 forwardDir = m_VehicleRigidbody.rotation * transform.forward;

        bool turnRight = m_Vehicle.rotationInput < 0f;
        bool turnLeft = m_Vehicle.rotationInput > 0f;

        if (turnRight) targetDir = m_VehicleRigidbody.rotation * transform.right;
        if (turnLeft) targetDir = m_VehicleRigidbody.rotation * -transform.right;

        float currentAngle = Vector3.SignedAngle(transform.forward, forwardDir, Vector3.up);
        float targetAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);

        float input = controller.Update(Time.fixedDeltaTime, currentAngle, targetAngle);
        m_VehicleRigidbody.AddTorque(0f, input * power, 0f, ForceMode.Force);
    }

    private void OnDrawGizmos()
    {
        if (m_VehicleRigidbody)
        {
            GizmosExtend.DrawArrow(transform.position, m_VehicleRigidbody.rotation * transform.forward, Color.green * 0.5f);
            GizmosExtend.DrawArrow(transform.position, m_VehicleRigidbody.rotation * transform.forward, Color.yellow * 0.5f);
        }
    }
}
