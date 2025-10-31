using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class MovementController : MonoBehaviour
{
    [SerializeField] private Transform LeftLegIKTarget;
    private Vector3 LeftLegIKTargetOriginePos;
    [SerializeField] private Transform RightLegIKTarget;
    private Vector3 RightLegIKTargetOriginePos;
    private float bodyYTarget = 0;
    [SerializeField] private AnimationCurve heightFootCurve;
    [SerializeField] private AnimationCurve speedStepCurve;
    // Distance in meters
    [SerializeField] private float maxStepLength = 1;
    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField] private float maxStepSpeed = 1;
    [SerializeField] private float minStepSpeed = 0.1f;
    private float stepSpeed = 1;
    [SerializeField] private float rotationSpeed = 180f; // deg/s, configuré dans l'inspector
    private RaycastHit hit;

    private PlayerInputActions input;
    [SerializeField] private float YBodyMoveFactor = 1;
    [SerializeField] private float bodyLerpBtwLegs = 0.3f;

    // Pied Gauche
    private bool leftPressed;
    private bool leftJustPressed;
    private bool leftJustReleased;
    private bool leftON; // left all pressed
    private bool leftOnGround = true;
    private bool leftLegMovingToOriginalPos;
    private float currentLeftStepLength;
    private float currentMaxLeftStepLength;
    private float currentLeftStepHeight = 0;

    // Pied Droite
    private bool rightPressed;
    private bool rightJustPressed;
    private bool rightJustReleased;
    private bool rightON; // right all pressed
    private bool rightOnGround = true;
    private bool rightLegMovingToOriginalPos;
    private float currentRightStepLength;
    private float currentMaxRightStepLength;
    private float currentRightStepHeight = 0;

    // Rotation
    private bool rightRotationInput = false;
    private bool leftRotationInput = false;

    [Header("Speed 20m Measurement")]
    [SerializeField] private float measurementDistance = 20f;
    [SerializeField] private bool debugSpeedMeasurement = true;
    private Vector3 lastPosForMeasurement;
    private float accumulatedDistanceForMeasurement = 0f;
    private float measurementStartTime = 0f;
    private Vector3 leftDesiredWorldPos;
    private Vector3 rightDesiredWorldPos;

    void Awake()
    {
        var keys = speedStepCurve.keys;
        keys[speedStepCurve.keys.Length - 1].value = minStepSpeed / maxStepSpeed; // ensure min speed is respected
        speedStepCurve.keys = keys;
        currentMaxLeftStepLength = maxStepLength;
        currentMaxRightStepLength = maxStepLength;
        input = new PlayerInputActions();
        if (Physics.Raycast(LeftLegIKTarget.transform.position, Vector3.down, out hit))
            {
                LeftLegIKTarget.transform.position = hit.point + Vector3.up * 0.1f;
                LeftLegIKTargetOriginePos = LeftLegIKTarget.transform.position;
            }
        if (Physics.Raycast(RightLegIKTarget.transform.position, Vector3.down, out hit))
        {
            RightLegIKTarget.transform.position = hit.point + Vector3.up * 0.1f;
            RightLegIKTargetOriginePos =  RightLegIKTarget.transform.position;
        }

        // initialiser les desired world positions à l'état actuel
        leftDesiredWorldPos = LeftLegIKTarget.position;
        rightDesiredWorldPos = RightLegIKTarget.position;

        // Init measurement for 20m speed
        lastPosForMeasurement = transform.position;
        accumulatedDistanceForMeasurement = 0f;
        measurementStartTime = Time.time;
    }
    
    void OnEnable()
    {
        input.Player.Enable();

        // Gauche
        input.Player.LeftStep.started += ctx =>
        {
            leftJustPressed = true;
            leftPressed = true;
            leftON = true;
        };
        input.Player.LeftStep.canceled += ctx =>
        {
            leftPressed = false;
            leftJustReleased = true;
            leftON = false;
        };

        // Droite
        input.Player.RightStep.started += ctx =>
        {
            rightJustPressed = true;
            rightPressed = true;
            rightON = true;
        };
        input.Player.RightStep.canceled += ctx =>
        {
            rightPressed = false;
            rightJustReleased = true;
            rightON = false;
        };

        input.Player.Right.started += ctx =>
        {
            rightRotationInput = true;
        };
        input.Player.Right.canceled += ctx => rightRotationInput = false; 
        input.Player.Left.started += ctx => leftRotationInput = true;
        input.Player.Left.canceled += ctx => leftRotationInput = false;
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Update()
    {
        // Rotation
        if (rightRotationInput || leftRotationInput)
        {
            float turnInput = 0;
            if (rightRotationInput) turnInput += 1;
            if (leftRotationInput) turnInput -= 1;

            // only rotate if one foot is on ground
            if (!leftOnGround && !rightOnGround) return;
            // choose pivot: prefer the foot that is on ground while the other is not
            Vector3 pivot = new Vector3();
            if (leftOnGround && !rightOnGround) pivot = LeftLegIKTarget.position;
            else if (rightOnGround && !leftOnGround) pivot = RightLegIKTarget.position;
            float angle = turnInput * rotationSpeed * Time.deltaTime;

            if (!leftOnGround || !rightOnGround)
            {
                // rotate the player transform around pivot (world space)
                transform.RotateAround(pivot, Vector3.up, angle);

                // also rotate the stored origin positions so the baseline (origins) follows the rotation
                LeftLegIKTargetOriginePos = Quaternion.AngleAxis(angle, Vector3.up) * (LeftLegIKTargetOriginePos - pivot) + pivot;
                RightLegIKTargetOriginePos = Quaternion.AngleAxis(angle, Vector3.up) * (RightLegIKTargetOriginePos - pivot) + pivot;
            }
        }

        if (leftLegMovingToOriginalPos || rightLegMovingToOriginalPos)
        {
            float distL = Vector3.Distance(LeftLegIKTarget.position, LeftLegIKTargetOriginePos);
            if (distL <= 0.05f)
            {
                LeftLegIKTarget.position = LeftLegIKTargetOriginePos;
                leftLegMovingToOriginalPos = false;
                leftOnGround = true;
                // currentMaxLeftStepLength = maxStepLength;
                // currentMaxRightStepLength = maxStepLength;
            }
            else
            {
                LeftLegIKTarget.position = Vector3.Lerp(LeftLegIKTarget.position, LeftLegIKTargetOriginePos, Time.deltaTime * 10);
            }
            
            float distR = Vector3.Distance(RightLegIKTarget.position, RightLegIKTargetOriginePos);
            if (distR <= 0.05f)
            {
                RightLegIKTarget.position = RightLegIKTargetOriginePos;
                rightLegMovingToOriginalPos = false;
                rightOnGround = true;
                // currentMaxLeftStepLength = maxStepLength;
                // currentMaxRightStepLength = maxStepLength;
            }
            else
            {
                RightLegIKTarget.position = Vector3.Lerp(RightLegIKTarget.position, RightLegIKTargetOriginePos, Time.deltaTime * 10);
            }
            
            MainBodyPositionUpdate();
            return;
        }

        //Left foot
        if (leftJustPressed)
        {
            leftOnGround = false;
            if (Vector3.Distance(LeftLegIKTargetOriginePos, LeftLegIKTarget.position) > 0.01f)
            {
                leftLegMovingToOriginalPos = true;
            }
            else
            {
                currentMaxLeftStepLength =  maxStepLength + RightLegIKTarget.localPosition.z - LeftLegIKTarget.localPosition.z;
                LeftLegIKTargetOriginePos = LeftLegIKTarget.position;
                if (currentMaxLeftStepLength < 0.1f)
                {
                    leftPressed = false;
                }
            }
        }
        else if (leftPressed)
        {
            leftOnGround = false;
            stepSpeed = speedStepCurve.Evaluate(currentLeftStepLength) * maxStepSpeed;
            currentLeftStepLength += Time.deltaTime * stepSpeed / currentMaxLeftStepLength;
            currentLeftStepHeight = Mathf.Clamp01(currentLeftStepHeight);
            currentLeftStepHeight = heightFootCurve.Evaluate(currentLeftStepLength);

            if (rightON || leftLegMovingToOriginalPos || currentLeftStepLength >= 1)
            {
                leftLegMovingToOriginalPos = true;
                leftPressed = false;
                return;
            }
            bodyYTarget = currentLeftStepHeight * maxStepHeight * YBodyMoveFactor; // Ajuste la hauteur du corps en fonction du pied gauche;
            LeftLegIKTarget.transform.position = LeftLegIKTargetOriginePos + currentLeftStepLength * currentMaxLeftStepLength * transform.forward + currentLeftStepHeight * maxStepHeight * transform.up;
        }
        else if (leftJustReleased)
        {
            currentLeftStepLength = 0;
            // currentMaxLeftStepLength = maxStepLength;
       
            leftLegMovingToOriginalPos = true;
            if (Physics.Raycast(LeftLegIKTarget.transform.position, Vector3.down, out hit, 10f))
            {
                LeftLegIKTargetOriginePos = hit.point + Vector3.up * 0.1f;
            }
            
        }
        else
        {
            // Lerp vers l'origine, mais snap uniquement quand la distance est suffisamment petite
            float distL = Vector3.Distance(LeftLegIKTarget.position, LeftLegIKTargetOriginePos);
            if (distL <= 0.05f)
            {
                leftOnGround = true;
                LeftLegIKTarget.position = LeftLegIKTargetOriginePos;
            }
            else
            {
                LeftLegIKTarget.position = Vector3.Lerp(LeftLegIKTarget.position, LeftLegIKTargetOriginePos, Time.deltaTime * 10);
            }
        }

        //Right foot
        if (rightJustPressed)
        {
            rightOnGround = false;
            if (Vector3.Distance(RightLegIKTargetOriginePos, RightLegIKTarget.position) > 0.01f)
            {
                rightLegMovingToOriginalPos = true;
            }
            else
            {
                currentMaxRightStepLength = maxStepLength +  LeftLegIKTarget.localPosition.z - RightLegIKTarget.localPosition.z;
                RightLegIKTargetOriginePos = RightLegIKTarget.position;
                if (currentMaxRightStepLength < 0.1f)
                {
                    rightPressed = false;
                }
            }
        }
        else if (rightPressed)
        {
            rightOnGround = false;
            stepSpeed = speedStepCurve.Evaluate(currentRightStepLength) * maxStepSpeed;
            currentRightStepLength += Time.deltaTime * stepSpeed / currentMaxRightStepLength;
            currentRightStepLength = Mathf.Clamp01(currentRightStepLength);
            currentRightStepHeight = heightFootCurve.Evaluate(currentRightStepLength);

            if (leftON || rightLegMovingToOriginalPos || currentRightStepLength >= 1)
            {
                rightLegMovingToOriginalPos = true;
                rightPressed = false;
                return;
            }
            bodyYTarget = currentRightStepHeight * maxStepHeight * YBodyMoveFactor; // Ajuste la hauteur du corps en fonction du pied droit;
            RightLegIKTarget.transform.position = RightLegIKTargetOriginePos + currentMaxRightStepLength * currentRightStepLength * transform.forward + currentRightStepHeight * maxStepHeight * transform.up ;
        }
        else if (rightJustReleased)
        {
            currentRightStepLength = 0;
            // currentMaxRightStepLength = maxStepLength;

            rightLegMovingToOriginalPos = true;
            if (Physics.Raycast(RightLegIKTarget.transform.position, Vector3.down, out hit, 10f))
            {
                RightLegIKTargetOriginePos = hit.point + Vector3.up * 0.1f;
            }
        }
        else
        {
            // Lerp vers l'origine, mais snap uniquement quand la distance est suffisamment petite
            float distR = Vector3.Distance(RightLegIKTarget.position, RightLegIKTargetOriginePos);
            if (distR <= 0.05f)
            {
                rightOnGround = true;
                RightLegIKTarget.position = RightLegIKTargetOriginePos;
            }
            else
            {
                RightLegIKTarget.position = Vector3.Lerp(RightLegIKTarget.position, RightLegIKTargetOriginePos, Time.deltaTime * 10);
            }
        }
        
        //Main Body
        MainBodyPositionUpdate();

        // Mesure la vitesse moyenne sur `measurementDistance` mètres et debug quand atteint.
        Update20mSpeedMeasurement();

        BoolAssignation();
    }

    private void MainBodyPositionUpdate()
    {
        Vector3 feetAverage;
        float bodyLerpSpeed;
        if (leftOnGround && !rightOnGround)
        {
            feetAverage = Vector3.Lerp(LeftLegIKTarget.position, RightLegIKTarget.position, bodyLerpBtwLegs);
            bodyLerpSpeed = 5f;
        }
        else if (!leftOnGround && rightOnGround)
        {
            feetAverage = Vector3.Lerp(RightLegIKTarget.position, LeftLegIKTarget.position, bodyLerpBtwLegs);
            bodyLerpSpeed = 5f;
        }
        else
        {
            feetAverage = (RightLegIKTargetOriginePos + LeftLegIKTargetOriginePos) * 0.5f;
            bodyLerpSpeed = 5;
        }
        Vector3 targetPos = new Vector3(feetAverage.x, bodyYTarget, feetAverage.z);
        // Lissage — ajuste le facteur si besoin
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * bodyLerpSpeed);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(LeftLegIKTargetOriginePos +  currentMaxLeftStepLength * transform.forward, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(RightLegIKTargetOriginePos + currentMaxRightStepLength * 1 * transform.forward, 0.1f);
    }
    private void BoolAssignation()
    {
        // Gauche
        if (leftJustPressed)
        {
            leftJustPressed = false;
        }

        if (leftPressed)
        {
            if (rightON) leftPressed = false; // empêcher le maintien si l'autre pied est appuyé
        }

        if (leftJustReleased)
        {
            leftJustReleased = false;
        }

        // Droite
        if (rightJustPressed)
        {
            rightJustPressed = false;
        }

        if (rightPressed)
        {
            if (leftON) rightPressed = false; // empêcher le maintien si l'autre pied est appuyé
        }

        if (rightJustReleased)
        {
            rightJustReleased = false;
        }
    }

    // Mesure la vitesse moyenne sur `measurementDistance` mètres et debug quand atteint.
    private void Update20mSpeedMeasurement()
    {
        Vector3 currentPos = transform.position;
        float step = Vector3.Distance(currentPos, lastPosForMeasurement);
        accumulatedDistanceForMeasurement += step;
        lastPosForMeasurement = currentPos;

        if (accumulatedDistanceForMeasurement >= measurementDistance)
        {
            float elapsed = Time.time - measurementStartTime;
            float speed = accumulatedDistanceForMeasurement / Mathf.Max(0.0001f, elapsed); // m/s
            if (debugSpeedMeasurement)
            {
                Debug.Log($"[MovementController] Avg speed over {accumulatedDistanceForMeasurement:F2} m = {speed:F2} m/s (time {elapsed:F2}s).");
            }
            // reset pour la prochaine mesure
            measurementStartTime = Time.time;
            accumulatedDistanceForMeasurement = 0f;
        }
    }
}