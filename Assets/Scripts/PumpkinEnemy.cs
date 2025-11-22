using UnityEngine;
using UnityEngine.AI;

public class PumpkinEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;
    
    [Header("Movement")]
    [SerializeField] float speed = 2f;
    [SerializeField] float keepDistance = 4f;
    [SerializeField] float tooCloseDistance = 2f;
    
    [Header("Poison Spit Attack")]
    [SerializeField] GameObject poisonPrefab;
    [SerializeField] Transform spitPoint;
    [SerializeField] float spitRange = 6f;
    [SerializeField] float spitCooldown = 2f;
    [SerializeField] float spitSpeed = 12f;
    [SerializeField] float poisonDamage = 15f;
    
    [Header("Health & Explosion")]
    [SerializeField] AudioClip explosionSound;
    [SerializeField] float maxHealth = 50f;
    [SerializeField] float explosionHealthThreshold = 15f;
    [SerializeField] float explosionRadius = 3f;
    [SerializeField] float explosionDamage = 30f;
    [SerializeField] GameObject explosionEffectPrefab;
    [SerializeField] Color explosionWarningColor = Color.red;
    [SerializeField] float warningDuration = 5f;
    [SerializeField] bool autoExplodeOnStart = false;
    [SerializeField] bool autoExplodeAfterTime = false;
    [SerializeField] float timeUntilAutoExplode = 15f;
    
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
    private float lifeTimer = 0f;
    private Vector3 originalScale;
    private bool hasSpawnedEffect = false;
    
    private enum PumpkinState { Chasing, KeepingDistance, Attacking, Exploding }
    private PumpkinState currentState = PumpkinState.Chasing;

    private void Start()
    {
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = keepDistance;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.radius = 0.25f;
        agent.avoidancePriority = Random.Range(40, 60);
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
        
        if (scaryBar == null)
        {
            scaryBar = FindObjectOfType<ScaryBarUI>();
        }
        
        if (spitPoint == null)
        {
            spitPoint = transform;
        }
        
        if (autoExplodeOnStart)
        {
            StartExplosion();
            Debug.Log("🎃 Abóbora vai explodir em 5 segundos!");
        }
    }

    private void Update()
    {
        if (target == null || agent == null) return;
        
        if (currentHealth <= explosionHealthThreshold && !isExploding)
        {
            StartExplosion();
        }
        
        if (isExploding)
        {
            UpdateExplosion();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        spitTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case PumpkinState.Chasing:
                agent.isStopped = false;
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target.position);
                }
                agent.stoppingDistance = keepDistance;
                
                if (distanceToTarget <= spitRange)
                {
                    currentState = PumpkinState.KeepingDistance;
                }
                break;
                
            case PumpkinState.KeepingDistance:
                if (distanceToTarget < tooCloseDistance)
                {
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    Vector3 retreatPos = transform.position + directionAway * keepDistance;
                    
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(retreatPos);
                    }
                }
                else if (distanceToTarget > spitRange)
                {
                    currentState = PumpkinState.Chasing;
                }
                else if (spitTimer >= spitCooldown)
                {
                    currentState = PumpkinState.Attacking;
                }
                break;
                
            case PumpkinState.Attacking:
                agent.isStopped = true;
                SpitPoison();
                spitTimer = 0f;
                currentState = PumpkinState.KeepingDistance;
                agent.isStopped = false;
                break;
        }
        
        UpdateAnimations();
    }
    
    void SpitPoison()
    {
        if (poisonPrefab == null)
        {
            Debug.LogWarning("PumpkinEnemy: Poison Prefab não configurado!");
            return;
        }
        
        Vector3 direction = (target.position - spitPoint.position).normalized;
        
        float angle = 0f;
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x < 0)
                angle = -90f;
            else
                angle = 90f;
        }
        else
        {
            if (direction.y > 0)
                angle = 180f;
            else
                angle = 0f;
        }
        
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        GameObject poison = Instantiate(poisonPrefab, spitPoint.position, rotation);
        
        Rigidbody2D rb = poison.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = poison.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        if (poison.GetComponent<Collider2D>() == null)
        {
            CircleCollider2D collider = poison.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
        }
        
        rb.linearVelocity = direction * spitSpeed;
        
        PoisonProjectile projectile = poison.GetComponent<PoisonProjectile>();
        if (projectile == null)
        {
            projectile = poison.AddComponent<PoisonProjectile>();
        }
        projectile.damage = poisonDamage;
        projectile.scaryBar = scaryBar;
    }
    
    void StartExplosion()
    {
        isExploding = true;
        explosionTimer = 0f;
        agent.isStopped = true;
        
        transform.localScale = originalScale;
        
        if (animator != null)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            animator.SetBool("stopped", true);
        }
        
        Debug.Log("⚠️ ABÓBORA VAI EXPLODIR! Fique longe! ⚠️");
    }
    
    void UpdateExplosion()
    {
        explosionTimer += Time.deltaTime;
        
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        
        if (spriteRenderer != null)
        {
            float pulseSpeed = 15f;
            float pulse = Mathf.Sin(explosionTimer * pulseSpeed * Mathf.PI);
            
            if (pulse > 0)
            {
                spriteRenderer.color = explosionWarningColor;
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }
        
        float shakeAmount = 0.1f;
        Vector3 randomOffset = (Vector3)(Random.insideUnitCircle * shakeAmount * Time.deltaTime);
        transform.position += randomOffset;
        
        float scaleGrow = 1f + (explosionTimer / warningDuration) * 0.5f;
        transform.localScale = originalScale * scaleGrow;
        
        if (explosionTimer >= warningDuration)
        {
            Explode();
        }
    }
    
    void Explode()
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2.6f);
        }
        
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
        
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
        Destroy(gameObject);
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        if (spriteRenderer != null && !isExploding)
        {
            StartCoroutine(DamageFlash());
        }
        
        Debug.Log($"💥 Abóbora levou {damage} de dano. HP: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= explosionHealthThreshold && currentHealth > 0 && !isExploding)
        {
            Debug.Log("⚠️ CUIDADO! HP crítico - abóbora vai explodir! ⚠️");
        }
        
        if (currentHealth <= 0 && !isExploding)
        {
            Debug.Log("💀 HP zerou! Explodindo imediatamente!");
            Explode();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<BulletScript>() != null)
        {
            TakeDamage(15f);
            Destroy(other.gameObject);
            Debug.Log("💥 Abóbora foi atingida por bullet do player!");
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
        
        if (velocity.magnitude < 0.1f || isExploding)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            return;
        }
        
        if (Mathf.Abs(moveHorizontal) > 0.1f && Mathf.Abs(moveVertical) > 0.1f)
        {
            if (Mathf.Abs(moveHorizontal) >= Mathf.Abs(moveVertical))
                moveVertical = 0;
            else
                moveHorizontal = 0;
        }
        
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

    // ======================================
    // MÉTODOS PÚBLICOS PARA BARRA DE VIDA
    // ======================================
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spitRange);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, keepDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, tooCloseDistance);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}