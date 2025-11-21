using UnityEngine;

public class SlashEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] bool fadeOut = true;
    [SerializeField] bool rotateTowardsTarget = false;
    [SerializeField] float rotationOffset = 0f; // Ajuste fino da rotação
    [SerializeField] Color slashColor = Color.white; // Cor do slash (padrão: branco)
    
    private SpriteRenderer spriteRenderer;
    private float duration;
    private float elapsed = 0f;
    private Color startColor;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Aplica a cor configurada
            spriteRenderer.color = slashColor;
            startColor = slashColor;
            
            // Garante que está visível na frente
            spriteRenderer.sortingOrder = 100;
        }
        
        // Rotaciona para a direção do player se configurado
        if (rotateTowardsTarget)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 direction = player.transform.position - transform.position;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle + rotationOffset);
            }
        }
    }

    private void Update()
    {
        // Fade out gradual
        if (fadeOut && spriteRenderer != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / 0.3f);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
    }
}
