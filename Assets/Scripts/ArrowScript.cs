using UnityEngine;

public class ArrowScript : MonoBehaviour
{
    [SerializeField] private float fearDamage = 15f;
    private bool hasHit = false; // Flag para verificar se já colidiu

    void Start()
    {
        // Ignora colisão com todos os inimigos
        GameObject[] archers = GameObject.FindGameObjectsWithTag("Enemy");
        Collider2D arrowCollider = GetComponent<Collider2D>();
        
        if (arrowCollider != null)
        {
            // Ignora colisão com inimigos
            foreach (GameObject archer in archers)
            {
                Collider2D archerCollider = archer.GetComponent<Collider2D>();
                if (archerCollider != null)
                {
                    Physics2D.IgnoreCollision(arrowCollider, archerCollider);
                }
            }
            
            // Ignora colisão com outras flechas
            GameObject[] arrows = GameObject.FindGameObjectsWithTag("Arrow");
            foreach (GameObject arrow in arrows)
            {
                if (arrow != gameObject) // Não ignora consigo mesmo
                {
                    Collider2D otherArrowCollider = arrow.GetComponent<Collider2D>();
                    if (otherArrowCollider != null)
                    {
                        Physics2D.IgnoreCollision(arrowCollider, otherArrowCollider);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Se já colidiu, ignora novas colisões

        Debug.Log("Trigger detectado com: " + other.name + " - Tag: " + other.tag);

        // Se colidir com o player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Flecha atingiu o player!");
            hasHit = true;
            
            // Causa medo ao player
            ScaryBarUI scaryBar = FindObjectOfType<ScaryBarUI>();
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearDamage);
            }
            
            Destroy(gameObject); // Destrói a flecha
            return;
        }
        
        // Se colidir com qualquer outro objeto que não seja Enemy ou Boss
        if (!other.CompareTag("Enemy") && !other.CompareTag("Boss") && !other.CompareTag("Player") && !other.CompareTag("Arrow"))
        {
            Debug.Log("Flecha colidiu com parede (Trigger): " + other.name);
            StickToWall(); // Gruda na parede
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return; // Se já colidiu, ignora novas colisões

        Debug.Log("Collision detectado com: " + collision.gameObject.name + " - Tag: " + collision.gameObject.tag);

        // Se colidir com o player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Flecha atingiu o player!");
            hasHit = true;
            
            // Causa medo ao player
            ScaryBarUI scaryBar = FindObjectOfType<ScaryBarUI>();
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearDamage);
            }
            
            Destroy(gameObject); // Destrói a flecha
            return;
        }
        
        // Se colidir com qualquer outro objeto que não seja Enemy ou Boss
        if (!collision.gameObject.CompareTag("Enemy") && !collision.gameObject.CompareTag("Boss") && !collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Arrow"))
        {
            Debug.Log("Flecha colidiu com parede (Collision): " + collision.gameObject.name);
            StickToWall(); // Gruda na parede
        }
    }

    private void StickToWall()
    {
        if (hasHit) return; // Já grudou, não precisa fazer nada
        
        hasHit = true;
        Debug.Log("StickToWall chamado!");

        // Para completamente o movimento
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic; // Torna kinematic para não ser afetado por física
            Debug.Log("Flecha parada e grudada!");
        }
        else
        {
            Debug.LogWarning("Rigidbody2D não encontrado na flecha!");
        }

        // A flecha já será destruída após o tempo definido no ArcherEnemyScript (5 segundos)
    }
}
