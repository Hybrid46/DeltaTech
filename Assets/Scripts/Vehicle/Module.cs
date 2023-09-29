using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Module : MonoBehaviour
{
    private protected Rigidbody vehicleRigidbody;
    private protected Transform m_Transform;
    private protected Vehicle m_Vehicle;

    public enum ModuleType { General, Drive, Turret, Ballast, Effector }
    public float hp;
    public float mass;

    [Range(-1.0f, 1.0f)]
    public float buoyancy;//should be multiplied by mass?

    void Start()
    {
        vehicleRigidbody = transform.root.GetComponent<Rigidbody>();
        m_Transform = transform;
    }

    void Update()
    {        
        CheckHP();
    }

    private void FixedUpdate()
    {
        ApplyForces();
    }

    private protected void OnCollisionEnter(Collision collision)
    {
        hp -= collision.relativeVelocity.magnitude * mass;
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

    private protected void ApplyForces()
    {

    }
}
