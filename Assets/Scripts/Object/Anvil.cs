using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Anvil : MonoBehaviour
{
    private const string LOG_PREFIX = "[Anvil]";
    
    public Transform fixedPosition;
    [Header("Addressables Recipes Label")] 
    public string recipeLabel = "WeaponRecipe";
    public ParticleSystem sparkEffect;

    private GameObject objectOnAnvil;
    private List<WeaponRecipe> loadedRecipes = new List<WeaponRecipe>();

    private void Awake()
    {
        // Addressables로 레시피 로드
        Addressables.LoadAssetsAsync<WeaponRecipe>(
            recipeLabel,
            recipe => loadedRecipes.Add(recipe)
        ).Completed += OnRecipesLoaded;
        
        Debug.Log($"{LOG_PREFIX} Anvil 초기화됨");
    }

    private void OnRecipesLoaded(AsyncOperationHandle<IList<WeaponRecipe>> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
            Debug.Log($"{LOG_PREFIX} Addressables로 {loadedRecipes.Count}개의 레시피 로드 완료");
        else
            Debug.LogWarning($"{LOG_PREFIX} Addressables로 레시피 로드 실패");
    }

    private void OnTriggerEnter(Collider other)
    {
        // HammerHead가 감지되면 즉시 제작 시도
        if (other.gameObject.name == "HammerHead")
        {
            Debug.Log($"{LOG_PREFIX} HammerHead 감지됨, 제작 시도");
            TryCraft();
            return;
        }

        var ctrl = ItemInteractionController.Instance;
        if (ctrl != null && other.transform.IsChildOf(ctrl.playerCamera))
        {
            Debug.Log($"{LOG_PREFIX} 플레이어 카메라 자식 오브젝트 무시: {other.name}");
            return;
        }

        // 아이템 컴포넌트 검색 - 직접 또는 부모에서
        ItemComponent item = other.GetComponent<ItemComponent>();
        
        // 직접 컴포넌트가 없으면 부모에서 검색 (파츠 인식)
        if (item == null)
        {
            item = other.GetComponentInParent<ItemComponent>();
            if (item != null)
            {
                Debug.Log($"{LOG_PREFIX} 파츠 '{other.name}'의 부모에서 아이템 '{item.itemName}' 발견");
                // 부모 게임오브젝트를 모루에 올림
                other = item.gameObject.GetComponent<Collider>();
                if (other == null)
                {
                    Debug.LogWarning($"{LOG_PREFIX} 부모 오브젝트 '{item.gameObject.name}'에 콜라이더가 없습니다.");
                    return;
                }
            }
        }
        
        if (item == null || item.itemType != ItemType.Resource || item.materialType != MaterialType.Metal)
        {
            Debug.Log($"{LOG_PREFIX} 적합하지 않은 아이템: {(item != null ? item.itemName : "null")}");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 아이템 '{item.itemName}' 모루에 배치");
        objectOnAnvil = other.gameObject;
        FixItemOnAnvil(objectOnAnvil);

        WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        if (weaponBase != null) 
        {
            weaponBase.isOnAnvil = true;
            Debug.Log($"{LOG_PREFIX} WeaponBase 상태 설정: isOnAnvil = true");
        }
    }

    private void FixItemOnAnvil(GameObject item)
    {
        if (item == null) return;
        
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        
        item.transform.SetParent(fixedPosition);
        item.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutSine);
        float currentY = item.transform.localEulerAngles.y;
        item.transform.DOLocalRotate(new Vector3(0f, currentY, 0f), 0.3f).SetEase(Ease.OutSine);
        
        Debug.Log($"{LOG_PREFIX} 아이템 '{item.name}' 모루에 고정됨");
    }

    private void OnTriggerExit(Collider other)
    {
        if (objectOnAnvil != other.gameObject) return;
        
        WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        if (weaponBase != null) 
        {
            weaponBase.isOnAnvil = false;
            Debug.Log($"{LOG_PREFIX} WeaponBase 상태 설정: isOnAnvil = false");
        }
        
        Debug.Log($"{LOG_PREFIX} 아이템 '{objectOnAnvil.name}' 모루에서 제거됨");
        objectOnAnvil = null;
    }

    // TryCraft를 호출하여 제작
    public void TryCraft()
    {
        if (objectOnAnvil == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 제작 실패: 모루 위에 아이템이 없음");
            return;
        }

        Debug.Log($"{LOG_PREFIX} 아이템 '{objectOnAnvil.name}' 제작 시도");
        
        // 모든 필요한 컴포넌트 검색
        ItemComponent itemComp = objectOnAnvil.GetComponent<ItemComponent>();
        WeaponBase weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        
        // 직접 컴포넌트가 없으면 부모에서 검색 (파츠 인식)
        if (itemComp == null)
        {
            itemComp = objectOnAnvil.GetComponentInParent<ItemComponent>();
            if (itemComp != null)
            {
                Debug.Log($"{LOG_PREFIX} 파츠의 부모에서 아이템 컴포넌트 '{itemComp.itemName}' 발견");
            }
        }
        
        if (weaponBase == null)
        {
            weaponBase = objectOnAnvil.GetComponentInParent<WeaponBase>();
            if (weaponBase != null)
            {
                Debug.Log($"{LOG_PREFIX} 파츠의 부모에서 무기 베이스 발견");
            }
        }
        
        if (itemComp == null || weaponBase == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} 제작 실패: 필요한 컴포넌트 없음 (ItemComponent: {itemComp != null}, WeaponBase: {weaponBase != null})");
            return;
        }

        // 레시피 매칭 시도
        Debug.Log($"{LOG_PREFIX} 레시피 매칭 시도 (로드된 레시피: {loadedRecipes.Count}개)");
        
        // 디버그: 충돌 데이터 출력
        Debug.Log($"{LOG_PREFIX} 무기 충돌 데이터:");
        foreach (var data in weaponBase.collisionDataList)
        {
            Debug.Log($"{LOG_PREFIX} - 파츠: {data.partName}, 충돌 횟수: {data.collisionCount}");
        }
        
        foreach (var recipe in loadedRecipes)
        {
            if (MatchRecipe(weaponBase, recipe))
            {
                Debug.Log($"{LOG_PREFIX} 레시피 매칭 성공: {recipe.weaponName}");
                CraftWithRecipe(recipe, itemComp);
                return;
            }
        }
        Debug.LogWarning($"{LOG_PREFIX} 일치하는 레시피가 없습니다.");
    }

    private bool MatchRecipe(WeaponBase wb, WeaponRecipe recipe)
    {
        if (wb.collisionDataList.Count != recipe.requiredCollisionCounts.Length)
        {
            Debug.Log($"{LOG_PREFIX} 레시피 불일치: 충돌 데이터 개수 불일치 (무기: {wb.collisionDataList.Count}, 레시피: {recipe.requiredCollisionCounts.Length})");
            return false;
        }
        
        for (int i = 0; i < recipe.requiredCollisionCounts.Length; i++)
        {
            if (wb.collisionDataList[i].collisionCount != recipe.requiredCollisionCounts[i])
            {
                Debug.Log($"{LOG_PREFIX} 레시피 불일치: 인덱스 {i}의 충돌 횟수 불일치 (무기: {wb.collisionDataList[i].collisionCount}, 레시피: {recipe.requiredCollisionCounts[i]})");
                return false;
            }
        }
        
        return true;
    }

    private void CraftWithRecipe(WeaponRecipe recipe, ItemComponent sourceItem)
    {
        Debug.Log($"{LOG_PREFIX} '{recipe.weaponName}' 제작 시작");
        
        var newWeapon = Instantiate(
            recipe.weaponPrefab,
            fixedPosition.position,
            objectOnAnvil.transform.rotation * Quaternion.Euler(0, 90, 0)
        );
        
        // 생성된 무기 고정 상태로 설정
        Rigidbody weaponRb = newWeapon.GetComponent<Rigidbody>();
        if (weaponRb != null)
        {
            weaponRb.isKinematic = true;
        }
        
        var newComp = newWeapon.GetComponent<ItemComponent>();
        if (newComp == null)
        {
            Debug.LogError($"{LOG_PREFIX} 제작 실패: 생성된 무기에 ItemComponent가 없음");
            Destroy(newWeapon);
            return;
        }

        newComp.itemName = recipe.weaponName;
        newComp.itemColor = sourceItem.itemColor;
        newComp.AddStatsFrom(sourceItem);
        newComp.AddMaterialsFrom(sourceItem);

        newComp.weight *= recipe.weightFactor;
        newComp.atkPower *= recipe.atkFactor;
        newComp.defPower *= recipe.defFactor;
        newComp.sellPrice = sourceItem.sellPrice + recipe.basePrice + Mathf.RoundToInt(newComp.atkPower * recipe.priceMultiplier);
        newComp.buyPrice = newComp.sellPrice * 2;

        // 컬러 적용
        var rend = newWeapon.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(rend.material);
            mat.color = newComp.itemColor;
            rend.material = mat;
        }

        Debug.Log($"{LOG_PREFIX} {newComp.itemName} 제작 완료: 공격력 {newComp.atkPower}, 방어력 {newComp.defPower}, 무게 {newComp.weight}, 재료 {newComp.materialsUsed.Count}개");

        // 새 무기를 모루에 배치
        FixItemOnAnvil(newWeapon);
        
        // 기존 아이템 제거
        Debug.Log($"{LOG_PREFIX} 기존 아이템 '{objectOnAnvil.name}' 제거");
        Destroy(objectOnAnvil);
        objectOnAnvil = newWeapon;
        
        // 이펙트 재생
        sparkEffect?.Play();
        Debug.Log($"{LOG_PREFIX} 제작 이펙트 재생");
    }
}

