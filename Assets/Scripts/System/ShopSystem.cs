using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopSystem : InteractiveObject
{
    public List<GameObject> itemPrefabs; // 판매할 아이템 프리팹 리스트 (Inspector에서 할당)
    public Transform spawnPoint; // 아이템이 생성될 위치
    public Transform sellPoint; // 아이템이 판매 후 이동할 위치
    public Collider shopCollider; // 아이템이 생성되는 영역
    public TextMeshProUGUI itemInfoText; // 아이템 이름과 가격을 표시할 UI 텍스트
    public ParticleSystem coinEffect;

    private GameObject currentItem; // 현재 생성된 아이템
    private float respawnDelay = 5f; // 아이템 재생성 대기 시간
    public float rotationSpeed = 30f; // 아이템 회전 속도 (초당 회전 각도)

    private void Start()
    {
        if (itemPrefabs.Count > 0)
        {
            SpawnRandomItem(); // 초기 아이템 생성
        }
        else
        {
            Debug.LogError("아이템 프리팹 리스트가 비어있습니다!");
        }
    }

    private void Update()
    {
        RotateCurrentItem();
    }

    /// 랜덤으로 아이템 생성
    private void SpawnRandomItem()
    {
        if (itemPrefabs.Count > 0 && spawnPoint != null)
        {
            // 리스트에서 랜덤 아이템 선택
            GameObject randomItemPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Count)];

            // 아이템 생성
            currentItem = Instantiate(randomItemPrefab, spawnPoint.position, spawnPoint.rotation);

            // 생성된 아이템의 Rigidbody 설정
            Rigidbody itemRigidbody = currentItem.GetComponent<Rigidbody>();
            if (itemRigidbody != null)
            {
                itemRigidbody.isKinematic = true; // Collider에서 튕겨나가지 않게 설정
            }

            // 아이템 이름 및 가격 UI 업데이트
            UpdateItemInfoText();

            Debug.Log($"새로운 아이템 {currentItem.name}이(가) 생성되었습니다.");
        }
        else
        {
            Debug.LogError("아이템 프리팹 리스트 또는 스폰 위치가 설정되지 않았습니다.");
        }
    }

    /// 아이템 회전
    private void RotateCurrentItem()
    {
        if (currentItem != null)
        {
            currentItem.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    /// UI 업데이트 (아이템 이름과 가격)
    private void UpdateItemInfoText()
    {
        if (currentItem != null)
        {
            ItemComponent itemComponent = currentItem.GetComponent<ItemComponent>();
            if (itemComponent != null)
            {
                if (itemInfoText != null)
                {
                    // 이름과 가격을 하나의 텍스트로 표시
                    itemInfoText.text = $"{itemComponent.itemName}\n{itemComponent.buyPrice} Gold";
                }
                else
                {
                    Debug.LogError("ItemInfoText가 설정되지 않았습니다.");
                }
            }
        }
    }

    /// 상호작용 메서드 (판매 기능)
    public override void Interaction()
    {
        if (currentItem == null)
        {
            Debug.LogWarning("판매할 아이템이 없습니다.");
            return;
        }

        // ItemComponent에서 가격 정보 가져오기
        ItemComponent itemComponent = currentItem.GetComponent<ItemComponent>();
        if (itemComponent == null)
        {
            Debug.LogError("아이템에 ItemComponent가 없습니다.");
            return;
        }

        // GameManager에서 플레이어 소지금 확인
        if (GameManager.Instance.playerGold >= itemComponent.buyPrice)
        {
            // 플레이어 돈 차감
            GameManager.Instance.SubtractGold(itemComponent.buyPrice);

            // 아이템 이동
            currentItem.transform.position = sellPoint.position;

            Rigidbody itemRigidbody = currentItem.GetComponent<Rigidbody>();
            itemRigidbody.isKinematic = false;

            coinEffect.Play();
            SoundManager.Instance.PlaySoundAtPosition("CoinDrop_1", transform.position);
            
            Debug.Log($"아이템 {itemComponent.itemName} 판매 완료, {itemComponent.buyPrice} Golds 차감");

            currentItem = null;

            // 5초 후 새로운 아이템 생성
            StartCoroutine(RespawnNewItem());
        }
        else
        {
            Debug.LogWarning("소지금이 부족하여 아이템을 구매할 수 없습니다.");
        }
    }

    /// 새로운 아이템 재생성
    private IEnumerator RespawnNewItem()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnRandomItem();
    }
}
