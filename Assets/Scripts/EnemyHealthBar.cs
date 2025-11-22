// ============================================
// BARRA DE VIDA UNIVERSAL PARA QUALQUER INIMIGO
// Funciona com: PumpkinEnemy, Enemy, Ghost, etc.
// ============================================

using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    [SerializeField] private Image healthBarFill; // Arraste o Image (Fill) aqui (opcional)
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); // Altura acima do inimigo
    [SerializeField] private bool hideWhenFull = false; // Esconde quando vida está cheia
    [SerializeField] private bool hideWhenDead = true; // Esconde quando morto
    
    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color midHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float midHealthThreshold = 0.5f; // 50%
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30%
    
    [Header("Size")]
    [SerializeField] private Vector2 barSize = new Vector2(1f, 0.15f); // Largura x Altura
    [SerializeField] private float barPadding = 4f; // Espaço interno
    
    private Canvas healthCanvas;
    private Camera mainCamera;
    private GameObject backgroundObj;
    
    // Suporte para diferentes tipos de inimigos
    private PumpkinEnemy pumpkinEnemy;
    private Component reaperEnemy; // Usa Component genérico para ReaperEnemy
    
    // Cache de valores
    private float currentHealth;
    private float maxHealth;

    private void Start()
    {
        mainCamera = Camera.main;
        
        // Detecta automaticamente qual tipo de inimigo é
        DetectEnemyType();
        
        // Espera 1 frame para garantir que o Start() do inimigo já rodou
        StartCoroutine(InitializeHealthBarDelayed());
    }

    private System.Collections.IEnumerator InitializeHealthBarDelayed()
    {
        // Espera o final do frame para garantir que todos os Start() já rodaram
        yield return new WaitForEndOfFrame();
        
        // Se não configurou manualmente, cria barra automaticamente
        if (healthBarFill == null)
        {
            CreateHealthBarAutomatically();
        }
        
        // Inicializa valores
        UpdateHealthValues();
        
        // Força primeira atualização visual
        UpdateHealthBar();
    }

    private void DetectEnemyType()
    {
        // Tenta detectar PumpkinEnemy
        pumpkinEnemy = GetComponent<PumpkinEnemy>();
        
        // Se não é Pumpkin, tenta detectar ReaperEnemy
        if (pumpkinEnemy == null)
        {
            // Usa reflexão para detectar ReaperEnemy sem precisar declarar variável
            var reaperComponent = GetComponent("ReaperEnemy");
            if (reaperComponent != null)
            {
                // Detectou Reaper, mas vamos usar via reflexão no UpdateHealthValues
                Debug.Log($"✅ EnemyHealthBar detectou ReaperEnemy em {gameObject.name}");
                return;
            }
        }
        
        if (pumpkinEnemy == null && GetComponent("ReaperEnemy") == null)
        {
            Debug.LogWarning($"⚠️ EnemyHealthBar em {gameObject.name}: Nenhum script de inimigo detectado! Certifique-se de ter PumpkinEnemy ou ReaperEnemy no objeto.");
        }
    }

    private void LateUpdate()
    {
        if (healthCanvas != null)
        {
            // Faz a barra sempre olhar para a câmera
            healthCanvas.transform.rotation = mainCamera.transform.rotation;
            
            // Atualiza a posição da barra
            healthCanvas.transform.position = transform.position + offset;
        }
        
        UpdateHealthBar();
    }

    private void UpdateHealthValues()
    {
        // Atualiza valores de vida baseado no tipo de inimigo
        if (pumpkinEnemy != null)
        {
            currentHealth = pumpkinEnemy.GetCurrentHealth();
            maxHealth = pumpkinEnemy.GetMaxHealth();
            // Debug.Log($"Pumpkin HP: {currentHealth}/{maxHealth}");
        }
        else
        {
            // Tenta pegar ReaperEnemy via reflexão
            var reaper = GetComponent("ReaperEnemy");
            if (reaper != null)
            {
                var type = reaper.GetType();
                var getCurrentHealthMethod = type.GetMethod("GetCurrentHealth");
                var getMaxHealthMethod = type.GetMethod("GetMaxHealth");
                
                if (getCurrentHealthMethod != null && getMaxHealthMethod != null)
                {
                    currentHealth = (float)getCurrentHealthMethod.Invoke(reaper, null);
                    maxHealth = (float)getMaxHealthMethod.Invoke(reaper, null);
                    // Debug.Log($"Reaper HP: {currentHealth}/{maxHealth}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ ReaperEnemy em {gameObject.name} não tem os métodos GetCurrentHealth/GetMaxHealth!");
                }
            }
        }
        
        // Verifica se os valores são válidos
        if (maxHealth <= 0)
        {
            Debug.LogWarning($"⚠️ {gameObject.name}: maxHealth é {maxHealth}! Verifique se o inimigo inicializou corretamente.");
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null) return;
        
        UpdateHealthValues();
        
        // Calcula porcentagem de vida
        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        
        // Atualiza fill amount
        healthBarFill.fillAmount = healthPercent;
        
        // Sistema de cores gradiente
        Color targetColor;
        if (healthPercent <= lowHealthThreshold)
        {
            targetColor = lowHealthColor; // Vermelho
        }
        else if (healthPercent <= midHealthThreshold)
        {
            // Interpola entre vermelho e amarelo
            float t = (healthPercent - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold);
            targetColor = Color.Lerp(lowHealthColor, midHealthColor, t);
        }
        else
        {
            // Interpola entre amarelo e verde
            float t = (healthPercent - midHealthThreshold) / (1f - midHealthThreshold);
            targetColor = Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        
        healthBarFill.color = targetColor;
        
        // Esconde/mostra barra baseado em configurações
        if (healthCanvas != null)
        {
            bool shouldShow = true;
            
            if (hideWhenFull && healthPercent >= 0.99f)
            {
                shouldShow = false;
            }
            
            if (hideWhenDead && currentHealth <= 0)
            {
                shouldShow = false;
            }
            
            healthCanvas.gameObject.SetActive(shouldShow);
        }
    }

    private void CreateHealthBarAutomatically()
    {
        // Cria Canvas
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;
        
        healthCanvas = canvasObj.AddComponent<Canvas>();
        healthCanvas.renderMode = RenderMode.WorldSpace;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;
        
        // Configura tamanho do canvas
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = barSize;
        
        // Cria Background
        backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(canvasObj.transform);
        Image bgImage = backgroundObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Cinza escuro
        
        RectTransform bgRect = backgroundObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Cria Fill (Barra de vida propriamente dita)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(backgroundObj.transform);
        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = fullHealthColor;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-barPadding, -barPadding); // Padding interno
        fillRect.anchoredPosition = Vector2.zero;
        
        Debug.Log($"✅ Barra de vida criada automaticamente para {gameObject.name}");
    }

    // Método público para ajustar offset em runtime
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    // Método público para ajustar tamanho em runtime
    public void SetBarSize(Vector2 newSize)
    {
        barSize = newSize;
        if (healthCanvas != null)
        {
            healthCanvas.GetComponent<RectTransform>().sizeDelta = newSize;
        }
    }
}

// ============================================
// ADICIONE ESTES MÉTODOS NOS SEUS INIMIGOS
// ============================================

/*
// Para PumpkinEnemy.cs - JÁ ADICIONADO! ✅

// Para ReaperEnemy.cs - JÁ ADICIONADO! ✅

// Se quiser adicionar mais inimigos no futuro, adicione os métodos:
public float GetCurrentHealth()
{
    return currentHealth; // ou health, dependendo do nome da variável
}

public float GetMaxHealth()
{
    return maxHealth;
}
*/