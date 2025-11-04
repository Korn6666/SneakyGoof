using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private float maxNoiseOnBar;
    [SerializeField] float noiseFirstThereshold;
    [SerializeField] float noiseSecondThereshold;
    [SerializeField] float noiseSpeedDecrease;
    [SerializeField] private Vector2 targetRandomRangeOnSecondStage = new Vector2(-90, 90);
    private float current_noiseLevel = 0f;
    public static Action<Vector3, float> OnNoiseEmitted; // Vector3: position of the noise, float: intensity of the noises
    void OnEnable() => OnNoiseEmitted += OnNoiseHeard;
    void OnDisable() => OnNoiseEmitted -= OnNoiseHeard;
    private bool firstStage = true;
    private bool secondStage = false;
    private bool thirdStage = false;
    [Header("Noise Bar UI")]
    [SerializeField] private Slider noiseBarSlider;
    [SerializeField] private Image FillArea;
    [SerializeField] private Color firstStageColor;
    [SerializeField] private Color secondStageColor;
    [SerializeField] private Color thirdStageColor;
    void OnNoiseHeard(Vector3 sourcePosition, float intensity)
    {
        current_noiseLevel += intensity;
        if (current_noiseLevel < noiseFirstThereshold)
        {
        }
        else if (current_noiseLevel < noiseSecondThereshold)
        {
            firstStage = false;
            secondStage = true;
            // Debug.Log("Eye heard noise at position: " + sourcePosition + " with intensity: " + intensity);
            lastKnownPlayerPosition = sourcePosition;
            timerEyePosition = -1; // Interrupt wait time to react immediately
        }
        else if (secondStage) // Frame when crossing the second threshold
        {
            secondStage = false;
            thirdStage = true;
            EyeOnPlayer();
        }
        else // Above second threshold
        {
            // Debug.Log("Lose");
        }
        noiseBarSlider.value = current_noiseLevel;
    }

    private void NoiseColorBarUpdate()
    {
        if (current_noiseLevel < noiseFirstThereshold)
        {
            firstStage = true;
            secondStage = false;
            thirdStage = false;
            FillArea.color = firstStageColor;
        }else if (current_noiseLevel < noiseSecondThereshold)
        {
            firstStage = false;
            secondStage = true;
            thirdStage = false;
            FillArea.color = secondStageColor;
        }
        else
        {
            firstStage = false;
            secondStage = false;
            thirdStage = true;
            FillArea.color = thirdStageColor;
        }
    }

    void Update()
    {
        EyeCloseAndOpenBehaviour();
        if (current_noiseLevel > 0)
        {
            current_noiseLevel -= noiseSpeedDecrease * Time.deltaTime;
            noiseBarSlider.value = current_noiseLevel;
        }
        NoiseColorBarUpdate();

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
            float angleToTarget = UnityEngine.Random.Range(targetRandomRangeOnSecondStage.x, targetRandomRangeOnSecondStage.y);
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

    private void EyeOnPlayer()
    {
        targetDirection = (lastKnownPlayerPosition - transform.position).normalized;
        targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = targetRotation;
        isTargetDefined = true;
        OpenTheEye();
        
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
