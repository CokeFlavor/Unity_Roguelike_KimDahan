using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashPower = 15f;    // 대쉬 힘
    [SerializeField] private float dashDuration = 0.2f; // 대쉬 지속 시간
    [SerializeField] private float dashCooldown = 1f;   // 대쉬 쿨타임
    [SerializeField] private int maxDashCount = 2;      // 최대 대쉬 가능 횟수

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 moveInput;
    
    // 대쉬 관련 상태 변수
    private bool isDashing;
    private int currentDashCount;
    private float lastDashTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        currentDashCount = maxDashCount; // 시작 시 대쉬 횟수 충전
    }

    void Update()
    {
        if (isDashing) return; // 대쉬 중에는 조작 불가

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        animator.SetFloat("Speed", moveInput.sqrMagnitude);

        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }

        // 대쉬 입력 체크 (Left Shift)
        if (Input.GetKeyDown(KeyCode.LeftShift) && currentDashCount > 0 && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(Dash());
        }

        // (선택 사항) 시간이 지나면 대쉬 횟수 회복 로직을 넣을 수도 있습니다.
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        rb.linearVelocity = moveInput.normalized * moveSpeed;
    }
    private IEnumerator Dash()
    {
        isDashing = true;
        currentDashCount--;
        lastDashTime = Time.time;

        // 현재 바라보고 있는 방향 또는 이동 방향으로 대쉬
        Vector2 dashDir = moveInput.normalized;
        if (dashDir == Vector2.zero) // 멈춰있을 때는 캐릭터가 바라보는 방향으로
        {
            dashDir = new Vector2(transform.localScale.x, 0);
        }

        rb.linearVelocity = dashDir * dashPower;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        
        // 대쉬 횟수 회복 (간단한 예시: 대쉬가 끝나고 일정 시간 뒤 회복)
        Invoke(nameof(RestoreDash), dashCooldown);
    }

    private void RestoreDash()
    {
        if (currentDashCount < maxDashCount)
        {
            currentDashCount++;
        }
    }
    public void IncreaseMoveSpeed(float amount)
    {
        moveSpeed += amount;
    }
}