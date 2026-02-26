using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;

    [Header("Heartbeat")]
    public float restingHeartRate = 60f;
    public float maxHeartRate = 200f;
    public float minHeartRate = 41f;
    public float heartRateIncrease = 30f;
    public float heartRateDecrease = 15f;
    public float slowDownThreshold = 170f;
    public float minSpeedMultiplier = 0.3f;
    public float sprintHeartRateMultiplier = 2.5f;

    [HideInInspector]
    public float currentHeartRate;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isDead = false;
    private bool isMoving = false;
    private bool isSprinting = false;
    private float speedMultiplier = 1f;

    public TextMeshProUGUI heartRateText;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHeartRate = restingHeartRate;
    }

    void Update()
    {
        if (isDead) return;

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        isMoving = moveInput.magnitude > 0.1f;
        isSprinting = isMoving && Input.GetKey(KeyCode.LeftShift);

        UpdateHeartRate();
        UpdateSpeedMultiplier();

        if (currentHeartRate >= maxHeartRate)
        {
            Die();
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        float currentSpeed = moveSpeed * speedMultiplier;
        if (isSprinting) currentSpeed *= sprintMultiplier;
        rb.linearVelocity = moveInput * currentSpeed;

        heartRateText.text = $"{currentHeartRate:F0} BPM";
    }

    void UpdateHeartRate()
    {
        if (isMoving)
        {
            float increase = heartRateIncrease;
            if (isSprinting) increase *= sprintHeartRateMultiplier;
            currentHeartRate += increase * Time.unscaledDeltaTime;
        }
        else
            currentHeartRate -= heartRateDecrease * Time.unscaledDeltaTime;

        currentHeartRate = Mathf.Clamp(currentHeartRate, restingHeartRate, maxHeartRate);
    }

    void UpdateSpeedMultiplier()
    {
        if (currentHeartRate >= slowDownThreshold)
        {
            float t = (currentHeartRate - slowDownThreshold) / (maxHeartRate - slowDownThreshold);
            speedMultiplier = Mathf.Lerp(1f, minSpeedMultiplier, t);
        }
        else
        {
            speedMultiplier = 1f;
        }
    }

    void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        speedMultiplier = 0f;
        Debug.Log("Player died! Heart rate too high!");
    }

    public void IncreaseHeartRate(float amount)
    {
        currentHeartRate += amount;
    }
}
