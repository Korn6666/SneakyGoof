using UnityEngine;

public class LegTriggered : MonoBehaviour
{
    enum BodyPart { Left, Right, Debug };
    [SerializeField] private BodyPart bodyPart;
    [SerializeField] private MovementController movementController;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }

        if (bodyPart == BodyPart.Left)
        {
            movementController.leftLegObstacleCounter += 1;
        }
        else if (bodyPart == BodyPart.Right)
        {
            movementController.rightLegObstacleCounter += 1;
        }

        if (bodyPart == BodyPart.Debug)
        {
            // Time.timeScale = 0f;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }
        Vector3 contactPoint = other.ClosestPoint(transform.position);

        if (bodyPart == BodyPart.Left)
        {
            movementController.obstacleOnLeftLegContactPoint = contactPoint;
        }
        else if (bodyPart == BodyPart.Right)
        {
            movementController.obstacleOnRightLegContactPoint = contactPoint;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }
        if (bodyPart == BodyPart.Left)
        {
            movementController.leftLegObstacleCounter -= 1;
        }
        else if (bodyPart == BodyPart.Right)
        {
            movementController.rightLegObstacleCounter -= 1;
        }
    }

}
