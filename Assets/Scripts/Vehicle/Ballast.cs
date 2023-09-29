using UnityEngine;

public class Ballast : Module
{
    public AnimationCurve powerCurve;
    public float topSpeed = 10.0f;
    public float motorTorque = 10.0f;

    void Update()
    {
        if (m_Vehicle.horizontalInput != 0.0f) BuoyancyForce(m_Vehicle.horizontalInput);
    }

    private void BuoyancyForce(float accelerationInput)
    {
        Vector3 accelerationDir = Vector3.up;
        float speed = Vector3.Dot(m_Transform.up, vehicleRigidbody.velocity);
        float normalizedSpeed = Mathf.Clamp01(Mathf.Abs(speed) / topSpeed);
        float availableTorque = powerCurve.Evaluate(normalizedSpeed) * accelerationInput * motorTorque;

        Vector3 force = accelerationDir * availableTorque;
        vehicleRigidbody.AddForceAtPosition(force, m_Transform.position);
    }
}
