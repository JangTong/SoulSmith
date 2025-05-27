// Hammer.cs
using UnityEngine;
using System.Collections;
using DG.Tweening;  // 이미 쓰고 계시니 추가 없으셔도 됩니다

public class Hammer : Tool
{
    public ParticleSystem sparkEffect;
    public float soundDelay = 0.3f;
    private bool isPlayingSound = false;

    public override void Use()
    {
        // 컨트롤러에서 카메라·거리 가져오기
        var ctrl = ItemInteractionController.Instance;
        Transform camera = ctrl.playerCamera;
        float range = ctrl.pickupDistance;

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

            Debug.Log("🔨 망치 타격: " + hit.collider.name);

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

            // 5) WeaponBase 처리
            var target = hit.collider.GetComponentInParent<WeaponBase>();
            if (target != null)
                target.IncrementCollisionCount(hit.collider.name);

            // 가까운 것 하나만
            break;
        }
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
