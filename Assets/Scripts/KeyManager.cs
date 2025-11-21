using UnityEngine;
using TMPro; // Necessário para usar o TextMeshPro

public class KeyManager : MonoBehaviour
{
    // Cria uma instância estática para facilitar o acesso de outros scripts (Singleton simples)
    public static KeyManager instance;

    [Header("Configurações da UI")]
    public TMP_Text keyCountText; // Arraste o componente de texto aqui no Inspector

    private int keyCount = 0;

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
        UpdateKeyCountText();
    }

    // Método chamado pela chave quando o player a coleta
    public void AddKey()
    {
        keyCount++;
        UpdateKeyCountText();
    }

    // Atualiza o que aparece na tela
    void UpdateKeyCountText()
    {
        keyCountText.text = keyCount.ToString();
    }
}
