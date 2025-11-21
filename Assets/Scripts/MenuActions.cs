using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuActions : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void IniciaJogo()
    {
        SceneManager.LoadScene(5);
    }

    public void LoadInstructions()
    {
        SceneManager.LoadScene(7);
    }

    public void LoadMenu()
    {
        SceneManager.LoadScene(8);
    }
}
