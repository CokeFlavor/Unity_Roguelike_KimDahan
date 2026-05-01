using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float damage = 10f; // 적 무기 설정에 따라 조절
    public float speed = 15f;
    public float lifeTime = 3f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 생성될 때 정해진 방향(transform.right)으로 발사
        rb.linearVelocity = transform.right * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 부딪힌 대상이 플레이어인지 확인
        if (collision.CompareTag("Player"))
        {
            // 2. 플레이어의 체력 스크립트를 가져옴
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                // 3. 데미지 입힘
                playerHealth.TakeDamage(damage);
            }

            // 4. 총알은 소멸
            Destroy(gameObject);
        }

        // 벽에 부딪히면 제거
        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}