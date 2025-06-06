using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class DialogueUIController : MonoBehaviour
{
    private const string LOG_PREFIX = "[DialogueUI]";

    [Header("UI 요소")]
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI nameText, dialogText;
    [SerializeField] Image portrait;
    [SerializeField] Button nextBtn, sellBtn;
    
    [Header("애니메이션 설정")]
    [SerializeField] float animDuration = 0.3f;
    [SerializeField] Ease expandEase = Ease.OutBack;
    [SerializeField] float contentFadeDuration = 0.2f;

    [Header("타이핑 효과 설정")]
    [SerializeField] float typingSpeed = 0.02f;  // 글자당 타이핑 시간
    [SerializeField] float punctuationDelay = 0.02f;  // 문장부호에서 추가 대기 시간

    public static event Action OnNextClicked;
    public static event Action OnSellClicked;

    private RectTransform panelRect;
    private Vector3 originalRotation;
    private bool isFirstDialogue = true;
    private bool isInDialogue = false;
    private bool isTyping = false;
    private string currentMessage = "";
    private Coroutine typingCoroutine;

    private void Awake()
    {
        InitializeComponents();
        SetupButtonListeners();
        ConfigurePortraitPivot();
        Debug.Log($"{LOG_PREFIX} 초기화 완료");
    }

    private void Update()
    {
        // 대화 중이고, 타이핑이 시작되었거나 타이핑이 완료된 상태에서만 스페이스바 입력 처리
        if (isInDialogue && (isTyping || (!isTyping && !string.IsNullOrEmpty(dialogText.text))) && Input.GetKeyDown(KeyCode.Space))
        {
            // Next 버튼과 같은 동작 수행
            HandleButtonClick(() => OnNextClicked?.Invoke());
        }
    }

    private void InitializeComponents()
    {
        if (panel != null)
        {
            panelRect = panel.GetComponent<RectTransform>();
            originalRotation = panelRect.localEulerAngles;
            panel.SetActive(false);
        }
    }

    private void SetupButtonListeners()
    {
        nextBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.AddListener(() => HandleButtonClick(() => OnNextClicked?.Invoke()));

        sellBtn.onClick.RemoveAllListeners();
        sellBtn.onClick.AddListener(() => HandleButtonClick(() => OnSellClicked?.Invoke()));
    }

    /// <summary>
    /// 모든 버튼 클릭을 처리하는 래퍼 메소드
    /// 타이핑 중일 경우 타이핑을 완료하고, 아닐 경우 원래 액션을 실행
    /// </summary>
    private void HandleButtonClick(Action originalAction)
    {
        if (isTyping)
        {
            SkipTyping();
        }
        else
        {
            originalAction?.Invoke();
        }
    }

    private void ConfigurePortraitPivot()
    {
        if (portrait != null)
        {
            RectTransform portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.pivot = new Vector2(0.5f, 0f);
        }
    }

    private void SetComponentsAlpha(float alpha)
    {
        Color color;
        
        // 메인 텍스트
        color = nameText.color;
        color.a = alpha;
        nameText.color = color;

        color = dialogText.color;
        color.a = alpha;
        dialogText.color = color;

        // 다음 버튼
        color = nextBtn.image.color;
        color.a = alpha;
        nextBtn.image.color = color;

        // 판매 버튼
        if (sellBtn.gameObject.activeSelf)
        {
            color = sellBtn.image.color;
            color.a = alpha;
            sellBtn.image.color = color;
        }
    }

    public void Show(string speaker, string message, Sprite portraitSprite, bool showSell)
    {
        bool shouldAnimate = isFirstDialogue;
        Debug.Log($"{LOG_PREFIX} 대화창 표시 - 화자: {speaker}, 첫 대화 여부: {shouldAnimate}");

        InitializeUIState(speaker, message, portraitSprite, showSell);

        if (shouldAnimate)
        {
            PlayOpenAnimation(portraitSprite);
        }
        else
        {
            ShowWithoutAnimation(portraitSprite);
        }

        isFirstDialogue = false;
        isInDialogue = true;
    }

    private void InitializeUIState(string speaker, string message, Sprite portraitSprite, bool showSell)
    {
        panel.SetActive(true);
        nameText.text = speaker;
        currentMessage = message;
        dialogText.text = "";  // 초기에는 비워둠
        portrait.gameObject.SetActive(false);
        
        if (portraitSprite != null)
        {
            portrait.sprite = portraitSprite;
            portrait.gameObject.SetActive(true);
            portrait.transform.localScale = Vector3.zero;
        }

        sellBtn.gameObject.SetActive(showSell);
        SetComponentsAlpha(0f);
    }

    private void PlayOpenAnimation(Sprite portraitSprite)
    {
        Debug.Log($"{LOG_PREFIX} 열기 애니메이션 시작");
        panelRect.localEulerAngles = new Vector3(90f, originalRotation.y, originalRotation.z);

        Sequence seq = DOTween.Sequence();
        seq.Append(panelRect.DOLocalRotate(originalRotation, animDuration)
                          .SetEase(expandEase))
           .OnComplete(() => {
               Sequence contentSeq = DOTween.Sequence();
               contentSeq.Join(DOTween.To(() => 0f, x => SetComponentsAlpha(x), 1f, contentFadeDuration)
                                    .SetEase(Ease.OutQuad));
               
               if (portraitSprite != null)
               {
                   contentSeq.Join(portrait.transform.DOScale(1f, contentFadeDuration)
                                         .SetEase(Ease.OutQuad));
               }

               contentSeq.OnComplete(() => {
                   StartTyping();
               });
           });
    }

    private void ShowWithoutAnimation(Sprite portraitSprite)
    {
        Debug.Log($"{LOG_PREFIX} 애니메이션 없이 표시");
        panelRect.localEulerAngles = originalRotation;
        SetComponentsAlpha(1f);
        
        if (portraitSprite != null)
        {
            portrait.transform.localScale = Vector3.one;
        }

        StartTyping();
    }

    private void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        nextBtn.interactable = true;  // 타이핑 중에도 버튼 활성화
        dialogText.text = "";
        
        foreach (char c in currentMessage)
        {
            dialogText.text += c;
            
            // 문장부호에 따른 추가 대기 시간
            if (c == '.' || c == ',' || c == '!' || c == '?')
            {
                yield return new WaitForSeconds(punctuationDelay);
            }
            else
            {
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        
        isTyping = false;
        Debug.Log($"{LOG_PREFIX} 타이핑 효과 완료");
    }

    public void SkipTyping()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            dialogText.text = currentMessage;
            isTyping = false;
            Debug.Log($"{LOG_PREFIX} 타이핑 효과 스킵");
        }
    }

    public void Hide()
    {
        if (!isInDialogue) return;

        Debug.Log($"{LOG_PREFIX} 대화창 숨기기 시작");
        
        if (isInDialogue)
        {
            PlayCloseAnimation();
        }
        else
        {
            panel.SetActive(false);
        }

        isInDialogue = false;
        isFirstDialogue = true;
    }

    private void PlayCloseAnimation()
    {
        Sequence hideSeq = DOTween.Sequence();

        hideSeq.Join(DOTween.To(() => 1f, x => SetComponentsAlpha(x), 0f, animDuration * 0.5f));
        
        if (portrait.gameObject.activeSelf)
        {
            hideSeq.Join(portrait.transform.DOScale(0f, animDuration * 0.5f)
                               .SetEase(Ease.InQuad));
        }

        hideSeq.Join(panelRect.DOLocalRotate(new Vector3(90f, originalRotation.y, originalRotation.z), animDuration)
                           .SetEase(Ease.InBack))
              .OnComplete(() => {
                  panel.SetActive(false);
                  Debug.Log($"{LOG_PREFIX} 대화창 숨기기 완료");
              });
    }

    private void OnDestroy()
    {
        if (panel != null)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            DOTween.Kill(panel.transform);
            DOTween.Kill(portrait.transform);
            DOTween.Kill(panelRect);
            DOTween.Kill(nameText);
            DOTween.Kill(dialogText);
            DOTween.Kill(nextBtn.image);
            if (sellBtn != null)
            {
                DOTween.Kill(sellBtn.image);
            }
            Debug.Log($"{LOG_PREFIX} 모든 Tween 정리 완료");
        }
    }
}