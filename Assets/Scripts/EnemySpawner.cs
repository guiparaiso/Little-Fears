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
    [SerializeField] string idleAnimationState = "walking_down"; // Animação padrão quando parado

    [Header("Visual Effects")]
    [SerializeField] bool useVisualEffects = false;   // Ative apenas se quiser efeito no sprite
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] float pulseSpeed = 2f;
    [SerializeField] Color glowColor = Color.gray;
    [SerializeField] float minAlpha = 0.3f;
    [SerializeField] float maxAlpha = 1f;

    [Header("Spawn Indicator")]
    [SerializeField] bool showSpawnIndicator = true;
    [SerializeField] GameObject spawnIndicatorPrefab;
    [SerializeField] float indicatorDuration = 1f;
    [SerializeField] Color indicatorColor = new Color(1f, 0.5f, 0f, 0.5f); // Laranja translúcido
    [SerializeField] float indicatorSize = 1f;

    [Header("Spawn Warning Effect")]
    [SerializeField] bool showWarningEffect = true;
    [SerializeField] float warningDuration = 1f; // Tempo do aviso antes de spawnar
    [SerializeField] Color warningColor = new Color(1f, 0f, 0f, 0.8f); // Vermelho brilhante
    [SerializeField] float warningPulseSpeed = 8f; // Velocidade da pulsação
    [SerializeField] float warningGlowIntensity = 2f; // Intensidade do brilho
    
    [Header("Spawn Flash Effect")]
    [SerializeField] bool flashOnSpawn = true;
    [SerializeField] float flashDuration = 0.3f;
    [SerializeField] Color flashColor = Color.white;

    [Header("Movement Settings - Flee from Player")]
    [SerializeField] bool canMove = false;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float fleeDistance = 5f; // Distância mínima para manter do player
    [SerializeField] float detectionRange = 10f; // Distância para começar a fugir

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

    private void Start()
    {
        // Importante: Remove a tag Enemy do spawner para não ser atacado

        if (col != null)
        {
            // Evita que o spawner bloqueie fisicamente inimigos
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log("Spawner Collider definido como isTrigger para não bloquear inimigos.");
            }
        }

        // Configura o Animator
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Garante que o Collider2D está configurado corretamente
        col = GetComponent<Collider2D>();
        if (col != null)
        {
            // Se for BoxCollider2D, garante que está centralizado e com tamanho adequado
            BoxCollider2D boxCol = col as BoxCollider2D;
            if (boxCol != null)
            {
                // Centraliza o offset
                boxCol.offset = Vector2.zero;
                Debug.Log($"Spawner Collider configurado - Offset: {boxCol.offset}, Size: {boxCol.size}");
            }
        }
        else
        {
            Debug.LogWarning("Spawner não tem Collider2D! Adicione um BoxCollider2D ou CircleCollider2D.");
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

        // Configura NavMeshAgent se canMove está ativado
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
            
            // Inicia parado com animação stopping
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
        spawnTimer += Time.deltaTime;

        // Inicia aviso antes de spawnar
        if (showWarningEffect && !isWarning && spawnTimer >= (spawnInterval - warningDuration) && currentEnemyCount < maxEnemies)
        {
            isWarning = true;
            warningTimer = 0f;
        }

        if (spawnTimer >= spawnInterval && currentEnemyCount < maxEnemies)
        {
            SpawnEnemy();
            spawnTimer = 0f;
            isWarning = false;
        }

        currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (useVisualEffects || isWarning)
        {
            UpdateGlowEffect();
        }

        // Animações separadas do movimento
        if (canMove && agent != null)
        {
            FleeFromPlayer();
            UpdateAnimations();
        }
        else
        {
            // Se não está se movendo, usa animação idle padrão
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
        
        // Desativa todas as animações de movimento
        animator.SetBool("stopped", false);
        animator.SetBool("walking_left", false);
        animator.SetBool("walking_right", false);
        animator.SetBool("walking_up", false);
        animator.SetBool("walking_down", false);
        
        // Se tem nome de animação idle, usa SetBool, senão força walking_down
        if (!string.IsNullOrEmpty(idleAnimationState))
        {
            animator.SetBool(idleAnimationState, true);
        }
        else
        {
            // Default: usa walking_down como idle
            animator.SetBool("walking_down", true);
        }
    }

    void UpdateGlowEffect()
    {
        if (spriteRenderer == null) return;
        
        // Se não tem visual effects habilitado mas está em warning, precisa do spriteRenderer
        if (!useVisualEffects && !isWarning) return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        
        if (isWarning)
        {
            // Efeito de aviso: pulsação rápida e intensa
            warningTimer += Time.deltaTime;
            float warningPulse = Mathf.Sin(warningTimer * warningPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
            Color warning = Color.Lerp(originalColor, warningColor, warningPulse * warningGlowIntensity);
            spriteRenderer.color = warning;
        }
        else if (isFlashing)
        {
            // Durante o flash, usa cor branca brilhante
            Color flash = flashColor;
            flash.a = 1f;
            spriteRenderer.color = flash;
        }
        else if (useVisualEffects)
        {
            // Pulsa normalmente
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

        // Se o player está muito perto, foge!
        if (distanceToPlayer < detectionRange)
        {
            // Calcula direção oposta ao player
            Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
            
            // Calcula posição de fuga
            Vector3 fleePosition = transform.position + directionAwayFromPlayer * fleeDistance;
            
            // Define destino no NavMesh
            agent.SetDestination(fleePosition);
        }
        else
        {
            // Se o player está longe, para de se mover
            agent.ResetPath();
        }
    }

    void UpdateAnimations()
    {
        if (animator == null || agent == null) return;

        // Usa a velocidade do NavMeshAgent para controlar animações
        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;

        // Verifica se está realmente parado (sem caminho ativo E velocidade baixa)
        bool isStopped = !agent.hasPath || agent.remainingDistance <= agent.stoppingDistance || velocity.magnitude < 0.1f;

        // Se está parado, usa animação idle
        if (isStopped)
        {
            UpdateIdleAnimation();
            return;
        }

        // Se está em movimento, desativa stopping e idle
        animator.SetBool("stopped", false);
        if (!string.IsNullOrEmpty(idleAnimationState))
        {
            animator.SetBool(idleAnimationState, false);
        }

        // Previne animação diagonal - prioriza o eixo com maior movimento
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

        // Movimento horizontal
        if (moveHorizontal < -0.1f) // Esquerda
        {
            animator.SetBool("walking_left", true);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        else if (moveHorizontal > 0.1f) // Direita
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", true);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        // Movimento vertical
        else if (moveVertical > 0.1f) // Cima
        {
            animator.SetBool("walking_up", true);
            animator.SetBool("walking_down", false);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
        }
        else if (moveVertical < -0.1f) // Baixo
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

        // Mostra indicador antes de spawnar (sem bloquear)
        if (showSpawnIndicator)
        {
            StartCoroutine(ShowSpawnIndicatorAsync(spawnPosition));
        }

        // Cria o inimigo imediatamente (não espera o indicador)
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null && player != null)
        {
            var targetField = enemyScript.GetType().GetField("target", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
            
            if (targetField != null)
                targetField.SetValue(enemyScript, player);
        }

        if (flashOnSpawn)
        {
            isFlashing = true;
            flashTimer = 0f;
        }
        
        // Reseta cor após spawn
        if (spriteRenderer != null && showWarningEffect)
        {
            spriteRenderer.color = originalColor;
        }

        currentEnemyCount++;
        Debug.Log($"Slave spawned! Total: {currentEnemyCount}");
    }

    System.Collections.IEnumerator ShowSpawnIndicatorAsync(Vector3 position)
    {
        GameObject indicator;

        // Usa prefab customizado se fornecido, senão cria um círculo brilhante
        if (spawnIndicatorPrefab != null)
        {
            indicator = Instantiate(spawnIndicatorPrefab, position, Quaternion.identity);
        }
        else
        {
            // Cria indicador visual brilhante
            indicator = new GameObject("SpawnIndicator");
            indicator.transform.position = position;
            indicator.transform.localScale = Vector3.one * indicatorSize;
            
            var sr = indicator.AddComponent<SpriteRenderer>();
            // Tenta carregar sprite de círculo, se não existir usa um quad
            Sprite circleSprite = Resources.Load<Sprite>("circle");
            if (circleSprite == null)
            {
                // Cria um sprite simples (quadrado)
                sr.sprite = null;
                sr.drawMode = SpriteDrawMode.Simple;
            }
            else
            {
                sr.sprite = circleSprite;
            }
            sr.color = warningColor;
            sr.sortingOrder = 10; // Fica acima de tudo
        }

        SpriteRenderer spriteRend = indicator.GetComponent<SpriteRenderer>();
        
        // Efeito de pulsação e crescimento
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * indicatorSize;
        float elapsed = 0f;

        while (elapsed < indicatorDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / indicatorDuration;
            
            // Pulsação rápida (brilho)
            float pulse = Mathf.Sin(elapsed * warningPulseSpeed * Mathf.PI) * 0.5f + 0.5f;
            
            // Escala crescente com pulsação
            float scaleMultiplier = 1f + (pulse * 0.3f);
            indicator.transform.localScale = Vector3.Lerp(startScale, endScale, t) * scaleMultiplier;
            
            // Cor pulsante que vai sumindo
            if (spriteRend != null)
            {
                Color glowColor = Color.Lerp(warningColor, Color.white, pulse * 0.5f);
                glowColor.a = Mathf.Lerp(warningColor.a, 0f, t * t); // Fade out acelerado
                spriteRend.color = glowColor;
            }
            
            yield return null;
        }

        Destroy(indicator);
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
            // Desenha área de detecção
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Desenha distância de fuga
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, fleeDistance);
        }
    }
}