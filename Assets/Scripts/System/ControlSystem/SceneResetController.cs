using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 설정 가능한 키로 게임을 완전히 재시작하는 컨트롤러
/// </summary>
public class SceneResetController : MonoBehaviour
{
    // Debug
    private const string LOG_PREFIX = "[SceneResetController]";
    
    [Header("재시작 설정")]
    [Tooltip("재시작을 실행할 키")]
    public KeyCode restartKey = KeyCode.Escape;
    
    [Tooltip("키 입력으로 재시작 활성화")]
    public bool enableKeyRestart = true;
    
    [Tooltip("재시작 전 대기 시간 (초)")]
    public float restartDelay = 0.5f;
    
    [Tooltip("재시작 중 중복 실행 방지")]
    public bool isRestarting = false;
    
    [Header("재시작 방식 선택")]
    [Tooltip("true: 게임 완전 재시작 (권장), false: 첫 씬으로 이동")]
    public bool useFullRestart = true;
    
    [Tooltip("첫 씬 이름 (useFullRestart가 false일 때 사용)")]
    public string firstSceneName = "SoulSmith_MainScene";

    private void Update()
    {
        if (enableKeyRestart && Input.GetKeyDown(restartKey) && !isRestarting)
        {
            StartRestart();
        }
    }
    
    /// <summary>
    /// 게임 완전 재시작 시작
    /// </summary>
    public void StartRestart()
    {
        if (isRestarting)
        {
            Debug.Log($"{LOG_PREFIX} 이미 재시작 중입니다.");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 게임 재시작 시작 (키: {restartKey})");
        isRestarting = true;
        
        // DOTween 애니메이션 중지
        DOTween.KillAll();
        
        if (useFullRestart)
        {
            // 완전 재시작 (권장)
            StartCoroutine(DelayedFullRestart());
        }
        else
        {
            // DontDestroyOnLoad 오브젝트 정리 후 첫 씬으로 이동
            StartCoroutine(DelayedSceneRestart());
        }
    }
    
    /// <summary>
    /// 재시작 키 변경
    /// </summary>
    public void SetRestartKey(KeyCode newKey)
    {
        restartKey = newKey;
        Debug.Log($"{LOG_PREFIX} 재시작 키가 {newKey}로 변경되었습니다.");
    }
    
    /// <summary>
    /// 키 입력 재시작 기능 토글
    /// </summary>
    public void SetKeyRestartEnabled(bool enabled)
    {
        enableKeyRestart = enabled;
        string status = enabled ? "활성화" : "비활성화";
        Debug.Log($"{LOG_PREFIX} 키 입력 재시작이 {status}되었습니다.");
    }
    
    /// <summary>
    /// 지연 후 게임 완전 재시작
    /// </summary>
    private IEnumerator DelayedFullRestart()
    {
        Debug.Log($"{LOG_PREFIX} {restartDelay}초 후 게임 완전 재시작");
        yield return new WaitForSeconds(restartDelay);
        
        // 게임 완전 재시작 (모든 DontDestroyOnLoad 오브젝트도 초기화)
#if UNITY_EDITOR
        // 에디터에서는 플레이 모드 중지 후 다시 시작
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log($"{LOG_PREFIX} 에디터 플레이 모드 중지 - 수동으로 다시 플레이 버튼을 눌러주세요");
#else
        // 빌드에서는 게임 완전 재시작
        System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".exe"));
        Application.Quit();
        Debug.Log($"{LOG_PREFIX} 게임 완전 재시작 실행");
#endif
    }
    
    /// <summary>
    /// 지연 후 DontDestroyOnLoad 정리 및 첫 씬으로 이동
    /// </summary>
    private IEnumerator DelayedSceneRestart()
    {
        Debug.Log($"{LOG_PREFIX} {restartDelay}초 후 DontDestroyOnLoad 정리 및 첫 씬 이동");
        yield return new WaitForSeconds(restartDelay);
        
        // DontDestroyOnLoad 오브젝트들 찾아서 삭제
        DestroyDontDestroyOnLoadObjects();
        
        // 첫 씬으로 이동
        SceneManager.LoadScene(firstSceneName, LoadSceneMode.Single);
        
        Debug.Log($"{LOG_PREFIX} 첫 씬으로 이동 완료: {firstSceneName}");
    }
    
    /// <summary>
    /// DontDestroyOnLoad 오브젝트들 강제 삭제
    /// </summary>
    private void DestroyDontDestroyOnLoadObjects()
    {
        Debug.Log($"{LOG_PREFIX} DontDestroyOnLoad 오브젝트 정리 시작");
        
        // DontDestroyOnLoad 씬의 모든 루트 오브젝트 찾기
        GameObject[] dontDestroyObjects = GameObject.FindObjectsOfType<GameObject>();
        int destroyCount = 0;
        
        foreach (GameObject obj in dontDestroyObjects)
        {
            // 루트 오브젝트이고 DontDestroyOnLoad 씬에 있는 경우
            if (obj.transform.parent == null && obj.scene.name == "DontDestroyOnLoad")
            {
                Debug.Log($"{LOG_PREFIX} DontDestroyOnLoad 오브젝트 삭제: {obj.name}");
                Destroy(obj);
                destroyCount++;
            }
        }
        
        Debug.Log($"{LOG_PREFIX} 총 {destroyCount}개의 DontDestroyOnLoad 오브젝트 삭제 완료");
    }
    
    /// <summary>
    /// 수동으로 재시작 실행 (Inspector에서 테스트용)
    /// </summary>
    [ContextMenu("수동 재시작 실행")]
    public void ManualRestart()
    {
        StartRestart();
    }
    
    /// <summary>
    /// 강제로 완전 재시작 (테스트용)
    /// </summary>
    [ContextMenu("강제 완전 재시작")]
    public void ForceFullRestart()
    {
        bool originalSetting = useFullRestart;
        useFullRestart = true;
        StartRestart();
        useFullRestart = originalSetting;
    }
    
    /// <summary>
    /// 현재 설정 상태 확인 (테스트용)
    /// </summary>
    [ContextMenu("현재 설정 확인")]
    public void CheckCurrentSettings()
    {
        Debug.Log($"{LOG_PREFIX} === 현재 설정 ===");
        Debug.Log($"재시작 키: {restartKey}");
        Debug.Log($"키 입력 활성화: {enableKeyRestart}");
        Debug.Log($"완전 재시작: {useFullRestart}");
        Debug.Log($"첫 씬 이름: {firstSceneName}");
        Debug.Log($"재시작 딜레이: {restartDelay}초");
    }
} 