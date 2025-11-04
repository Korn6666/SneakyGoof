using UnityEngine;
using UnityEngine.SceneManagement;

public class UIOnGame : MonoBehaviour
{
    [SerializeField] private int menuIndexScene;
    [SerializeField] private int nextLevelIndexScene;
    public void BackToMenu()
    {
        SceneManager.LoadScene(menuIndexScene);
    }
    public void LoadThisLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void LoadNextLevel()
    {
        SceneManager.LoadScene(nextLevelIndexScene);
    }
}
