using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] float spawnInterval = 2f;
    [SerializeField] int maxEnemies = 10;
    
    [Header("Spawn Area")]
    [SerializeField] Vector2 spawnAreaMin;
    [SerializeField] Vector2 spawnAreaMax;
    [SerializeField] bool useRandomSpawn = true;
    
    [Header("Target")]
    [SerializeField] Transform player;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] string idleAnimationState = "walking_down";
    
    [Header("Visual Effects")]
    [SerializeField] bool useVisualEffects = false;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float pulseSpeed = 2f;
    [SerializeField] Color glowColor = Color.gray;
    [SerializeField] float minAlpha = 0.3f;
    [SerializeField] float maxAlpha = 1f;
    
    [Header("Spawn Indicator")]
    [SerializeField] bool showSpawnIndicator = true;
    [SerializeField] GameObject spawnIndicatorPrefab;
    [SerializeField] float indicatorDuration = 1f;
    [SerializeField] Color indicatorColor = new Color(1f, 0.5f, 0f, 0.5f);
    [SerializeField] float indicatorSize = 1f;
    
    [Header("Spawn Warning Effect")]
    [SerializeField] bool showWarningEffect = true;
    [SerializeField] float warningDuration = 1f;
    [SerializeField] Color warningColor = new Color(1f, 0f, 0f, 0.8f);
    [SerializeField] float warningPulseSpeed = 8f;
    [SerializeField] float warningGlowIntensity = 2f;
    
    [Header("Spawn Flash Effect")]
    [SerializeField] bool flashOnSpawn = true;
    [SerializeField] float flashDuration = 0.3f;
    [SerializeField] Color flashColor = Color.white;
    
    [Header("Movement Settings - Flee from Player")]
    [SerializeField] bool canMove = false;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float fleeDistance = 5f;
    [SerializeField] float detectionRange = 10f;
    
    private float spawnTimer = 0f;
    private int currentEnemyCount = 0;
    private float pulseTimer = 0f;
    private bool isFlashing = false;
    private float flashTimer = 0f;
    private bool isWarning = false;
    private float warningTimer = 0f;
    private Color originalColor;
    private Collider2D col;
    private NavMeshAgent agent;
    private bool isDestroyed = false; // FLAG para controlar destruição
    private System.Collections.Generic.List<GameObject> activeIndicators = new System.Collections.Generic.List<GameObject>(); // Lista de portais ativos

    private void Start()
    {
        col = GetComponent<Collider2D>();
        
        if (col != null)
        {
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log("Spawner Collider definido como isTrigger para não bloquear inimigos.");
            }
            
            BoxCollider2D boxCol = col as BoxCollider2D;
            if (boxCol != null)
            {
                boxCol.offset = Vector2.zero;
                Debug.Log($"Spawner Collider configurado - Offset: {boxCol.offset}, Size: {boxCol.size}");
            }
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("Player não encontrado!");
        }

        if ((useVisualEffects || showWarningEffect) && spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null && (useVisualEffects || showWarningEffect))
        {
            originalColor = spriteRenderer.color;
        }

        if (canMove)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
            agent.speed = moveSpeed;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            
            if (animator != null)
            {
                animator.SetBool("stopped", true);
                animator.SetBool("walking_left", false);
                animator.SetBool("walking_right", false);
                animator.SetBool("walking_up", false);
                animator.SetBool("walking_down", false);
            }
        }
    }

    private void Update()
    {
        if (isDestroyed) return; // Não processa se foi destruído

        spawnTimer += Time.deltaTime;

        if (showWarningEffect && !isWarning && spawnTimer >= (spawnInterval - warningDuration) && currentEnemyCount < maxEnemies)
        {
            isWarning = true;
            warningTimer = 0f;
        }

        if (spawnTimer >= spawnInterval && currentEnemyCount < maxEnemies)
        {
            if (player != null && player.gameObject != null && player.gameObject.activeInHierarchy)
            {
                SpawnEnemy();
            }
            spawnTimer = 0f;
            isWarning = false;
        }

        currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (useVisualEffects || isWarning)
        {
            UpdateGlowEffect();
        }

        if (canMove && agent != null)
        {
            FleeFromPlayer();
            UpdateAnimations();
        }
        else
        {
            UpdateIdleAnimation();
        }

        if (isFlashing)
        {
            UpdateFlashEffect();
        }

        if (isWarning)
        {
            UpdateWarningEffect();
        }
    }
    
    void UpdateIdleAnimation()
    {
        if (animator == null) return;
        
        animator.SetBool("stopped", false);
        animator.SetBool("walking_left", false);
        animator.SetBool("walking_right", false);
        animator.SetBool("walking_up", false);
        animator.SetBool("walking_down", false);
        
        if (!string.IsNullOrEmpty(idleAnimationState))
        {
            animator.SetBool(idleAnimationState, true);
        }
        else
        {
            animator.SetBool("walking_down", true);
        }
    }

    void UpdateGlowEffect()
    {
        if (spriteRenderer == null) return;
        if (!useVisualEffects && !isWarning) return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        
        if (isWarning)
        {
            warningTimer += Time.deltaTime;
            float warningPulse = Mathf.Sin(warningTimer * warningPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
            Color warning = Color.Lerp(originalColor, warningColor, warningPulse * warningGlowIntensity);
            spriteRenderer.color = warning;
        }
        else if (isFlashing)
        {
            Color flash = flashColor;
            flash.a = 1f;
            spriteRenderer.color = flash;
        }
        else if (useVisualEffects)
        {
            float alpha = Mathf.Lerp(Mathf.Max(minAlpha, 0.3f), maxAlpha, (Mathf.Sin(pulseTimer) + 1f) / 2f);
            Color newColor = glowColor;
            newColor.a = alpha;
            spriteRenderer.color = newColor;
        }
    }
    
    void UpdateWarningEffect()
    {
        warningTimer += Time.deltaTime;
    }

    void UpdateFlashEffect()
    {
        flashTimer += Time.deltaTime;
        
        if (flashTimer >= flashDuration)
        {
            isFlashing = false;
            flashTimer = 0f;
        }
    }

    void FleeFromPlayer()
    {
        if (player == null || agent == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < detectionRange)
        {
            Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
            Vector3 fleePosition = transform.position + directionAwayFromPlayer * fleeDistance;
            agent.SetDestination(fleePosition);
        }
        else
        {
            agent.ResetPath();
        }
    }

    void UpdateAnimations()
    {
        if (animator == null || agent == null) return;

        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;

        bool isStopped = !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance || velocity.magnitude < 0.1f;

        if (isStopped)
        {
            UpdateIdleAnimation();
            return;
        }

        animator.SetBool("stopped", false);
        if (!string.IsNullOrEmpty(idleAnimationState))
        {
            animator.SetBool(idleAnimationState, false);
        }

        if (Mathf.Abs(moveHorizontal) > 0.1f && Mathf.Abs(moveVertical) > 0.1f)
        {
            if (Mathf.Abs(moveHorizontal) >= Mathf.Abs(moveVertical))
            {
                moveVertical = 0;
            }
            else
            {
                moveHorizontal = 0;
            }
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

    void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab não foi atribuído!");
            return;
        }

        Vector2 spawnPosition;
        if (useRandomSpawn)
        {
            spawnPosition = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );
        }
        else
        {
            spawnPosition = transform.position;
        }

        StartCoroutine(ShowSpawnIndicatorAndSpawnAsync(spawnPosition));
    }

    System.Collections.IEnumerator ShowSpawnIndicatorAndSpawnAsync(Vector3 position)
    {
        GameObject indicator = null;

        // Cria o indicador
        if (spawnIndicatorPrefab != null)
        {
            indicator = Instantiate(spawnIndicatorPrefab, position, Quaternion.identity);
        }
        else
        {
            indicator = new GameObject("SpawnIndicator");
            indicator.transform.position = position;
            indicator.transform.localScale = Vector3.one * indicatorSize;
            
            var sr = indicator.AddComponent<SpriteRenderer>();
            Sprite circleSprite = Resources.Load<Sprite>("circle");
            if (circleSprite != null)
            {
                sr.sprite = circleSprite;
            }
            sr.color = warningColor;
            sr.sortingOrder = 10;
        }

        SpriteRenderer spriteRend = indicator.GetComponent<SpriteRenderer>();
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * indicatorSize * 1.5f;
        float elapsed = 0f;

        // Loop de animação do portal
        while (elapsed < indicatorDuration)
        {
            // VERIFICAÇÃO CRÍTICA: Se o spawner foi destruído, cancela tudo
            if (this == null || isDestroyed || gameObject == null || !gameObject.activeInHierarchy)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
                yield break; // Para a corrotina imediatamente
            }

            // Se o player morreu, cancela o spawn
            if (player == null || player.gameObject == null || !player.gameObject.activeInHierarchy)
            {
                if (indicator != null)
                {
                    Destroy(indicator);
                }
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = elapsed / indicatorDuration;
            float pulse = Mathf.Sin(elapsed * warningPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
            float scaleMultiplier = 1f + (pulse * 0.3f);
            
            if (indicator != null)
            {
                indicator.transform.localScale = Vector3.Lerp(startScale, endScale, t) * scaleMultiplier;
            }

            if (spriteRend != null)
            {
                Color glowColor = Color.Lerp(warningColor, Color.white, pulse * 0.5f);
                glowColor.a = Mathf.Lerp(warningColor.a, 0f, t * t);
                spriteRend.color = glowColor;
            }

            yield return null;
        }

        // Última verificação antes de spawnar
        if (this == null || isDestroyed || gameObject == null || !gameObject.activeInHierarchy)
        {
            if (indicator != null)
            {
                Destroy(indicator);
            }
            yield break;
        }

        // Destrói o indicador
        if (indicator != null)
        {
            Destroy(indicator);
        }

        // Spawna o inimigo
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        
        if (enemyScript != null && player != null)
        {
            var targetField = enemyScript.GetType().GetField("target", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            if (targetField != null)
                targetField.SetValue(enemyScript, player);
        }

        if (spriteRenderer != null && showWarningEffect)
        {
            spriteRenderer.color = originalColor;
        }

        currentEnemyCount++;
        //Debug.Log($"Slave spawned! Total: {currentEnemyCount}");

        if (flashOnSpawn)
        {
            isFlashing = true;
            flashTimer = 0f;
        }
    }

    // ÚNICO OnDestroy - cancela todas as corrotinas e animações
    private void OnDestroy()
    {
        isDestroyed = true; // Marca como destruído
        StopAllCoroutines(); // Para todas as corrotinas
        
        // Para todas as animações
        if (animator != null)
        {
            animator.enabled = false; // Desativa o animator
        }
        
        // Para o movimento se existir
        if (agent != null)
        {
            agent.enabled = false;
        }
    }

    // Auto-gerenciamento: Spawner pode ser destruído por bullets
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detecta bullets do player pelo script BulletScript
        if (other.GetComponent<BulletScript>() != null)
        {
            Destroy(other.gameObject); // Destrói o bullet
            Destroy(gameObject); // Destrói o spawner
            Debug.Log("🏭 Spawner foi destruído por bullet do player!");
        }
    }

    // Auto-gerenciamento: Spawner pode ser destruído por bullets (versão Collision)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Detecta bullets do player pelo script BulletScript
        if (collision.gameObject.GetComponent<BulletScript>() != null)
        {
            Destroy(collision.gameObject); // Destrói o bullet
            Destroy(gameObject); // Destrói o spawner
            Debug.Log("🏭 Spawner foi destruído por bullet do player (Collision)!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (useRandomSpawn)
        {
            Gizmos.color = Color.red;
            Vector3 center = new Vector3(
                (spawnAreaMin.x + spawnAreaMax.x) / 2,
                (spawnAreaMin.y + spawnAreaMax.y) / 2,
                0
            );
            Vector3 size = new Vector3(
                spawnAreaMax.x - spawnAreaMin.x,
                spawnAreaMax.y - spawnAreaMin.y,
                0
            );
            Gizmos.DrawWireCube(center, size);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }

        if (canMove)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fleeDistance);
        }
    }
}