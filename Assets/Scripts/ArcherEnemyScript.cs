using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class ArcherEnemyScript : MonoBehaviour
{
    [Header("Death Effect")]
    [SerializeField] AudioClip deathSound;
    
    [SerializeField] Transform target;
    [SerializeField] float chaseSpeed = 3f;
    [SerializeField] float retreatSpeed = 5f;
    
    [Header("Distance Settings")]
    [SerializeField] float shootingRange = 6f;
    [SerializeField] float tooCloseDistance = 3f;
    [SerializeField] float maxRange = 10f;
    
    [Header("Shooting Settings")]
    [SerializeField] GameObject arrowPrefab;
    [SerializeField] float arrowSpeed = 8f;
    [SerializeField] float shootInterval = 1.5f;
    
    [Header("Performance")]
    [SerializeField] float pathUpdateInterval = 0.2f;
    [SerializeField] float animationUpdateInterval = 0.1f;

    [Header("Fear Settings")]
    [SerializeField] float fearOnHit = 15f;
    [SerializeField] ScaryBarUI scaryBar;

    [SerializeField] private Animator animator;

    NavMeshAgent agent;
    private float pathUpdateTimer;
    private float animationUpdateTimer;
    private float shootTimer = 0f;
    private Vector3 lastVelocity;
    private Vector3 retreatTarget;
    private bool isDying = false; // Flag para evitar múltiplas mortes

    public string objectID;

    private enum State { Chasing, Shooting, Retreating }
    private State currentState = State.Chasing;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        agent.radius = 0.2f;
        agent.avoidancePriority = Random.Range(40, 60);
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
        
        pathUpdateTimer = Random.Range(0f, pathUpdateInterval);
        animationUpdateTimer = Random.Range(0f, animationUpdateInterval);
        shootTimer = shootInterval;

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
                agent.speed = chaseSpeed;
                agent.stoppingDistance = shootingRange;
                
                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    pathUpdateTimer = pathUpdateInterval;
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(target.position);
                    }
                }
                
                if (distanceToTarget <= shootingRange)
                {
                    currentState = State.Shooting;
                    shootTimer = 0f;
                }
                break;

            case State.Shooting:
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                
                if (animator != null)
                {
                    animator.SetBool("walking_left", false);
                    animator.SetBool("walking_right", false);
                    animator.SetBool("walking_up", false);
                    animator.SetBool("walking_down", false);
                }
                
                shootTimer -= Time.deltaTime;
                if (shootTimer <= 0f)
                {
                    ShootArrow();
                    shootTimer = shootInterval;
                }
                
                if (distanceToTarget < tooCloseDistance)
                {
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    retreatTarget = transform.position + directionAway * (shootingRange - distanceToTarget + 1f);
                    currentState = State.Retreating;
                    agent.isStopped = false;
                }
                else if (distanceToTarget > maxRange)
                {
                    currentState = State.Chasing;
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

                if (distanceToTarget >= shootingRange * 0.9f || (!agent.pathPending && agent.remainingDistance <= 0.5f))
                {
                    currentState = State.Shooting;
                    shootTimer = 0f;
                }
                break;
        }

        if (currentState != State.Shooting)
        {
            animationUpdateTimer -= Time.deltaTime;
            if (animationUpdateTimer <= 0f)
            {
                animationUpdateTimer = animationUpdateInterval;
                lastVelocity = agent.velocity;
                UpdateAnimations();
            }
        }
    }

    private void ShootArrow()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("ArcherEnemy: ArrowPrefab não configurado!");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("ArcherEnemy: Target não encontrado!");
            return;
        }

        Debug.Log("Archer atirando flecha!");

        Vector2 direction = (target.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        GameObject newArrow = Instantiate(arrowPrefab, transform.position, rotation);

        Rigidbody2D arrowRb = newArrow.GetComponent<Rigidbody2D>();
        if (arrowRb == null)
        {
            arrowRb = newArrow.AddComponent<Rigidbody2D>();
        }

        arrowRb.bodyType = RigidbodyType2D.Dynamic;
        arrowRb.gravityScale = 0;
        arrowRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        arrowRb.linearVelocity = direction * arrowSpeed;

        Destroy(newArrow, 5f);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;

        if (velocity.magnitude < 0.1f)
        {
            return;
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
            Debug.Log("🔊 Som de morte do Archer tocando!");
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
        
        Debug.Log("🏹 Archer foi eliminado!");
        
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