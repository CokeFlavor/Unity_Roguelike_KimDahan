using UnityEngine;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    public static PlayerUIManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI powerText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 게임 시작 시 약간의 지연 후 초기화 (다른 스크립트의 Start가 완료된 후 실행되도록)
        Invoke("InitializeUI", 0.1f);
    }

    void InitializeUI()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 2. 장탄 수 및 공격력 초기값 (WeaponController 스크립트)
        var weapon = player.GetComponentInChildren<WeaponController>();
        if (weapon != null)
        {
            UpdateAmmoUI(weapon.currentWeapon.maxAmmo, weapon.currentWeapon.maxAmmo);
            UpdatePowerUI(weapon.currentWeapon.damage);
        }
    }
    public void UpdateAmmoUI(int current, int max)
    {
        if (ammoText != null) ammoText.text = $"Ammo: {current} / {max}";
    }

    public void UpdatePowerUI(float power)
    {
        if (powerText != null) powerText.text = $"Power: {power:F1}"; // 소수점 첫째자리까지
    }
}