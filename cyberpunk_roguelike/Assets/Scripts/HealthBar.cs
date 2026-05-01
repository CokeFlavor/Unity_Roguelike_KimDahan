using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트 사용을 위해 필수

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;

    // 최대 체력 설정
    public void SetMaxHealth(float health)
    {
        hpSlider.maxValue = health;
        hpSlider.value = health;
    }

    // 현재 체력 업데이트
    public void SetHealth(float health)
    {
        hpSlider.value = health;
    }
}