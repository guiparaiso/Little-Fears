using UnityEngine;

public class UniqueObject : MonoBehaviour
{
    [Header("ID Único")]
    public string objectID;

    void Start()
    {
        // Ao iniciar, pergunta ao GameManager: "Eu já morri/fui pego?"
        if (GameManager.instance.IsObjectRegistered(objectID))
        {
            // Se sim, se destrói imediatamente antes do jogador ver
            Destroy(this.gameObject);
        }
    }

    // Chame este método quando o inimigo morrer ou a chave for pega
    public void RegisterAsDeleted()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.RegisterObject(objectID);
        }
    }
}