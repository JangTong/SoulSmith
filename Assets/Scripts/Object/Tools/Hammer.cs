// Hammer.cs
using UnityEngine;
using System.Collections;
using DG.Tweening;  // 이미 쓰고 계시니 추가 없으셔도 됩니다

public class Hammer : Tool
{
    private const string LOG_PREFIX = "[Hammer]";
    
    public ParticleSystem sparkEffect;
    public float soundDelay = 0.3f;
    private bool isPlayingSound = false;

    public override void Use()
    {
        // 컨트롤러에서 카메라·거리 가져오기
        var ctrl = ItemInteractionController.Instance;
        Transform camera = ctrl.playerCamera;
        float range = ctrl.pickupDistance;

        // 레이캐스트 발사
        Ray ray = new Ray(camera.position, camera.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, range);

        foreach (var hit in hits)
        {
            // 1) 카메라 자식(=들고 있거나 장착된 아이템)은 무시
            if (hit.collider.transform.IsChildOf(camera))
                continue;

            // 2) Items 태그만 처리
            if (!hit.collider.CompareTag("Items"))
                continue;

            Debug.Log($"{LOG_PREFIX} 망치 타격: {hit.collider.name}");

            // 3) 파티클
            if (sparkEffect != null)
            {
                sparkEffect.transform.position = hit.point;
                sparkEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                sparkEffect.Play();
            }

            // 4) 사운드
            if (!isPlayingSound)
                ctrl.StartCoroutine(PlayHammerSound(hit.point));

            // 5) 타격된 아이템 처리
            Transform hitTransform = hit.collider.transform;
            
            // WeaponBase 확인 (모루 위든 아니든 먼저 처리)
            var weaponBase = hit.collider.GetComponent<WeaponBase>();
            if (weaponBase == null)
                weaponBase = hit.collider.GetComponentInParent<WeaponBase>();
            
            if (weaponBase != null)
            {
                Debug.Log($"{LOG_PREFIX} WeaponBase 타격: {hit.collider.name}, isOnAnvil: {weaponBase.isOnAnvil}");
                
                // 항상 충돌 카운트 증가 (모루 위에서만 실제로 증가됨)
                int newCount = weaponBase.IncrementCollisionCount(hit.collider.name);
                
                // 모루 위 아이템이면 Anvil 제작 시도
                if (weaponBase.isOnAnvil)
                {
                    Debug.Log($"{LOG_PREFIX} 모루 위 아이템 타격 - 현재 충돌 카운트: {newCount}");
                    Anvil anvil = FindAnvilWithItem(weaponBase.gameObject);
                    if (anvil != null)
                    {
                        Debug.Log($"{LOG_PREFIX} Anvil 제작 시도");
                        anvil.TryCraft();
                    }
                    else
                    {
                        Debug.LogWarning($"{LOG_PREFIX} 모루 위 아이템이지만 해당 Anvil을 찾을 수 없음");
                    }
                }
            }
            // WeaponBase가 없으면 CraftingTable 확인
            else
            {
                CraftingTable craftingTable = FindCraftingTable(hitTransform);
                
                if (craftingTable != null)
                {
                    Debug.Log($"{LOG_PREFIX} CraftingTable 파츠 타격: {hit.collider.name}");
                    craftingTable.HandleHammerHit(hitTransform, hit.point);
                }
            }

            // 가까운 것 하나만 처리
            break;
        }
    }
    
    // 타격된 오브젝트의 CraftingTable 찾기
    private CraftingTable FindCraftingTable(Transform hitTransform)
    {
        // 씬 내 모든 CraftingTable 검색
        CraftingTable[] allTables = GameObject.FindObjectsOfType<CraftingTable>();
        
        foreach (var table in allTables)
        {
            if (table.currentBlade != null)
            {
                // 타격된 오브젝트가 블레이드 자신이거나 그 자식인지 확인
                if (hitTransform == table.currentBlade.transform || hitTransform.IsChildOf(table.currentBlade.transform))
                {
                    Debug.Log($"{LOG_PREFIX} FindCraftingTable: 블레이드 계층에서 발견");
                    return table;
                }
            }
            
            // partsHolder의 자식들도 확인
            if (table.partsHolder != null)
            {
                if (hitTransform.IsChildOf(table.partsHolder))
                {
                    Debug.Log($"{LOG_PREFIX} FindCraftingTable: partsHolder 계층에서 발견");
                    return table;
                }
            }
        }
        
        Debug.Log($"{LOG_PREFIX} FindCraftingTable: CraftingTable을 찾을 수 없음");
        return null;
    }

    // 지정된 아이템을 관리하는 Anvil 찾기
    private Anvil FindAnvilWithItem(GameObject item)
    {
        Anvil[] allAnvils = GameObject.FindObjectsOfType<Anvil>();
        
        foreach (var anvil in allAnvils)
        {
            if (anvil.GetObjectOnAnvil() == item)
            {
                Debug.Log($"{LOG_PREFIX} FindAnvilWithItem: 아이템 '{item.name}'을 관리하는 Anvil 발견");
                return anvil;
            }
        }
        
        Debug.Log($"{LOG_PREFIX} FindAnvilWithItem: 아이템 '{item.name}'을 관리하는 Anvil을 찾을 수 없음");
        return null;
    }

    private IEnumerator PlayHammerSound(Vector3 position)
    {
        isPlayingSound = true;
        string[] soundNames = { "HammerHeat_1", "HammerHeat_3" };
        int idx = Random.Range(0, soundNames.Length);
        SoundManager.Instance.PlaySoundAtPosition(soundNames[idx], position);
        yield return new WaitForSeconds(soundDelay);
        isPlayingSound = false;
    }
}
