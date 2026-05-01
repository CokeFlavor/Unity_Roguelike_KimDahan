using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonExit : MonoBehaviour
{
    public bool isBossDefeated = false;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        BossEnemy.OnBossDefeated += ActivateExit; // 보스 사망 이벤트 구독
        
        // 처음엔 비활성 상태 (색상이나 투명도로 표시)
        if (sr != null) sr.color = new Color(1, 1, 1, 0.3f); 
    }

    void OnDestroy()
    {
        BossEnemy.OnBossDefeated -= ActivateExit; // 이벤트 구독 해제
    }

    void ActivateExit()
    {
        isBossDefeated = true;
        if (sr != null) sr.color = Color.white; // 밝게 활성화
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (isBossDefeated)
            {
                GameClear();
            }
            else
            {
                Debug.Log("보스를 먼저 처치해야 합니다!");
            }
        }
    }

    void GameClear()
    {
        Debug.Log("★ 게임 클리어 ★");
        GameUIManager.Instance.ShowClearScreen();
        // 클리어 UI를 띄우거나 다음 씬으로 이동
        // SceneManager.LoadScene("ClearScene");
    }
}