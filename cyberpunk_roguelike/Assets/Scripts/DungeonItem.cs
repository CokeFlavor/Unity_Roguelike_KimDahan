using UnityEngine;

public class DungeonItem : MonoBehaviour
{
    public enum ItemType { Health, FireRate, MoveSpeed, Damage, Bullet }
    [Header("Item Settings")]
    public ItemType itemType;
    public float value = 10f; // 회복량이나 증가 수치

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ApplyEffect(collision.gameObject);
            Destroy(gameObject); // 아이템 획득 후 파괴
        }
    }

    private void ApplyEffect(GameObject player)
    {
        // 플레이어의 컴포넌트들을 가져옵니다.
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        // 무기 컨트롤러와 이동 스크립트의 이름은 사용 중인 클래스명으로 확인해 주세요.
        WeaponController weapon = player.GetComponentInChildren<WeaponController>();
        Player movement = player.GetComponent<Player>();

        switch (itemType)
        {
            case ItemType.Health:
                if (health != null) health.Heal(value);
                break;
            case ItemType.FireRate:
                if (weapon != null) weapon.IncreaseFireRate(value); // 가령 0.1f 감소 등
                break;
            case ItemType.MoveSpeed:
                if (movement != null) movement.IncreaseMoveSpeed(value);
                break;
            case ItemType.Damage:
                if (weapon != null) weapon.IncreaseDamage(value);
                break;
            case ItemType.Bullet:
                if(weapon != null) weapon.IncreaseMaxAmmo((int)value);
                break;
        }
        Debug.Log($"{itemType} 아이템 획득!");
    }
}