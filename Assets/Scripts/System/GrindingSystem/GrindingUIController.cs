using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GrindingUIController : MonoBehaviour
{
    private const string LOG_PREFIX = "[GrindingUI]";
    
    [Header("UI 컴포넌트")]
    public RectTransform grindingPanel;
    public RectTransform gaugeBackground;
    public RectTransform perfectZone;
    public RectTransform goodZone;
    public RectTransform cursor;
    public TextMeshProUGUI judgmentText;
    
    [Header("판정 결과 이미지")]
    public Image judgmentImage;
    public Sprite perfectSprite;
    public Sprite goodSprite;
    
    // 미니게임 상태
    private bool isPlaying = false;
    private float cursorSpeed = 1f;
    private float currentPosition = 0f;
    private bool movingRight = true;
    private GrindingWheel grindingWheel;
    private GameObject currentWeapon;
    private bool canHit = false;
    private GrindingMiniGame.GrindingResult lastResult;
    
    private void Awake()
    {
        if (grindingPanel != null)
        {
            grindingPanel.gameObject.SetActive(false);
        }
        
        // 판정 이미지 초기화
        if (judgmentImage != null)
        {
            judgmentImage.gameObject.SetActive(false);
        }
        
        Debug.Log($"{LOG_PREFIX} UI 컨트롤러 초기화 완료");
    }
    
    /// <summary>
    /// 연마 UI 오픈
    /// </summary>
    public void OpenUI(GameObject weapon, float weaponAttack, GrindingWheel wheel)
    {
        currentWeapon = weapon;
        grindingWheel = wheel;
        cursorSpeed = wheel.CalculateCursorSpeed(weaponAttack);
        
        // UI 활성화
        if (grindingPanel != null)
        {
            grindingPanel.gameObject.SetActive(true);
        }
        
        // 무기 정보 표시
        var weaponItem = weapon.GetComponent<ItemComponent>();
        if (weaponItem != null && judgmentText != null)
        {
            judgmentText.text = $"{weaponItem.itemName} 연마";
        }
        
        // 난이도 표시
        if (judgmentText != null)
        {
            string difficulty = wheel.miniGame.GetDifficultyText(weaponAttack);
            string details = wheel.miniGame.GetDifficultyDetails(weaponAttack);
            judgmentText.text = $"난이도: {difficulty}\n{details}";
        }
        
        // 게이지 존 설정
        SetupGaugeZones(weaponAttack);
        
        Debug.Log($"{LOG_PREFIX} UI 오픈 - 무기: {weaponItem?.itemName}, 속도: {cursorSpeed:F1}x");
    }
    
    /// <summary>
    /// 연마 UI 닫기
    /// </summary>
    public void CloseUI()
    {
        if (grindingPanel != null)
        {
            grindingPanel.gameObject.SetActive(false);
        }
        
        ResetUI();
        Debug.Log($"{LOG_PREFIX} UI 닫기");
    }
    
    /// <summary>
    /// 미니게임 라운드 시작
    /// </summary>
    public IEnumerator PlayRound(int round, float weaponAttack)
    {
        isPlaying = true;
        canHit = false;
        
        // 라운드 표시
        if (judgmentText != null)
        {
            judgmentText.text = $"{round + 1} / 3 라운드";
        }
        
        // 준비 시간
        yield return new WaitForSeconds(0.5f);
        
        // 커서 시작
        ResetCursor();
        canHit = true;
        
        // 타임아웃 (5초)
        float timeout = 5f;
        float elapsed = 0f;
        
        while (isPlaying && elapsed < timeout)
        {
            UpdateCursor();
            
            // 스페이스바 입력 체크
            if (Input.GetKeyDown(KeyCode.Space) && canHit)
            {
                var result = ProcessHit(weaponAttack);
                ShowHitResult(result);
                
                isPlaying = false;
                break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 타임아웃 시 실패 처리
        if (elapsed >= timeout && isPlaying)
        {
            var failResult = new GrindingMiniGame.GrindingResult(
                GrindingMiniGame.JudgmentType.Fail, 
                0f, 
                0f, 
                currentPosition
            );
            ShowHitResult(failResult);
            isPlaying = false;
        }
        
        canHit = false;
        yield return new WaitForSeconds(0.3f);
    }
    
    private void SetupGaugeZones(float weaponAttack)
    {
        if (grindingWheel == null) return;
        
        // Perfect 존 설정
        if (perfectZone != null)
        {
            float perfectRange = grindingWheel.GetAdjustedRange(grindingWheel.PerfectRange, weaponAttack);
            float perfectWidth = perfectRange * 2f * gaugeBackground.rect.width;
            perfectZone.sizeDelta = new Vector2(perfectWidth, perfectZone.sizeDelta.y);
        }
        
        // Good 존 설정
        if (goodZone != null)
        {
            float goodRange = grindingWheel.GetAdjustedRange(grindingWheel.GoodRange, weaponAttack);
            float goodWidth = goodRange * 2f * gaugeBackground.rect.width;
            goodZone.sizeDelta = new Vector2(goodWidth, goodZone.sizeDelta.y);
        }
    }
    
    private void ResetCursor()
    {
        currentPosition = 0f;
        movingRight = true;
        UpdateCursorPosition();
    }
    
    private void UpdateCursor()
    {
        if (!isPlaying) return;
        
        float speed = cursorSpeed * Time.deltaTime;
        
        if (movingRight)
        {
            currentPosition += speed;
            if (currentPosition >= 1f)
            {
                currentPosition = 1f;
                movingRight = false;
            }
        }
        else
        {
            currentPosition -= speed;
            if (currentPosition <= 0f)
            {
                currentPosition = 0f;
                movingRight = true;
            }
        }
        
        UpdateCursorPosition();
    }
    
    private void UpdateCursorPosition()
    {
        if (cursor != null && gaugeBackground != null)
        {
            float xPos = (currentPosition - 0.5f) * gaugeBackground.rect.width;
            cursor.anchoredPosition = new Vector2(xPos, cursor.anchoredPosition.y);
        }
    }
    
    private GrindingMiniGame.GrindingResult ProcessHit(float weaponAttack)
    {
        var result = grindingWheel.miniGame.CalculateResult(currentPosition, weaponAttack);
        
        Debug.Log($"{LOG_PREFIX} {currentPosition:F3}, 결과: {result.judgment}");
        
        return result;
    }
    
    private void ShowHitResult(GrindingMiniGame.GrindingResult result)
    {
        // 판정 텍스트 표시
        if (judgmentText != null)
        {
            judgmentText.text = grindingWheel.miniGame.GetJudgmentText(result.judgment);
            judgmentText.color = grindingWheel.miniGame.GetJudgmentColor(result.judgment);
            
            // 페이드 애니메이션
            judgmentText.DOFade(1f, 0.2f).OnComplete(() => {
                judgmentText.DOFade(0f, 1f).SetDelay(1f);
            });
        }
        
        // 판정 이미지 표시
        ShowJudgmentImage(result.judgment);
        
        // **판정 파티클 효과 재생**
        if (grindingWheel != null)
        {
            grindingWheel.PlayJudgmentParticle(result.judgment);
        }
        
        // 라운드 결과 저장
        lastResult = result;
        
        // 플래시 효과
        ShowFlashEffect(result.judgment);
    }
    
    /// <summary>
    /// 판정 결과 이미지 표시
    /// </summary>
    private void ShowJudgmentImage(GrindingMiniGame.JudgmentType judgment)
    {
        if (judgmentImage == null) return;
        
        // 판정에 따른 스프라이트 설정
        Sprite targetSprite = GetJudgmentSprite(judgment);
        if (targetSprite == null) 
        {
            Debug.Log($"{LOG_PREFIX} 판정 {judgment}에 대한 스프라이트가 없습니다.");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 판정 이미지 표시: {judgment}");
        
        judgmentImage.sprite = targetSprite;
        judgmentImage.gameObject.SetActive(true);
        
        // 이미지 애니메이션 (팝업 효과)
        judgmentImage.transform.localScale = Vector3.zero;
        judgmentImage.color = Color.white; // 항상 기본 흰색으로 유지
        
        Sequence imageSequence = DOTween.Sequence();
        
        // 팝업 애니메이션
        imageSequence.Append(judgmentImage.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
        imageSequence.Append(judgmentImage.transform.DOScale(1f, 0.1f));
        imageSequence.AppendInterval(0.5f);
        
        // 페이드아웃
        imageSequence.Append(judgmentImage.DOFade(0f, 0.3f));
        imageSequence.OnComplete(() => {
            judgmentImage.gameObject.SetActive(false);
            judgmentImage.color = Color.white; // 색상 리셋
            Debug.Log($"{LOG_PREFIX} 판정 이미지 애니메이션 완료");
        });
    }
    

    
    /// <summary>
    /// 판정에 따른 스프라이트 반환
    /// </summary>
    private Sprite GetJudgmentSprite(GrindingMiniGame.JudgmentType judgment)
    {
        switch (judgment)
        {
            case GrindingMiniGame.JudgmentType.Perfect:
                return perfectSprite;
            case GrindingMiniGame.JudgmentType.Good:
                return goodSprite;
            case GrindingMiniGame.JudgmentType.Fail:
                return null; // Fail은 이미지 표시하지 않음
            default:
                return null;
        }
    }
    
    private void ShowFlashEffect(GrindingMiniGame.JudgmentType judgment)
    {
        if (judgmentText == null) return;
        
        Color flashColor = grindingWheel.miniGame.GetJudgmentColor(judgment);
        flashColor.a = 0.5f;
        
        judgmentText.color = flashColor;
        judgmentText.DOFade(0.8f, 0.1f).OnComplete(() => {
            judgmentText.DOFade(0f, 0.3f);
        });
    }
    
    /// <summary>
    /// 최종 결과 표시 (심플 버전)
    /// </summary>
    public void ShowFinalResult(float finalSmoothIncrease)
    {
        
        if (judgmentText != null)
        {
            // 마지막 결과 기반으로 간단한 메시지 표시
            string resultMessage = "연마 완료!";
            if (lastResult != null)
            {
                resultMessage = $"{lastResult.judgment}!\n연마 완료";
            }
            
            judgmentText.text = resultMessage;
            judgmentText.color = Color.green;
            
            // 간단한 스케일 애니메이션
            judgmentText.transform.localScale = Vector3.zero;
            judgmentText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
    }
    
    /// <summary>
    /// 게임 결과 반환 (GrindingWheel에서 사용)
    /// </summary>
    public GrindingMiniGame.GrindingResult GetLastResult()
    {
        return lastResult;
    }
    
    private void ResetUI()
    {
        isPlaying = false;
        canHit = false;
        currentPosition = 0f;
        
        // 텍스트 초기화
        if (judgmentText != null) 
        {
            judgmentText.text = "";
            judgmentText.color = Color.white;
        }
        
        // 이미지 초기화
        if (judgmentImage != null)
        {
            judgmentImage.gameObject.SetActive(false);
            judgmentImage.color = Color.white;
            judgmentImage.transform.localScale = Vector3.one;
        }
    }
    
    // Update for input (나중에 InputSystem으로 교체 가능)
    private void Update()
    {
        // 미니게임 중 SpaceBar 타이밍 히트만 처리 (PlayRound의 while 루프에서 실제 처리됨)
        // 연마 완료 후 자동 종료되므로 별도 SpaceBar 처리 불필요
    }
    

} 