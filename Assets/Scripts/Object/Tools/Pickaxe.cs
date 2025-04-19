using UnityEngine;

public class Pickaxe : Tool
{
    public float digRadius = 1.5f;
    public float digStrength = 0.5f;
    public LayerMask terrainLayer;
    public ParticleSystem digEffect;

    public override void Use()
    {
        Transform camera = ItemPickup.Instance.playerCamera;
        float range = ItemPickup.Instance.pickupDistance;

        Ray ray = new Ray(camera.position, camera.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, range, terrainLayer);

        foreach (RaycastHit hit in hits)
        {
            // 자기 자신 또는 자식 객체 무시
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            Debug.Log("⛏️ 곡괭이 사용: " + hit.collider.name);

            TerrainChunk chunk = hit.collider.GetComponent<TerrainChunk>();
            if (chunk != null)
            {
                chunk.DigAtWorldPosition(hit.point, digRadius, digStrength);
            }
            else
            {
                Debug.LogWarning("⛏️ TerrainChunk가 아닌 오브젝트입니다: " + hit.collider.name);
            }

            if (digEffect != null)
            {
                digEffect.transform.position = hit.point;
                digEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                digEffect.Play();
            }

            break; // 첫 번째 유효한 충돌만 처리
        }
    }
}
