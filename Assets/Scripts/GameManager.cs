using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Dados do Jogador")]
    public int totalKeys = 0;
    public float currentFear = 0f;

    // Lista para guardar IDs de coisas que já foram coletadas ou mortas
    // HashSet é mais rápido que List para verificar se algo existe
    private HashSet<string> registeredObjects = new HashSet<string>();

    void Awake()
    {
        // Padrão Singleton para garantir que só exista UM GameManager
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Isso impede que este objeto seja destruído ao mudar de cena
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
}