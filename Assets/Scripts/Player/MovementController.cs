using Unity.Mathematics;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.UIElements;

[RequireComponent(typeof(NoiseEmitter))]
public class MovementController : MonoBehaviour
{
    [SerializeField] private Transform LeftLegIKTarget;
    private Vector3 LeftLegIKTargetOriginePos;
    [SerializeField] private Transform RightLegIKTarget;
    private Vector3 RightLegIKTargetOriginePos;
    private float bodyYTarget = 0;
    [SerializeField] private float bodyYOffset = 1f;
    [SerializeField] private AnimationCurve heightFootCurve;
    [SerializeField] private AnimationCurve speedStepCurve;
    [SerializeField] private float toleranceDistanceForStep = 0.1f;
    [SerializeField] private float speedFootToGround = 20;
    // Distance in meters
    [SerializeField] private float maxStepLength = 1;
    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField] private float maxStepSpeed = 1;
    [SerializeField] private float minStepSpeed = 0.1f;
    private float stepSpeed = 1;
    [SerializeField] private float rotationSpeed = 180f; // deg/s, configuré dans l'inspector
    private float distDiffFromOldOriginePos = 0;
    private RaycastHit hit;

    private PlayerInputActions input;
    [SerializeField] private float YBodyMoveFactor = 1;
    [SerializeField] private float bodyLerpBtwLegs = 0.3f;
    private float initialYBodyPos;
    public Vector3 obstacleOnLeftLegContactPoint = Vector3.zero;
    public Vector3 obstacleOnRightLegContactPoint = Vector3.zero;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask groundMask;
    
    public enum Foot { Left, Right }

    // Pied Gauche
    private bool leftPressed;
    private bool nextLeftPressed;
    private bool leftJustPressed;
    private bool leftJustReleased;
    private bool leftON; // left all pressed
    private bool leftOnGround = true;
    private bool leftLegMovingToOriginalPos;
    private float currentLeftStepLength;
    private float currentMaxLeftStepLength;
    private float currentLeftStepHeight = 0;
    private bool leftLegOnObstacle;
    public int leftLegObstacleCounter = 0;
    private Vector3 leftDesiredWorldPos;
    private float signedObsLeftForwardDirFromTranform;
    private float signedObsLeftForwardDirFromTarget;
    private float signedObsLeftRightDir;

    // Pied Droit
    private bool rightPressed;
    private bool nextRightPressed;
    private bool rightJustPressed;
    private bool rightJustReleased;
    private bool rightON; // right all pressed
    private bool rightOnGround = true;
    private bool rightLegMovingToOriginalPos;
    private float currentRightStepLength;
    private float currentMaxRightStepLength;
    private float currentRightStepHeight = 0;
    private bool rightLegOnObstacle;
    public int rightLegObstacleCounter = 0;
    private Vector3 rightDesiredWorldPos;
    private float signedObsRightForwardDirFromTranform;
    private float signedObsRightForwardDirFromTarget;
    private float signedObsRightRightDir;

    // Rotation
    private bool rightRotationInput = false;
    private bool leftRotationInput = false;

    private NoiseEmitter noiseEmitter;

    [Header("Speed 20m Measurement")]
    [SerializeField] private float measurementDistance = 20f;
    [SerializeField] private bool debugSpeedMeasurement = true;
    private Vector3 lastPosForMeasurement;
    private float accumulatedDistanceForMeasurement = 0f;
    private float measurementStartTime = 0f;

    void Awake()
    {
        initialYBodyPos = transform.position.y;
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

        //Get noise emitter
        noiseEmitter = GetComponent<NoiseEmitter>();
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
        ObstacleDataUpdate();

        Rotation();

        FootMovingToOriginalPos();

        ProcessFoot(Foot.Left);
        ProcessFoot(Foot.Right);

        //Main Body
        MainBodyPositionUpdate();

        BoolAssignation();
    }
    
    // Appliquer les positions monde calculées aux transforms enfants après que le body ait bougé.
    // LateUpdate écrase l'effet de la parenté chaque frame.
    void LateUpdate()
    {
        if (LeftLegIKTarget != null) LeftLegIKTarget.position = leftDesiredWorldPos;
        if (RightLegIKTarget != null) RightLegIKTarget.position = rightDesiredWorldPos;
        if (!nextLeftPressed)
        {
            leftPressed = false;
            nextLeftPressed = true;
        }
        if (!nextRightPressed)
        {
            rightPressed = false;
            nextRightPressed = true;
        }
        
    }

    void ProcessFoot(Foot foot)
    {
        if (rightLegMovingToOriginalPos || leftLegMovingToOriginalPos)
        {
            return;
        }

        // === Sélection des variables selon le pied ===

        bool justPressed = (foot == Foot.Left) ? leftJustPressed : rightJustPressed;
        bool pressed = (foot == Foot.Left) ? leftPressed : rightPressed;
        bool justReleased = (foot == Foot.Left) ? leftJustReleased : rightJustReleased;
        bool onGround = (foot == Foot.Left) ? leftOnGround : rightOnGround;
        bool otherFootON = (foot == Foot.Left) ? rightON : leftON;
        bool movingToOrigin = (foot == Foot.Left) ? leftLegMovingToOriginalPos : rightLegMovingToOriginalPos;

        float currentStepLen = (foot == Foot.Left) ? currentLeftStepLength : currentRightStepLength;
        float currentMaxLen = (foot == Foot.Left) ? currentMaxLeftStepLength : currentMaxRightStepLength;

        Transform IKTarget = (foot == Foot.Left) ? LeftLegIKTarget : RightLegIKTarget;
        Vector3 IKTargetOrigin = (foot == Foot.Left) ? LeftLegIKTargetOriginePos : RightLegIKTargetOriginePos;

        float currentHeight = (foot == Foot.Left) ? currentLeftStepHeight : currentRightStepHeight;
        Vector3 desiredPos = (foot == Foot.Left) ? leftDesiredWorldPos : rightDesiredWorldPos;

        bool legOnObstacle = (foot == Foot.Left) ? leftLegOnObstacle : rightLegOnObstacle;
        float signedObsForwardDirFromTranform = (foot == Foot.Left) ? signedObsLeftForwardDirFromTranform : signedObsRightForwardDirFromTranform;
        float signedObsForwardDirFromTarget = (foot == Foot.Left) ? signedObsLeftForwardDirFromTarget : signedObsRightForwardDirFromTarget;
        float signedObsRightDir = (foot == Foot.Left) ? signedObsLeftRightDir : signedObsRightRightDir;

        // === LOGIQUE COMMUNE ===

        if (justPressed)
        {
            float otherDist = Vector3.Distance(
                (foot == Foot.Left ? RightLegIKTarget : LeftLegIKTarget).position,
                foot == Foot.Left ? RightLegIKTargetOriginePos : LeftLegIKTargetOriginePos
            );

            if (otherFootON || !onGround || otherDist > toleranceDistanceForStep)
            {
                justPressed = false;
                pressed = false;
            }
            else
            {
                distDiffFromOldOriginePos = 0;
                float signedForward = Vector3.Dot(
                    (foot == Foot.Left ? RightLegIKTarget : LeftLegIKTarget).position - IKTarget.position,
                    transform.forward
                );

                currentMaxLen = maxStepLength + signedForward;

                if (currentMaxLen < 0.1f)
                    pressed = false;
            }
        }
        else if (pressed)
        {
            stepSpeed = speedStepCurve.Evaluate(currentStepLen) * maxStepSpeed;
            // currentStepLen += Time.deltaTime * stepSpeed / currentMaxLen;

            if (legOnObstacle)
            {
                if (rightRotationInput)
                {
                    //Recule le pied si il est devant et l'avance s'il est derrière
                    currentStepLen -= Time.deltaTime * Mathf.Sign(signedObsForwardDirFromTranform) * stepSpeed / currentMaxLen;
                }
                else if (leftRotationInput)
                {
                    //Recule le pied si il est devant et l'avance s'il est derrière
                    currentStepLen -= Time.deltaTime * Mathf.Sign(signedObsForwardDirFromTranform) * stepSpeed / currentMaxLen;
                }else if (signedObsForwardDirFromTarget < 0)
                {
                    currentStepLen += Time.deltaTime * stepSpeed / currentMaxLen;
                }
            }
            else
            {
                currentStepLen += Time.deltaTime * stepSpeed / currentMaxLen;
            }

            currentStepLen = Mathf.Clamp(currentStepLen, -1, 1);
            currentHeight = heightFootCurve.Evaluate(currentStepLen);

            if (otherFootON || movingToOrigin || Mathf.Abs(currentStepLen) >= 1)
            {
                currentStepLen = 0;
                movingToOrigin = true;

                if (foot == Foot.Left) nextLeftPressed = false;
                else nextRightPressed = false;
            }
            else
            {
                onGround = false;

                bodyYTarget = currentHeight * maxStepHeight * YBodyMoveFactor;

                desiredPos =
                    IKTargetOrigin
                    - distDiffFromOldOriginePos * transform.forward // Compensation du changement de IKTargetOrigin dans Rotation()
                    + currentStepLen * currentMaxLen * transform.forward
                    + currentHeight * maxStepHeight * transform.up;
            }
        }
        else if (justReleased)
        {
            currentStepLen = 0;
            if (!onGround)
            {
                NewOriginalPos(foot);
                movingToOrigin = true;
            }
        }

        // === RÉÉCRITURE DANS LES VARIABLES GLOBALES ===

        if (foot == Foot.Left)
        {
            leftJustPressed = justPressed;
            leftPressed = pressed;
            leftJustReleased = justReleased;
            leftOnGround = onGround;
            leftLegMovingToOriginalPos = movingToOrigin;

            currentLeftStepLength = currentStepLen;
            currentMaxLeftStepLength = currentMaxLen;
            currentLeftStepHeight = currentHeight;
            leftDesiredWorldPos = desiredPos;
        }
        else
        {
            rightJustPressed = justPressed;
            rightPressed = pressed;
            rightJustReleased = justReleased;
            rightOnGround = onGround;
            rightLegMovingToOriginalPos = movingToOrigin;

            currentRightStepLength = currentStepLen;
            currentMaxRightStepLength = currentMaxLen;
            currentRightStepHeight = currentHeight;
            rightDesiredWorldPos = desiredPos;
        }
    }
    
    private void ObstacleDataUpdate()
    {
        if (leftLegObstacleCounter > 0)
        {
            leftLegOnObstacle = true;
        }
        else
        {
            leftLegOnObstacle = false;
            leftLegObstacleCounter = 0;
        }
        if (rightLegObstacleCounter > 0)
        {
            rightLegOnObstacle = true;
        }
        else
        {
            rightLegOnObstacle = false;
            rightLegObstacleCounter = 0;
        }

        if (leftLegOnObstacle)
        {
            signedObsLeftForwardDirFromTranform = Vector3.Dot(obstacleOnLeftLegContactPoint - transform.position, transform.forward);
            signedObsLeftForwardDirFromTarget = Vector3.Dot(obstacleOnLeftLegContactPoint - LeftLegIKTarget.position, transform.forward);
            // if (signedObsLeftForwardDirFromTarget < 0) Debug.Log("c negatif à gaiche");
            signedObsLeftRightDir = Vector3.Dot(obstacleOnLeftLegContactPoint - LeftLegIKTarget.position, transform.right);
        }else
        {
            signedObsLeftForwardDirFromTranform = 0;
            signedObsLeftForwardDirFromTarget = 0;
            signedObsLeftRightDir = 0;
        }
        
        if (rightLegOnObstacle)
        {
            signedObsRightForwardDirFromTranform = Vector3.Dot(obstacleOnRightLegContactPoint - transform.position, transform.forward);
            signedObsRightForwardDirFromTarget = Vector3.Dot(obstacleOnRightLegContactPoint - RightLegIKTarget.position, transform.forward);
            signedObsRightRightDir = Vector3.Dot(obstacleOnRightLegContactPoint - RightLegIKTarget.position, transform.right);
            // Debug.Log(signedObsRightRightDir);
        }
        else
        {   
            signedObsRightForwardDirFromTranform = 0;
            signedObsRightForwardDirFromTarget = 0;
            signedObsRightRightDir = 0;
        }
    }



    private void Rotation()
    {
        if (rightRotationInput || leftRotationInput)
        {

            float turnInput = 0;
            bool obstacleOnLeft = signedObsLeftRightDir < 0 || signedObsRightRightDir < 0;
            bool obsacleOnRight = signedObsLeftRightDir > 0 || signedObsRightRightDir > 0;

            if (rightRotationInput && !obsacleOnRight) turnInput += 1;
            if (leftRotationInput && !obstacleOnLeft) turnInput -= 1;

            // only rotate if one foot is on ground
            if (leftOnGround != rightOnGround)
            {
                // choose pivot: prefer the foot that is on ground while the other is not
                Vector3 pivot = (LeftLegIKTarget.position + RightLegIKTarget.position) * 0.5f;
                if (leftOnGround && !rightOnGround) pivot = LeftLegIKTarget.position;
                else if (rightOnGround && !leftOnGround) pivot = RightLegIKTarget.position;
                float angle = turnInput * rotationSpeed * Time.deltaTime;

                // rotate the player transform around pivot (world space)
                transform.RotateAround(pivot, Vector3.up, angle);

                // also rotate the stored origin positions so the baseline (origins) follows the rotation
                Vector3 leftNewPos = Quaternion.AngleAxis(angle, Vector3.up) * (LeftLegIKTargetOriginePos - pivot) + pivot;
                LeftLegIKTargetOriginePos = IKTargetPositionFinder(leftNewPos, Foot.Left);
                Vector3 rightNewPos = Quaternion.AngleAxis(angle, Vector3.up) * (RightLegIKTargetOriginePos - pivot) + pivot;
                RightLegIKTargetOriginePos = IKTargetPositionFinder(rightNewPos, Foot.Right);
            }
        }
    }

    private Vector3 IKTargetPositionFinder(Vector3 oldPos, Foot foot)
    {
        if (Physics.OverlapSphere(oldPos, 0.1f, obstacleMask).Length == 0)
        {
            return oldPos;
        }
        // Find pos on ground under the foot
        RaycastHit raycastHit;
        if (foot == Foot.Left)
        {
            Physics.Raycast(LeftLegIKTarget.transform.position, Vector3.down, out raycastHit, 10f, groundMask);
        }
        else
        {
            Physics.Raycast(RightLegIKTarget.transform.position, Vector3.down, out raycastHit, 10f, groundMask);
        }

        Vector3 groundPointUnderFoot = raycastHit.point;
        Vector3 rayDir = oldPos - groundPointUnderFoot;

        if (Physics.Raycast(groundPointUnderFoot, rayDir.normalized, out raycastHit, rayDir.magnitude, obstacleMask))
        {
            distDiffFromOldOriginePos += Vector3.Distance(oldPos, raycastHit.point  - rayDir * 0.1f);
            return raycastHit.point  - rayDir * 0.1f;
        }
        else
        {
            return oldPos;
        }
    }

    private void FootMovingToOriginalPos()
    {
        if (leftLegMovingToOriginalPos)
        {
            float distL = Vector3.Distance(LeftLegIKTarget.position, LeftLegIKTargetOriginePos);
            if (distL <= 0.05f)
            {
                // set desired world pos; actual transform will be applied in LateUpdate
                leftDesiredWorldPos = LeftLegIKTargetOriginePos;
                leftLegMovingToOriginalPos = false;
                leftOnGround = true;
                noiseEmitter.MakeNoise(1);
            }
            else
            {
                leftDesiredWorldPos = Vector3.Lerp(LeftLegIKTarget.position, LeftLegIKTargetOriginePos, Time.deltaTime * speedFootToGround);
            }
        }
        if (rightLegMovingToOriginalPos)
        {
            float distR = Vector3.Distance(RightLegIKTarget.position, RightLegIKTargetOriginePos);
            if (distR <= 0.05f)
            {
                rightDesiredWorldPos = RightLegIKTargetOriginePos;
                rightLegMovingToOriginalPos = false;
                rightOnGround = true;
                noiseEmitter.MakeNoise(1);
            }
            else
            {
                rightDesiredWorldPos = Vector3.Lerp(RightLegIKTarget.position, RightLegIKTargetOriginePos, Time.deltaTime * speedFootToGround);
            }
        }
    
    }
    
    private void NewOriginalPos(Foot foot)
    {
        if (foot == Foot.Left)
        {
            leftLegMovingToOriginalPos = true;
            if (Physics.Raycast(LeftLegIKTarget.transform.position, Vector3.down, out hit, 10f))
            {
                 LeftLegIKTargetOriginePos = hit.point + Vector3.up * 0.1f;
            }
        }
        else
        {
            rightLegMovingToOriginalPos = true;
            if (Physics.Raycast(RightLegIKTarget.transform.position, Vector3.down, out hit, 10f))
            {
                 RightLegIKTargetOriginePos = hit.point + Vector3.up * 0.1f;
            }
        }
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
        Vector3 targetPos = new Vector3(feetAverage.x, initialYBodyPos + bodyYTarget + bodyYOffset, feetAverage.z);
        // Lissage — ajuste le facteur si besoin
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * bodyLerpSpeed);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(LeftLegIKTargetOriginePos, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(RightLegIKTargetOriginePos, 0.1f);
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