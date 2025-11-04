using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private int indexSceneToLoad = 0;
    public void OnButtonPlay()
    {
        SceneManager.LoadScene(indexSceneToLoad);
    }
    public void OnButtonQuit()
    {
        Application.Quit();
    }
}
