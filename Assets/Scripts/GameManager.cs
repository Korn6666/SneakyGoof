using UnityEngine;
using UnityEngine.Timeline;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;
    [SerializeField] private MovementController PlayerController;
    [SerializeField] private Eye_Behaviour EyeController;
    [SerializeField] private GameObject GameOverPanel;
    [SerializeField] private GameObject WinPanel;
    [SerializeField] private float WinDistance;

    void Start()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else if (singleton != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (WinConditions())
        {
            OnWin();
        }
    }

    private bool WinConditions()
    {
        return Vector3.Distance(PlayerController.transform.position, EyeController.transform.position) < WinDistance;
    }
    public void OnWin()
    {
        PlayerController.enabled = false;
        EyeController.enabled = false;
        WinPanel.SetActive(true);
    }

    public void OnLose()
    {
        PlayerController.enabled = false;
        EyeController.enabled = false;
        GameOverPanel.SetActive(true);
    }
}
