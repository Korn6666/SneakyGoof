using System;
using UnityEngine;

public class Eye_Behaviour : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform pupil;
    [SerializeField] private GameObject eyeOpenVisual;
    [SerializeField] private float detectionAngleRange = 90f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float timeBetweenDirectionChange = 5f;
    [SerializeField] private Vector2 eyeOpenDurationRange = new Vector2(8, 12);
    [SerializeField] private Vector2 eyeClosedDurationRange = new Vector2(5, 10);
    private Vector3 targetDirection;
    private Quaternion targetRotation;
    private Vector3 lastKnownPlayerPosition;
    private float timerEyePosition = 0;
    private bool isTargetDefined = false;
    private bool eyeOpened = true;
    private float timerEyeOpened = 0f;
    private float timerEyeClosed = 0f;

    //Noise Behaviour
    [Header("Noise Behaviour")]
    public float noiseFirstThereshold;
    [SerializeField] private Vector2 targetRandomRangeAboveFirstThreshold = new Vector2(-90, 90);
    public float noiseSecondThereshold;
    public float noiseSpeedDecrease;
    private float current_noiseLevel = 0f;
    public static Action<Vector3, float> OnNoiseEmitted; // Vector3: position of the noise, float: intensity of the noises
    void OnEnable() => OnNoiseEmitted += OnNoiseHeard;
    void OnDisable() => OnNoiseEmitted -= OnNoiseHeard;
    private bool firstStage = true;
    private bool secondStage = false;
    private bool thirdStage = false;

    void OnNoiseHeard(Vector3 sourcePosition, float intensity)
    {
        current_noiseLevel += intensity;
        if (current_noiseLevel >= noiseFirstThereshold)
        {
            firstStage = false;
            secondStage = true; 
            Debug.Log("Eye heard noise at position: " + sourcePosition + " with intensity: " + intensity);
            lastKnownPlayerPosition = sourcePosition;
            timerEyePosition = -1; // Interrupt wait time to react immediately
        }else if (current_noiseLevel >= noiseSecondThereshold && secondStage)
        {
            secondStage = false;
            thirdStage = true;
            OpenTheEye();
            Debug.Log("Eye heard loud noise at position: " + sourcePosition + " with intensity: " + intensity);
            // Implement behavior when loud noise is heard
        }else if (current_noiseLevel > noiseSecondThereshold && thirdStage)
        {
            // Further behavior for very loud noises can be implemented here
            Debug.Log("Lose");
        }
    }

    void Update()
    {
        EyeCloseAndOpenBehaviour();
        current_noiseLevel -= noiseSpeedDecrease * Time.deltaTime;
        current_noiseLevel = Mathf.Max(0, current_noiseLevel); // Ensure noise level doesn't go below 0

        if (secondStage || (firstStage && eyeOpened))
        {
            RandomRotation();
        }
        
        if (eyeOpened)
        {
            PlayerDetection();
        }
    }
    private void PlayerDetection()
    {
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(pupil.transform.forward, directionToPlayer);
        if (angleToPlayer <= detectionAngleRange / 2f) // If player is within detection angle
        {
            if (Physics.Raycast(transform.position + directionToPlayer, directionToPlayer, out RaycastHit hit)) // Raycast to check for line of sight
            {
                Debug.DrawRay(transform.position + directionToPlayer, directionToPlayer * hit.distance, Color.blue);
                if (hit.transform.CompareTag("Player")) // If the raycast hits the player
                {
                    // Implement behavior when player is detected
                }
            }
        }
    }
    private void RandomRotation()
    {
        if (!isTargetDefined && timerEyePosition <= 0 && firstStage)
        {
            isTargetDefined = true;
            targetDirection = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) * Vector3.forward;
            targetRotation = Quaternion.LookRotation(targetDirection);
        }
        else if (!isTargetDefined && timerEyePosition <= 0 && secondStage)
        {
            targetDirection = (lastKnownPlayerPosition - transform.position).normalized;
            float angleToTarget = UnityEngine.Random.Range(targetRandomRangeAboveFirstThreshold.x, targetRandomRangeAboveFirstThreshold.y);
            targetDirection = Quaternion.Euler(0, angleToTarget, 0) * targetDirection;
            targetRotation = Quaternion.LookRotation(targetDirection);
            isTargetDefined = true;
        }
        else if (timerEyePosition > 0)
        {
            timerEyePosition -= Time.deltaTime;
        }

        if (Quaternion.Angle(transform.rotation, targetRotation) < 1f && isTargetDefined)
        {
            if (timerEyePosition <= 0)
            {
                timerEyePosition = timeBetweenDirectionChange;
            }
            isTargetDefined = false;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void EyeCloseAndOpenBehaviour()
    {
        if (eyeOpened)
        {
            if (timerEyeOpened > 0f)
            {
                timerEyeOpened -= Time.deltaTime;
            }
            else
            {
                CloseTheEye();
            }
        }
        else
        {
            if (timerEyeClosed > 0f)
            {
                timerEyeClosed -= Time.deltaTime;
            }
            else
            {
                OpenTheEye();
            }
        }
        
    }
    private void OpenTheEye()
    {
        eyeOpened = true;
        timerEyeOpened = UnityEngine.Random.Range(eyeOpenDurationRange.x, eyeOpenDurationRange.y);
        eyeOpenVisual.SetActive(true);
    }
    
    private void CloseTheEye()
    {
        eyeOpened = false;
        timerEyeClosed = UnityEngine.Random.Range(eyeClosedDurationRange.x, eyeClosedDurationRange.y);
        eyeOpenVisual.SetActive(false);
    }
}
