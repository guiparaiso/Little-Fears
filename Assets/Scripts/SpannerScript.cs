using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SpannerScript : MonoBehaviour
{
    [Header("Death Effect")]
    [SerializeField] AudioClip deathSound;
    [SerializeField] Transform target;
    [SerializeField] float speed = 3.5f;
    [SerializeField] float stoppingDistance = 1.5f;
    
    [Header("Attack Behavior")]
    [SerializeField] float retreatDistance = 2f;
    [SerializeField] float attackDuration = 0.3f;
    [SerializeField] float retreatSpeed = 5f;
    [SerializeField] bool useAttackAnimation = true;
    [SerializeField] GameObject slashEffectPrefab;
    [SerializeField] Vector3 slashOffset = Vector3.zero;
    [SerializeField] float slashScale = 2f;
    [SerializeField] float slashDuration = 0.5f;
    
    [Header("Performance")]
    [SerializeField] float pathUpdateInterval = 0.2f;
    [SerializeField] float animationUpdateInterval = 0.1f;

    [Header("Fear Settings")]
    [SerializeField] float fearOnCollision = 20f;
    [SerializeField] ScaryBarUI scaryBar;

    [SerializeField] private Animator animator;

    NavMeshAgent agent;
    private float pathUpdateTimer;
    private float animationUpdateTimer;
    private float attackTimer = 0f;
    private Vector3 lastVelocity;
    private Vector3 retreatTarget;
    private bool isDying = false; // Flag para evitar múltiplas mortes

    public string objectID;

    private enum State { Chasing, Attacking, Retreating }
    private State currentState = State.Chasing;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = stoppingDistance;
        
        agent.radius = 0.2f;
        agent.avoidancePriority = UnityEngine.Random.Range(40, 60);
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        if (scaryBar == null)
        {
            scaryBar = FindObjectOfType<ScaryBarUI>();
        }

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
        
        pathUpdateTimer = UnityEngine.Random.Range(0f, pathUpdateInterval);
        animationUpdateTimer = UnityEngine.Random.Range(0f, animationUpdateInterval);

        if (GameManager.instance != null)
        {
            if (GameManager.instance.IsObjectRegistered(objectID))
            {
                Destroy(gameObject);
            }
        }
    }

    private void Update()
    {
        if (isDying) return; // Se está morrendo, não processa nada
        if (target == null || agent == null) return;
        if (!agent.enabled || !agent.isOnNavMesh) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case State.Chasing:
                agent.isStopped = false;
                agent.speed = speed;
                
                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    pathUpdateTimer = pathUpdateInterval;
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(target.position);
                    }
                }
                
                agent.stoppingDistance = 0f;
                break;

            case State.Attacking:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                attackTimer += Time.deltaTime;
                
                if (attackTimer >= attackDuration)
                {
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    retreatTarget = transform.position + directionAway * retreatDistance;
                    
                    currentState = State.Retreating;
                    agent.isStopped = false;
                    
                    if (useAttackAnimation && animator != null)
                    {
                        animator.SetBool("attacking", false);
                    }
                }
                break;

            case State.Retreating:
                agent.isStopped = false;
                agent.speed = retreatSpeed;
                agent.stoppingDistance = 0.1f;
                
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(retreatTarget);
                }

                if (!agent.pathPending && agent.remainingDistance <= 0.5f)
                {
                    currentState = State.Chasing;
                }
                break;
        }

        animationUpdateTimer -= Time.deltaTime;
        if (animationUpdateTimer <= 0f)
        {
            animationUpdateTimer = animationUpdateInterval;
            lastVelocity = agent.velocity;
            UpdateAnimations();
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        if (currentState == State.Attacking && useAttackAnimation)
        {
            animator.SetBool("attacking", true);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
            return;
        }

        float moveHorizontal = lastVelocity.x;
        float moveVertical = lastVelocity.y;

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
        else
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
    }

    // MÉTODO NOVO - Gerencia a morte com som
    private void Die()
    {
        if (isDying) return; // Evita morrer múltiplas vezes
        isDying = true;
        
        // Registra no GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.RegisterObject(objectID);
        }
        
        // Toca som de morte
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
            Debug.Log("🔊 Som de morte do Spanner tocando!");
        }
        
        // Esconde visualmente (desativa sprite)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.enabled = false;
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
        
        Debug.Log("🔧 Spanner foi eliminado!");
        
        // Destrói após um delay (tempo suficiente pro som tocar)
        Destroy(gameObject, deathSound != null ? deathSound.length : 0.5f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDying) return; // Ignora colisões se já está morrendo
        
        // Detecta bullets do player
        if (other.GetComponent<BulletScript>() != null)
        {
            Destroy(other.gameObject); // Destrói o bullet
            Die(); // Chama método de morte
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearOnCollision);
                Debug.Log("Spanner causou medo!");
            }
            
            currentState = State.Attacking;
            attackTimer = 0f;
            
            if (useAttackAnimation && slashEffectPrefab != null)
            {
                Vector3 slashPosition = other.transform.position + slashOffset;
                GameObject slash = Instantiate(slashEffectPrefab, slashPosition, Quaternion.identity);
                slash.transform.position = new Vector3(slashPosition.x, slashPosition.y, other.transform.position.z - 0.1f);
                slash.transform.localScale = Vector3.one * slashScale;
                
                Debug.Log($"✅ Slash criado no PLAYER - Pos: {slash.transform.position}, Scale: {slash.transform.localScale}");
                
                SpriteRenderer sr = slash.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Debug.Log($"✅ Sprite: {sr.sprite?.name ?? "NULL"}, Color: {sr.color}");
                    Debug.Log($"✅ Sorting: Layer={sr.sortingLayerName}, Order={sr.sortingOrder}");
                }
                
                Destroy(slash, slashDuration);
            }
            else
            {
                if (!useAttackAnimation)
                    Debug.LogWarning("⚠️ useAttackAnimation está DESATIVADO!");
                if (slashEffectPrefab == null)
                    Debug.LogWarning("⚠️ slashEffectPrefab está NULL! Arraste o prefab no Inspector.");
                    
                if (useAttackAnimation && animator != null)
                {
                    UpdateAnimations();
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying) return; // Ignora colisões se já está morrendo
        
        // Detecta bullets do player
        if (collision.gameObject.GetComponent<BulletScript>() != null)
        {
            Destroy(collision.gameObject); // Destrói o bullet
            Die(); // Chama método de morte
        }
    }
}