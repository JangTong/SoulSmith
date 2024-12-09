using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤 인스턴스

    public int playerGold = 0; // 플레이어의 소지금
    public TextMeshProUGUI goldText; // 소지금을 표시할 TextMeshProUGUI 텍스트

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 변경되어도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateGoldUI(); // 초기 소지금 표시
    }

    /// 소지금을 증가시킵니다.
    public void AddGold(int amount)
    {
        playerGold += amount;
        UpdateGoldUI();
    }

    /// 소지금을 감소시킵니다.
    public void SubtractGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
            UpdateGoldUI();
        }
        else
        {
            Debug.LogWarning("소지금이 부족합니다.");
        }
    }

    /// 소지금을 표시하는 UI를 업데이트합니다.
    private void UpdateGoldUI()
    {
        if (goldText != null)
        {
            goldText.text = $"{playerGold}";
        }
        else
        {
            Debug.LogError("goldText가 설정되지 않았습니다.");
        }
    }
}
