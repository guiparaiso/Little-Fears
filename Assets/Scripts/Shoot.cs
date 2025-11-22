using UnityEngine;

public class Shoot : MonoBehaviour
{

    [Header("Bullet")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private PlayerMovement playerMovement;

    void Start()
    {
        // Se não foi atribuído no Inspector, tenta pegar automaticamente
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire();
        }
    }

    public void Fire()
    {

        if (bulletPrefab == null || playerMovement == null)
        {
            Debug.LogWarning("BulletPrefab ou PlayerMovement não configurado!");
            return;
        }

        // Cria o bullet um pouco à frente do jogador para evitar colisão
        Vector3 spawnOffset = (Vector3)playerMovement.lastDirection * 0.5f;
        GameObject newBullet = Instantiate(bulletPrefab, transform.position + spawnOffset, Quaternion.identity);
        
        // Adiciona Rigidbody2D ao bullet se não tiver
        Rigidbody2D bulletRb = newBullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null)
        {
            bulletRb = newBullet.AddComponent<Rigidbody2D>();
        }
        
        // Configurações do Rigidbody2D para movimento consistente
        bulletRb.gravityScale = 0; // Sem gravidade
        bulletRb.constraints = RigidbodyConstraints2D.FreezeRotation; // Não rotaciona
        bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Melhor detecção

        // Define a velocidade do bullet na direção que o jogador está virado
        bulletRb.linearVelocity = playerMovement.lastDirection * bulletSpeed;
        
        // Destrói o bullet após 3 segundos
        Destroy(newBullet, 3f);
    }
}
