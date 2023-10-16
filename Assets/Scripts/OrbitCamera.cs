using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2f, -5f);
    public float followSpeed = 5.0f;
    public float rotationSpeed = 5.0f;
    public float minVerticalAngle = 1f;
    public float maxVerticalAngle = 70f;
    public float minDistance = 2.0f;
    public float maxDistance = 10.0f;
    public float zoomSpeed = 5.0f;

    private float currentDistance;
    private float currentVerticalAngle;
    private float currentHorizontalAngle;

    private bool isRightMouseButtonHeld;

    //private Vector3 lastPosition;

    private void Start()
    {
        //if (!IsOwner) return;

        currentDistance = offset.magnitude;
        currentVerticalAngle = Mathf.Asin(offset.normalized.y) * Mathf.Rad2Deg;
        currentHorizontalAngle = Mathf.Atan2(offset.x, -offset.z) * Mathf.Rad2Deg;
        //lastPosition = transform.position;
    }

    private void Update()
    {
        //if (!IsOwner) return;

        isRightMouseButtonHeld = Input.GetMouseButton(1);
    }

    private void LateUpdate()
    {
        //if (!IsOwner) return;

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        float horizontalInput = 0;
        float verticalInput = 0;

        if (isRightMouseButtonHeld)
        {
            horizontalInput = Input.GetAxis("Mouse X");
            verticalInput = Input.GetAxis("Mouse Y");
        }

        currentDistance = Mathf.Clamp(currentDistance - scrollInput * zoomSpeed, minDistance, maxDistance);

        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle - verticalInput * rotationSpeed, minVerticalAngle, maxVerticalAngle);
        currentHorizontalAngle += horizontalInput * rotationSpeed;

        float verticalRadians = currentVerticalAngle * Mathf.Deg2Rad;
        float horizontalRadians = currentHorizontalAngle * Mathf.Deg2Rad;

        Vector3 desiredPosition = target.position + new Vector3(currentDistance * Mathf.Sin(verticalRadians) * Mathf.Cos(horizontalRadians),
                                                                currentDistance * Mathf.Cos(verticalRadians),
                                                                -currentDistance * Mathf.Sin(verticalRadians) * Mathf.Sin(horizontalRadians));

        Quaternion desiredRotation = Quaternion.LookRotation(target.position - desiredPosition, Vector3.up);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

        /*
        float positionDifference = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        
        //Vector3 ForwardPosition = target.position + target.TransformDirection(offset);
        //transform.position = Vector3.Lerp(transform.position, ForwardPosition, positionDifference * Time.deltaTime);
        
        Quaternion ForwardRotation = Quaternion.LookRotation(target.position - transform.forward * 10, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, ForwardRotation, positionDifference * Time.deltaTime);
        */
    }
}
