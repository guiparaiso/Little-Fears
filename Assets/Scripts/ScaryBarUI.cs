using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ScaryBarUI : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;    // opcional (só pra referência)
    public Image filledImage;        // Image.Type = Filled (Horizontal recommended)

    [Header("Config")]
    public float maxFear = 100f;
    public float fillSmoothing = 6f;     // quanto maior, mais rápido o visual segue o valor real
    public float decayPerSecond = 0f;    // medo reduz com o tempo (0 = sem decay)
    public bool clampToZero = true;

    [Header("Feedbacks")]
    public Color normalColor = Color.white;
    public Color tenseColor = Color.yellow;
    public Color panicColor = Color.red;
    public float tenseThreshold = 0.3f;  // 30% -> muda para tenseColor
    public float panicThreshold = 0.7f;  // 70% -> muda para panicColor
    
    [Header("Player Damage Feedback")]
    public SpriteRenderer playerSpriteRenderer; // Arraste o SpriteRenderer do player aqui
    public Color damageFlashColor = Color.red; // Cor do flash ao levar dano
    public float flashDuration = 0.2f; // Duração do flash
    public int flashCount = 2; // Quantas vezes pisca

    public AudioSource audioSource;
    public AudioClip onFilledClip;

    public UnityEvent onScaryBarFilled;

    // estado
    private float currentFear = 0f;
    private float visualFear = 0f; // usado para Lerp visual
    private bool filledTriggered = false;
    private bool isFlashing = false; // Controla se já está piscando

    void Reset()
    {
        // tentativa automática de achar o filledImage se o dev esquecer
        if (filledImage == null)
            filledImage = GetComponentInChildren<Image>();
    }

    void Start()
    {
        if (filledImage == null)
        {
            Debug.LogError("ScaryBarUI: atribua o Filled Image no inspector e defina Image Type = Filled.");
            enabled = false;
            return;
        }

        // garante formato inicial
        filledImage.type = Image.Type.Filled;
        filledImage.fillMethod = Image.FillMethod.Horizontal;
        filledImage.fillAmount = 0f;

        if (GameManager.instance != null)
        {
            currentFear = GameManager.instance.currentFear;
            visualFear = currentFear; // Para não ter "lerp" estranho na troca de cena
        }
        else
        {
            currentFear = 0f;
            visualFear = 0f;
        }

            UpdateColor();
    }

    void Update()
    {
        // decay ao longo do tempo
        if (decayPerSecond > 0f && currentFear > 0f)
            AddFear(-decayPerSecond * Time.deltaTime);

        // suaviza a transição visual
        visualFear = Mathf.Lerp(visualFear, currentFear, 1f - Mathf.Exp(-fillSmoothing * Time.deltaTime));

        if (GameManager.instance != null)
        {
            GameManager.instance.currentFear = currentFear;
        }

        // converte pra 0..1 e aplica no Image.fillAmount
        float normalized = Mathf.Clamp01(visualFear / Mathf.Max(0.0001f, maxFear));
        filledImage.fillAmount = normalized;

        UpdateColor();

        CheckFilled();
    }

    /// <summary>
    /// Define diretamente o valor de medo.
    /// </summary>
    public void SetFear(float amount)
    {
        currentFear = clampToZero ? Mathf.Clamp(amount, 0f, maxFear) : Mathf.Min(amount, maxFear);
        // opcional: ao setar, atualiza visual imediatamente
        // visualFear = currentFear;
    }

    /// <summary>
    /// Adiciona (ou subtrai se negativo) medo.
    /// </summary>
    public void AddFear(float amount)
    {
        float prev = currentFear;
        currentFear = clampToZero ? Mathf.Clamp(currentFear + amount, 0f, maxFear) : Mathf.Min(currentFear + amount, maxFear);

        // Efeito de flash quando leva dano (amount positivo) e NÃO está já piscando
        if (amount > 0 && currentFear > prev && playerSpriteRenderer != null && !isFlashing)
        {
            StartCoroutine(DamageFlash());
        }

        if(currentFear >= maxFear)
        {
            GameManager.instance.GameOver();
            currentFear = GameManager.instance.currentFear;
            visualFear = currentFear;
        }
    }

    /// <summary>
    /// Preenche ao longo do tempo (útil para barulho contínuo).
    /// </summary>
    public IEnumerator AddFearOverTime(float amount, float duration)
    {
        float added = 0f;
        float sign = Mathf.Sign(amount);
        float rate = Mathf.Abs(amount) / Mathf.Max(duration, 0.0001f);

        while (Mathf.Abs(added) < Mathf.Abs(amount))
        {
            float step = sign * rate * Time.deltaTime;
            AddFear(step);
            added += step;
            yield return null;
        }
    }

    private void UpdateColor()
    {
        float n = Mathf.Clamp01(currentFear / Mathf.Max(0.0001f, maxFear));
        if (n >= panicThreshold) filledImage.color = panicColor;
        else if (n >= tenseThreshold) filledImage.color = tenseColor;
        else filledImage.color = normalColor;
    }

    private void CheckFilled()
    {
        if (!filledTriggered && currentFear >= maxFear)
        {
            filledTriggered = true;
            onScaryBarFilled?.Invoke();
            if (audioSource != null && onFilledClip != null)
                audioSource.PlayOneShot(onFilledClip);
        }
        else if (filledTriggered && currentFear < maxFear)
        {
            // permite retrigger quando encher, esvaziar e encher novamente
            filledTriggered = false;
        }
    }
    
    /// <summary>
    /// Faz o player piscar vermelho quando leva dano
    /// </summary>
    private IEnumerator DamageFlash()
    {
        if (playerSpriteRenderer == null) yield break;
        
        isFlashing = true; // Marca que está piscando
        Color originalColor = playerSpriteRenderer.color;
        
        for (int i = 0; i < flashCount; i++)
        {
            // Flash vermelho
            playerSpriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(flashDuration / 2f);
            
            // Volta à cor original
            playerSpriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration / 2f);
        }
        
        // Garante que voltou à cor original
        playerSpriteRenderer.color = originalColor;
        isFlashing = false; // Libera para piscar novamente
    }
}
