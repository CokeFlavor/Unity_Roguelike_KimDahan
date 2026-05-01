using UnityEngine;

public class DungeonDoor : MonoBehaviour
{
    public int roomId = -1; // Dungeonizer가 배치 후 설정해줄 ID
    private Collider2D physicsCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        physicsCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Lock()
    {
        physicsCollider.isTrigger = false; // 지나가지 못하게 물리 충돌 활성화
        spriteRenderer.color = Color.red;   // 시각적 피드백 (사이버펑크 레드)
    }

    public void Unlock()
    {
        physicsCollider.isTrigger = true;  // 통과 가능하게 변경
        spriteRenderer.color = Color.cyan; // 해제 상태 (네온 블루)
    }
}