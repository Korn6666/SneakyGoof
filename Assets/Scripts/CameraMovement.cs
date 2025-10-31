using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform eye;
    private Vector3 offset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Quaternion targetRotationY;
    private float distanceBehindPlayer;
    [SerializeField] private float speedLerp = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        offset = transform.position - player.position;
        distanceBehindPlayer = offset.magnitude;
    }

    // Update is called once per frame
    void Update()
    {
        // Direction de l’œil vers le joueur
        Vector3 direction = (eye.position - player.position).normalized;

        // Position cible : derrière le joueur, dans la direction opposée à l’œil
        Vector3 targetPosition = player.position - direction * distanceBehindPlayer;

        // On peut ajouter un petit offset vertical
        targetPosition += new Vector3(0, offset.y, 0);

        // La caméra regarde l’œil
        Quaternion targetRotation = Quaternion.LookRotation(eye.position - targetPosition);

        // Mouvement fluide
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speedLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speedLerp);
    }
}