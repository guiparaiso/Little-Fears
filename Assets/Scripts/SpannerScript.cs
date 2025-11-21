using UnityEngine;
using UnityEngine.AI;

public class SpannerScript : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 3.5f;
    [SerializeField] float stoppingDistance = 1.5f; // Reduzido para não ficar em cima do player
    
    [Header("Attack Behavior")]
    [SerializeField] bool useAttackBehavior = true; // Ativa/desativa comportamento de ataque
    [SerializeField] float attackDistance = 1.5f; // Distância menor para atacar mais fácil
    [SerializeField] float retreatDistance = 3f; // Distância para recuar (reduzida)
    [SerializeField] float attackDuration = 0.3f; // Tempo do ataque (mais rápido)
    [SerializeField] float retreatSpeed = 4.5f; // Velocidade ao recuar
    
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
    private float attackTimer;
    private Vector3 lastVelocity;
    private float stuckTimer;
    private Vector3 lastPosition;
    
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
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (target == null || agent == null) return;
        
        // Verifica se o agent está ativo e no NavMesh antes de tentar usar
        if (!agent.enabled || !agent.isOnNavMesh) return;

        // Atualiza cooldown de ataque
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }

        // Atualiza estado baseado na distância
        UpdateState();
        
        // Verifica se está travado
        CheckIfStuck();

        // Atualiza destino apenas periodicamente
        pathUpdateTimer -= Time.deltaTime;
        if (pathUpdateTimer <= 0f)
        {
            pathUpdateTimer = pathUpdateInterval;
            UpdateMovement();
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

    private void UpdateState()
    {
        if (target == null) return;
        
        // Se comportamento de ataque está desativado, apenas persegue
        if (!useAttackBehavior)
        {
            currentState = State.Chasing;
            agent.speed = speed;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        switch (currentState)
        {
            case State.Chasing:
                agent.speed = speed;
                // Entra em ataque quando chega perto
                if (distanceToTarget <= attackDistance)
                {
                    currentState = State.Attacking;
                    attackTimer = attackDuration;
                }
                break;

            case State.Attacking:
                // Mantém velocidade normal durante ataque
                agent.speed = speed;
                // Após atacar, sempre recua
                if (attackTimer <= 0)
                {
                    currentState = State.Retreating;
                    agent.speed = retreatSpeed;
                }
                break;

            case State.Retreating:
                agent.speed = retreatSpeed;
                // Quando está longe suficiente, volta a PERSEGUIR (não ataca direto)
                if (distanceToTarget >= retreatDistance)
                {
                    currentState = State.Chasing; // VOLTA A PERSEGUIR
                    agent.speed = speed;
                }
                // Se perdeu o caminho ou ficou preso, volta a perseguir
                else if (!agent.hasPath && distanceToTarget > attackDistance)
                {
                    currentState = State.Chasing;
                    agent.speed = speed;
                }
                break;
        }
    }

    private void UpdateMovement()
    {
        if (target == null || !agent.isOnNavMesh) return;

        switch (currentState)
        {
            case State.Chasing:
                // Persegue o player normalmente
                agent.isStopped = false;
                
                // Verifica se consegue calcular caminho válido
                UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
                if (agent.CalculatePath(target.position, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(target.position);
                }
                else
                {
                    // Se não consegue chegar direto, tenta se aproximar
                    agent.SetDestination(target.position);
                }
                
                agent.stoppingDistance = 0.5f; // Para antes de ficar em cima do player
                break;

            case State.Attacking:
                // Para e fica atacando
                agent.isStopped = true;
                agent.ResetPath();
                break;

            case State.Retreating:
                // Recua do player - move-se diretamente para trás
                agent.isStopped = false;
                Vector3 directionAway = (transform.position - target.position).normalized;
                Vector3 retreatPos = transform.position + directionAway * 1.5f;
                
                // Verifica se o caminho de recuo é válido
                UnityEngine.AI.NavMeshPath retreatPath = new UnityEngine.AI.NavMeshPath();
                if (agent.CalculatePath(retreatPos, retreatPath) && retreatPath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(retreatPos);
                    agent.stoppingDistance = 0.1f;
                }
                else
                {
                    // Se não conseguir recuar, volta a perseguir
                    currentState = State.Chasing;
                    agent.speed = speed;
                }
                break;
        }
    }
    
    private void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;
        
        // Verifica a cada 1 segundo se está travado
        if (stuckTimer >= 1f)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            // Se moveu muito pouco e deveria estar se movendo
            if (distanceMoved < 0.5f && agent.hasPath && currentState == State.Chasing)
            {
                // Força recalcular caminho ou simplifica comportamento
                agent.ResetPath();
                
                // Se estiver muito longe e travado, desativa ataque temporariamente
                float distToTarget = Vector3.Distance(transform.position, target.position);
                if (distToTarget > attackDistance * 2)
                {
                    useAttackBehavior = false;
                }
            }
            else if (distanceMoved > 0.5f)
            {
                // Se voltou a se mover, reativa ataque
                useAttackBehavior = true;
            }
            
            lastPosition = transform.position;
            stuckTimer = 0f;
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
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearOnCollision);
            }
        }
    }
}
