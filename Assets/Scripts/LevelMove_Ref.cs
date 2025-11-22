using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMove_Ref : MonoBehaviour
{
    public int sceneBuildIndex;

    // Level move zone enter, if collider is a player
    // Move game to another scene
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger Entered");

        // Garante que só reage ao Player
        if (!other.CompareTag("Player"))
            return;

        // Se acabou de carregar a cena, ignorar o trigger da porta
        if (GameManager.instance != null && GameManager.instance.ignoreDoorOnSceneLoad)
        {
            Debug.Log("Ignorando porta porque acabou de carregar a cena");
            return;
        }

        // Salva posição do player na cena atual antes de trocar
        if (GameManager.instance != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            GameManager.instance.SavePlayerPosition(currentScene, other.transform.position);
        }

        // Player entered, so move level
        Debug.Log("Switching Scene to " + sceneBuildIndex);
        SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
    }
}
