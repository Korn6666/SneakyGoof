using System;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform eye;
    private Vector3 offset;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float distanceBehindPlayer;
    [SerializeField] private float speedLerp = 5f;
    [SerializeField] private float limitDist = 3;

    // Section Camera moves by player
    private PlayerInputActions input;
    private Vector2 inputVector;
    private Vector3 inputDir;
    private float deadzone = 0.1f;
    private bool eyeCameraDir = true;

    void Start()
    {
        input = player.GetComponent<MovementController>().input;
        input.Player.CameraMove.performed += ctx => inputVector = ctx.ReadValue<Vector2>();
        input.Player.CameraMove.canceled += ctx => inputVector = Vector2.zero;
        input.Player.CameraMode.performed += ctx => eyeCameraDir = !eyeCameraDir;
        offset = transform.position - player.position;
        distanceBehindPlayer = offset.magnitude;
    }
    void Update()
    {
        float distance = Vector3.Distance(eye.position, player.position);
        if (distance < limitDist)
        {
            return;
        }

        Vector3 direction;
        if (eyeCameraDir)
        {
            direction = (eye.position - player.position).normalized;
        }
        else
        {
            direction = player.forward;
        }

        if (inputVector.sqrMagnitude > deadzone * deadzone)
        {
            inputDir = new Vector3(inputVector.x, 0, inputVector.y);
        }
        else
        {
            inputDir = Vector3.zero;
        }
        
        float angleY = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;

        Quaternion horizontalRot = Quaternion.Euler(0f, angleY, 0f);
        direction = horizontalRot * direction;

        Vector3 targetPosition = player.position - direction * distanceBehindPlayer;

        targetPosition.y = transform.position.y;
        
        if (inputVector.sqrMagnitude > deadzone * deadzone)
        {
            Vector3 lookDirection = player.position - targetPosition;
            lookDirection.y = direction.y;
            targetRotation = Quaternion.LookRotation(lookDirection);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(direction);
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * speedLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * speedLerp);
    }
}