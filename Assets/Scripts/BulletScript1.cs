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
        // Se colidir com um inimigo
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Bullet atingiu inimigo!");
            Destroy(other.gameObject); // Destrói o inimigo
            Destroy(gameObject); // Destrói o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o player
        else if (!other.CompareTag("Player"))
        {
            Debug.Log("Bullet colidiu com: " + other.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se colidir com um inimigo (usando Collision ao invés de Trigger)
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Bullet atingiu inimigo!");
            Destroy(collision.gameObject); // Destrói o inimigo
            Destroy(gameObject); // Destrói o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o player
        else if (!collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Bullet colidiu com: " + collision.gameObject.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }
}
