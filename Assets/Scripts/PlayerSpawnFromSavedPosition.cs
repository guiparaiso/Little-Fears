using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerSpawnFromSavedPosition : MonoBehaviour
{
    void Start()
    {
        if (GameManager.instance == null)
            return;

        string currentScene = SceneManager.GetActiveScene().name;

        Vector3 savedPos;
        if (GameManager.instance.TryGetPlayerPosition(currentScene, out savedPos))
        {
            // Se já existe posição salva para essa cena, reposiciona o player
            transform.position = savedPos;
        }

        // Ao entrar na cena, ignorar portas por um pequeno tempo
        GameManager.instance.ignoreDoorOnSceneLoad = true;
        StartCoroutine(EnableDoorsAfterDelay());
    }

    private IEnumerator EnableDoorsAfterDelay()
    {
        // Pequeno delay para evitar que o spawn em cima da porta
        // cause troca de cena imediata
        yield return new WaitForSeconds(0.1f);

        if (GameManager.instance != null)
        {
            GameManager.instance.ignoreDoorOnSceneLoad = false;
        }
    }
}
