using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

public class RecipeBookController : MonoBehaviour
{
    private const string LOG_PREFIX = "[RecipeBookController]";
    
    [Header("페이지 설정")]
    [SerializeField] private CanvasGroup leftPage;
    [SerializeField] private CanvasGroup rightPage;
    [SerializeField] private float pageTransitionDuration = 0.5f;
    
    [Header("히트포인트 텍스트")]
    [SerializeField] private TMP_Text[] leftHitPointTexts = new TMP_Text[5];
    [SerializeField] private TMP_Text[] rightHitPointTexts = new TMP_Text[5];
    
    [Header("레시피 정보")]
    [SerializeField] private TMP_Text leftWeaponNameText;
    [SerializeField] private TMP_Text leftWeaponDescriptionText;
    [SerializeField] private TMP_Text rightWeaponNameText;
    [SerializeField] private TMP_Text rightWeaponDescriptionText;
    
    [Header("3D 오브젝트 표시")]
    [SerializeField] private RectTransform leftWeaponDisplayRect;
    [SerializeField] private RectTransform rightWeaponDisplayRect;
    [SerializeField] private Camera uiCamera; // UI 카메라 참조
    [SerializeField] private float weaponSpawnDistance = 2f; // 카메라로부터의 거리
    [SerializeField] private float rotationSpeed = 30f;
    
    [Header("네비게이션")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageNumberText;
    
    [Header("레시피 라벨")]
    [SerializeField] private string recipeLabel = "WeaponRecipe";
    
    [Header("UI 컨테이너")]
    [SerializeField] private GameObject recipeBookUI;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float uiOpenDuration = 0.8f;
    [SerializeField] private float uiCloseDuration = 0.5f;
    [SerializeField] private float weaponSpawnDuration = 0.6f;
    [SerializeField] private float pageFlipDuration = 0.7f;
    
    // 런타임 데이터
    private List<WeaponRecipe> loadedRecipes = new List<WeaponRecipe>();
    private int currentPageIndex = 0;
    private bool isTransitioning = false;
    private GameObject currentLeftWeapon;
    private GameObject currentRightWeapon;
    private bool isUIActive = false;
    
    // 애니메이션 관련
    private Sequence uiAnimationSequence;
    private Vector3 originalLeftPageScale;
    private Vector3 originalRightPageScale;
    private Vector3 originalUIScale;
    
    private void Start()
    {
        InitializeUI();
        LoadRecipes();
        
        // 초기에는 UI 비활성화
        if (recipeBookUI != null)
        {
            recipeBookUI.SetActive(false);
        }
    }
    
    private void Update()
    {
        HandleInput();
        RotateWeaponDisplays();
    }
    
    private void InitializeUI()
    {
        // 버튼 이벤트 연결
        if (prevButton != null)
        {
            prevButton.onClick.AddListener(() => ChangePage(-1));
            AddButtonHoverEffect(prevButton);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => ChangePage(1));
            AddButtonHoverEffect(nextButton);
        }
        
        // 원래 스케일 저장
        if (recipeBookUI != null)
            originalUIScale = recipeBookUI.transform.localScale;
        if (leftPage != null)
            originalLeftPageScale = leftPage.transform.localScale;
        if (rightPage != null)
            originalRightPageScale = rightPage.transform.localScale;
        
        // 초기 페이지 설정
        if (leftPage != null)
        {
            leftPage.alpha = 1f;
            leftPage.interactable = true;
            leftPage.blocksRaycasts = true;
        }
        
        if (rightPage != null)
        {
            rightPage.alpha = 1f;
            rightPage.interactable = true;
            rightPage.blocksRaycasts = true;
        }
        
        // 3D 오브젝트가 스폰될 위치 검증
        if (leftWeaponDisplayRect == null || rightWeaponDisplayRect == null)
        {
            Debug.LogError($"{LOG_PREFIX} leftWeaponDisplayRect 또는 rightWeaponDisplayRect가 없습니다.");
        }
        
        if (uiCamera == null)
        {
            uiCamera = Camera.main;
            Debug.LogWarning($"{LOG_PREFIX} uiCamera가 설정되지 않아 Main Camera를 사용합니다.");
        }
        
        Debug.Log($"{LOG_PREFIX} UI 초기화 완료");
    }
    
    private void LoadRecipes()
    {
        StartCoroutine(LoadRecipesAsync());
    }
    
    private IEnumerator LoadRecipesAsync()
    {
        Debug.Log($"{LOG_PREFIX} 레시피 로드 시작 - 라벨: {recipeLabel}");
        
        var handle = Addressables.LoadAssetsAsync<WeaponRecipe>(recipeLabel, null);
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedRecipes.Clear();
            loadedRecipes.AddRange(handle.Result);
            
            Debug.Log($"{LOG_PREFIX} {loadedRecipes.Count}개 레시피 로드 완료");
            
            // 로드된 레시피 목록 출력
            for (int i = 0; i < loadedRecipes.Count; i++)
            {
                var recipe = loadedRecipes[i];
                if (recipe != null)
                {
                    Debug.Log($"{LOG_PREFIX} 레시피 {i}: {recipe.weaponName}");
                    Debug.Log($"{LOG_PREFIX} - 히트포인트: [{string.Join(",", recipe.requiredCollisionCounts)}]");
                    Debug.Log($"{LOG_PREFIX} - 무기 프리팹: {(recipe.weaponPrefab != null ? recipe.weaponPrefab.name : "null")}");
                }
                else
                {
                    Debug.LogWarning($"{LOG_PREFIX} 레시피 {i}: null");
                }
            }
            
            // 첫 번째 페이지 표시
            UpdateCurrentPage();
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} 레시피 로드 실패: {handle.OperationException}");
            
            // Addressable 설정 확인 메시지
            Debug.LogError($"{LOG_PREFIX} Addressable 설정 확인 사항:");
            Debug.LogError($"{LOG_PREFIX} 1. WeaponRecipe 에셋에 '{recipeLabel}' 라벨이 설정되어 있는지 확인");
            Debug.LogError($"{LOG_PREFIX} 2. Addressable Groups에서 빌드되었는지 확인");
            Debug.LogError($"{LOG_PREFIX} 3. Window > Asset Management > Addressables > Groups 에서 확인");
        }
    }
    
    private void HandleInput()
    {
        // R키로 UI 열기/닫기
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRecipeBookUI();
            return;
        }
        
        // UI가 비활성화되어 있으면 페이지 네비게이션 입력 무시
        if (!isUIActive || isTransitioning) return;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangePage(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangePage(1);
        }
        
        // ESC키로 UI 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseRecipeBookUI();
        }
    }
    
    private void ToggleRecipeBookUI()
    {
        if (isUIActive)
        {
            CloseRecipeBookUI();
        }
        else
        {
            OpenRecipeBookUI();
        }
    }
    
    private void OpenRecipeBookUI()
    {
        if (isUIActive || recipeBookUI == null) return;
        
        Debug.Log($"{LOG_PREFIX} Recipe Book UI 열기");
        isUIActive = true;
        
        // PlayerController의 UI 상태 활성화
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.ToggleUI(true);
        }
        
        // UIManager 비활성화
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.SetFocusActive(false);
        }
        
        recipeBookUI.SetActive(true);
        UpdateCurrentPage();
        PlayOpenAnimation();
    }
    
    private void CloseRecipeBookUI()
    {
        if (!isUIActive || recipeBookUI == null) return;
        
        Debug.Log($"{LOG_PREFIX} Recipe Book UI 닫기");
        
        // PlayerController의 UI 상태 비활성화
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.ToggleUI(false);
        }
        
        // UIManager 활성화
        var uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.SetFocusActive(true);
        }
        
        PlayCloseAnimation(() =>
        {
            recipeBookUI.SetActive(false);
            isUIActive = false;
        });
    }
    
    private void ChangePage(int direction)
    {
        if (isTransitioning || loadedRecipes.Count == 0) return;
        
        int targetPage = currentPageIndex + direction;
        int maxPage = Mathf.CeilToInt(loadedRecipes.Count / 2f) - 1;
        
        if (targetPage < 0 || targetPage > maxPage) return;
        
        currentPageIndex = targetPage;
        StartCoroutine(TransitionToPageAnimated());
        
        Debug.Log($"{LOG_PREFIX} 페이지 변경: {currentPageIndex}/{maxPage}");
    }
    
    private IEnumerator TransitionToPage()
    {
        isTransitioning = true;
        
        // 페이지 페이드 아웃
        var fadeOutSequence = DOTween.Sequence();
        if (leftPage != null)
            fadeOutSequence.Join(leftPage.DOFade(0f, pageTransitionDuration * 0.5f));
        if (rightPage != null)
            fadeOutSequence.Join(rightPage.DOFade(0f, pageTransitionDuration * 0.5f));
        
        yield return fadeOutSequence.WaitForCompletion();
        
        // 페이지 내용 업데이트
        UpdateCurrentPage();
        
        // 페이지 페이드 인
        var fadeInSequence = DOTween.Sequence();
        if (leftPage != null)
            fadeInSequence.Join(leftPage.DOFade(1f, pageTransitionDuration * 0.5f));
        if (rightPage != null)
            fadeInSequence.Join(rightPage.DOFade(1f, pageTransitionDuration * 0.5f));
        
        yield return fadeInSequence.WaitForCompletion();
        
        isTransitioning = false;
    }
    
    private void UpdateCurrentPage()
    {
        if (loadedRecipes.Count == 0)
        {
            Debug.LogWarning($"{LOG_PREFIX} 로드된 레시피가 없습니다.");
            ClearPageContent(true);
            ClearPageContent(false);
            return;
        }
        
        Debug.Log($"{LOG_PREFIX} 페이지 업데이트 - 현재 페이지: {currentPageIndex}");
        
        // 왼쪽 페이지 (currentPageIndex * 2)
        int leftRecipeIndex = currentPageIndex * 2;
        if (leftRecipeIndex < loadedRecipes.Count)
        {
            var leftRecipe = loadedRecipes[leftRecipeIndex];
            if (leftRecipe != null)
            {
                Debug.Log($"{LOG_PREFIX} 왼쪽 페이지에 레시피 표시: {leftRecipe.weaponName}");
                UpdatePageContent(leftRecipe, true);
            }
            else
            {
                Debug.Log($"{LOG_PREFIX} 왼쪽 페이지 레시피가 null");
                ClearPageContent(true);
            }
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} 왼쪽 페이지 인덱스 초과: {leftRecipeIndex}");
            ClearPageContent(true);
        }
        
        // 오른쪽 페이지 (currentPageIndex * 2 + 1)
        int rightRecipeIndex = currentPageIndex * 2 + 1;
        if (rightRecipeIndex < loadedRecipes.Count)
        {
            var rightRecipe = loadedRecipes[rightRecipeIndex];
            if (rightRecipe != null)
            {
                Debug.Log($"{LOG_PREFIX} 오른쪽 페이지에 레시피 표시: {rightRecipe.weaponName}");
                UpdatePageContent(rightRecipe, false);
            }
            else
            {
                Debug.Log($"{LOG_PREFIX} 오른쪽 페이지 레시피가 null");
                ClearPageContent(false);
            }
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} 오른쪽 페이지 인덱스 초과: {rightRecipeIndex}");
            ClearPageContent(false);
        }
        
        UpdatePageNumber();
        UpdateNavigationButtons();
    }
    
    private void UpdatePageContent(WeaponRecipe recipe, bool isLeftPage)
    {
        if (recipe == null) return;
        
        // 텍스트 업데이트
        var nameText = isLeftPage ? leftWeaponNameText : rightWeaponNameText;
        var descText = isLeftPage ? leftWeaponDescriptionText : rightWeaponDescriptionText;
        var hitPointTexts = isLeftPage ? leftHitPointTexts : rightHitPointTexts;
        
        if (nameText != null)
        {
            nameText.text = recipe.weaponName;
        }
        
        if (descText != null)
        {
            string description = $"가중치: {recipe.weightFactor:F1}\n";
            description += $"공격력: {recipe.atkFactor:F1}\n";
            description += $"방어력: {recipe.defFactor:F1}\n";
            description += $"기본 가격: {recipe.basePrice}\n";
            description += $"가격 배수: {recipe.priceMultiplier:F1}";
            descText.text = description;
        }
        
        // 히트포인트 표시
        if (hitPointTexts != null && recipe.requiredCollisionCounts != null)
        {
            for (int i = 0; i < hitPointTexts.Length; i++)
            {
                if (hitPointTexts[i] != null)
                {
                    if (i < recipe.requiredCollisionCounts.Length)
                    {
                        hitPointTexts[i].text = recipe.requiredCollisionCounts[i].ToString();
                    }
                    else
                    {
                        hitPointTexts[i].text = "0";
                    }
                }
            }
        }
        
        // 3D 무기 표시
        UpdateWeaponDisplay(recipe, isLeftPage);
    }
    
    private void UpdateWeaponDisplay(WeaponRecipe recipe, bool isLeftPage)
    {
        if (recipe == null || recipe.weaponPrefab == null) return;
        
        var displayRect = isLeftPage ? leftWeaponDisplayRect : rightWeaponDisplayRect;
        var currentWeapon = isLeftPage ? currentLeftWeapon : currentRightWeapon;
        
        if (displayRect == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} {(isLeftPage ? "왼쪽" : "오른쪽")} 무기 표시 RectTransform이 없습니다.");
            return;
        }
        
        if (uiCamera == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} UI 카메라가 없습니다.");
            return;
        }
        
        // 기존 무기 제거
        if (currentWeapon != null)
        {
            DestroyImmediate(currentWeapon);
        }
        
        try
        {
            // RectTransform의 월드 좌표 계산
            Vector3 worldPosition = GetWorldPositionFromRect(displayRect);
            
            // 새 무기 스폰 (월드 좌표에)
            var weaponObj = Instantiate(recipe.weaponPrefab);
            weaponObj.transform.position = worldPosition;
            weaponObj.transform.rotation = Quaternion.identity;
            weaponObj.transform.localScale = Vector3.one * 0.5f; // UI에 맞게 크기 조절
            
            // 물리 컴포넌트 비활성화 (표시용)
            var rigidbody = weaponObj.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
            }
            
            var colliders = weaponObj.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // 애니메이션 적용
            AnimateWeaponSpawn(weaponObj, isLeftPage);
            
            // 참조 저장
            if (isLeftPage)
                currentLeftWeapon = weaponObj;
            else
                currentRightWeapon = weaponObj;
            
            Debug.Log($"{LOG_PREFIX} {(isLeftPage ? "왼쪽" : "오른쪽")} 무기 생성 완료: {recipe.weaponName} at {worldPosition}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{LOG_PREFIX} 무기 생성 실패: {e.Message}");
        }
    }
    
    private Vector3 GetWorldPositionFromRect(RectTransform rectTransform)
    {
        // RectTransform의 중심을 스크린 좌표로 변환
        Vector3 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, rectTransform.position);
        
        // 스크린 좌표를 월드 좌표로 변환 (카메라로부터 일정 거리에)
        Vector3 worldPosition = uiCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, weaponSpawnDistance));
        
        return worldPosition;
    }
    
    private void ClearPageContent(bool isLeftPage)
    {
        // 텍스트 클리어
        var nameText = isLeftPage ? leftWeaponNameText : rightWeaponNameText;
        var descText = isLeftPage ? leftWeaponDescriptionText : rightWeaponDescriptionText;
        var hitPointTexts = isLeftPage ? leftHitPointTexts : rightHitPointTexts;
        
        if (nameText != null)
        {
            nameText.text = "";
        }
        
        if (descText != null)
        {
            descText.text = "";
        }
        
        if (hitPointTexts != null)
        {
            for (int i = 0; i < hitPointTexts.Length; i++)
            {
                if (hitPointTexts[i] != null)
                {
                    hitPointTexts[i].text = "0";
                }
            }
        }
        
        // 3D 무기 제거
        var currentWeapon = isLeftPage ? currentLeftWeapon : currentRightWeapon;
        if (currentWeapon != null)
        {
            DestroyImmediate(currentWeapon);
            if (isLeftPage)
                currentLeftWeapon = null;
            else
                currentRightWeapon = null;
        }
    }
    
    private void RotateWeaponDisplays()
    {
        if (rotationSpeed <= 0f) return;
        
        if (currentLeftWeapon != null)
        {
            currentLeftWeapon.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
        }
        
        if (currentRightWeapon != null)
        {
            currentRightWeapon.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
        }
    }
    
    private void UpdatePageNumber()
    {
        if (pageNumberText != null && loadedRecipes.Count > 0)
        {
            int totalPages = Mathf.CeilToInt(loadedRecipes.Count / 2f);
            pageNumberText.text = $"{currentPageIndex + 1} / {totalPages}";
        }
        else if (pageNumberText != null)
        {
            pageNumberText.text = "0 / 0";
        }
    }
    
    private void UpdateNavigationButtons()
    {
        int maxPage = Mathf.CeilToInt(loadedRecipes.Count / 2f) - 1;
        
        if (prevButton != null)
        {
            prevButton.interactable = currentPageIndex > 0;
        }
        
        if (nextButton != null)
        {
            nextButton.interactable = currentPageIndex < maxPage;
        }
    }
    
    private void OnDestroy()
    {
        // DOTween 시퀀스 정리
        KillUIAnimations();
        
        // 기존 무기 정리
        if (currentLeftWeapon != null)
        {
            DestroyImmediate(currentLeftWeapon);
        }
        
        if (currentRightWeapon != null)
        {
            DestroyImmediate(currentRightWeapon);
        }
        
        Debug.Log($"{LOG_PREFIX} RecipeBookController 정리 완료");
    }
    
    // 외부에서 호출 가능한 메서드들
    public void OpenRecipeBook()
    {
        OpenRecipeBookUI();
    }
    
    public void CloseRecipeBook()
    {
        CloseRecipeBookUI();
    }
    
    public bool IsRecipeBookOpen()
    {
        return isUIActive;
    }
    
    private void AddButtonHoverEffect(Button button)
    {
        if (button == null) return;
        
        var originalScale = button.transform.localScale;
        
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // 마우스 진입
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((eventData) =>
        {
            button.transform.DOScale(originalScale * 1.1f, 0.2f);
        });
        trigger.triggers.Add(enterEntry);
        
        // 마우스 나가기
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((eventData) =>
        {
            button.transform.DOScale(originalScale, 0.2f);
        });
        trigger.triggers.Add(exitEntry);
    }
    
    private void PlayOpenAnimation()
    {
        if (recipeBookUI == null) return;
        
        KillUIAnimations();
        
        // 시작 상태 설정
        recipeBookUI.transform.localScale = Vector3.zero;
        
        uiAnimationSequence = DOTween.Sequence();
        uiAnimationSequence.Append(recipeBookUI.transform.DOScale(originalUIScale * 1.1f, uiOpenDuration * 0.6f).SetEase(Ease.OutBack));
        uiAnimationSequence.Append(recipeBookUI.transform.DOScale(originalUIScale, uiOpenDuration * 0.4f).SetEase(Ease.InOutQuad));
        
        Debug.Log($"{LOG_PREFIX} UI 열기 애니메이션 시작");
    }
    
    private void PlayCloseAnimation(System.Action onComplete)
    {
        if (recipeBookUI == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        KillUIAnimations();
        
        uiAnimationSequence = DOTween.Sequence();
        uiAnimationSequence.Append(recipeBookUI.transform.DOScale(Vector3.zero, uiCloseDuration).SetEase(Ease.InBack));
        uiAnimationSequence.OnComplete(() => onComplete?.Invoke());
        
        Debug.Log($"{LOG_PREFIX} UI 닫기 애니메이션 시작");
    }
    
    private IEnumerator TransitionToPageAnimated()
    {
        isTransitioning = true;
        
        // 페이지 페이드 아웃
        var fadeOutSequence = DOTween.Sequence();
        if (leftPage != null)
            fadeOutSequence.Join(leftPage.DOFade(0f, pageFlipDuration * 0.5f));
        if (rightPage != null)
            fadeOutSequence.Join(rightPage.DOFade(0f, pageFlipDuration * 0.5f));
        
        yield return fadeOutSequence.WaitForCompletion();
        
        // 페이지 내용 업데이트
        UpdateCurrentPage();
        
        // 페이지 페이드 인
        var fadeInSequence = DOTween.Sequence();
        if (leftPage != null)
            fadeInSequence.Join(leftPage.DOFade(1f, pageFlipDuration * 0.5f));
        if (rightPage != null)
            fadeInSequence.Join(rightPage.DOFade(1f, pageFlipDuration * 0.5f));
        
        yield return fadeInSequence.WaitForCompletion();
        
        isTransitioning = false;
    }
    
    private void AnimateWeaponSpawn(GameObject weaponObj, bool isLeftPage)
    {
        if (weaponObj == null) return;
        
        // 시작 상태 설정
        weaponObj.transform.localScale = Vector3.zero;
        
        // 스폰 애니메이션
        var spawnSequence = DOTween.Sequence();
        spawnSequence.Append(weaponObj.transform.DOScale(Vector3.one * 1.2f, weaponSpawnDuration * 0.7f).SetEase(Ease.OutBack));
        spawnSequence.Append(weaponObj.transform.DOScale(Vector3.one, weaponSpawnDuration * 0.3f).SetEase(Ease.InOutQuad));
        
        Debug.Log($"{LOG_PREFIX} {(isLeftPage ? "왼쪽" : "오른쪽")} 무기 스폰 애니메이션 시작");
    }
    
    private void KillUIAnimations()
    {
        if (uiAnimationSequence != null && uiAnimationSequence.IsActive())
        {
            uiAnimationSequence.Kill();
        }
    }
} 