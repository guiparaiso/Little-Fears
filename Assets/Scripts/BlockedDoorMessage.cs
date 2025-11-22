using UnityEngine;

public class MensagemProximidade : MonoBehaviour
{
    [Header("Configuração")]
    // Arraste o objeto de Texto da sua UI para este campo no Inspector
    public GameObject messageUI;

    void Start()
    {
        // Garante que a mensagem comece desligada
        messageUI.SetActive(false);
    }

    // Detecta quando o Player ENTRA na área
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            messageUI.SetActive(true);
        }
    }

    // Detecta quando o Player SAI da área
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            messageUI.SetActive(false);
        }
    }
}
