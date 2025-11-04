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
