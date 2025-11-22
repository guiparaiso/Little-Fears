using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    private int currentHealth;
    private bool isDead = false;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bossBulletPrefab;
    [SerializeField] private float bulletSpeed = 5f;
    [SerializeField] private float shootInterval = 1f;
    [SerializeField] private Transform player;

    [Header("Float Settings")]
    [SerializeField] private float floatAmplitude = 0.5f; // Amplitude do movimento vertical
    [SerializeField] private float floatSpeed = 2f; // Velocidade da flutuação

    [Header("Damage Feedback")]
    [SerializeField] private Color damageColor = Color.red; // Cor ao receber dano
    [SerializeField] private float flashDuration = 0.2f; // Duração do piscar

    private float shootTimer;
    private Vector3 startPosition;
    private float floatTimer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        shootTimer = shootInterval; // Começa pronto para atirar

        // Obtém o SpriteRenderer para o efeito de piscar
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            Debug.LogWarning("Boss: SpriteRenderer não encontrado! Efeito de piscar não funcionará.");
        }

        // Tenta encontrar o player automaticamente se não foi configurado
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("Boss: Player não encontrado! Configure o Player no Inspector.");
            }
        }
    }

    void Update()
    {
        // Se o Boss está morto, não faz nada
        if (isDead)
            return;

        // Movimento de flutuação
        FloatMovement();

        // Sistema de disparo
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            ShootAtPlayer();
            shootTimer = shootInterval; // Reseta o timer
        }
    }

    void FloatMovement()
    {
        // Incrementa o timer de flutuação
        floatTimer += Time.deltaTime * floatSpeed;

        // Calcula a nova posição X usando seno para movimento suave
        float newX = startPosition.x + Mathf.Sin(floatTimer) * floatAmplitude;

        // Aplica a nova posição mantendo Y e Z constantes
        transform.position = new Vector3(newX, startPosition.y, startPosition.z);
    }

    void ShootAtPlayer()
    {
        if (bossBulletPrefab == null)
        {
            Debug.LogWarning("Boss: BossBulletPrefab não configurado!");
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("Boss: Player não encontrado!");
            return;
        }

        // Cria o bullet na posição do boss
        GameObject newBullet = Instantiate(bossBulletPrefab, transform.position, Quaternion.identity);

        // Calcula a direção para o player
        Vector2 direction = (player.position - transform.position).normalized;
        
        Debug.Log($"🎯 Boss atirando! Posição Boss: {transform.position}, Posição Player: {player.position}, Direção: {direction}");

        // Configura o Rigidbody2D do bullet
        Rigidbody2D bulletRb = newBullet.GetComponent<Rigidbody2D>();
        if (bulletRb == null)
        {
            bulletRb = newBullet.AddComponent<Rigidbody2D>();
            Debug.Log("⚠️ Boss Bullet não tinha Rigidbody2D - foi adicionado!");
        }

        bulletRb.gravityScale = 0;
        bulletRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        bulletRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Define a velocidade na direção do player
        bulletRb.linearVelocity = direction * bulletSpeed;
        
        Debug.Log($"💨 Velocidade do bullet: {bulletRb.linearVelocity}, Speed: {bulletSpeed}");

        // Destrói o bullet após 5 segundos
        Destroy(newBullet, 5f);
    }

    // Método público para o Boss receber dano
    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"Boss recebeu {damage} de dano! Vida restante: {currentHealth}/{maxHealth}");

        // Efeito de piscar
        StartCoroutine(FlashDamage());

        // Verifica se o Boss morreu
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Corrotina para efeito de piscar ao receber dano
    private IEnumerator FlashDamage()
    {
        if (spriteRenderer == null)
            yield break;

        // Muda para a cor de dano
        spriteRenderer.color = damageColor;

        // Espera a duração do flash
        yield return new WaitForSeconds(flashDuration);

        // Volta para a cor original
        spriteRenderer.color = originalColor;
    }

    // Método chamado quando o Boss morre
    private void Die()
    {
        isDead = true;
        Debug.Log("Boss derrotado!");

        // Aqui você pode adicionar efeitos de morte, animação, etc.
        // Por exemplo:
        // Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Destrói o Boss
        Destroy(gameObject);
    }

    // Auto-gerenciamento: Detecta bullets do player
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detecta bullets do player pelo script BulletScript
        if (other.GetComponent<BulletScript>() != null)
        {
            TakeDamage(1); // Causa 1 de dano ao Boss
            Destroy(other.gameObject); // Destrói o bullet
            Debug.Log("Boss foi atingido por bullet do player!");
        }
    }

    // Auto-gerenciamento: Detecta bullets do player (versão Collision)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Detecta bullets do player pelo script BulletScript
        if (collision.gameObject.GetComponent<BulletScript>() != null)
        {
            TakeDamage(1); // Causa 1 de dano ao Boss
            Destroy(collision.gameObject); // Destrói o bullet
            Debug.Log("Boss foi atingido por bullet do player (Collision)!");
        }
    }
}
