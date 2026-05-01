using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public HealthBar healthBar;
    void Start()
    {
        currentHealth = maxHealth;
        // 시작할 때 UI 초기화
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        
        // 체력 UI 갱신
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        if (currentHealth <= 0) Die();
    }
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthBar != null) healthBar.SetHealth(currentHealth);
    }
    void Die()
    {
        Debug.Log("플레이어 사망");
        GameUIManager.Instance.ShowGameOverScreen();
    }
}