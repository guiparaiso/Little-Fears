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
    
    [Header("Movement")]
    [SerializeField] float speed = 3.5f;
    [SerializeField] float ragingSpeed = 5f; // Velocidade quando está machucado
    [SerializeField] float chaseDistance = 15f;
    
    [Header("Teleport Attack")]
    [SerializeField] bool canTeleport = true;
    [SerializeField] float teleportCooldown = 4f; // Reduzido de 5 para 4 (teleporta mais rápido)
    [SerializeField] float teleportDistance = 2.5f; // Reduzido - teleporta MAIS PERTO
    [SerializeField] float teleportMinDistance = 1f; // Reduzido - pode ficar mais perto
    [SerializeField] float teleportWarningDuration = 0.7f; // Reduzido de 1 para 0.7s (menos tempo de aviso)
    [SerializeField] GameObject teleportEffectPrefab; // Efeito de fumaça/sombra
    [SerializeField] float teleportEffectDuration = 0.5f;
    [SerializeField] Color teleportFlashColor = new Color(0.5f, 0f, 0.5f, 1f); // Roxo escuro
    [SerializeField] Color teleportWarningColor = Color.red; // Vermelho de aviso
    
    [Header("Cursed Ground Attack")]
    [SerializeField] bool canCurseGround = true;
    [SerializeField] float curseCooldown = 6f; // Reduzido de 8 para 6 (mais áreas amaldiçoadas)
    [SerializeField] int maxCursedAreas = 3; // NOVO: Máximo de áreas simultâneas
    [SerializeField] GameObject cursedGroundPrefab; // Prefab da área amaldiçoada
    [SerializeField] float curseRadius = 2.5f; // Aumentado de 2 para 2.5 (área maior)
    [SerializeField] float curseDuration = 5f; // Aumentado de 4 para 5 (dura mais tempo)
    [SerializeField] float curseDamage = 15f; // Aumentado de 10 para 15 (mais dano)
    [SerializeField] float curseDamageInterval = 0.5f; // Intervalo de dano (0.5s = 2x por segundo)
    
    [Header("Combat")]
    [SerializeField] float attackRange = 1.5f;
    [SerializeField] float meleeDamage = 20f;
    [SerializeField] float meleeAttackCooldown = 1.2f; // Cooldown entre ataques corpo-a-corpo
    [SerializeField] bool isInvulnerableDuringTeleport = true; // Não pode ser atingido durante teleporte
    
    [Header("Fear Settings")]
    [SerializeField] ScaryBarUI scaryBar;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    private float teleportTimer = 0f;
    private float curseTimer = 0f;
    private float meleeAttackTimer = 0f;
    private bool isTeleporting = false;
    private bool isCasting = false;
    private int currentCursedAreas = 0;
    
    // Sistema de fases
    private bool isEnraged = false; // Fica furioso quando HP < 50%
    private bool isDesperado = false; // Fica desesperado quando HP < 25%
    
    private enum ReaperState { Idle, Chasing, Attacking, Teleporting, Cursing }
    private ReaperState currentState = ReaperState.Chasing;

    private void Start()
    {
        // Configura NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = 0f; // Precisa chegar perto para colidir
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.radius = 0.25f;
        agent.avoidancePriority = Random.Range(30, 50);
        
        // Pega SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Encontra player
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
        
        // Inicializa HP
        currentHealth = maxHealth;
        
        // Randomiza timers iniciais para não atacar todos ao mesmo tempo
        teleportTimer = Random.Range(teleportCooldown * 0.3f, teleportCooldown * 0.7f);
        curseTimer = Random.Range(curseCooldown * 0.3f, curseCooldown * 0.7f);
        meleeAttackTimer = meleeAttackCooldown; // Pode atacar imediatamente
    }

    private void Update()
    {
        if (target == null || agent == null) return;
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // Atualiza sistema de fases baseado em HP
        float healthPercent = currentHealth / maxHealth;
        if (healthPercent <= 0.25f && !isDesperado)
        {
            isDesperado = true;
            isEnraged = true;
            agent.speed = ragingSpeed * 1.2f; // Ainda mais rápido!
            Debug.Log("💀💀💀 REAPER ESTÁ DESESPERADO! (HP < 25%)");
        }
        else if (healthPercent <= 0.5f && !isEnraged)
        {
            isEnraged = true;
            agent.speed = ragingSpeed;
            Debug.Log("💀💀 REAPER ESTÁ FURIOSO! (HP < 50%)");
        }
        
        // Atualiza cooldowns (mais rápido quando enraged)
        float cooldownMultiplier = isEnraged ? 0.7f : 1f;
        teleportTimer += Time.deltaTime;
        curseTimer += Time.deltaTime;
        meleeAttackTimer += Time.deltaTime;
        
        // Máquina de estados
        switch (currentState)
        {
            case ReaperState.Idle:
                // REMOVIDO: Agora sempre persegue
                currentState = ReaperState.Chasing;
                break;
                
            case ReaperState.Chasing:
                // SEMPRE persegue o player
                agent.isStopped = false;
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target.position);
                }
                
                // IA melhorada: Escolhe ataques de forma mais inteligente
                
                // COMBO ESPECIAL: Teleporte + Área quando desesperado
                if (isDesperado && canTeleport && canCurseGround && 
                    teleportTimer >= teleportCooldown * cooldownMultiplier && 
                    curseTimer >= curseCooldown * cooldownMultiplier && 
                    currentCursedAreas < maxCursedAreas)
                {
                    StartCoroutine(PerformTeleportCurseCombo());
                }
                // Prioridade 1: Área amaldiçoada (pode criar em qualquer distância)
                else if (canCurseGround && curseTimer >= curseCooldown * cooldownMultiplier && 
                        currentCursedAreas < maxCursedAreas)
                {
                    // Chance maior de usar quando enraged
                    if (!isEnraged || Random.value < 0.7f)
                    {
                        StartCoroutine(PerformCurseGround());
                    }
                }
                // Prioridade 2: Teleporte para se aproximar e atacar
                else if (canTeleport && teleportTimer >= teleportCooldown * cooldownMultiplier)
                {
                    // Teleporta para perto do player (objetivo: ataque corpo-a-corpo)
                    if (distanceToTarget > attackRange * 2f || 
                        (isEnraged && Random.value < 0.4f) || 
                        (isDesperado && Random.value < 0.6f))
                    {
                        StartCoroutine(PerformTeleport());
                    }
                }
                // Prioridade 3: Continua perseguindo até colidir com o player
                // O ataque acontece via OnTriggerEnter2D quando colidir
                break;
                break;
                
            case ReaperState.Teleporting:
            case ReaperState.Cursing:
                // Estados controlados pelas corrotinas
                agent.isStopped = true;
                break;
        }
        
        // Atualiza animações
        UpdateAnimations();
    }
    
    IEnumerator PerformTeleport()
    {
        if (isTeleporting) yield break;
        
        isTeleporting = true;
        currentState = ReaperState.Teleporting;
        teleportTimer = 0f;
        
        agent.isStopped = true;
        
        // AVISO VISUAL: Pisca vermelho rapidamente
        if (spriteRenderer != null && animator != null)
        {
            float warningElapsed = 0f;
            
            // Animação de warning
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
                
                // Pisca rápido entre vermelho e branco
                float pulse = Mathf.Sin(warningElapsed * 20f * Mathf.PI);
                if (pulse > 0)
                {
                    spriteRenderer.color = teleportWarningColor; // Vermelho
                }
                else
                {
                    spriteRenderer.color = Color.white; // Branco
                }
                
                // Treme no lugar
                Vector3 shake = Random.insideUnitCircle * 0.05f;
                transform.position += (Vector3)shake;
                
                yield return null;
            }
        }
        
        Debug.Log("⚠️ Reaper vai teleportar!");
        
        // Efeito de desaparecimento
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Flash visual roxo
        if (spriteRenderer != null)
        {
            spriteRenderer.color = teleportFlashColor;
        }
        
        yield return new WaitForSeconds(0.3f);
        
        // Calcula posição de teleporte (perto do player, mas não muito)
        Vector3 directionToPlayer = (target.position - transform.position).normalized;
        Vector3 teleportPos = target.position - directionToPlayer * Random.Range(teleportMinDistance, teleportDistance);
        
        // Verifica se a posição é válida no NavMesh
        UnityEngine.AI.NavMeshHit hit;
        if (UnityEngine.AI.NavMesh.SamplePosition(teleportPos, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas))
        {
            // Teleporta
            if (agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
            else
            {
                transform.position = hit.position;
            }
            
            // Efeito de aparecimento
            if (teleportEffectPrefab != null)
            {
                Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
            }
            
            Debug.Log("💀 Reaper teleportou!");
        }
        
        // Restaura cor
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        yield return new WaitForSeconds(teleportEffectDuration);
        
        // Garante que volta a se mover
        isTeleporting = false;
        agent.isStopped = false;
        currentState = ReaperState.Chasing;
        
        // Força atualização do destino
        if (agent.isOnNavMesh && target != null)
        {
            agent.SetDestination(target.position);
        }
    }
    
    IEnumerator PerformCurseGround()
    {
        if (isCasting) yield break;
        
        isCasting = true;
        currentState = ReaperState.Cursing;
        curseTimer = 0f;
        
        agent.isStopped = true;
        
        // Animação de parado + casting
        if (animator != null)
        {
            animator.SetBool("stopping", true);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            // animator.SetTrigger("CastCurse"); // Se tiver animação específica
        }
        
        yield return new WaitForSeconds(0.5f); // Tempo de cast
        
        // Cria área amaldiçoada na posição do player
        Vector3 cursePosition = target.position;
        
        GameObject cursedArea;
        if (cursedGroundPrefab != null)
        {
            cursedArea = Instantiate(cursedGroundPrefab, cursePosition, Quaternion.identity);
        }
        else
        {
            // Cria área simples se não tem prefab
            cursedArea = new GameObject("CursedGround");
            cursedArea.transform.position = cursePosition;
            
            // Adiciona visual básico
            SpriteRenderer sr = cursedArea.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.3f, 0f, 0.3f, 0.5f); // Roxo transparente
            sr.sortingOrder = -1; // Atrás de tudo
            
            // Cria sprite circular básico (deixa sem sprite, só a cor)
            // O visual será apenas um quadrado colorido - melhor criar um prefab depois
        }
        
        // Adiciona componente de dano
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
        curseScript.reaper = this; // Passa referência para decrementar contador
        
        currentCursedAreas++; // Incrementa contador
        
        Debug.Log($"💀 Reaper criou chão amaldiçoado! Áreas ativas: {currentCursedAreas}/{maxCursedAreas}");
        
        yield return new WaitForSeconds(0.5f);
        
        // Garante que volta a se mover
        isCasting = false;
        agent.isStopped = false;
        currentState = ReaperState.Chasing;
        
        // Força atualização do destino
        if (agent.isOnNavMesh && target != null)
        {
            agent.SetDestination(target.position);
        }
    }
    
    // NOVO: Combo de teleporte + área amaldiçoada
    IEnumerator PerformTeleportCurseCombo()
    {
        Debug.Log("💀💀💀 REAPER USA COMBO ESPECIAL: TELEPORTE + ÁREA!");
        
        // Teleporta primeiro
        yield return StartCoroutine(PerformTeleport());
        
        // Espera um pouco
        yield return new WaitForSeconds(0.3f);
        
        // Cria área amaldiçoada imediatamente após teleporte
        yield return StartCoroutine(PerformCurseGround());
    }
    
    // NOVO: Flash visual ao atacar
    IEnumerator AttackFlash()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalCol = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        
        yield return new WaitForSeconds(0.1f);
        
        spriteRenderer.color = originalCol;
    }
    
    // NOVO: Sistema de dano (para balas do player)
    public void TakeDamage(float damage)
    {
        // Não recebe dano se está teleportando
        if (isInvulnerableDuringTeleport && isTeleporting)
        {
            Debug.Log("💀 Reaper é invulnerável durante teleporte!");
            return;
        }
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"💀 Reaper recebeu {damage} de dano! HP: {currentHealth}/{maxHealth}");
        
        // Efeito visual
        if (spriteRenderer != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        // Morre se HP chegou a 0
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
        Debug.Log("💀☠️ REAPER FOI DERROTADO!");
        
        // Efeito de morte (pode adicionar partículas, som, etc)
        if (teleportEffectPrefab != null)
        {
            Instantiate(teleportEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Destrói todas as áreas amaldiçoadas
        CursedGroundArea[] areas = FindObjectsOfType<CursedGroundArea>();
        foreach (CursedGroundArea area in areas)
        {
            if (area.reaper == this)
            {
                Destroy(area.gameObject);
            }
        }
        
        Destroy(gameObject);
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;
        
        // Se está parado (teleportando, cursando ou atacando)
        if (velocity.magnitude < 0.1f || isTeleporting || isCasting)
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            animator.SetBool("stopping", true);
            return;
        }
        
        animator.SetBool("stopping", false);
        
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
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ataque corpo-a-corpo quando colidir com o player
        if (other.CompareTag("Player") && meleeAttackTimer >= meleeAttackCooldown)
        {
            if (scaryBar != null)
            {
                scaryBar.AddFear(meleeDamage);
                meleeAttackTimer = 0f; // Reseta cooldown
                Debug.Log("💀 Reaper atacou corpo-a-corpo!");
                
                // Efeito visual do ataque
                if (spriteRenderer != null)
                {
                    StartCoroutine(AttackFlash());
                }
            }
        }
        
        // Detecta balas do player
        BulletScript bullet = other.GetComponent<BulletScript>();
        if (bullet != null)
        {
            float damage = 10f; // Dano padrão da bala
            TakeDamage(damage);
            Destroy(other.gameObject); // Destrói a bala
        }
    }
    
    // OnGUI para mostrar barra de HP (simples)
    private void OnGUI()
    {
        if (!showHealthBar || target == null) return;
        
        // Só mostra se está perto do player
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > 10f) return;
        
        // Converte posição world para screen
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        
        if (screenPos.z > 0) // Está na frente da câmera
        {
            // Inverte Y porque GUI usa coordenadas diferentes
            screenPos.y = Screen.height - screenPos.y;
            
            // Desenha barra de fundo (preto)
            GUI.backgroundColor = Color.black;
            GUI.Box(new Rect(screenPos.x - 30, screenPos.y - 5, 60, 8), "");
            
            // Desenha barra de HP (vermelho -> amarelo -> verde baseado em HP)
            float healthPercent = currentHealth / maxHealth;
            Color barColor;
            if (healthPercent > 0.5f)
                barColor = Color.Lerp(Color.yellow, Color.green, (healthPercent - 0.5f) * 2f);
            else
                barColor = Color.Lerp(Color.red, Color.yellow, healthPercent * 2f);
            
            GUI.backgroundColor = barColor;
            GUI.Box(new Rect(screenPos.x - 28, screenPos.y - 3, 56 * healthPercent, 4), "");
        }
    }
    
    // Método público para decrementar contador de áreas
    public void OnCursedAreaDestroyed()
    {
        currentCursedAreas--;
        if (currentCursedAreas < 0) currentCursedAreas = 0;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Desenha alcance de perseguição
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);
        
        // Desenha alcance de ataque
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Desenha alcance de teleporte
        if (target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(target.position, teleportDistance);
        }
    }
}
