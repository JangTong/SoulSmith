using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    [SerializeField] private Transform leftWeaponDisplay;
    [SerializeField] private Transform rightWeaponDisplay;
    [SerializeField] private float rotationSpeed = 30f;
    
    [Header("네비게이션")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text pageNumberText;
    
    [Header("레시피 라벨")]
    [SerializeField] private string recipeLabel = "WeaponRecipe";
    
    // 런타임 데이터
    private List<WeaponRecipe> loadedRecipes = new List<WeaponRecipe>();
    private int currentPageIndex = 0;
    private bool isTransitioning = false;
    private GameObject currentLeftWeapon;
    private GameObject currentRightWeapon;
    
    private void Start()
    {
        InitializeUI();
        LoadRecipes();
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
            prevButton.onClick.AddListener(() => ChangePage(-1));
        
        if (nextButton != null)
            nextButton.onClick.AddListener(() => ChangePage(1));
        
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
        
        Debug.Log($"{LOG_PREFIX} UI 초기화 완료");
    }
    
    private void LoadRecipes()
    {
        StartCoroutine(LoadRecipesAsync());
    }
    
    private IEnumerator LoadRecipesAsync()
    {
        Debug.Log($"{LOG_PREFIX} 레시피 로드 시작");
        
        var handle = Addressables.LoadAssetsAsync<WeaponRecipe>(recipeLabel, null);
        yield return handle;
        
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            loadedRecipes.Clear();
            loadedRecipes.AddRange(handle.Result);
            
            Debug.Log($"{LOG_PREFIX} {loadedRecipes.Count}개 레시피 로드 완료");
            
            // 첫 번째 페이지 표시
            UpdateCurrentPage();
        }
        else
        {
            Debug.LogError($"{LOG_PREFIX} 레시피 로드 실패: {handle.OperationException}");
        }
    }
    
    private void HandleInput()
    {
        if (isTransitioning) return;
        
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangePage(-1);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangePage(1);
        }
    }
    
    private void ChangePage(int direction)
    {
        if (isTransitioning || loadedRecipes.Count == 0) return;
        
        int newIndex = currentPageIndex + direction;
        
        // 페이지 범위 확인 (2개씩 표시하므로 최대 인덱스는 (총 개수 - 1) / 2)
        int maxPageIndex = Mathf.Max(0, (loadedRecipes.Count - 1) / 2);
        
        if (newIndex < 0 || newIndex > maxPageIndex) return;
        
        currentPageIndex = newIndex;
        StartCoroutine(TransitionToPage());
    }
    
    private IEnumerator TransitionToPage()
    {
        isTransitioning = true;
        
        // 페이지 페이드 아웃
        var fadeOutTween = DOTween.To(() => 1f, x => 
        {
            if (leftPage != null) leftPage.alpha = x;
            if (rightPage != null) rightPage.alpha = x;
        }, 0f, pageTransitionDuration * 0.5f);
        
        yield return fadeOutTween.WaitForCompletion();
        
        // 페이지 내용 업데이트
        UpdateCurrentPage();
        
        // 페이지 페이드 인
        var fadeInTween = DOTween.To(() => 0f, x => 
        {
            if (leftPage != null) leftPage.alpha = x;
            if (rightPage != null) rightPage.alpha = x;
        }, 1f, pageTransitionDuration * 0.5f);
        
        yield return fadeInTween.WaitForCompletion();
        
        isTransitioning = false;
    }
    
    private void UpdateCurrentPage()
    {
        if (loadedRecipes.Count == 0) return;
        
        // 왼쪽 페이지 업데이트
        int leftIndex = currentPageIndex * 2;
        if (leftIndex < loadedRecipes.Count)
        {
            UpdatePageContent(loadedRecipes[leftIndex], true);
        }
        else
        {
            ClearPageContent(true);
        }
        
        // 오른쪽 페이지 업데이트
        int rightIndex = currentPageIndex * 2 + 1;
        if (rightIndex < loadedRecipes.Count)
        {
            UpdatePageContent(loadedRecipes[rightIndex], false);
        }
        else
        {
            ClearPageContent(false);
        }
        
        // 페이지 번호 업데이트
        UpdatePageNumber();
        
        // 버튼 상태 업데이트
        UpdateNavigationButtons();
    }
    
    private void UpdatePageContent(WeaponRecipe recipe, bool isLeftPage)
    {
        if (recipe == null) return;
        
        // 히트포인트 텍스트 업데이트
        var hitPointTexts = isLeftPage ? leftHitPointTexts : rightHitPointTexts;
        for (int i = 0; i < hitPointTexts.Length && i < recipe.requiredCollisionCounts.Length; i++)
        {
            if (hitPointTexts[i] != null)
            {
                hitPointTexts[i].text = $"타격 {i + 1}: {recipe.requiredCollisionCounts[i]}회";
            }
        }
        
        // 무기 이름 업데이트
        var nameText = isLeftPage ? leftWeaponNameText : rightWeaponNameText;
        if (nameText != null)
        {
            nameText.text = recipe.weaponName;
        }
        
        // 무기 설명 업데이트 (스탯 정보 포함)
        var descriptionText = isLeftPage ? leftWeaponDescriptionText : rightWeaponDescriptionText;
        if (descriptionText != null)
        {
            string description = $"가중치: {recipe.weightFactor:F1}\n";
            description += $"공격력: {recipe.atkFactor:F1}\n";
            description += $"방어력: {recipe.defFactor:F1}\n";
            description += $"기본 가격: {recipe.basePrice}\n";
            description += $"가격 배수: {recipe.priceMultiplier:F1}";
            
            descriptionText.text = description;
        }
        
        // 3D 오브젝트 표시
        UpdateWeaponDisplay(recipe, isLeftPage);
    }
    
    private void UpdateWeaponDisplay(WeaponRecipe recipe, bool isLeftPage)
    {
        var displayTransform = isLeftPage ? leftWeaponDisplay : rightWeaponDisplay;
        if (displayTransform == null || recipe.weaponPrefab == null) return;
        
        // 기존 무기 오브젝트 제거
        if (isLeftPage && currentLeftWeapon != null)
        {
            DestroyImmediate(currentLeftWeapon);
        }
        else if (!isLeftPage && currentRightWeapon != null)
        {
            DestroyImmediate(currentRightWeapon);
        }
        
        // 새 무기 오브젝트 생성
        GameObject weaponObj = Instantiate(recipe.weaponPrefab, displayTransform);
        weaponObj.transform.localPosition = Vector3.zero;
        weaponObj.transform.localRotation = Quaternion.identity;
        
        // 물리 컴포넌트 비활성화 (UI 표시용)
        var rigidbody = weaponObj.GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
        }
        
        var collider = weaponObj.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 참조 저장
        if (isLeftPage)
        {
            currentLeftWeapon = weaponObj;
        }
        else
        {
            currentRightWeapon = weaponObj;
        }
    }
    
    private void ClearPageContent(bool isLeftPage)
    {
        // 히트포인트 텍스트 초기화
        var hitPointTexts = isLeftPage ? leftHitPointTexts : rightHitPointTexts;
        for (int i = 0; i < hitPointTexts.Length; i++)
        {
            if (hitPointTexts[i] != null)
            {
                hitPointTexts[i].text = "";
            }
        }
        
        // 무기 이름 초기화
        var nameText = isLeftPage ? leftWeaponNameText : rightWeaponNameText;
        if (nameText != null)
        {
            nameText.text = "";
        }
        
        // 무기 설명 초기화
        var descriptionText = isLeftPage ? leftWeaponDescriptionText : rightWeaponDescriptionText;
        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
        
        // 3D 오브젝트 제거
        if (isLeftPage && currentLeftWeapon != null)
        {
            DestroyImmediate(currentLeftWeapon);
            currentLeftWeapon = null;
        }
        else if (!isLeftPage && currentRightWeapon != null)
        {
            DestroyImmediate(currentRightWeapon);
            currentRightWeapon = null;
        }
    }
    
    private void RotateWeaponDisplays()
    {
        if (currentLeftWeapon != null)
        {
            currentLeftWeapon.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
        
        if (currentRightWeapon != null)
        {
            currentRightWeapon.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
    
    private void UpdatePageNumber()
    {
        if (pageNumberText != null)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(loadedRecipes.Count / 2f));
            pageNumberText.text = $"{currentPageIndex + 1} / {totalPages}";
        }
    }
    
    private void UpdateNavigationButtons()
    {
        if (prevButton != null)
        {
            prevButton.interactable = currentPageIndex > 0;
        }
        
        if (nextButton != null)
        {
            int maxPageIndex = Mathf.Max(0, (loadedRecipes.Count - 1) / 2);
            nextButton.interactable = currentPageIndex < maxPageIndex;
        }
    }
    
    private void OnDestroy()
    {
        // DOTween 시퀀스 정리
        DOTween.Kill(this);
        
        // 3D 오브젝트 정리
        if (currentLeftWeapon != null)
        {
            DestroyImmediate(currentLeftWeapon);
        }
        
        if (currentRightWeapon != null)
        {
            DestroyImmediate(currentRightWeapon);
        }
        
        Debug.Log($"{LOG_PREFIX} 리소스 정리 완료");
    }
} 