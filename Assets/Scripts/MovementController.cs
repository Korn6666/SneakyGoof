using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementController : MonoBehaviour
{
    [SerializeField] private Transform LeftLegIKTarget;
    private Vector3 LeftLegIKTargetOriginePos;
    [SerializeField] private Transform RightLegIKTarget;
    private Vector3 RightLegIKTargetOriginePos;
    private Vector3 bodyPos;
    [SerializeField] private float offSetYBody = -0.4f;
    [SerializeField] private AnimationCurve heightFootCurve;
    // Distance in meters
    [SerializeField] private float maxStepLength = 1;
    [SerializeField] private float maxStepHeight = 0.3f;
    [SerializeField] private float stepSpeed = 1;
    private float currentStepLength = 0;
    private float currentStepHeight = 0;
    private RaycastHit hit;

    private PlayerInputActions input;

    // Gauche
    private bool leftPressed;
    private bool leftJustPressed;
    private bool leftJustReleased;

    // Droite
    private bool rightPressed;
    private bool rightJustPressed;
    private bool rightJustReleased;

    void Awake()
    {
        input = new PlayerInputActions();
        LeftLegIKTargetOriginePos = LeftLegIKTarget.position;
        RightLegIKTargetOriginePos = RightLegIKTarget.position;
    }

    void OnEnable()
    {
        input.Player.Enable();

        // Gauche
        input.Player.LeftStep.started += ctx =>
        {
            leftJustPressed = true;
            leftPressed = true;
        };
        input.Player.LeftStep.canceled += ctx =>
        {
            leftPressed = false;
            leftJustReleased = true;
        };

        // Droite
        input.Player.RightStep.started += ctx =>
        {
            rightJustPressed = true;
            rightPressed = true;
        };
        input.Player.RightStep.canceled += ctx =>
        {
            rightPressed = false;
            rightJustReleased = true;
        };
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Update()
    {
        //Left foot
        if (leftJustPressed)
        {
            LeftLegIKTargetOriginePos = LeftLegIKTarget.transform.position;
        }
        else if (leftPressed)
        {
            currentStepLength += Time.deltaTime * stepSpeed;
            currentStepHeight = heightFootCurve.Evaluate(currentStepLength);

            LeftLegIKTarget.transform.position = LeftLegIKTargetOriginePos + transform.forward * currentStepLength * maxStepLength + transform.up * currentStepHeight * maxStepHeight;
        }
        else if (leftJustReleased)
        {
            currentStepLength = 0;
            if (Physics.Raycast(LeftLegIKTarget.transform.position, Vector3.down, out hit))
            {
                LeftLegIKTarget.transform.position = hit.point;
            }
        }
        else
        {
            LeftLegIKTarget.position = LeftLegIKTargetOriginePos;
        }

        //Right foot
        if (rightJustPressed)
        {
            RightLegIKTargetOriginePos = RightLegIKTarget.transform.position;
        }
        else if (rightPressed)
        {
            currentStepLength += Time.deltaTime;
            currentStepHeight = heightFootCurve.Evaluate(currentStepLength);

            RightLegIKTarget.transform.position = RightLegIKTargetOriginePos + transform.forward * currentStepLength * maxStepLength + transform.up * currentStepHeight * maxStepHeight;
        }
        else if (rightJustReleased)
        {
            currentStepLength = 0;
            if (Physics.Raycast(RightLegIKTarget.transform.position, Vector3.down, out hit))
            {
                RightLegIKTarget.transform.position = hit.point;
            }
        }
        else
        {
            //Line to change
            RightLegIKTarget.position = RightLegIKTargetOriginePos;
        }

        //Main Body
        bodyPos = (RightLegIKTarget.position + LeftLegIKTarget.position) / 2;
        transform.position = new Vector3(bodyPos.x, bodyPos.y + offSetYBody, bodyPos.z);
        BoolAssignation();
    }
    
    private void BoolAssignation()
    {
        // Gauche
        if (leftJustPressed)
        {
            Debug.Log("Pied gauche : début appui");
            leftJustPressed = false;
        }

        if (leftPressed)
        {
            Debug.Log($"Pied gauche maintenue");
        }

        if (leftJustReleased)
        {
            Debug.Log("Pied gauche : relâché");
            leftJustReleased = false;
        }

        // Droite
        if (rightJustPressed)
        {
            Debug.Log("Pied droit : début appui");
            rightJustPressed = false;
        }

        if (rightPressed)
        {
            Debug.Log($"Pied droit maintenu");
        }

        if (rightJustReleased)
        {
            Debug.Log("Pied droit : relâché");
            rightJustReleased = false;
        }
    }
}
