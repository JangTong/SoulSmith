using UnityEngine;
using System.Collections;

public class Hammer : Tool
{
    public ParticleSystem sparkEffect;
    public float soundDelay = 0.3f;
    private bool isPlayingSound = false;

    public override void Use()
    {
        Transform camera = ItemPickup.Instance.playerCamera;
        float range = ItemPickup.Instance.pickupDistance;

        Ray ray = new Ray(camera.position, camera.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, range);

        foreach (RaycastHit hit in hits)
        {
            // 자기 자신 무시
            if (ItemPickup.Instance.pickedItem != null &&
                hit.collider.transform.IsChildOf(ItemPickup.Instance.pickedItem.transform))
                continue;

            // 태그 확인
            if (!hit.collider.CompareTag("Items"))
                continue;

            Debug.Log("🔨 망치 타격: " + hit.collider.name);

            // 파티클 위치만 옮겨서 재생
            if (sparkEffect != null)
            {
                sparkEffect.transform.position = hit.point;
                sparkEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                sparkEffect.Play();
            }

            // 사운드 재생
            if (!isPlayingSound)
            {
                ItemPickup.Instance.StartCoroutine(PlayHammerSound(hit.point));
            }

            // 충돌 카운트 처리
            WeaponBase targetWeapon = hit.collider.GetComponentInParent<WeaponBase>();
            if (targetWeapon != null)
            {
                targetWeapon.IncrementCollisionCount(hit.collider.name);
            }

            break; // 가장 가까운 대상만 처리
        }
    }

    private IEnumerator PlayHammerSound(Vector3 position)
    {
        isPlayingSound = true;

        string[] soundNames = { "HammerHeat_1", "HammerHeat_3" };
        int randIndex = Random.Range(0, soundNames.Length);

        SoundManager.Instance.PlaySoundAtPosition(soundNames[randIndex], position);

        yield return new WaitForSeconds(soundDelay);
        isPlayingSound = false;
    }
}
