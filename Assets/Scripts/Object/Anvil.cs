using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Anvil : MonoBehaviour
{
    public Transform fixedPosition;
    public List<WeaponRecipe> recipes = new List<WeaponRecipe>();
    [Header("Addressables Recipes Label")] 
    public string recipeLabel = "WeaponRecipe";
    public ParticleSystem sparkEffect;

    private GameObject objectOnAnvil;
    private ItemComponent itemComp;
    private WeaponBase weaponBase;
    private List<WeaponRecipe> loadedRecipes = new List<WeaponRecipe>();

    private void Awake()
    {
        // Addressables로 레시피 로드
        Addressables.LoadAssetsAsync<WeaponRecipe>(
            recipeLabel,
            recipe => loadedRecipes.Add(recipe)
        ).Completed += OnRecipesLoaded;
    }

    private void OnRecipesLoaded(AsyncOperationHandle<IList<WeaponRecipe>> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
            Debug.Log($"Loaded {loadedRecipes.Count} recipes via Addressables.");
        else
            Debug.LogWarning("Failed to load WeaponRecipe assets via Addressables.");
    }

    private void OnTriggerEnter(Collider other)
    {
        // HammerHead가 감지되면 즉시 제작 시도
        if (other.gameObject.name == "HammerHead")
        {
            TryCraft();
            return;
        }

        // 아이템 고정 처리
        if (ItemPickup.Instance.currentState != ItemPickupState.Idle) return;
        var comp = other.GetComponent<ItemComponent>();
        if (comp == null || comp.itemType != ItemType.Resource || comp.materialType != MaterialType.Metal) return;

        objectOnAnvil = other.gameObject;
        other.GetComponent<Rigidbody>().isKinematic = true;
        other.transform.SetParent(fixedPosition);
        other.transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutSine);

        weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        if (weaponBase != null) weaponBase.isOnAnvil = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (objectOnAnvil != other.gameObject) return;
        weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        if (weaponBase != null) weaponBase.isOnAnvil = false;
        objectOnAnvil = null;
    }

    // TryCraft를 호출하여 제작
    public void TryCraft()
    {
        if (objectOnAnvil == null) return;
        itemComp = objectOnAnvil.GetComponent<ItemComponent>();
        weaponBase = objectOnAnvil.GetComponent<WeaponBase>();
        if (itemComp == null || weaponBase == null) return;

        foreach (var recipe in loadedRecipes)
        {
            if (MatchRecipe(weaponBase, recipe))
            {
                CraftWithRecipe(recipe);
                return;
            }
        }
        Debug.Log("일치하는 레시피가 없습니다.");
    }

    private bool MatchRecipe(WeaponBase wb, WeaponRecipe recipe)
    {
        for (int i = 0; i < recipe.requiredCollisionCounts.Length; i++)
        {
            if (wb.collisionDataList[i].collisionCount != recipe.requiredCollisionCounts[i])
                return false;
        }
        return true;
    }

    private void CraftWithRecipe(WeaponRecipe recipe)
    {
        var newWeapon = Instantiate(
            recipe.weaponPrefab,
            fixedPosition.position,
            objectOnAnvil.transform.rotation * Quaternion.Euler(0, 90, 0)
        );
        var newComp = newWeapon.GetComponent<ItemComponent>();
        if (newComp == null) return;

        newComp.itemName = recipe.weaponName;
        newComp.itemColor = itemComp.itemColor;
        newComp.AddStatsFrom(itemComp);
        newComp.AddMaterialsFrom(itemComp);

        newComp.weight *= recipe.weightFactor;
        newComp.atkPower *= recipe.atkFactor;
        newComp.defPower *= recipe.defFactor;
        newComp.sellPrice = itemComp.sellPrice + recipe.basePrice + Mathf.RoundToInt(newComp.atkPower * recipe.priceMultiplier);
        newComp.buyPrice = newComp.sellPrice * 2;

        // 컬러 적용
        var rend = newWeapon.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = new Material(rend.material);
            mat.color = newComp.itemColor;
            rend.material = mat;
        }

        Debug.Log($"{newComp.itemName} 제작 완료: 공격력 {newComp.atkPower}, 재료 {newComp.materialsUsed.Count}개");

        Destroy(objectOnAnvil);
        objectOnAnvil = null;
        sparkEffect?.Play();
    }
}
