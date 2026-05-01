using UnityEngine;
using UnityEngine.UI; // UI 사용을 위해 추가
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    public float moveSpeed = 3f;
    public float attackDamage = 10f;
    public float maxHP = 50f;
    public float attackRange = 8f; 
    public float attackCooldown = 2f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab; 
    [SerializeField] private Transform firePoint;     

    [Header("Boss Settings")]
    public bool isBoss = false; // 인스펙터에서 보스라면 체크
    private Slider bossHPBar;   // 화면 상단 보스 HP 바

    private float currentHP;
    private float lastAttackTime;
    private bool isPlayerInRoom = false;
    private Transform playerTransform;
    
    public int myRoomId = -1; 
    private PlayerLocationTracker tracker;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHP = maxHP;
        
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            tracker = playerTransform.GetComponent<PlayerLocationTracker>();
        }

        // 보스일 경우 상단 HP 바 연결
        if (isBoss)
        {
            GameObject barObj = GameObject.Find("BossHPBar");
            if (barObj != null)
            {
                bossHPBar = barObj.GetComponent<Slider>();
                bossHPBar.maxValue = maxHP;
                bossHPBar.value = maxHP;
                // 초기에는 비활성화 상태여야 함 (RoomManager가 켬)
            }
        }

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (tracker != null && tracker.currentRoomId == myRoomId)
        {
            isPlayerInRoom = true;
        }
        else
        {
            isPlayerInRoom = false;
            rb.linearVelocity = Vector2.zero;
        }

        if (!isPlayerInRoom || playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= attackRange)
        {
            AttackPlayer();
        }
        else
        {
            MoveToPlayer();
        }

        if (isPlayerInRoom) {
            Debug.DrawLine(transform.position, playerTransform.position, Color.red);
        }
    }

    void MoveToPlayer()
    {
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        if (direction.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
        }
    }

    void AttackPlayer()
    {
        rb.linearVelocity = Vector2.zero; 
        RotateFirePoint();

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Shoot();
            lastAttackTime = Time.time;
        }
    }

    void RotateFirePoint()
    {
        if (firePoint == null) return;

        Vector2 lookDir = (playerTransform.position - transform.position).normalized;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        
        if (transform.localScale.x < 0)
        {
            firePoint.rotation = Quaternion.Euler(0, 0, angle + 180f);
        }
        else
        {
            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        // 보스라면 상단 HP 바 갱신
        if (isBoss && bossHPBar != null)
        {
            bossHPBar.value = currentHP;
        }

        if (currentHP <= 0) Die();
    }

    void Die()
    {
        if (isBoss)
        {
            Debug.Log("보스 사망");
            if (bossHPBar != null) bossHPBar.gameObject.SetActive(false);
            
            // 보스 사망 시 탈출구 활성화 신호 (DungeonExit가 ActivateExit 함수를 가지고 있어야 함)[cite: 2]
            GameObject exitObj = GameObject.FindWithTag("Exit");
            if (exitObj != null)
            {
                exitObj.SendMessage("ActivateExit", SendMessageOptions.DontRequireReceiver);
            }
        }
        Destroy(gameObject);
    }
    // 보스 전용 UI 연결 함수 추가
    public void SetupBossUI(Slider hpBar)
    {
        if (isBoss)
        {
            bossHPBar = hpBar;
            bossHPBar.maxValue = maxHP; // 최대 체력 동기화
            bossHPBar.value = currentHP; // 현재 체력 동기화
            Debug.Log("보스와 UI가 성공적으로 연동되었습니다.");
        }
    }
}