using UnityEngine;

public class Module : MonoBehaviour
{
    private protected Rigidbody m_VehicleRigidbody;
    private protected Transform m_Transform;
    private protected Vehicle m_Vehicle;

    public float hp;
    public float mass;

    private protected virtual void Start()
    {
        m_VehicleRigidbody = transform.root.GetComponent<Rigidbody>();
        m_Vehicle = transform.root.GetComponent<Vehicle>();
        m_Transform = transform;
    }

    private protected virtual void Update()
    {        
        CheckHP();
    }

    private protected virtual void OnCollisionEnter(Collision collision)
    {
        hp -= collision.relativeVelocity.magnitude;
        CheckHP();
    }

    private protected void CheckHP()
    {
        if (hp <= 0.0f)
        {
            m_Vehicle.RemoveModule(this);
            Destroy(gameObject);
        }
    }
}
