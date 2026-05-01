using UnityEngine;
using System.Collections;

public class WeaponController : MonoBehaviour
{
    public WeaponData currentWeapon; // 현재 장착된 무기 정보
    public SpriteRenderer weaponRenderer;
    public Transform firePoint;      // 총알이 나갈 위치 (플레이어 앞쪽)

    private int currentAmmo;         // 현재 남은 총알
    private float lastFireTime;      // 마지막 발사 시간
    private bool isReloading;        // 재장전 중인지 확인

    void Start()
    {
        // 시작할 때 탄창을 채웁니다.
        currentAmmo = currentWeapon.maxAmmo;
        UpdateWeaponSprite();
    }

    void Update()
    {
        if (isReloading) return;
        HandleWeaponRotation();
        // 마우스 왼쪽 버튼 클릭 시 발사
        if (Input.GetButton("Fire1") && Time.time >= lastFireTime + currentWeapon.fireRate)
        {
            if (currentAmmo > 0)
            {
                Shoot();
            }
            else
            {
                StartCoroutine(Reload());
            }
        }

        // R키를 눌러 수동 재장전
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < currentWeapon.maxAmmo)
        {
            StartCoroutine(Reload());
        }
    }

    void Shoot()
    {
        lastFireTime = Time.time;
        currentAmmo--;
        PlayerUIManager.Instance.UpdateAmmoUI(currentAmmo, currentWeapon.maxAmmo);

        // 총알 생성 및 발사
        GameObject bullet = Instantiate(currentWeapon.bulletPrefab, firePoint.position, firePoint.rotation);
        
        // 총알 스크립트에 데미지 정보 전달 (총알에 Bullet 스크립트가 있다고 가정)
        // bullet.GetComponent<Bullet>().damage = currentWeapon.damage;

        Debug.Log($"{currentWeapon.weaponName} 발사! 남은 탄약: {currentAmmo}/{currentWeapon.maxAmmo}");
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("재장전 중...");

        yield return new WaitForSeconds(currentWeapon.reloadTime);

        currentAmmo = currentWeapon.maxAmmo;
        isReloading = false;
        Debug.Log("재장전 완료!");
    }
    void UpdateWeaponSprite()
    {
        if (currentWeapon != null && weaponRenderer != null)
        {
            weaponRenderer.sprite = currentWeapon.weaponSprite;
        }
    }

    void HandleWeaponRotation()
    {
        // 1. 마우스 위치를 월드 좌표로 변환
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDir = (mousePos - transform.position).normalized;

        // 2. 마우스를 향한 월드 기준 각도 계산
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;

        // 3. 부모(Player)의 Scale 반전 여부에 따른 축 보정
        if (transform.parent.localScale.x < 0)
        {
            // 플레이어가 왼쪽(-1)을 보고 있다면, 자식의 좌표계도 뒤집힌 상태입니다.
            // 이때는 각도에 180도를 더해줘야 '빨간색 화살표'가 마우스를 정확히 가리킵니다.
            transform.rotation = Quaternion.Euler(0, 0, angle);
            weaponRenderer.flipX = true;
        }
        else
        {
            // 플레이어가 오른쪽(1)을 보고 있다면 정상 각도를 적용합니다.
            transform.rotation = Quaternion.Euler(0, 0, angle); 
            weaponRenderer.flipX = false;
        }

        // 4. 스프라이트 상하 반전 (조준 각도에 따른 시각적 보정)
        // 플레이어의 반전과 상관없이 '실제 조준 방향'이 왼쪽이면 뒤집어줍니다.
        if (angle > 90 || angle < -90)
        {
            weaponRenderer.flipY = true;
        }
        else
        {
            weaponRenderer.flipY = false;
        }   
    }
    public void IncreaseFireRate(float amount)
    {
        // 발사 간격(cooldown)을 줄여서 속도를 높입니다.
        currentWeapon.fireRate = Mathf.Max(0.05f, currentWeapon.fireRate - amount); 
    }

    public void IncreaseDamage(float amount)
    {
        currentWeapon.damage += amount;
        PlayerUIManager.Instance.UpdatePowerUI(currentWeapon.damage);
    }

    public void IncreaseMaxAmmo(int amount)
    {
        currentWeapon.maxAmmo += amount;
    }
}