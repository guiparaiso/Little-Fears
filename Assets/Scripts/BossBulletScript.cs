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
        //Debug.Log("BossBullet colidiu com: " + other.name + " - Tag: " + other.tag);
        // Se colidir com o player
        if (other.CompareTag("Player"))
        {
            // Aqui você pode adicionar dano ao player
            Destroy(gameObject); // Destrói apenas o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o Boss
        else if (!other.CompareTag("Enemy") && !other.CompareTag("Boss") && !other.CompareTag("Player") && !other.CompareTag("Arrow") && !other.name.Contains("CursedGroundArea(Clone)") && !other.name.Contains("limit") && !other.CompareTag("fase"))
        {
            Debug.Log("BossBullet colidiu com: " + other.name + " - Tag: " + other.tag);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log("BossBullet colidiu com: " + collision.gameObject.name + " - Tag: " + collision.gameObject.tag);
        // Se colidir com o player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Aqui você pode adicionar dano ao player
            Destroy(gameObject); // Destrói apenas o bullet
        }
        // Se colidir com qualquer outro objeto que não seja o Boss
        else if (!collision.gameObject.CompareTag("Enemy") && !collision.gameObject.CompareTag("Boss") && !collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Arrow") && !collision.gameObject.name.Contains("CursedGroundArea(Clone)") && !collision.gameObject.name.Contains("limit") && !collision.gameObject.CompareTag("fase"))
        {
            Debug.Log("BossBullet colidiu (collision) com: " + collision.gameObject.name + " - Tag: " + collision.gameObject.tag);
            Destroy(gameObject); // Destrói apenas o bullet
        }
    }
}
