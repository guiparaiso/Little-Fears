using UnityEngine;

public class BossBulletScript : MonoBehaviour
{
    void Start()
    {
        // Ignora colisão com o próprio Boss
        GameObject boss = GameObject.FindGameObjectWithTag("Boss");
        if (boss != null)
        {
            Collider2D bossCollider = boss.GetComponent<Collider2D>();
            Collider2D bulletCollider = GetComponent<Collider2D>();
            if (bossCollider != null && bulletCollider != null)
            {
                Physics2D.IgnoreCollision(bulletCollider, bossCollider);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se colidir com o player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Boss bullet atingiu o player!");
            // Aqui você pode adicionar dano ao player
            Destroy(gameObject); // Destrói apenas o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o Boss
        else if (!other.CompareTag("Boss"))
        {
            Debug.Log("Boss bullet colidiu com: " + other.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se colidir com o player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Boss bullet atingiu o player!");
            // Aqui você pode adicionar dano ao player
            Destroy(gameObject); // Destrói apenas o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o Boss
        else if (!collision.gameObject.CompareTag("Boss"))
        {
            Debug.Log("Boss bullet colidiu com: " + collision.gameObject.name);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }
}
