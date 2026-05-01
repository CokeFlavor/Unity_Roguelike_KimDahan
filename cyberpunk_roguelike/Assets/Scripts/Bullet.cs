using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 20f; // 총알 속도
    [SerializeField] private float lifeTime = 2f; // 총알 생존 시간
    
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // 총알이 생성되자마자 자신의 '오른쪽(빨간 화살표)' 방향으로 힘을 가합니다.
        // WeaponController에서 총알을 생성할 때 회전값을 주었으므로 
        // transform.right가 마우스 방향이 됩니다.
        rb.linearVelocity = transform.right * speed;

        // 일정 시간이 지나면 자동으로 사라지게 설정 (메모리 관리)
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 적에게 부딪혔을 때 처리
        if (collision.CompareTag("Enemy"))
        {
            // EnemyAI 스크립트의 TakeDamage 호출 (이전에 만든 함수)
            collision.GetComponent<EnemyAI>()?.TakeDamage(10f);
            Destroy(gameObject); // 적과 부딪히면 총알 제거
        }
        
        // 벽에 부딪혔을 때 처리 (Dungeonizer 벽 태그 확인 필요)
        if (collision.CompareTag("Wall"))
        {
            Destroy(gameObject);
        }
    }
}