using GizmoExtension;
using UnityEngine;

public class Gyroscope : Module
{
    [SerializeField] private PIDController controller = new PIDController(1f, 1f, 1f, 1f, true, -1f, 1f);
    public float forwardAngle;
    public float upAngle;
    public float rightAngle;
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
        Vector3 targetPosition = m_VehicleRigidbody.position + Vector3.forward;
        targetPosition.y = m_VehicleRigidbody.position.y;    //ignore difference in Y
        Align(targetPosition);
    }

    private void Align(Vector3 targetPosition)
    {
        Vector3 targetDir = (targetPosition - m_VehicleRigidbody.position).normalized;
        Vector3 forwardDir = m_VehicleRigidbody.rotation * Vector3.forward;

        float currentAngle = Vector3.SignedAngle(Vector3.forward, forwardDir, Vector3.up);
        float targetAngle = Vector3.SignedAngle(Vector3.forward, targetDir, Vector3.up);

        float input = controller.Update(Time.fixedDeltaTime, currentAngle, targetAngle);
        m_VehicleRigidbody.AddTorque(new Vector3(0, input * power, 0));
    }

    private void OnDrawGizmos()
    {
        if (m_VehicleRigidbody)
        {
            GizmosExtend.DrawArrow(transform.position, m_VehicleRigidbody.rotation * Vector3.forward, Color.yellow * 0.5f);
        }
    }
}
