using System.Collections.Generic;
using UnityEngine;

public class WeaponBase : MonoBehaviour
{
    public bool isOnAnvil = false;

    [System.Serializable]
    public class WeaponCollisionData
    {
        public string partName;
        public int collisionCount;
    }

    public List<WeaponCollisionData> collisionDataList = new List<WeaponCollisionData>();

    void Awake()
    {
        InitializeCollisionParts();
    }

    private void InitializeCollisionParts()
    {
        collisionDataList.Clear();

        // 자식 오브젝트 중 WeaponColliderHandler가 붙은 것들을 모두 검색
        WeaponColliderHandler[] parts = GetComponentsInChildren<WeaponColliderHandler>();

        foreach (var part in parts)
        {
            WeaponCollisionData data = new WeaponCollisionData
            {
                partName = part.gameObject.name,
                collisionCount = 0
            };

            collisionDataList.Add(data);
        }
    }

    public int IncrementCollisionCount(string partName)
    {
        if (!isOnAnvil) return 0;

        foreach (var data in collisionDataList)
        {
            if (data.partName == partName)
            {
                data.collisionCount++;

                // 해당 파츠 찾기
                Transform partTransform = transform.Find(partName);
                if (partTransform != null)
                {
                    WeaponColliderHandler handler = partTransform.GetComponent<WeaponColliderHandler>();
                    if (handler != null)
                    {
                        handler.SetEmissionLevel(data.collisionCount);
                    }
                }

                return data.collisionCount;
            }
        }

        return 0;
    }
}
