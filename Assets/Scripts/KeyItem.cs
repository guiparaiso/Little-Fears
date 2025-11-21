using UnityEngine;
using UnityEngine.InputSystem;

public class KeyItem : MonoBehaviour
{
    public string keyID;
    void Start()
    {
        // 1. Verifica no GameManager se essa chave já foi pega no passado
        if (GameManager.instance != null)
        {
            if (GameManager.instance.IsObjectRegistered(keyID))
            {
                // Se já foi pega antes, destrói ela imediatamente ao carregar a cena
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            KeyManager.instance.AddKey();
            GameManager.instance.RegisterObject(keyID);
            Destroy(gameObject);
        }
    }
}
