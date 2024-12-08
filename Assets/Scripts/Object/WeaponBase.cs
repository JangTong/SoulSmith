using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CollisionData
{
    public string colliderName; // Collider 이름
    public int collisionCount;  // 충돌 횟수
}

public class WeaponBase : MonoBehaviour
{
    [SerializeField]
    public List<CollisionData> collisionDataList = new List<CollisionData>(); // 충돌 데이터 리스트

    private void Start()
    {
        // 자식 Collider 오브젝트 초기화
        InitializeChildColliders();
    }

    /// 자식 Collider 오브젝트 초기화
    private void InitializeChildColliders()
    {
        foreach (Transform child in transform)
        {
            Collider collider = child.GetComponent<Collider>();

            if (collider != null)
            {
                // 충돌 데이터 초기화
                collisionDataList.Add(new CollisionData
                {
                    colliderName = child.name,
                    collisionCount = 0
                });

                // Trigger 설정
                collider.isTrigger = true;

                // WeaponColliderHandler 추가 및 초기화
                WeaponColliderHandler handler = child.gameObject.AddComponent<WeaponColliderHandler>();
                handler.Initialize(this, child.name);
            }
            else
            {
                Debug.LogWarning($"{child.name}에 Collider가 없습니다.");
            }
        }
    }

    /// 특정 자식 Collider의 충돌 카운트를 증가
    public void IncrementCollisionCount(string colliderName)
    {
        var collisionData = collisionDataList.Find(data => data.colliderName == colliderName);
        if (collisionData != null)
        {
            collisionData.collisionCount++;
            Debug.Log($"충돌 감지: {colliderName}, 현재 충돌 횟수: {collisionData.collisionCount}");
        }
        else
        {
            Debug.LogWarning($"Collider 이름 '{colliderName}'를 찾을 수 없습니다.");
        }
    }

    /// 특정 Collider의 충돌 횟수 가져오기
    public int GetCollisionCount(string colliderName)
    {
        var collisionData = collisionDataList.Find(data => data.colliderName == colliderName);
        return collisionData != null ? collisionData.collisionCount : 0;
    }
}
