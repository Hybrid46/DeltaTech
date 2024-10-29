using UnityEngine;

public class Propeller : Module
{
    public AnimationCurve powerCurve;
    public float topSpeed = 10.0f;
    public float motorTorque = 10.0f;

    public override void Update()
    {
        base.Update();

        if (m_Vehicle.verticalInput != 0.0f) AccelerationForce(m_Vehicle.verticalInput);
    }

    private void AccelerationForce(float accelerationInput)
    {
        Vector3 accelerationDir = m_Transform.forward;
        float carSpeed = Vector3.Dot(m_Transform.forward, m_VehicleRigidbody.linearVelocity);
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(carSpeed) / topSpeed);
        float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelerationInput * motorTorque;

        Vector3 force = accelerationDir * availableTorque;
        m_VehicleRigidbody.AddForceAtPosition(force, m_Transform.position);
    }
}
