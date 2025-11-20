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
        
        // Ignora colisão com inimigos (só atinge player e paredes)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Collider2D bulletCollider = GetComponent<Collider2D>();
        
        foreach (GameObject enemy in enemies)
        {
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (bulletCollider != null && enemyCollider != null)
            {
                Physics2D.IgnoreCollision(bulletCollider, enemyCollider);
            }
        }
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
        // Se acertou qualquer coisa que não seja inimigo (parede, obstáculo)
        else if (!other.CompareTag("Enemy"))
        {
            Debug.Log("Veneno colidiu com: " + other.name);
            
            // Efeito visual
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se acertou o player
        if (collision.gameObject.CompareTag("Player"))
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
        // Se acertou qualquer coisa que não seja inimigo
        else if (!collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Veneno colidiu com: " + collision.gameObject.name);
            
            // Efeito visual
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Destroy(gameObject);
        }
    }
}
