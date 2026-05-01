using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject clearPanel;
    public GameObject gameOverPanel;
    
    public static bool skipStartMenu = false;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작 시 메인 UI만 표시하고 게임 일시정지 (선택 사항)
        if (skipStartMenu)
        {
            Time.timeScale = 1f;
            startPanel.SetActive(false);
            skipStartMenu = false; // 다음을 위해 초기화
        }
        else
        {
            ShowStartMenu();
        }
    }

    // --- 화면 표시 로직 ---

    public void ShowStartMenu()
    {
        Time.timeScale = 0f; // 게임 멈춤
        startPanel.SetActive(true);
        clearPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        Debug.Log("ShowStartMenu 실행됨!"); // 콘솔창에 뜨는지 확인
        if (startPanel != null)
        {
            startPanel.SetActive(true);
            Debug.Log(startPanel.name + " 패널을 활성화했습니다.");
        }
    }

    public void ShowClearScreen()
    {
        Time.timeScale = 0f;
        clearPanel.SetActive(true);
    }

    public void ShowGameOverScreen()
    {
        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }

    // --- 버튼 연결 로직 (Button의 OnClick에 연결) ---

    public void OnStartButtonClick()
    {
        Time.timeScale = 1f; // 게임 재개
        startPanel.SetActive(false);
        // 필요 시 씬을 새로 로드하거나 던전 생성을 시작합니다.
    }

    public void OnRestartButtonClick()
    {
        skipStartMenu = true; // "다음에 시작할 땐 메뉴 띄우지 마"라고 알림
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuButtonClick()
    {
        // 메인 메뉴 씬이 있다면 이동, 없다면 시작 패널 표시
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnExitButtonClick()
{
    #if UNITY_EDITOR
        // 에디터에서 실행 중일 때는 재생 모드를 끕니다.
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // 실제 빌드된 게임에서는 프로그램을 종료합니다.
        Application.Quit();
    #endif
    
    Debug.Log("게임이 종료되었습니다.");
}
}