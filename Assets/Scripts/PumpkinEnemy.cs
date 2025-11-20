using UnityEngine;
using UnityEngine.AI;

public class PumpkinEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;
    
    [Header("Movement")]
    [SerializeField] float speed = 2f;
    [SerializeField] float keepDistance = 4f; // Mantém distância do player para cuspir
    [SerializeField] float tooCloseDistance = 2f; // Se chegar muito perto, recua
    
    [Header("Poison Spit Attack")]
    [SerializeField] GameObject poisonPrefab; // Prefab do projétil de veneno
    [SerializeField] Transform spitPoint; // Ponto de onde sai o veneno (boca)
    [SerializeField] float spitRange = 6f; // Alcance do ataque
    [SerializeField] float spitCooldown = 2f; // Tempo entre cuspes
    [SerializeField] float spitSpeed = 8f; // Velocidade do projétil
    [SerializeField] float poisonDamage = 15f; // Dano de medo do veneno
    
    [Header("Health & Explosion")]
    [SerializeField] float maxHealth = 50f;
    [SerializeField] float explosionHealthThreshold = 15f; // Explode quando HP < 15
    [SerializeField] float explosionRadius = 3f;
    [SerializeField] float explosionDamage = 30f;
    [SerializeField] GameObject explosionEffectPrefab; // Efeito visual da explosão
    [SerializeField] Color explosionWarningColor = Color.red;
    [SerializeField] float warningDuration = 2f; // Tempo piscando antes de explodir (aumentado para 2s)
    [SerializeField] bool autoExplodeAfterTime = false; // DESATIVADO - só explode com dano
    [SerializeField] float timeUntilAutoExplode = 15f; // Tempo até explodir sozinha
    
    [Header("Fear Settings")]
    [SerializeField] ScaryBarUI scaryBar;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private NavMeshAgent agent;
    private float currentHealth;
    private float spitTimer = 0f;
    private bool isExploding = false;
    private float explosionTimer = 0f;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float lifeTimer = 0f; // Tempo de vida total
    private Vector3 originalScale; // Guarda escala original
    
    private enum PumpkinState { Chasing, KeepingDistance, Attacking, Exploding }
    private PumpkinState currentState = PumpkinState.Chasing;

    private void Start()
    {
        // Inicializa health
        currentHealth = maxHealth;
        
        // Guarda escala original
        originalScale = transform.localScale;
        
        // Configura NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = keepDistance;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.radius = 0.25f;
        agent.avoidancePriority = Random.Range(40, 60);
        
        // Pega SpriteRenderer para efeito visual
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Encontra player automaticamente
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
        
        // Encontra ScaryBar
        if (scaryBar == null)
        {
            scaryBar = FindObjectOfType<ScaryBarUI>();
        }
        
        // Se não tem spitPoint definido, usa própria posição
        if (spitPoint == null)
        {
            spitPoint = transform;
        }
    }

    private void Update()
    {
        if (target == null || agent == null) return;
        
        // Verifica se deve explodir por HP baixo
        if (currentHealth <= explosionHealthThreshold && !isExploding)
        {
            StartExplosion();
        }
        
        // Se está explodindo, só conta timer
        if (isExploding)
        {
            UpdateExplosion();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        spitTimer += Time.deltaTime;
        
        // Máquina de estados
        switch (currentState)
        {
            case PumpkinState.Chasing:
                // Se está longe, aproxima
                agent.isStopped = false;
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target.position);
                }
                agent.stoppingDistance = keepDistance;
                
                // Se chegou na distância ideal, mantém distância
                if (distanceToTarget <= spitRange)
                {
                    currentState = PumpkinState.KeepingDistance;
                }
                break;
                
            case PumpkinState.KeepingDistance:
                // Mantém distância e circula o player
                
                // Se muito perto, recua
                if (distanceToTarget < tooCloseDistance)
                {
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    Vector3 retreatPos = transform.position + directionAway * keepDistance;
                    
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(retreatPos);
                    }
                }
                // Se muito longe, aproxima
                else if (distanceToTarget > spitRange)
                {
                    currentState = PumpkinState.Chasing;
                }
                // Na distância certa, tenta cuspir
                else if (spitTimer >= spitCooldown)
                {
                    currentState = PumpkinState.Attacking;
                }
                break;
                
            case PumpkinState.Attacking:
                // Para e cospe veneno
                agent.isStopped = true;
                SpitPoison();
                spitTimer = 0f;
                currentState = PumpkinState.KeepingDistance;
                agent.isStopped = false;
                break;
        }
        
        // Atualiza animações
        UpdateAnimations();
    }
    
    void SpitPoison()
    {
        if (poisonPrefab == null)
        {
            Debug.LogWarning("PumpkinEnemy: Poison Prefab não configurado!");
            return;
        }
        
        // Cria projétil de veneno
        GameObject poison = Instantiate(poisonPrefab, spitPoint.position, Quaternion.identity);
        
        // Calcula direção para o player
        Vector3 direction = (target.position - spitPoint.position).normalized;
        
        // Adiciona Rigidbody2D se não tiver
        Rigidbody2D rb = poison.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = poison.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }
        
        // Aplica velocidade
        rb.linearVelocity = direction * spitSpeed;
        
        // Adiciona script de dano ao projétil
        PoisonProjectile projectile = poison.GetComponent<PoisonProjectile>();
        if (projectile == null)
        {
            projectile = poison.AddComponent<PoisonProjectile>();
        }
        projectile.damage = poisonDamage;
        projectile.scaryBar = scaryBar;
        
        Debug.Log("Abóbora cuspiu veneno!");
    }
    
    void StartExplosion()
    {
        isExploding = true;
        explosionTimer = 0f;
        agent.isStopped = true;
        
        // Volta para escala original (se estava menor)
        transform.localScale = originalScale;
        
        // Ativa animação de parado/stopping se tiver
        if (animator != null)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            animator.SetBool("stopping", true);
        }
        
        Debug.Log("⚠️ ABÓBORA VAI EXPLODIR! Fique longe! ⚠️");
    }
    
    void UpdateExplosion()
    {
        explosionTimer += Time.deltaTime;
        
        // Para de se mover
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        // Efeito de piscar MAIS VISÍVEL
        if (spriteRenderer != null)
        {
            float pulseSpeed = 15f; // Pisca mais rápido
            float pulse = Mathf.Sin(explosionTimer * pulseSpeed * Mathf.PI);
            
            // Alterna entre vermelho brilhante e cor original
            if (pulse > 0)
            {
                spriteRenderer.color = explosionWarningColor; // Vermelho total
            }
            else
            {
                spriteRenderer.color = originalColor; // Cor normal
            }
        }
        
        // Efeito de tremor/shake
        float shakeAmount = 0.1f;
        Vector3 randomOffset = (Vector3)(Random.insideUnitCircle * shakeAmount * Time.deltaTime);
        transform.position += randomOffset;
        
        // Aumenta de tamanho gradualmente (fica inchando) - MANTENDO O EFEITO!
        float scaleGrow = 1f + (explosionTimer / warningDuration) * 0.5f; // Aumentado para crescer até 50% maior
        transform.localScale = originalScale * scaleGrow;
        
        // Explode após warning
        if (explosionTimer >= warningDuration)
        {
            Explode();
        }
    }
    
    void Explode()
    {
        // Cria efeito visual
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Causa dano em área
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (scaryBar != null)
                {
                    scaryBar.AddFear(explosionDamage);
                    Debug.Log("Explosão causou medo!");
                }
            }
        }
        
        Debug.Log("BOOM! Abóbora explodiu!");
        
        // Destrói a abóbora
        Destroy(gameObject);
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // Feedback visual de dano
        if (spriteRenderer != null && !isExploding)
        {
            StartCoroutine(DamageFlash());
        }
        
        Debug.Log($"💥 Abóbora levou {damage} de dano. HP: {currentHealth}/{maxHealth}");
        
        // Aviso quando está perto de explodir
        if (currentHealth <= explosionHealthThreshold && currentHealth > 0 && !isExploding)
        {
            Debug.Log("⚠️ CUIDADO! HP crítico - abóbora vai explodir! ⚠️");
        }
        
        // Explode se HP chegou a 0 ou abaixo
        if (currentHealth <= 0 && !isExploding)
        {
            Debug.Log("💀 HP zerou! Explodindo imediatamente!");
            Explode();
        }
    }
    
    // Método opcional para dano por colisão - configure as tags no Unity depois
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Se for projétil do player, tenta pegar o dano dele
        if (other.gameObject.layer != gameObject.layer && !other.CompareTag("Player") && !other.CompareTag("Enemy"))
        {
            // Tenta pegar o script BulletScript
            var bulletScript = other.GetComponent<BulletScript>();
            if (bulletScript != null)
            {
                TakeDamage(15f);
                Debug.Log("Abóbora foi atingida por projétil!");
                return;
            }
            
            // Detecta por nome do objeto como fallback
            if (other.name.Contains("Bullet") || other.name.Contains("Projectile") || other.name.Contains("bullet"))
            {
                TakeDamage(15f);
                Debug.Log("Abóbora foi atingida por projétil!");
            }
        }
    }
    
    System.Collections.IEnumerator DamageFlash()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;
        
        // Se está parado ou explodindo
        if (velocity.magnitude < 0.1f || isExploding)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            return;
        }
        
        // Previne diagonal
        if (Mathf.Abs(moveHorizontal) > 0.1f && Mathf.Abs(moveVertical) > 0.1f)
        {
            if (Mathf.Abs(moveHorizontal) >= Mathf.Abs(moveVertical))
                moveVertical = 0;
            else
                moveHorizontal = 0;
        }
        
        // Animações direcionais
        if (moveHorizontal < -0.1f)
        {
            animator.SetBool("walking_left", true);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        else if (moveHorizontal > 0.1f)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", true);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        else if (moveVertical > 0.1f)
        {
            animator.SetBool("walking_up", true);
            animator.SetBool("walking_down", false);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
        }
        else if (moveVertical < -0.1f)
        {
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", true);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Desenha alcance do cuspe
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spitRange);
        
        // Desenha distância de manutenção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, keepDistance);
        
        // Desenha distância "muito perto"
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);
        
        // Desenha raio de explosão
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
