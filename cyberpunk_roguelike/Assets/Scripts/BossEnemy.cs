using UnityEngine;

public class BossEnemy : MonoBehaviour
{
    public delegate void BossDeathHandler();
    public static event BossDeathHandler OnBossDefeated;

    // 보스 사망 시 호출
    public void OnDestroy()
    {
        // 씬이 종료될 때가 아닌 실제 사망 시에만 실행되도록 체크
        if (!gameObject.scene.isLoaded) return;
        
        OnBossDefeated?.Invoke();
        Debug.Log("보스 처치! 탈출구가 활성화되었습니다.");
    }
}