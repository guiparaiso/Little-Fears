using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float speed = 3.5f;
    [SerializeField] float stoppingDistance = 2.0f;

    [Header("Fear Settings")]
    [SerializeField] float fearOnCollision = 20f;  // quanto medo aumenta na colisão
    [SerializeField] ScaryBarUI scaryBar;       // arraste a ScaryBar aqui no Inspector

    [SerializeField]
    private Animator animator;

    NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

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
        if (target != null)
        {
            agent.SetDestination(target.position);
            agent.stoppingDistance = stoppingDistance;

            // Controle de animações baseado na direção do movimento
            Vector3 velocity = agent.velocity;
            float moveHorizontal = velocity.x;
            float moveVertical = velocity.y;

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
                this.animator.SetBool("walking_left", true);
                this.animator.SetBool("walking_right", false);
                this.animator.SetBool("walking_up", false);
                this.animator.SetBool("walking_down", false);
            }
            else if (moveHorizontal > 0.1f) // Movendo para a direita
            {
                this.animator.SetBool("walking_left", false);
                this.animator.SetBool("walking_right", true);
                this.animator.SetBool("walking_up", false);
                this.animator.SetBool("walking_down", false);
            }
            // Movimento vertical
            else if (moveVertical > 0.1f) // Movendo para cima
            {
                this.animator.SetBool("walking_up", true);
                this.animator.SetBool("walking_down", false);
                this.animator.SetBool("walking_left", false);
                this.animator.SetBool("walking_right", false);
            }
            else if (moveVertical < -0.1f) // Movendo para baixo
            {
                this.animator.SetBool("walking_up", false);
                this.animator.SetBool("walking_down", true);
                this.animator.SetBool("walking_left", false);
                this.animator.SetBool("walking_right", false);
            }
            else // Parado
            {
                this.animator.SetBool("walking_left", false);
                this.animator.SetBool("walking_right", false);
                this.animator.SetBool("walking_up", false);
                this.animator.SetBool("walking_down", false);
            }
        }
    }

    // Este método é chamado automaticamente quando o inimigo colide com o jogador
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (scaryBar != null)
            {
                scaryBar.AddFear(fearOnCollision);
                Debug.Log("Inimigo causou medo!");
            }
        }
    }
}
