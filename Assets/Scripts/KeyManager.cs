using UnityEngine;
using TMPro; // Necessário para usar o TextMeshPro

public class KeyManager : MonoBehaviour
{
    // Cria uma instância estática para facilitar o acesso de outros scripts (Singleton simples)
    public static KeyManager instance;

    [Header("Configurações da UI")]
    public TMP_Text keyCountText; // Arraste o componente de texto aqui no Inspector

    void Awake()
    {
        // Configura a instância
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Ao iniciar a cena, já busca o valor correto
        if (GameManager.instance != null)
        {
            UpdateKeyCountText();
        }
    }

    // Método chamado pela chave quando o player a coleta
    public void AddKey()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.totalKeys++;
            // Atualiza o texto pegando o valor do GM
            UpdateKeyCountText();
        }
    }

    // Atualiza o que aparece na tela
    void UpdateKeyCountText()
    {
        keyCountText.text = GameManager.instance.totalKeys.ToString();
    }
}
