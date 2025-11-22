using UnityEngine;
using UnityEngine.InputSystem;

public class HiddenMessage : MonoBehaviour
{
    [Header("Configuração")]
    // Arraste o objeto de Texto da sua UI para este campo no Inspector
    public GameObject messageUI;
    private bool onArea;

    void Start()
    {
        // Garante que a mensagem comece desligada
        messageUI.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (onArea && Input.GetKeyDown(KeyCode.X))
        {
            messageUI.SetActive(!messageUI.activeSelf);
        }
    }

    // Detecta quando o Player ENTRA na área
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {

            onArea = true;
            
        }
    }

    // Detecta quando o Player SAI da área
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            onArea = false;
            messageUI.SetActive(false);
        }
    }
}
