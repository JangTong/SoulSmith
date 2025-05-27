// Pickaxe.cs
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
        var ctrl = ItemInteractionController.Instance;
        Transform camera = ctrl.playerCamera;
        float range = ctrl.pickupDistance;

        Ray ray = new Ray(camera.position, camera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, terrainLayer))
        {
            Debug.Log("⛏️ 곡괭이 사용: " + hit.collider.name);

            Vector3 digPoint = hit.point;
            Collider[] cols = Physics.OverlapSphere(digPoint, digRadius, terrainLayer);
            var affected = new List<TerrainChunk>();

            foreach (var col in cols)
            {
                // 카메라 자식(들고 있거나 장착된 아이템)은 제외
                if (col.transform.IsChildOf(camera))
                    continue;

                var chunk = col.GetComponent<TerrainChunk>();
                if (chunk != null && !affected.Contains(chunk))
                {
                    affected.Add(chunk);
                    chunk.DigAtWorldPosition(digPoint, digRadius, digStrength);
                }
            }

            // 파티클
            if (digEffect != null)
            {
                digEffect.transform.position = hit.point;
                digEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                digEffect.Play();
            }
        }
    }
}
