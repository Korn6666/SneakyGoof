using UnityEngine;

public class IsTriggered : MonoBehaviour
{
    enum BodyPart {LeftFoot, RightFoot, ArroundLeftFoot, ArroundRightFoot};
    [SerializeField] private BodyPart bodyPart;
    [SerializeField] private MovementController movementController;
    private void OnTriggerEnter(Collider other)
    {
        Vector3 contactPoint = other.ClosestPoint(transform.position);
        movementController.obstacleContactPoint = contactPoint;
        if (bodyPart == BodyPart.LeftFoot)
        {
            movementController.leftFootOnObstacle = true;
        }
        else if (bodyPart == BodyPart.RightFoot)
        {
            movementController.rightFootOnObstacle = true;
        }else if (bodyPart == BodyPart.ArroundLeftFoot)
        {
            movementController.aroundLeftFootOnObstacle = true;   
        }else if (bodyPart == BodyPart.ArroundRightFoot)
        {
            movementController.aroundRightFootOnObstacle = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (bodyPart == BodyPart.LeftFoot)
        {
            movementController.leftFootOnObstacle = false;
        }
        else if (bodyPart == BodyPart.RightFoot)
        {
            movementController.rightFootOnObstacle = false;
        }else if (bodyPart == BodyPart.ArroundLeftFoot)
        {
            movementController.aroundLeftFootOnObstacle = false;  
        }else if (bodyPart == BodyPart.ArroundRightFoot)
        {
            movementController.aroundRightFootOnObstacle = false;  
        }
    }
    
}
