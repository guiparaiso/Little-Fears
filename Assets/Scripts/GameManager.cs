using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Dados do Jogador")]
    public int totalKeys = 0;
    public float currentFear = 0f;

    [Header("Controle de portas")]
    // Usado para ignorar o trigger das portas logo após carregar a cena,
    // evitando loop de entra/sai infinito
    public bool ignoreDoorOnSceneLoad = false;

    // Lista para guardar IDs de coisas que já foram coletadas ou mortas
    // HashSet é mais rápido que List para verificar se algo existe
    private HashSet<string> registeredObjects = new HashSet<string>();

    // Posição salva do player por cena (nome da cena -> posição)
    private Dictionary<string, Vector3> savedPlayerPositions = new Dictionary<string, Vector3>();

    void Awake()
    {
        // Padrão Singleton para garantir que só exista UM GameManager
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Impede que este objeto seja destruído ao mudar de cena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- Métodos para gerenciar objetos (Inimigos/Chaves) ---

    // Verifica se um objeto já foi "registrado" (coletado/morto)
    public bool IsObjectRegistered(string id)
    {
        return registeredObjects.Contains(id);
    }

    // Registra um objeto para que ele não apareça mais
    public void RegisterObject(string id)
    {
        if (!registeredObjects.Contains(id))
        {
            registeredObjects.Add(id);
        }
    }

    // --- Posição do player por cena ---

    public void SavePlayerPosition(string sceneName, Vector3 position)
    {
        savedPlayerPositions[sceneName] = position;
    }

    public bool TryGetPlayerPosition(string sceneName, out Vector3 position)
    {
        return savedPlayerPositions.TryGetValue(sceneName, out position);
    }

    // --- DERROTA: chamado pela ScaryBarUI ou outros sistemas ---
    public void GameOver()
    {
        Debug.Log("Game Over!");

        // Aqui você decide o que fazer no game over.
        // Exemplo 1: recarregar a cena atual:
        // Scene current = SceneManager.GetActiveScene();
        // SceneManager.LoadScene(current.name);

        // Exemplo 2: carregar uma cena específica:
        // SceneManager.LoadScene("GameOverScene");
    }

    // --- VITÓRIA: chamado pelo Boss.cs quando o boss morre ---
    public void GameEnd()
    {
        Debug.Log("Game End (vitória)!");

        // Aqui você decide o que acontece quando o jogador vence.
        // Exemplo: carregar uma cena de vitória:
        // SceneManager.LoadScene("WinScene");

        // Ou mostrar uma tela de "Você venceu!" com UI, etc.
    }
}
