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
        if (Physics.Raycast(ray, out RaycastHit hit, range, terrainLayer))
        {
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

            // 이펙트 재생
            if (digEffect != null)
            {
                digEffect.transform.position = hit.point;
                digEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                digEffect.Play();
            }
        }
    }
}
