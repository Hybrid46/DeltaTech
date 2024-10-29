using UnityEngine;

public class Turret : Controller {
    [SerializeField]
    PIDController controller;
    [SerializeField]
    float power;
    [SerializeField]
    Transform target;

    new Rigidbody rigidbody;

    public override float Power {
        get {
            return power;
        }
        set {
            power = value;
        }
    }

    void Start() {
        rigidbody = GetComponent<Rigidbody>();
        controller = new PIDController(true);
    }

    public override PIDController GetController() {
        return controller;
    }

    public override void SetTarget(int index) {
    }

    void FixedUpdate() {
        Vector3 targetPosition = target.position;
        targetPosition.y = rigidbody.position.y;    //ignore difference in Y
        Vector3 targetDir = (targetPosition - rigidbody.position).normalized;
        Vector3 forwardDir = rigidbody.rotation * Vector3.forward;

        float currentAngle = Vector3.SignedAngle(Vector3.forward, forwardDir, Vector3.up);
        float targetAngle = Vector3.SignedAngle(Vector3.forward, targetDir, Vector3.up);

        float input = controller.Update(Time.fixedDeltaTime, currentAngle, targetAngle);
        rigidbody.AddTorque(new Vector3(0, input * power, 0));
    }
}
