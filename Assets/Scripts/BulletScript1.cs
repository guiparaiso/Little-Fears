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
        // Se colidir com o Boss
        if (other.CompareTag("Boss"))
        {
            Debug.Log("Bullet atingiu o Boss!");
            Boss boss = other.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(1); // Causa 1 de dano ao Boss
            }
            Destroy(gameObject); // Destrói o bullet
        }
        // Se colidir com um inimigo
        else if (other.CompareTag("Enemy"))
        {
            Debug.Log("Bullet atingiu inimigo!");
            
            // Verifica se é uma PumpkinEnemy (tem sistema de HP)
            PumpkinEnemy pumpkin = other.GetComponent<PumpkinEnemy>();
            if (pumpkin != null)
            {
                pumpkin.TakeDamage(15f); // Causa dano ao invés de destruir
                Debug.Log("Bullet causou dano ao Pumpkin!");
            }
            else
            {
                // Inimigos normais sem HP são destruídos
                Destroy(other.gameObject);
            }
            
            Destroy(gameObject); // Destrói o bullet
        }
        else if (other.CompareTag("fase")) {
            Debug.Log("Bullet colidiu com fase: " + other.gameObject.name);
        }
        // Se colidir com qualquer outro objeto que não seja o player
        else if (!other.CompareTag("Player") && !other.name.Contains("limit"))
        {
            Debug.Log("Bullet colidiu com: " + other.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se colidir com o Boss
        if (collision.gameObject.CompareTag("Boss"))
        {
            Debug.Log("Bullet atingiu o Boss!");
            Boss boss = collision.gameObject.GetComponent<Boss>();
            if (boss != null)
            {
                boss.TakeDamage(1); // Causa 1 de dano ao Boss
            }
            Destroy(gameObject); // Destrói o bullet
        }
        // Se colidir com um inimigo (usando Collision ao invés de Trigger)
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Bullet atingiu inimigo!");
            Destroy(collision.gameObject); // Destrói o inimigo
            Destroy(gameObject); // Destrói o bullet
        }

        else if (collision.gameObject.CompareTag("wall"))
        {
            // Não faz nada, ignora colisão com o player
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
