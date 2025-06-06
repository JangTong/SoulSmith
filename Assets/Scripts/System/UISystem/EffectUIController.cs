using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 화면 전환 효과를 담당하는 UI 컨트롤러 (페이드 효과)
/// </summary>
public class EffectUIController : MonoBehaviour
{
    private const string LOG_PREFIX = "[EffectUIController]";
    
    [Header("페이드 효과")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Color fadeColor = Color.black;
    
    [Header("초기 상태 설정")]
    [Tooltip("게임 시작 시 화면 상태")]
    [SerializeField] private InitialState initialState = InitialState.Bright;
    
    public enum InitialState
    {
        [Tooltip("밝은 상태 (패널 비활성화)")]
        Bright,
        [Tooltip("어두운 상태 (패널 활성화)")]
        Dark
    }
    
    private Tween currentTween;
    private bool isInitialized = false;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        if (fadeImage != null)
        {
            fadeImage.type = Image.Type.Simple;
            
            // 초기 상태에 따라 설정
            switch (initialState)
            {
                case InitialState.Bright:
                    fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // 투명
                    fadeImage.gameObject.SetActive(false);
                    Debug.Log($"{LOG_PREFIX} 초기 상태: 밝음 (패널 비활성화)");
                    break;
                    
                case InitialState.Dark:
                    fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f); // 불투명
                    fadeImage.gameObject.SetActive(true);
                    Debug.Log($"{LOG_PREFIX} 초기 상태: 어두움 (패널 활성화)");
                    break;
            }
        }
        
        isInitialized = true;
        Debug.Log($"{LOG_PREFIX} 초기화 완료");
    }
    
    /// <summary>
    /// 페이드 인 (투명 → 불투명, 화면 어두워짐)
    /// </summary>
    public void FadeIn()
    {
        FadeIn(fadeDuration);
    }
    
    /// <summary>
    /// 페이드 인 (지속시간 지정)
    /// </summary>
    public void FadeIn(float duration)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았습니다!");
            return;
        }
        
        if (fadeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} fadeImage가 없습니다!");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 페이드 인 시작 (지속시간: {duration}초)");
        
        // 기존 애니메이션 중지
        currentTween?.Kill();
        
        // 시작 상태: 투명
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.gameObject.SetActive(true);
        
        // 불투명하게 페이드 인
        currentTween = fadeImage.DOFade(1f, duration)
            .SetEase(fadeCurve)
            .OnComplete(() =>
            {
                Debug.Log($"{LOG_PREFIX} 페이드 인 완료");
            });
    }
    
    /// <summary>
    /// 페이드 아웃 (불투명 → 투명, 화면 밝아짐)
    /// </summary>
    public void FadeOut()
    {
        FadeOut(fadeDuration);
    }
    
    /// <summary>
    /// 페이드 아웃 (지속시간 지정)
    /// </summary>
    public void FadeOut(float duration)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았습니다!");
            return;
        }
        
        if (fadeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} fadeImage가 없습니다!");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 페이드 아웃 시작 (지속시간: {duration}초)");
        
        // 기존 애니메이션 중지
        currentTween?.Kill();
        
        // 시작 상태: 불투명
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        fadeImage.gameObject.SetActive(true);
        
        // 투명하게 페이드 아웃
        currentTween = fadeImage.DOFade(0f, duration)
            .SetEase(fadeCurve)
            .OnComplete(() =>
            {
                fadeImage.gameObject.SetActive(false);
                Debug.Log($"{LOG_PREFIX} 페이드 아웃 완료");
            });
    }
    
    /// <summary>
    /// 즉시 어두운 상태로 설정
    /// </summary>
    public void SetDarkImmediate()
    {
        if (!isInitialized) return;
        
        currentTween?.Kill();
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            fadeImage.gameObject.SetActive(true);
        }
        Debug.Log($"{LOG_PREFIX} 즉시 어두운 상태로 설정");
    }
    
    /// <summary>
    /// 즉시 밝은 상태로 설정
    /// </summary>
    public void SetBrightImmediate()
    {
        if (!isInitialized) return;
        
        currentTween?.Kill();
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.gameObject.SetActive(false);
        }
        Debug.Log($"{LOG_PREFIX} 즉시 밝은 상태로 설정");
    }
    
    /// <summary>
    /// 페이드 지속시간 설정
    /// </summary>
    public void SetFadeDuration(float duration)
    {
        fadeDuration = duration;
        Debug.Log($"{LOG_PREFIX} 페이드 지속시간 설정: {duration}초");
    }
    
    /// <summary>
    /// 페이드 색상 설정
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        Debug.Log($"{LOG_PREFIX} 페이드 색상 설정: {color}");
    }
    
    private void OnDestroy()
    {
        currentTween?.Kill();
    }
    
    // 에디터 테스트용 메서드들
    [ContextMenu("페이드 인 테스트")]
    private void TestFadeIn()
    {
        if (Application.isPlaying) FadeIn();
    }
    
    [ContextMenu("페이드 아웃 테스트")]
    private void TestFadeOut()
    {
        if (Application.isPlaying) FadeOut();
    }
} 