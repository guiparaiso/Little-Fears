using UnityEngine;

public class PoisonProjectile : MonoBehaviour
{
    [HideInInspector] public float damage = 15f;
    [HideInInspector] public ScaryBarUI scaryBar;
    
    [SerializeField] float lifetime = 5f; // Tempo até desaparecer
    [SerializeField] GameObject hitEffectPrefab; // Efeito ao colidir
    
    private void Start()
    {
        // Destrói após lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se acertou o player
        if (other.CompareTag("Player"))
        {
            if (scaryBar != null)
            {
                scaryBar.AddFear(damage);
                Debug.Log($"Veneno acertou o player! Dano: {damage}");
            }
            
            // Efeito visual
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
        // Se acertou parede/obstáculo
        else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacles") || 
                 other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            // Efeito visual
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
}
