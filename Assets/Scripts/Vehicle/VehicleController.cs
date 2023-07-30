using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public float motorForce = 1000f;
    public float maxSteerAngle = 30f;
    public CustomWheelSuspension[] wheelSuspensions;
    public Rigidbody vehicleRigidbody { get; private set; }

    private void Start()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();
    }

    /*
     //TODO central controller for wheels
    private void FixedUpdate()
    {
        float motor = Input.GetAxis("Vertical") * motorForce;
        float steering = Input.GetAxis("Horizontal") * maxSteerAngle;

        foreach (CustomWheelSuspension wheelSuspension in wheelSuspensions)
        {
            
        }
    }*/
}
