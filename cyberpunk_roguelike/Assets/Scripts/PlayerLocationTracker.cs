using UnityEngine;
using Dungeonizer;
using UnityEngine.UI;
public class PlayerLocationTracker : MonoBehaviour
{
    public Dungeonizer.Dungeonizer dungeonizer;
    public int currentRoomId = -1;
    
    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    private int gridX;
    private int gridY;

    [SerializeField] private GameObject bossHPBarUI;
    void Update()
    {
        if (dungeonizer == null) return;

        // 1. 플레이어 월드 좌표를 그리드로 변환
        gridX = Mathf.RoundToInt(transform.position.x / dungeonizer.tileScaling);
        gridY = dungeonizer.makeIt3d 
            ? Mathf.RoundToInt(transform.position.z / dungeonizer.tileScaling) 
            : Mathf.RoundToInt(transform.position.y / dungeonizer.tileScaling);

        // 2. 해당 좌표의 타일을 찾아 방 ID 갱신
        var dungeonData = dungeonizer.GetDungeonData();
        if (dungeonData != null && dungeonData.map != null)
        {
            // type 1(방 바닥) 또는 type 3(복도/문) 타일을 모두 검색
            var tile = dungeonData.FindAt(gridX, gridY, t => t.type == 1 || t.type == 3);
            
            if (tile != null && tile.room != null)
            {
                if (currentRoomId != tile.room.room_id)
                {
                    currentRoomId = tile.room.room_id;
                    if (showDebugInfo) Debug.Log($"방 입장! 현재 방 ID: {currentRoomId}");
                }
            }
            else
            {
                // 방이나 복도가 아닌 곳(벽 내부 등)에 있을 때
                if (currentRoomId != -1)
                {
                    currentRoomId = -1;
                    if (showDebugInfo) Debug.Log("방에서 벗어남");
                }
            }
        }
    }

    // Scene 뷰에서 현재 추적 중인 그리드 위치를 시각화
    void OnDrawGizmos()
    {
        if (!showDebugInfo || dungeonizer == null) return;

        Gizmos.color = Color.green;
        Vector3 debugPos = dungeonizer.makeIt3d 
            ? new Vector3(gridX * dungeonizer.tileScaling, 0.5f, gridY * dungeonizer.tileScaling)
            : new Vector3(gridX * dungeonizer.tileScaling, gridY * dungeonizer.tileScaling, 0);
            
        Gizmos.DrawWireCube(debugPos, Vector3.one * dungeonizer.tileScaling);
    }
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("RoomArea")) {
            RoomIDHolder holder = other.GetComponent<RoomIDHolder>();
            if (holder != null) {
                currentRoomId = holder.roomId;
                
                // 모든 방 진입 시 해당 RoomManager를 깨워줍니다.
                RoomManager[] managers = FindObjectsByType<RoomManager>(FindObjectsSortMode.None);
                foreach (var rm in managers) {
                    if (rm.roomId == currentRoomId) {
                        rm.OnPlayerEnter(); // 보스방이든 일반방이든 강제 실행
                        break;
                    }
                }

                // 보스 방인 경우에만 UI 추가 활성화[cite: 2]
                if (currentRoomId == Dungeonizer.Dungeonizer.TargetRoomId) {
                    ActivateBossUI();
                }
            }
        }
    }
    void ActivateBossUI()
    {
        if (bossHPBarUI != null)
        {
            bossHPBarUI.SetActive(true); // 보스 UI 활성화
            
            // 현재 보스 방의 RoomManager를 찾아서 문을 잠급니다.
            RoomManager[] managers = FindObjectsByType<RoomManager>(FindObjectsSortMode.None);
            foreach (var rm in managers)
            {
                if (rm.roomId == currentRoomId)
                {
                    rm.ForceLockdown(); // 별도로 분리된 잠금 함수 호출
                    break;
                }
            }

            // 보스 데이터 연동 (이전 단계 코드)
            ConnectBossData();
        }
    }

    void ConnectBossData()
    {
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        Slider hpSlider = bossHPBarUI.GetComponent<Slider>();

        foreach (var enemy in enemies)
        {
            if (enemy.myRoomId == currentRoomId && enemy.isBoss)
            {
                enemy.SetupBossUI(hpSlider); // 보스 체력 데이터 동기화
                break;
            }
        }
    }
    void NotifyRoomManager(int id)
    {
        // 씬 내의 해당 방 Manager를 찾아 입장 알림
        RoomManager[] managers = FindObjectsByType<RoomManager>(FindObjectsSortMode.None);
        foreach (var rm in managers)
        {
            if (rm.roomId == id)
            {
                rm.OnPlayerEnter();
                break;
            }
        }
    }
}