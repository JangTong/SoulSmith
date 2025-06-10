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
    
    [Header("이미지 표시 효과")]
    [SerializeField] private Image displayImage;
    [Tooltip("이미지 표시용 별도 UI 사용 여부 (false면 fadeImage 공용 사용)")]
    [SerializeField] private bool useSeparateDisplayImage = false;
    
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
    
    private Tween currentFadeTween;
    private Tween currentImageTween;
    private bool isInitialized = false;
    private Image activeImage; // 현재 사용 중인 이미지 (fadeImage 또는 displayImage)
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 페이드 이미지 초기화
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
        
        // 디스플레이 이미지 초기화
        if (useSeparateDisplayImage && displayImage != null)
        {
            displayImage.type = Image.Type.Simple;
            displayImage.color = new Color(1f, 1f, 1f, 0f); // 투명 상태로 시작
            displayImage.gameObject.SetActive(false);
            Debug.Log($"{LOG_PREFIX} 별도 디스플레이 이미지 초기화");
        }
        
        // 활성 이미지 결정 (이미지 표시 전용)
        activeImage = useSeparateDisplayImage && displayImage != null ? displayImage : fadeImage;
        
        isInitialized = true;
        Debug.Log($"{LOG_PREFIX} 초기화 완료 (별도 이미지 사용: {useSeparateDisplayImage})");
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
        currentFadeTween?.Kill();
        
        // 시작 상태: 투명
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.gameObject.SetActive(true);
        
        // 불투명하게 페이드 인
        currentFadeTween = fadeImage.DOFade(1f, duration)
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
        
        Debug.Log($"{LOG_PREFIX} 페이드 아웃 시작 (지속시간: {duration}초, 현재 알파: {fadeImage.color.a})");
        
        // 기존 애니메이션 중지 (이미지 관련 애니메이션과 구분)
        currentFadeTween?.Kill();
        
        // 현재 상태 확인 및 로그
        Debug.Log($"{LOG_PREFIX} 페이드 아웃 - 현재 fadeImage 상태: 활성={fadeImage.gameObject.activeSelf}, 색상={fadeImage.color}");
        
        // 시작 상태: 불투명 (현재 상태가 투명이면 불투명으로 먼저 설정)
        if (fadeImage.color.a < 0.1f)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
            Debug.Log($"{LOG_PREFIX} 페이드 아웃 - 투명 상태에서 시작하므로 불투명으로 설정");
        }
        fadeImage.gameObject.SetActive(true);
        
        // 투명하게 페이드 아웃
        currentFadeTween = fadeImage.DOFade(0f, duration)
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
        
        currentFadeTween?.Kill();
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
        
        currentFadeTween?.Kill();
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
    
    #region 이미지 표시 기능
    
    /// <summary>
    /// 스프라이트를 즉시 표시 (페이드 효과 없음)
    /// </summary>
    public void ShowImage(Sprite sprite)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았습니다!");
            return;
        }
        
        if (sprite == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 스프라이트가 null입니다!");
            return;
        }
        
        if (activeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} 활성 이미지가 없습니다!");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 이미지 즉시 표시: {sprite.name}");
        
        // 기존 애니메이션 중지
        currentImageTween?.Kill();
        
        // 스프라이트 설정 및 즉시 표시
        activeImage.sprite = sprite;
        activeImage.color = Color.white; // 완전 불투명
        activeImage.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 스프라이트를 페이드 인 효과와 함께 표시 (UnityEvent 호환)
    /// </summary>
    public void ShowImageWithFadeIn(Sprite sprite)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았습니다!");
            return;
        }
        
        if (sprite == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 스프라이트가 null입니다!");
            return;
        }
        
        if (activeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} 활성 이미지가 없습니다!");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 이미지 페이드 인 표시: {sprite.name} (지속시간: {fadeDuration}초)");
        
        // 기존 애니메이션 중지
        currentImageTween?.Kill();
        
        // 스프라이트 설정 및 투명 상태로 시작
        activeImage.sprite = sprite;
        activeImage.color = new Color(1f, 1f, 1f, 0f); // 투명
        activeImage.gameObject.SetActive(true);
        
        // 페이드 인 애니메이션
        currentImageTween = activeImage.DOFade(1f, fadeDuration)
            .SetEase(fadeCurve)
            .OnComplete(() =>
            {
                Debug.Log($"{LOG_PREFIX} 이미지 페이드 인 완료: {sprite.name}");
            });
    }
    
    /// <summary>
    /// 현재 표시된 이미지를 페이드 아웃 (UnityEvent 호환)
    /// </summary>
    public void FadeOutImage()
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았습니다!");
            return;
        }
        
        if (activeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} 활성 이미지가 없습니다!");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 이미지 페이드 아웃 시작 (지속시간: {fadeDuration}초)");
        
        // 기존 애니메이션 중지
        currentImageTween?.Kill();
        
        // 페이드 아웃 애니메이션
        currentImageTween = activeImage.DOFade(0f, fadeDuration)
            .SetEase(fadeCurve)
            .OnComplete(() =>
            {
                activeImage.gameObject.SetActive(false);
                activeImage.sprite = null; // 스프라이트 참조 해제
                Debug.Log($"{LOG_PREFIX} 이미지 페이드 아웃 완료");
            });
    }
    
    /// <summary>
    /// 현재 이미지를 즉시 숨김
    /// </summary>
    public void HideImageImmediate()
    {
        if (!isInitialized) return;
        
        currentImageTween?.Kill();
        if (activeImage != null)
        {
            activeImage.color = new Color(1f, 1f, 1f, 0f); // 투명
            activeImage.gameObject.SetActive(false);
            activeImage.sprite = null; // 스프라이트 참조 해제
        }
        Debug.Log($"{LOG_PREFIX} 이미지 즉시 숨김");
    }
    
    /// <summary>
    /// 스프라이트만 변경 (현재 알파값 유지)
    /// </summary>
    public void SetImageSprite(Sprite sprite)
    {
        if (!isInitialized)
        {
            Debug.LogError($"{LOG_PREFIX} 초기화되지 않았습니다!");
            return;
        }
        
        if (activeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} 활성 이미지가 없습니다!");
            return;
        }
        
        activeImage.sprite = sprite;
        Debug.Log($"{LOG_PREFIX} 스프라이트 변경: {(sprite != null ? sprite.name : "null")}");
    }
    
    /// <summary>
    /// 크로스 페이드: 현재 이미지에서 새 이미지로 부드럽게 전환 (UnityEvent 호환)
    /// </summary>
    public void CrossFadeImage(Sprite newSprite)
    {
        if (!isInitialized || newSprite == null || activeImage == null)
        {
            Debug.LogError($"{LOG_PREFIX} 크로스 페이드 불가: 초기화={isInitialized}, 스프라이트={newSprite != null}, 이미지={activeImage != null}");
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 크로스 페이드 시작: {newSprite.name} (지속시간: {fadeDuration}초)");
        
        // 기존 애니메이션 중지
        currentImageTween?.Kill();
        
        // 반으로 나누어서 페이드 아웃 → 스프라이트 변경 → 페이드 인
        float halfDuration = fadeDuration * 0.5f;
        
        currentImageTween = activeImage.DOFade(0f, halfDuration)
            .SetEase(fadeCurve)
            .OnComplete(() =>
            {
                // 중간에 스프라이트 변경
                activeImage.sprite = newSprite;
                
                // 페이드 인
                currentImageTween = activeImage.DOFade(1f, halfDuration)
                    .SetEase(fadeCurve)
                    .OnComplete(() =>
                    {
                        Debug.Log($"{LOG_PREFIX} 크로스 페이드 완료: {newSprite.name}");
                    });
            });
    }
    
    /// <summary>
    /// duration 설정과 함께 이미지 페이드 인 (편의 메서드)
    /// </summary>
    public void ShowImageWithFadeInAndDuration(Sprite sprite, float duration)
    {
        SetFadeDuration(duration);
        ShowImageWithFadeIn(sprite);
    }
    
    /// <summary>
    /// duration 설정과 함께 크로스 페이드 (편의 메서드)
    /// </summary>
    public void CrossFadeImageWithDuration(Sprite newSprite, float duration)
    {
        SetFadeDuration(duration);
        CrossFadeImage(newSprite);
    }
    
    /// <summary>
    /// duration 설정과 함께 페이드 아웃 (편의 메서드)
    /// </summary>
    public void FadeOutImageWithDuration(float duration)
    {
        SetFadeDuration(duration);
        FadeOutImage();
    }
    
    #endregion
    
    private void OnDestroy()
    {
        currentFadeTween?.Kill();
        currentImageTween?.Kill();
    }
    
    // 에디터 테스트용 메서드들
    [ContextMenu("🌑 페이드 인 테스트")]
    private void TestFadeIn()
    {
        if (Application.isPlaying) FadeIn();
    }
    
    [ContextMenu("🌕 페이드 아웃 테스트")]
    private void TestFadeOut()
    {
        if (Application.isPlaying) FadeOut();
    }
    
    [ContextMenu("🖼️ 이미지 페이드 아웃 테스트")]
    private void TestImageFadeOut()
    {
        if (Application.isPlaying) FadeOutImage();
    }
    
    [ContextMenu("👁️ 이미지 즉시 숨김 테스트")]
    private void TestHideImage()
    {
        if (Application.isPlaying) HideImageImmediate();
    }
} 