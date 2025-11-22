using UnityEngine;
using UnityEngine.AI;

public class ArcherEnemyScript : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float chaseSpeed = 3f;
    [SerializeField] float retreatSpeed = 5f;
    
    [Header("Distance Settings")]
    [SerializeField] float shootingRange = 6f; // Distância ideal para atirar
    [SerializeField] float tooCloseDistance = 3f; // Muito perto, precisa recuar
    [SerializeField] float maxRange = 10f; // Muito longe, precisa perseguir
    
    [Header("Shooting Settings")]
    [SerializeField] GameObject arrowPrefab;
    [SerializeField] float arrowSpeed = 8f;
    [SerializeField] float shootInterval = 1.5f; // Intervalo entre flechas
    
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
    
    // Estados
    private enum State { Chasing, Shooting, Retreating }
    private State currentState = State.Chasing;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = chaseSpeed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        // Configurações para evitar colisões entre inimigos
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
        
        // Randomiza timers
        pathUpdateTimer = Random.Range(0f, pathUpdateInterval);
        animationUpdateTimer = Random.Range(0f, animationUpdateInterval);
        shootTimer = shootInterval; // Começa pronto para atirar
    }

    private void Update()
    {
        if (target == null || agent == null) return;
        
        if (!agent.enabled || !agent.isOnNavMesh) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Máquina de estados baseada em distância
        switch (currentState)
        {
            case State.Chasing:
                // Persegue até atingir a distância ideal de tiro
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
                
                // Quando chega na distância de tiro, muda para atirar
                if (distanceToTarget <= shootingRange)
                {
                    currentState = State.Shooting;
                    shootTimer = 0f; // Reseta timer para atirar imediatamente
                }
                break;

            case State.Shooting:
                // Para e atira flechas
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                
                // Para todas as animações de movimento
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
                
                // Se o player se aproximar demais, recua
                if (distanceToTarget < tooCloseDistance)
                {
                    // Calcula para onde recuar
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    retreatTarget = transform.position + directionAway * (shootingRange - distanceToTarget + 1f);
                    currentState = State.Retreating;
                    agent.isStopped = false;
                }
                // Se o player se afastar muito, persegue novamente
                else if (distanceToTarget > maxRange)
                {
                    currentState = State.Chasing;
                }
                break;

            case State.Retreating:
                // Recua rapidamente para manter distância
                agent.isStopped = false;
                agent.speed = retreatSpeed;
                agent.stoppingDistance = 0.1f;
                
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(retreatTarget);
                }

                // Quando atinge distância segura, volta a atirar
                if (distanceToTarget >= shootingRange * 0.9f || (!agent.pathPending && agent.remainingDistance <= 0.5f))
                {
                    currentState = State.Shooting;
                    shootTimer = 0f; // Atira logo após recuar
                }
                break;
        }

        // Atualiza animação periodicamente (apenas se não estiver no estado Shooting)
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

        // Calcula a direção para o player
        Vector2 direction = (target.position - transform.position).normalized;

        // Calcula o ângulo de rotação para a flecha apontar na direção correta
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        // Cria a flecha na posição do archer com rotação correta
        GameObject newArrow = Instantiate(arrowPrefab, transform.position, rotation);

        // Configura o Rigidbody2D da flecha
        Rigidbody2D arrowRb = newArrow.GetComponent<Rigidbody2D>();
        if (arrowRb == null)
        {
            arrowRb = newArrow.AddComponent<Rigidbody2D>();
        }

        // Configurações corretas para detectar colisões
        arrowRb.bodyType = RigidbodyType2D.Dynamic;
        arrowRb.gravityScale = 0;
        arrowRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Define a velocidade na direção do player
        arrowRb.linearVelocity = direction * arrowSpeed;

        // Destrói a flecha após 5 segundos
        Destroy(newArrow, 5f);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Controle de animações baseado na direção do movimento
        Vector3 velocity = agent.velocity;
        float moveHorizontal = velocity.x;
        float moveVertical = velocity.y;

        // Se está parado (atacando), mantém última direção
        if (velocity.magnitude < 0.1f)
        {
            // Durante ataque, pode adicionar animação de ataque aqui se tiver
            return;
        }

        // Previne animação diagonal - prioriza o eixo com maior movimento
        if (Mathf.Abs(moveHorizontal) > 0.1f && Mathf.Abs(moveVertical) > 0.1f)
        {
            if (Mathf.Abs(moveHorizontal) >= Mathf.Abs(moveVertical))
            {
                moveVertical = 0; // Prioriza movimento horizontal
            }
            else
            {
                moveHorizontal = 0; // Prioriza movimento vertical
            }
        }

        // Movimento horizontal
        if (moveHorizontal < -0.1f) // Movendo para a esquerda
        {
            animator.SetBool("walking_left", true);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        else if (moveHorizontal > 0.1f) // Movendo para a direita
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", true);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
        // Movimento vertical
        else if (moveVertical > 0.1f) // Movendo para cima
        {
            animator.SetBool("walking_up", true);
            animator.SetBool("walking_down", false);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
        }
        else if (moveVertical < -0.1f) // Movendo para baixo
        {
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", true);
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
        }
    }

    // Auto-gerenciamento: Detecta bullets do player
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detecta bullets do player pelo script BulletScript
        if (other.GetComponent<BulletScript>() != null)
        {
            Destroy(other.gameObject); // Destrói o bullet
            Destroy(gameObject); // Destrói o archer (1 hit kill)
            Debug.Log("🏹 Archer foi eliminado por bullet do player!");
        }
    }

    // Auto-gerenciamento: Detecta bullets do player (versão Collision)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Detecta bullets do player pelo script BulletScript
        if (collision.gameObject.GetComponent<BulletScript>() != null)
        {
            Destroy(collision.gameObject); // Destrói o bullet
            Destroy(gameObject); // Destrói o archer (1 hit kill)
            Debug.Log("🏹 Archer foi eliminado por bullet do player (Collision)!");
        }
    }
}
