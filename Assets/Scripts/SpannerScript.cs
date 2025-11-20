using UnityEngine;
using UnityEngine.AI;

public class SpannerScript : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 3.5f;
    [SerializeField] float stoppingDistance = 1.5f; // Reduzido para não ficar em cima do player
    
    [Header("Attack Behavior")]
    [SerializeField] float retreatDistance = 2f; // Distância para recuar após ataque (em unidades)
    [SerializeField] float attackDuration = 0.3f; // Tempo pausado durante ataque (em segundos)
    [SerializeField] float retreatSpeed = 5f; // Velocidade do recuo
    
    [Header("Performance")]
    [SerializeField] float pathUpdateInterval = 0.2f; // Atualiza destino a cada 0.2s
    [SerializeField] float animationUpdateInterval = 0.1f; // Atualiza animação a cada 0.1s

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
    
    // Estados
    private enum State { Chasing, Attacking, Retreating }
    private State currentState = State.Chasing;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = stoppingDistance;
        
        // Configurações para evitar colisões entre inimigos
        agent.radius = 0.2f; // Reduz o radius para caber mais
        agent.avoidancePriority = Random.Range(40, 60); // Prioridade aleatória para não travarem
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; // Ativa desvio

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
        
        // Randomiza timers para não todos atualizarem ao mesmo tempo
        pathUpdateTimer = Random.Range(0f, pathUpdateInterval);
        animationUpdateTimer = Random.Range(0f, animationUpdateInterval);
    }

    private void Update()
    {
        if (target == null || agent == null) return;
        
        // Verifica se o agent está ativo e no NavMesh antes de tentar usar
        if (!agent.enabled || !agent.isOnNavMesh) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Máquina de estados (IGUAL AO ENEMY.CS)
        switch (currentState)
        {
            case State.Chasing:
                // Persegue o player
                agent.isStopped = false;
                agent.speed = speed;
                
                // Atualiza destino periodicamente
                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    pathUpdateTimer = pathUpdateInterval;
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(target.position);
                    }
                }
                
                // stoppingDistance = 0 para colar no player
                agent.stoppingDistance = 0f;
                break;

            case State.Attacking:
                // PARA completamente durante ataque
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                attackTimer += Time.deltaTime;
                
                if (attackTimer >= attackDuration)
                {
                    // Calcula para onde recuar
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    retreatTarget = transform.position + directionAway * retreatDistance;
                    
                    currentState = State.Retreating;
                    agent.isStopped = false;
                }
                break;

            case State.Retreating:
                // Recua RAPIDAMENTE para longe
                agent.isStopped = false;
                agent.speed = retreatSpeed; // Mais rápido que perseguição
                agent.stoppingDistance = 0.1f;
                
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(retreatTarget);
                }

                // Volta a perseguir quando chega perto do destino de recuo
                if (!agent.pathPending && agent.remainingDistance <= 0.5f)
                {
                    currentState = State.Chasing;
                }
                break;
        }

        // Atualiza animação apenas periodicamente
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

        float moveHorizontal = lastVelocity.x;
        float moveVertical = lastVelocity.y;

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
        // Movimento vertical
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
        else // Parado
        {
            animator.SetBool("walking_left", false);
            animator.SetBool("walking_right", false);
            animator.SetBool("walking_up", false);
            animator.SetBool("walking_down", false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Causa medo
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearOnCollision);
                Debug.Log("Spanner causou medo!");
            }
            
            // Inicia ataque e recuo (IGUAL AO ENEMY.CS)
            currentState = State.Attacking;
            attackTimer = 0f;
        }
    }
}
