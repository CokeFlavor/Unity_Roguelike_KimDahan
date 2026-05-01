using UnityEngine;

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public Sprite weaponSprite;     // 무기 이미지 추가
    public float damage = 10f;
    public float fireRate = 0.2f;
    public int maxAmmo = 30;
    public float reloadTime = 1.5f;
    public GameObject bulletPrefab;
}