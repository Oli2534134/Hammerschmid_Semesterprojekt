using UnityEngine;
using UnityEngine.UI;

public class PlayerAdrenaline : MonoBehaviour
{
    [Header("Adrenaline Settings")]
    public float currentAdrenaline = 0f;
    public float maxAdrenaline = 100f;
    public float decreaseRate = 3f;
    public float delayBeforeDecrease = 3f;

    [Header("Adrenaline Gains")]
    [Tooltip("Adrenalin, das man beim Töten eines Gegners bekommt.")]
    public float adrenalinePerKill = 20f;
    [Tooltip("Adrenalin-Multiplikator für erlittenen Schaden (Schaden * Multiplikator).")]
    public float adrenalineDamageMultiplier = 1f;
    [Tooltip("Minimum Adrenalin pro Treffer.")]
    public float minAdrenalineFromDamage = 5f;
    [Tooltip("Maximum Adrenalin pro Treffer.")]
    public float maxAdrenalineFromDamage = 25f;
    [Tooltip("Adrenalin, das man beim Drücken der Leertaste (Debug) bekommt.")]
    public float adrenalineOnSpaceBar = 20f;

    [Header("UI")]
    public Slider adrenalineSlider;

    [Header("Frenzy Mode (Wahnmodus) Settings")]
    public float frenzyDuration = 5f;
    public float frenzyCooldown = 10f;
    public bool isFrenzyActive = false;
    
    [Header("Player Stats Multipliers")]
    public float speedMultiplier = 1.5f;
    public float fireRateMultiplier = 1.5f;

    private float _lastIncreaseTime;
    private float _frenzyTimer;
    private float _cooldownTimer;
    private bool _isOnCooldown = false;

    void Start()
    {
        currentAdrenaline = 0f;
        UpdateUI();
    }

    void Update()
    {
        adrenalineSlider.value = currentAdrenaline;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddAdrenaline(adrenalineOnSpaceBar);
            Debug.Log("Leertaste gedrückt! Adrenalin jetzt: " + currentAdrenaline);
        }

        if (isFrenzyActive)
        {
            HandleFrenzyMode();
        }
        else
        {
            if (_isOnCooldown)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0)
                {
                    _isOnCooldown = false;
                    Debug.Log("Cooldown beendet! Wahnmodus wieder verfügbar.");
                }
            }
            
            HandleAdrenalineDecrease();
        }
    }

    public void AddAdrenaline(float amount)
    {
        if (isFrenzyActive || _isOnCooldown) return;

        currentAdrenaline += amount;
        _lastIncreaseTime = Time.time;

        if (currentAdrenaline >= maxAdrenaline)
        {
            currentAdrenaline = maxAdrenaline;
            ActivateFrenzyMode();
        }

        UpdateUI();
    }

    private void HandleAdrenalineDecrease()
    {
        if (currentAdrenaline > 0 && Time.time - _lastIncreaseTime >= delayBeforeDecrease)
        {
            currentAdrenaline -= decreaseRate * Time.deltaTime;
            if (currentAdrenaline < 0) currentAdrenaline = 0;
            UpdateUI();
        }
    }

    private void ActivateFrenzyMode()
    {
        isFrenzyActive = true;
        _frenzyTimer = frenzyDuration;
        currentAdrenaline = 0f;
        UpdateUI();

        ApplyFrenzyEffects(true);
        Debug.Log("Wahnmodus Aktiviert!");
    }

    private void HandleFrenzyMode()
    {
        _frenzyTimer -= Time.deltaTime;
        if (_frenzyTimer <= 0)
        {
            DeactivateFrenzyMode();
        }
    }

    private void DeactivateFrenzyMode()
    {
        isFrenzyActive = false;
        _isOnCooldown = true;
        _cooldownTimer = frenzyCooldown;
        ApplyFrenzyEffects(false);
        Debug.Log("Wahnmodus Beendet! Cooldown gestartet.");
    }

    private void ApplyFrenzyEffects(bool activate)
    {
        if (activate)
        {
        }
        else
        {
        }
    }

    private void UpdateUI()
    {
        if (adrenalineSlider != null)
        {
            adrenalineSlider.maxValue = maxAdrenaline;
            adrenalineSlider.value = currentAdrenaline;
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (isFrenzyActive || _isOnCooldown) return;

        float calculatedAdrenaline = damageAmount * adrenalineDamageMultiplier;
        float clampedAdrenaline = Mathf.Clamp(calculatedAdrenaline, minAdrenalineFromDamage, maxAdrenalineFromDamage);
        
        Debug.Log("Spieler nimmt Schaden! Adrenalin wird erhöht um: " + clampedAdrenaline);

        AddAdrenaline(clampedAdrenaline); 
    }

    public void OnEnemyKill()
    {
        AddAdrenaline(adrenalinePerKill); 
    }
}
