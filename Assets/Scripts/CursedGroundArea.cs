using UnityEngine;
using System.Collections;

public class CursedGroundArea : MonoBehaviour
{
    [HideInInspector] public float radius = 2f;
    [HideInInspector] public float duration = 4f;
    [HideInInspector] public float damagePerTick = 5f;
    [HideInInspector] public float damageInterval = 0.5f;
    [HideInInspector] public ScaryBarUI scaryBar;
    [HideInInspector] public ReaperEnemy reaper;
    
    [Header("Visual Effects")]
    [SerializeField] bool showWarning = true;
    [SerializeField] float warningDuration = 0.5f;
    [SerializeField] bool isTranslucent = false; // Se true, usa transparência
    [SerializeField] Color darkColor = new Color(0.2f, 0f, 0.2f, 1f); // Roxo escuro
    [SerializeField] Color lightColor = new Color(0.6f, 0f, 0.6f, 1f); // Roxo claro
    
    private CircleCollider2D areaCollider;
    private SpriteRenderer spriteRenderer;
    private bool playerInside = false;
    private float damageTimer = 0f;
    private bool isActive = false; // Começa inativo durante warning
    private float creationTime;
    
    private void Start()
    {
        creationTime = Time.time;
        
        // Adiciona collider circular
        areaCollider = gameObject.GetComponent<CircleCollider2D>();
        if (areaCollider == null)
        {
            areaCollider = gameObject.AddComponent<CircleCollider2D>();
        }
        areaCollider.isTrigger = true;
        areaCollider.radius = radius;
        
        // Configura escala visual
        transform.localScale = Vector3.one * radius * 2f;
        
        // Efeito de pulsação
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            StartCoroutine(WarningThenActivate());
        }
        else
        {
            // Se não tem sprite, ativa imediatamente
            isActive = true;
        }
        
        // Auto-destrói após duration (+ warning time)
        float totalDuration = showWarning ? duration + warningDuration : duration;
        Destroy(gameObject, totalDuration);
        
        Debug.Log($"⚫ Área amaldiçoada criada! Raio: {radius}, Duração: {duration}s, Dano: {damagePerTick} a cada {damageInterval}s");
    }
    
    private void OnDestroy()
    {
        // Notifica o Reaper que a área foi destruída
        if (reaper != null)
        {
            reaper.OnCursedAreaDestroyed();
        }
    }
    
    private void Update()
    {
        // Se player está dentro E área está ativa, causa dano periodicamente
        if (playerInside && isActive)
        {
            damageTimer += Time.deltaTime;
            
            if (damageTimer >= damageInterval)
            {
                damageTimer = 0f;
                
                if (scaryBar != null)
                {
                    // Dano aumenta ligeiramente com o tempo (fica mais perigoso)
                    float timeAlive = Time.time - creationTime - (showWarning ? warningDuration : 0);
                    float damageMultiplier = 1f + (timeAlive / duration) * 0.5f; // Até +50% de dano
                    float actualDamage = damagePerTick * damageMultiplier;
                    
                    scaryBar.AddFear(actualDamage);
                    Debug.Log($"⚫ Chão amaldiçoado causou {actualDamage:F1} de dano!");
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && isActive)
        {
            playerInside = true;
            damageTimer = damageInterval; // Causa dano imediato ao entrar
            Debug.Log("⚠️ Player entrou na área amaldiçoada!");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            damageTimer = 0f;
            Debug.Log("✅ Player saiu da área amaldiçoada!");
        }
    }
    
    // NOVO: Warning antes de ativar
    IEnumerator WarningThenActivate()
    {
        if (spriteRenderer == null) yield break;
        
        if (showWarning)
        {
            // Fase de warning: pisca vermelho rapidamente
            float elapsed = 0f;
            Color warningColor = new Color(1f, 0f, 0f, 0.3f); // Vermelho transparente
            
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                
                float pulse = Mathf.Sin(elapsed * 15f * Mathf.PI);
                if (pulse > 0)
                    spriteRenderer.color = warningColor;
                else
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.1f); // Quase invisível
                
                // Escala também pulsa
                float scale = radius * 2f * (1f + Mathf.Sin(elapsed * 15f * Mathf.PI) * 0.2f);
                transform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            Debug.Log("⚫ Área amaldiçoada ATIVADA!");
        }
        
        // Agora ativa o dano
        isActive = true;
        
        // Começa efeito normal de pulsação
        StartCoroutine(PulseEffect());
    }
    
    IEnumerator PulseEffect()
    {
        if (spriteRenderer == null) yield break;
        
        // Aplica transparência se configurado
        Color dark = darkColor;
        Color light = lightColor;
        
        if (isTranslucent)
        {
            dark.a = 0.5f;
            light.a = 0.7f;
        }
        
        float elapsed = 0f;
        
        while (true)
        {
            elapsed += Time.deltaTime * 2.5f; // Pulsa mais rápido
            
            float t = Mathf.Sin(elapsed * Mathf.PI) * 0.5f + 0.5f;
            spriteRenderer.color = Color.Lerp(dark, light, t);
            
            // Variação de escala mais intensa
            float scale = radius * 2f * (1f + Mathf.Sin(elapsed * Mathf.PI) * 0.15f);
            transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
    }
}
