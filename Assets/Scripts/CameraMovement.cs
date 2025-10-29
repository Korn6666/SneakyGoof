using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    private Vector3 offset;
    private Vector3 targetPosition;
    [SerializeField] private float speedLerp = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = transform.position - target.position;
    }

    // Update is called once per frame
    void Update()
    {
        targetPosition = new Vector3(target.position.x + offset.x, offset.y, target.position.z + offset.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speedLerp);
    }
}
