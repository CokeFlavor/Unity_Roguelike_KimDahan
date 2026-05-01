using UnityEngine;
using System.Collections.Generic;

public class RoomManager : MonoBehaviour
{
    public int roomId;
    public List<EnemyAI> enemiesInRoom = new List<EnemyAI>();
    public List<DungeonDoor> doors = new List<DungeonDoor>();
    
    private bool isLockedDown = false;
    private bool isCleared = false;

    public void OnPlayerEnter()
    {
        if (isCleared || isLockedDown) return;

        if (enemiesInRoom.Count > 0)
        {
            isLockedDown = true;
            foreach (var door in doors) door.Lock();
        }
    }

    void Update()
    {
        if (isLockedDown && !isCleared)
        {
            enemiesInRoom.RemoveAll(e => e == null); // 죽은 적 제거

            if (enemiesInRoom.Count == 0)
            {
                foreach (var door in doors) door.Unlock();
                isLockedDown = false;
                isCleared = true;
            }
        }
    }
    
    public void ForceLockdown()
    {
        if (isLockedDown) return; // 이미 잠겼다면 무시

        isLockedDown = true;
        foreach (var door in doors)
        {
            if (door != null) door.Lock(); // 문을 즉시 잠금
        }
        Debug.Log($"{roomId}번 보스 방 문이 강제로 잠겼습니다.");
    }
}