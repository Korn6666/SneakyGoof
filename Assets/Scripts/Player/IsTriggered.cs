using UnityEngine;

public class IsTriggered : MonoBehaviour
{
    enum BodyPart {LeftFoot, RightFoot};
    [SerializeField] private BodyPart bodyPart;
    [SerializeField] private MovementController movementController;
    private void OnTriggerEnter(Collider other)
    {
        if (bodyPart == BodyPart.LeftFoot)
        {
            movementController.leftFootOnObstacle = true;
        }
        else
        {
            movementController.rightFootOnObstacle = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (bodyPart == BodyPart.LeftFoot)
        {
            movementController.leftFootOnObstacle = false;
        }
        else
        {
            movementController.rightFootOnObstacle = false;
        }
    }
    
}
