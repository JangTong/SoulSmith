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

            // 5) 타격된 아이템의 CraftingTable 찾기
            Transform hitTransform = hit.collider.transform;
            CraftingTable craftingTable = FindCraftingTable(hitTransform);
            
            // CraftingTable이 있으면 HandleHammerHit 호출
            if (craftingTable != null)
            {
                Debug.Log($"{LOG_PREFIX} CraftingTable 파츠 타격: {hit.collider.name}");
                craftingTable.HandleHammerHit(hitTransform, hit.point);
            }
            // 다른 타격 가능 대상 처리
            else
            {
                var target = hit.collider.GetComponentInParent<WeaponBase>();
                if (target != null)
                {
                    Debug.Log($"{LOG_PREFIX} WeaponBase 타격: {hit.collider.name}");
                    target.IncrementCollisionCount(hit.collider.name);
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
