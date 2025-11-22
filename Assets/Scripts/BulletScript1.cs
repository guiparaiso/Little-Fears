using UnityEngine;

public class BulletScript : MonoBehaviour
{
    void Start()
    {
        // Ignora colisão com o player que disparou
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            Collider2D bulletCollider = GetComponent<Collider2D>();
            if (playerCollider != null && bulletCollider != null)
            {
                Physics2D.IgnoreCollision(bulletCollider, playerCollider);
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // TODOS os inimigos agora se auto-gerenciam no OnTriggerEnter2D deles
        // Boss, PumpkinEnemy, ReaperEnemy, ArcherEnemy, Spanner, Enemy genérico, e EnemySpawner
        
        // Se colidir com qualquer inimigo ou boss, eles mesmos tratam
        if (other.CompareTag("Boss") || 
            other.CompareTag("Enemy") ||
            other.GetComponent<PumpkinEnemy>() != null || 
            other.GetComponent<ReaperEnemy>() != null)
        {
            // Não faz nada - o inimigo se auto-gerencia
            Debug.Log("Bullet atingiu inimigo: " + other.name + " - Deixa o inimigo se auto-gerenciar.");
            return;
        }
        
        // Colisões com objetos do cenário
        if (other.CompareTag("fase"))
        {
            Debug.Log("Bullet colidiu com fase: " + other.gameObject.name);
        }
        // Se colidir com qualquer outro objeto que não seja o player
        else if (!other.CompareTag("Player") && !other.name.Contains("limit") && !other.name.Contains("CursedGroundArea(Clone)"))
        {
            Debug.Log("Bullet colidiu com: " + other.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // TODOS os inimigos agora se auto-gerenciam
        // Boss, PumpkinEnemy, ReaperEnemy, ArcherEnemy, Spanner, Enemy genérico, e EnemySpawner
        
        // Se colidir com qualquer inimigo ou boss, eles mesmos tratam
        if (collision.gameObject.CompareTag("Boss") || 
            collision.gameObject.CompareTag("Enemy") ||
            collision.gameObject.GetComponent<PumpkinEnemy>() != null || 
            collision.gameObject.GetComponent<ReaperEnemy>() != null)
        {
            // Não faz nada - o inimigo se auto-gerencia
            Debug.Log("Bullet atingiu inimigo (Collision): " + collision.gameObject.name + " - Deixa o inimigo se auto-gerenciar.");
            return;
        }

        // Colisão com parede
        if (collision.gameObject.CompareTag("wall"))
        {
            Debug.Log("Bullet colidiu com parede: " + collision.gameObject.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o player e não seja "limit"
        else if (!collision.gameObject.CompareTag("Player") && !collision.gameObject.name.Contains("limit"))
        {
            Debug.Log("Bullet colidiu (Collision) com: " + collision.gameObject.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }
}
