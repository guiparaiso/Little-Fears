using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 3.5f;
    [SerializeField] float stoppingDistance = 0.8f;

    [Header("Attack Behavior")]
    [SerializeField] float retreatDistance = 2f; // Distância para recuar após ataque (em unidades)
    [SerializeField] float attackDuration = 0.3f; // Tempo pausado durante ataque (em segundos)
    [SerializeField] float retreatSpeed = 5f; // Velocidade do recuo

    [Header("Fear Settings")]
    [SerializeField] float fearOnCollision = 20f;  // quanto medo aumenta na colisão
    [SerializeField] ScaryBarUI scaryBar;       // arraste a ScaryBar aqui no Inspector

    [SerializeField]
    private Animator animator;

    NavMeshAgent agent;
    
    // Estados de comportamento
    private enum EnemyState { Chasing, Attacking, Retreating }
    private EnemyState currentState = EnemyState.Chasing;
    private float attackTimer = 0f;
    private Vector3 retreatTarget;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = stoppingDistance;
        
        // Configurações para evitar travamento
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        agent.radius = 0.2f;
        agent.avoidancePriority = Random.Range(40, 60);

        // tenta encontrar automaticamente se não foi arrastado no inspetor
        if (scaryBar == null)
        {
            scaryBar = FindObjectOfType<ScaryBarUI>();
            if (scaryBar == null)
                Debug.LogWarning("Enemy: nenhum ScaryBarUI encontrado na cena!");
        }
    }

    private void Update()
    {
        if (target == null || agent == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Máquina de estados
        switch (currentState)
        {
            case EnemyState.Chasing:
                // Persegue o player
                agent.isStopped = false;
                agent.speed = speed;
                if (agent.isOnNavMesh)
                {
                    agent.SetDestination(target.position);
                }
                // stoppingDistance = 0 para colar no player
                agent.stoppingDistance = 0f;
                break;

            case EnemyState.Attacking:
                // PARA completamente durante ataque
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                attackTimer += Time.deltaTime;
                
                if (attackTimer >= attackDuration)
                {
                    // aCalcula para onde recuar
                    Vector3 directionAway = (transform.position - target.position).normalized;
                    retreatTarget = transform.position + directionAway * retreatDistance;
                    
                    currentState = EnemyState.Retreating;
                    agent.isStopped = false;
                }
                break;

            case EnemyState.Retreating:
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
                    currentState = EnemyState.Chasing;
                }
                break;
        }

        // Atualiza animações
        UpdateAnimations();
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

    // Este método é chamado automaticamente quando o inimigo colide com o jogador
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Causa medo
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearOnCollision);
                Debug.Log("Inimigo causou medo!");
            }
            
            // Inicia ataque e recuo
            currentState = EnemyState.Attacking;
            attackTimer = 0f;
        }
    }
}
