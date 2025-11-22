using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ReaperEnemy : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;
    
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] bool showHealthBar = true;
    private float currentHealth;
    
    [Header("Sound")]
    [SerializeField] AudioClip ambientSound; // Som que toca enquanto está vivo
    [SerializeField] AudioClip deathSound; // Som ao morrer
    [SerializeField] float ambientVolume = 0.5f; // Volume do som ambiente (0-1)
    [SerializeField] bool fadeInAmbient = true; // Fade in ao spawnar
    [SerializeField] float fadeInDuration = 1f; // Tempo do fade in
    [SerializeField] bool fadeOutOnDeath = true; // Fade out ao morrer
    [SerializeField] float fadeOutDuration = 0.5f; // Tempo do fade out
    [SerializeField] float maxAudibleDistance = 20f; // Distância máxima para ouvir (3D sound)
    private AudioSource ambientAudioSource;
    
    [Header("Movement")]
    [SerializeField] float speed = 3.5f;
    [SerializeField] float ragingSpeed = 5f;
    [SerializeField] float chaseDistance = 15f;
    
    [Header("Teleport Attack")]
    [SerializeField] bool canTeleport = true;
    [SerializeField] float teleportCooldown = 4f;
    [SerializeField] float teleportDistance = 2.5f;
    [SerializeField] float teleportMinDistance = 1f;
    [SerializeField] float teleportWarningDuration = 0.7f;
    [SerializeField] GameObject teleportEffectPrefab;
    [SerializeField] float teleportEffectDuration = 0.5f;
    [SerializeField] Color teleportFlashColor = new Color(0.5f, 0f, 0.5f, 1f);
    [SerializeField] Color teleportWarningColor = Color.red;
    
    [Header("Cursed Ground Attack")]
    [SerializeField] bool canCurseGround = true;
    [SerializeField] float curseCooldown = 6f;
    [SerializeField] int maxCursedAreas = 3;
    [SerializeField] GameObject cursedGroundPrefab;
    [SerializeField] float curseRadius = 2.5f;
    [SerializeField] float curseDuration = 5f;
    [SerializeField] float curseDamage = 15f;
    [SerializeField] float curseDamageInterval = 0.5f;
    
    [Header("Combat")]
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float meleeDamage = 20f;
    [SerializeField] float meleeAttackCooldown = 1.2f;
    [SerializeField] float retreatDistance = 2f;
    [SerializeField] float attackDuration = 0.3f;
    [SerializeField] float retreatSpeed = 5f;
    [SerializeField] bool isInvulnerableDuringTeleport = true;
    [SerializeField] bool useAttackAnimation = true;
    [SerializeField] GameObject slashEffectPrefab;
    [SerializeField] Vector3 slashOffset = Vector3.zero;
    [SerializeField] float slashScale = 2f;
    [SerializeField] float slashDuration = 0.5f;
    
    [Header("Fear Settings")]
    [SerializeField] ScaryBarUI scaryBar;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    private float teleportTimer = 0f;
    private float curseTimer = 0f;
    private float meleeAttackTimer = 0f;
    private float attackTimer = 0f;
    private Vector3 retreatTarget;
    private bool isTeleporting = false;
    private bool isCasting = false;
    private int currentCursedAreas = 0;
    private bool isDying = false;
    
    private bool isEnraged = false;
    private bool isDesperado = false;
    
    private enum ReaperState { Idle, Chasing, Attacking, Retreating, Teleporting, Cursing }
    private ReaperState currentState = ReaperState.Chasing;

    public string objectID;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = 0.1f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.radius = 0.25f;
        agent.avoidancePriority = Random.Range(30, 50);
        
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
        
        currentHealth = maxHealth;
        
        teleportTimer = Random.Range(teleportCooldown * 0.3f, teleportCooldown * 0.7f);
        curseTimer = Random.Range(curseCooldown * 0.3f, curseCooldown * 0.7f);
        meleeAttackTimer = meleeAttackCooldown;

        if (GameManager.instance != null)
        {
            if (GameManager.instance.IsObjectRegistered(objectID))
            {
                Destroy(gameObject);
                return;
            }
        }
        
        // Configura som ambiente
        SetupAmbientSound();
    }

    private void SetupAmbientSound()
    {
        if (ambientSound == null)
        {
            Debug.LogWarning("⚠️ Reaper: ambientSound não configurado!");
            return;
        }
        
        // Adiciona AudioSource ao GameObject
        ambientAudioSource = gameObject.AddComponent<AudioSource>();
        ambientAudioSource.clip = ambientSound;
        ambientAudioSource.loop = true; // Loop infinito
        ambientAudioSource.playOnAwake = false;
        ambientAudioSource.spatialBlend = 1f; // 3D sound (0 = 2D, 1 = 3D)
        ambientAudioSource.minDistance = 5f; // Distância mínima (volume máximo)
        ambientAudioSource.maxDistance = maxAudibleDistance; // Distância máxima
        ambientAudioSource.rolloffMode = AudioRolloffMode.Linear; // Decai linearmente
        
        // Inicia com volume 0 se vai fazer fade in
        if (fadeInAmbient)
        {
            ambientAudioSource.volume = 0f;
            StartCoroutine(FadeInAmbientSound());
        }
        else
        {
            ambientAudioSource.volume = ambientVolume;
        }
        
        // Começa a tocar
        ambientAudioSource.Play();
        Debug.Log("🔊 Reaper: Som ambiente iniciado!");
    }

    IEnumerator FadeInAmbientSound()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;
            ambientAudioSource.volume = Mathf.Lerp(0f, ambientVolume, t);
            yield return null;
        }
        
        ambientAudioSource.volume = ambientVolume;
    }

    IEnumerator FadeOutAmbientSound()
    {
        if (ambientAudioSource == null) yield break;
        
        float startVolume = ambientAudioSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            ambientAudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }
        
        ambientAudioSource.volume = 0f;
        ambientAudioSource.Stop();
    }

    private void Update()
    {
        if (isDying) return; // Para tudo se está morrendo
        if (target == null || agent == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Atualiza sistema de fases baseado em HP
        float healthPercent = currentHealth / maxHealth;
        if (healthPercent <= 0.25f && !isDesperado)
        {
            isDesperado = true;
            isEnraged = true;
            agent.speed = ragingSpeed * 1.2f;
            
            // Aumenta volume do som quando desesperado
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = ambientVolume * 1.3f;
            }
            
            Debug.Log("💀💀💀 REAPER ESTÁ DESESPERADO! (HP < 25%)");
        }
        else if (healthPercent <= 0.5f && !isEnraged)
        {
            isEnraged = true;
            agent.speed = ragingSpeed;
            
            // Aumenta levemente o volume quando enraged
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = ambientVolume * 1.15f;
            }
            
            Debug.Log("💀💀 REAPER ESTÁ FURIOSO! (HP < 50%)");
        }
        
        float cooldownMultiplier = isEnraged ? 0.7f : 1f;
        teleportTimer += Time.deltaTime;
        curseTimer += Time.deltaTime;
        meleeAttackTimer += Time.deltaTime;
        
        switch (currentState)
        {
            case ReaperState.Idle:
                currentState = ReaperState.Chasing;
                break;
                
            case ReaperState.Chasing:
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = 0.1f;
                    agent.SetDestination(target.position);
                }
                
                if (isDesperado && canTeleport && canCurseGround && 
                    teleportTimer >= teleportCooldown * cooldownMultiplier && 
                    curseTimer >= curseCooldown * cooldownMultiplier && 
                    currentCursedAreas < maxCursedAreas)
                {
                    StartCoroutine(PerformTeleportCurseCombo());
                }
                else if (canCurseGround && curseTimer >= curseCooldown * cooldownMultiplier && 
                        currentCursedAreas < maxCursedAreas)
                {
                    if (!isEnraged || Random.value < 0.7f)
                    {
                        StartCoroutine(PerformCurseGround());
                    }
                }
                else if (canTeleport && teleportTimer >= teleportCooldown * cooldownMultiplier)
                {
                    if (distanceToTarget > attackRange * 2f || 
                        (isEnraged && Random.value < 0.4f) || 
                        (isDesperado && Random.value < 0.6f))
                    {
                        StartCoroutine(PerformTeleport());
                    }
                }
                break;
                
            case ReaperState.Attacking:
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.velocity = Vector3.zero;
                }
                attackTimer += Time.deltaTime;
                
                if (attackTimer >= attackDuration)
                {
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    retreatTarget = transform.position + directionAway * retreatDistance;
                    
                    currentState = ReaperState.Retreating;
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                    }
                }
                break;
                
            case ReaperState.Retreating:
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = retreatSpeed;
                    agent.stoppingDistance = 0.2f;
                    agent.SetDestination(retreatTarget);
                }

                if (!agent.pathPending && agent.remainingDistance <= 0.5f)
                {
                    agent.speed = isEnraged ? ragingSpeed : speed;
                    currentState = ReaperState.Chasing;
                }
                break;
                
            case ReaperState.Teleporting:
            case ReaperState.Cursing:
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                }
                break;
        }
        
        UpdateAnimations();
    }
    
    IEnumerator PerformTeleport()
    {
        if (isTeleporting) yield break;
        
        isTeleporting = true;
        currentState = ReaperState.Teleporting;
        teleportTimer = 0f;
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        if (spriteRenderer != null && animator != null)
        {
            float warningElapsed = 0f;
            
            if (animator != null)
            {
                animator.SetBool("stopping", true);
                animator.SetBool("walking_left", false);
                animator.SetBool("walking_right", false);
                animator.SetBool("walking_up", false);
                animator.SetBool("walking_down", false);
            }
            
            while (warningElapsed < teleportWarningDuration)
            {
                warningElapsed += Time.deltaTime;
                
                float pulse = Mathf.Sin(warningElapsed * 20f * Mathf.PI);
                if (pulse > 0)
                {
                    spriteRenderer.color = teleportWarningColor;
                }
                else
                {
                    spriteRenderer.color = Color.white;
                }
                
                Vector3 shake = Random.insideUnitCircle * 0.05f;
                transform.position += (Vector3)shake;
                
                yield return null;
            }
        }
        
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = teleportFlashColor;
        }
        
        yield return new WaitForSeconds(0.3f);
        
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
        float randomDistance = Random.Range(teleportMinDistance, teleportDistance);
        Vector3 teleportPos = target.position + (Vector3)randomDirection * randomDistance;
        
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(teleportPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            if (agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
            else
            {
                transform.position = hit.position;
            }
            
            if (teleportEffectPrefab != null)
            {
                Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
            }
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        yield return new WaitForSeconds(teleportEffectDuration);
        
        isTeleporting = false;
        currentState = ReaperState.Chasing;
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }
    
    IEnumerator PerformCurseGround()
    {
        if (isCasting) yield break;
        
        isCasting = true;
        currentState = ReaperState.Cursing;
        curseTimer = 0f;
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        
        if (animator != null)
        {
            animator.SetBool("stopping", true);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        Vector3 cursePosition = target.position;
        
        GameObject cursedArea;
        if (cursedGroundPrefab != null)
        {
            cursedArea = Instantiate(cursedGroundPrefab, cursePosition, Quaternion.identity);
        }
        else
        {
            cursedArea = new GameObject("CursedGround");
            cursedArea.transform.position = cursePosition;
            
            SpriteRenderer sr = cursedArea.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0f, 0.3f, 0.5f);
            sr.sortingOrder = -1;
        }
        
        CursedGroundArea curseScript = cursedArea.GetComponent<CursedGroundArea>();
        if (curseScript == null)
        {
            curseScript = cursedArea.AddComponent<CursedGroundArea>();
        }
        
        curseScript.radius = curseRadius;
        curseScript.duration = curseDuration;
        curseScript.damagePerTick = curseDamage * curseDamageInterval;
        curseScript.damageInterval = curseDamageInterval;
        curseScript.scaryBar = scaryBar;
        curseScript.reaper = this;
        
        currentCursedAreas++;
        
        yield return new WaitForSeconds(0.5f);
        
        isCasting = false;
        currentState = ReaperState.Chasing;
        
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
    }
    
    IEnumerator PerformTeleportCurseCombo()
    {
        yield return StartCoroutine(PerformTeleport());
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(PerformCurseGround());
    }
    
    IEnumerator AttackFlash()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalCol = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        
        yield return new WaitForSeconds(0.1f);
        
        spriteRenderer.color = originalCol;
    }
    
    public void TakeDamage(float damage)
    {
        if (isInvulnerableDuringTeleport && isTeleporting)
        {
            Debug.Log("💀 Reaper é invulnerável durante teleporte!");
            return;
        }
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"💀 Reaper recebeu {damage} de dano! HP: {currentHealth}/{maxHealth}");
        
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalCol = spriteRenderer.color;
        
        for (int i = 0; i < 2; i++)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.05f);
            spriteRenderer.color = originalCol;
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    private void Die()
    {
        if (isDying) return;
        isDying = true;
        
        Debug.Log("💀☠️ REAPER FOI DERROTADO!");

        if (GameManager.instance != null)
        {
            GameManager.instance.RegisterObject(objectID);
        }

        // Para o som ambiente com fade out
        if (fadeOutOnDeath && ambientAudioSource != null)
        {
            StartCoroutine(FadeOutAmbientSound());
        }
        else if (ambientAudioSource != null)
        {
            ambientAudioSource.Stop();
        }
        
        // Toca som de morte
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }
        
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }
        
        CursedGroundArea[] areas = FindObjectsOfType<CursedGroundArea>();
        foreach (CursedGroundArea area in areas)
        {
            if (area.reaper == this)
            {
                Destroy(area.gameObject);
            }
        }
        
        // Esconde visualmente
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Desativa componentes
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        
        // Destrói após delay (tempo do som ou fade out)
        float destroyDelay = fadeOutOnDeath ? fadeOutDuration : 0.5f;
        if (deathSound != null && deathSound.length > destroyDelay)
        {
            destroyDelay = deathSound.length;
        }
        
        Destroy(gameObject, destroyDelay);
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;
        
        if (isTeleporting || isCasting)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            animator.SetBool("stopped", true);
            return;
        }
        
        if (currentState == ReaperState.Attacking || currentState == ReaperState.Retreating)
        {
            return;
        }
        
        if (velocity.magnitude < 0.1f)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            animator.SetBool("stopped", true);
            return;
        }
        
        animator.SetBool("stopped", false);
        
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
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDying) return;
        
        if (other.CompareTag("Player") && meleeAttackTimer >= meleeAttackCooldown && currentState == ReaperState.Chasing)
        {
            currentState = ReaperState.Attacking;
            attackTimer = 0f;
            meleeAttackTimer = 0f;
            
            if (scaryBar != null)
            {
                scaryBar.AddFear(meleeDamage);
                Debug.Log("💀 Reaper atacou corpo-a-corpo!");
                
                if (spriteRenderer != null)
                {
                    StartCoroutine(AttackFlash());
                }
                
                if (useAttackAnimation && slashEffectPrefab != null)
                {
                    Vector3 slashPosition = other.transform.position + slashOffset;
                    GameObject slash = Instantiate(slashEffectPrefab, slashPosition, Quaternion.identity);
                    slash.transform.position = new Vector3(slashPosition.x, slashPosition.y, other.transform.position.z - 0.1f);
                    slash.transform.localScale = Vector3.one * slashScale;
                    Destroy(slash, slashDuration);
                }
            }
        }
        
        if (other.GetComponent<BulletScript>() != null)
        {
            float damage = 10f;
            TakeDamage(damage);
            Destroy(other.gameObject);
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying) return;
        
        if (collision.gameObject.GetComponent<BulletScript>() != null)
        {
            float damage = 10f;
            TakeDamage(damage);
            Destroy(collision.gameObject);
        }
    }
    
    public void OnCursedAreaDestroyed()
    {
        currentCursedAreas--;
        if (currentCursedAreas < 0) currentCursedAreas = 0;
    }
}