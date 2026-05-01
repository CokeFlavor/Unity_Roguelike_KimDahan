using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    public float maxHealth = 500f;
    public float currentHealth;
    private Slider bossHPBar;

    void Start()
    {
        currentHealth = maxHealth;
        // 씬 내의 BossHPBar를 찾아 연결 (태그나 이름을 활용)
        GameObject barObj = GameObject.Find("BossHPBar");
        if (barObj != null)
        {
            bossHPBar = barObj.GetComponent<Slider>();
            bossHPBar.maxValue = maxHealth;
            bossHPBar.value = maxHealth;
        }
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (bossHPBar != null) bossHPBar.value = currentHealth;

        if (currentHealth <= 0)
        {
            if (bossHPBar != null) bossHPBar.gameObject.SetActive(false);
            Destroy(gameObject); // BossEnemy.cs의 OnDestroy가 실행되어 Exit가 활성화됨
        }
    }
}