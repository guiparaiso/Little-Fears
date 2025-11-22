using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMove_LockedByKeys : MonoBehaviour
{
    public int sceneBuildIndex;   // índice da cena do boss
    public int requiredKeys = 4;  // quantas chaves são necessárias

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Ignora o trigger logo após carregar a cena
        if (GameManager.instance != null && GameManager.instance.ignoreDoorOnSceneLoad)
        {
            Debug.Log("Ignorando porta do boss porque acabou de carregar a cena");
            return;
        }

        int currentKeys = 0;

        if (GameManager.instance != null)
        {
            currentKeys = GameManager.instance.totalKeys;
        }

        if (currentKeys >= requiredKeys)
        {
            // Salva posição antes de sair
            if (GameManager.instance != null)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                GameManager.instance.SavePlayerPosition(currentScene, other.transform.position);
            }

            Debug.Log("Chaves suficientes (" + currentKeys + "). Switch Scene to " + sceneBuildIndex);
            SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("Porta trancada. Chaves atuais: " + currentKeys + " / " + requiredKeys);
            // Aqui depois dá pra tocar som, mostrar mensagem na UI etc.
        }
    }
}
