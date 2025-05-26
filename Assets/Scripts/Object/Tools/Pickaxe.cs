using UnityEngine;
using System.Collections.Generic;

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

            Vector3 digPoint = hit.point;

            // ✅ 구형 범위 내 TerrainChunk 수집
            Collider[] colliders = Physics.OverlapSphere(digPoint, digRadius, terrainLayer);
            List<TerrainChunk> affectedChunks = new();

            foreach (var col in colliders)
            {
                TerrainChunk chunk = col.GetComponent<TerrainChunk>();
                if (chunk != null && !affectedChunks.Contains(chunk)) // 중복 제거
                {
                    affectedChunks.Add(chunk);
                    chunk.DigAtWorldPosition(digPoint, digRadius, digStrength);
                }
            }

            // 파티클 재생
            if (digEffect != null)
            {
                digEffect.transform.position = hit.point;
                digEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                digEffect.Play();
            }
        }
}

}
