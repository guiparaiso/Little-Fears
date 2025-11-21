using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMove_LockedByKeys : MonoBehaviour
{
    public int sceneBuildIndex;   // índice da cena para onde a porta leva
    public int requiredKeys = 4;  // quantas chaves são necessárias

    private void OnTriggerEnter2D(Collider2D other)
    {
        print("Trigger Entered");

        // Garante que só reage ao Player
        if (other.tag == "Player")
        {
            int currentKeys = 0;

            // Pega o número de chaves do GameManager, se existir
            if (GameManager.instance != null)
            {
                currentKeys = GameManager.instance.totalKeys;
            }

            // Verifica se tem chaves suficientes
            if (currentKeys >= requiredKeys)
            {
                print("Chaves suficientes (" + currentKeys + "). Switch Scene to " + sceneBuildIndex);
                SceneManager.LoadScene(sceneBuildIndex, LoadSceneMode.Single);
            }
            else
            {
                print("Porta trancada. Chaves atuais: " + currentKeys + " / " + requiredKeys);
                // aqui depois você pode tocar um som, mostrar mensagem na UI, etc.
            }
        }
    }
}
